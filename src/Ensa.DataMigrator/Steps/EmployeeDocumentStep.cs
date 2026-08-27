using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Which document belongs to which employee: 2,634,267 rows, second only to the visits.
/// <para>
/// This is where a training certificate stops being an anonymous PDF and becomes proof that a
/// named person attended a named session. 2,603,272 of these rows name a training and 2,597,099
/// name the exact plan line it was delivered on, which is why <see cref="PlanLineMapStep"/> exists
/// at all: without that map the certificate survives and the reason for it does not.
/// </para>
/// <para>
/// <b>Bulk-written with a watermark.</b> Nothing looks an employee document up by its legacy id,
/// so a map of 2.6 million translations would be work nobody uses. Reading in id order and
/// remembering how far it got is enough for a re-run to continue rather than duplicate — the same
/// arrangement the visits use.
/// </para>
/// </summary>
public sealed class EmployeeDocumentStep : IMigrationStep
{
    public int Order => 94;

    public string Name => "employee-documents";

    public string Description => "Which document belongs to which employee (2.6M rows, bulk-loaded)";

    private const int ChunkSize = 100_000;

    private static readonly string[] Columns =
    [
        "CompanyEmployeeId", "DocumentId", "DocumentDate", "TrainingId", "TrainingPlanLineId",
        "WorkPlanLineId", "CertificateId", "OtherCertificateName", "TeamDocumentType",
        "GroupCode", "Source", "IbysStatus", "IbysSubmissionAttempt", "IbysStatusCode",
        "IbysMessage", "IbysNotificationNo", "IsActive", "CreationTime", "IsDeleted", "TenantId",
    ];

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        await using (var model = context.CreateDbContext())
        {
            BulkWriter.EnsureNoConverters(model, "CompanyEmployeeDocument");
        }

        var employeeMap = await context.IdMap.LoadAsync("FirmaPersonel_T", cancellationToken);
        var documentMap = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);
        var trainingMap = await context.IdMap.LoadAsync("Egitim_T", cancellationToken);
        var certificateMap = await context.IdMap.LoadAsync("SertifikaListesi_T", cancellationToken);
        var trainingLineMap = await context.IdMap.LoadAsync("EgitimPlaniSatirlari_T", cancellationToken);
        var workLineMap = await context.IdMap.LoadAsync("CalismaPlaniSatirlari_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        // Said once, up front, rather than discovered in the row counts afterwards: if the plan
        // line maps were refused, every certificate still lands but loses the session it came from.
        if (trainingLineMap.Count == 0)
        {
            context.Logger.LogWarning(
                "    training plan lines are not mapped — 2,597,099 certificates will be written "
                + "without the session they were delivered on. Run the plan-line-map step first.");
        }

        var watermark = await context.IdMap.GetWatermarkAsync("FirmaPersonelDosya_T", cancellationToken);
        var startedAt = watermark;

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var lostTrainingLines = 0;

        while (true)
        {
            var chunkRead = 0;
            var chunkOrphaned = 0;
            var chunkLostLines = 0;
            var lastId = watermark;
            var after = watermark;

            async IAsyncEnumerable<object?[]> RowsAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
            {
                const string sql = """
                    SELECT TOP (@take) FirmaPersonelDosyaId, FirmaPersonelId, DosyaId, EgitimId,
                           EgitimPlaniSatiriId, CalismaPlaniSatiriId, SertifikaId, DigerSertifikaAdi,
                           RiskDegerlendirmeEkibiDosyasi, AcilDurumEkibiDosyasi, IsgKuruluDosyasi,
                           EvrakTarihi, GrupKod, Kaynak, Aktif,
                           IBYSDurum, IBYSGonderimDenemesi, IBYSDurumKodu, IBYSMesaji, IBYSBildirimNo,
                           KurumId, EklemeTarihi
                    FROM FirmaPersonelDosya_T
                    WHERE FirmaPersonelDosyaId > @after
                    ORDER BY FirmaPersonelDosyaId;
                    """;

                await using var connection = await context.OpenLegacyAsync(token);
                await using var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 };
                command.Parameters.AddWithValue("@take", ChunkSize);
                command.Parameters.AddWithValue("@after", after);

                await using var reader = await command.ExecuteReaderAsync(token);

                while (await reader.ReadAsync(token))
                {
                    chunkRead++;
                    lastId = Required(reader, "FirmaPersonelDosyaId");

                    // Employee, file and organization are all required: the row is a statement
                    // that a named person holds a named file, and it means nothing without any
                    // one of the three.
                    if (!employeeMap.TryGetValue(Required(reader, "FirmaPersonelId"), out var employeeId)
                        || !documentMap.TryGetValue(Required(reader, "DosyaId"), out var documentId)
                        || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                    {
                        chunkOrphaned++;
                        continue;
                    }

                    var trainingLineId = Lookup(trainingLineMap, Int(reader, "EgitimPlaniSatiriId"));
                    if (trainingLineId is null && Int(reader, "EgitimPlaniSatiriId") is not null)
                    {
                        chunkLostLines++;
                    }

                    yield return
                    [
                        employeeId,
                        documentId,
                        Date(reader, "EvrakTarihi"),
                        Lookup(trainingMap, Int(reader, "EgitimId")),
                        trainingLineId,
                        Lookup(workLineMap, Int(reader, "CalismaPlaniSatiriId")),
                        Lookup(certificateMap, Int(reader, "SertifikaId")),
                        Fit(context, "CompanyEmployeeDocument", "OtherCertificateName",
                            Text(reader, "DigerSertifikaAdi")),
                        (int)TeamTypeOf(reader),
                        Fit(context, "CompanyEmployeeDocument", "GroupCode", Text(reader, "GrupKod")),
                        Fit(context, "CompanyEmployeeDocument", "Source", Text(reader, "Kaynak")),
                        (int)IbysStatusOf(Text(reader, "IBYSDurum")),
                        Int(reader, "IBYSGonderimDenemesi"),
                        Fit(context, "CompanyEmployeeDocument", "IbysStatusCode", Text(reader, "IBYSDurumKodu")),
                        Fit(context, "CompanyEmployeeDocument", "IbysMessage", Text(reader, "IBYSMesaji")),
                        Fit(context, "CompanyEmployeeDocument", "IbysNotificationNo", Text(reader, "IBYSBildirimNo")),
                        Bit(reader, "Aktif"),
                        Date(reader, "EklemeTarihi") ?? DateTime.Now,
                        false,
                        tenantId,
                    ];
                }
            }

            int chunkWritten;
            if (context.DryRun)
            {
                await foreach (var _ in RowsAsync(cancellationToken))
                {
                }

                chunkWritten = chunkRead - chunkOrphaned;
            }
            else
            {
                chunkWritten = await context.Bulk.WriteAsync(
                    "ensa.CompanyEmployeeDocument", Columns, RowsAsync(cancellationToken), cancellationToken);
            }

            read += chunkRead;
            written += chunkWritten;
            orphaned += chunkOrphaned;
            lostTrainingLines += chunkLostLines;

            if (chunkRead == 0)
            {
                break;
            }

            watermark = lastId;

            if (!context.DryRun)
            {
                await context.IdMap.SetWatermarkAsync("FirmaPersonelDosya_T", watermark, cancellationToken);
            }

            context.Logger.LogInformation(
                "    employee documents: {Written} written so far (legacy id {Watermark})", written, watermark);

            if (context.DryRun)
            {
                break;
            }
        }

        var note = $"employee documents: {written} written";
        if (startedAt > 0)
        {
            note += $" (resumed from legacy id {startedAt})";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (employee, file or organization missing)";
        }

        if (lostTrainingLines > 0)
        {
            note += $", {lostTrainingLines} lost the training plan line they name";
        }

        if (context.DryRun)
        {
            note += " — dry run stops after one chunk";
        }

        return new StepResult(read, written, orphaned, note);
    }

    // ------------------------------------------------------------------ mapping

    /// <summary>
    /// The three team flags collapse into one enum, because a document belongs to one team.
    /// <para>
    /// They are three independent bits in the legacy table and could in principle all be set;
    /// across all 2,634,267 rows they never are — 20 rows are risk assessment team documents and
    /// one is a committee document. The order below is therefore not a precedence anybody has to
    /// rely on, only a rule for a case that does not arise.
    /// </para>
    /// </summary>
    private static EmployeeTeamDocumentType TeamTypeOf(SqlDataReader reader)
    {
        if (Bit(reader, "RiskDegerlendirmeEkibiDosyasi"))
        {
            return EmployeeTeamDocumentType.RiskAssessmentTeam;
        }

        if (Bit(reader, "AcilDurumEkibiDosyasi"))
        {
            return EmployeeTeamDocumentType.EmergencyTeam;
        }

        return Bit(reader, "IsgKuruluDosyasi")
            ? EmployeeTeamDocumentType.OhsCommittee
            : EmployeeTeamDocumentType.None;
    }

    /// <summary>
    /// <c>IBYSDurum</c> is a three-character column holding <c>"1"</c>, <c>"-1"</c> or nothing.
    /// <para>
    /// 9,581 rows carry <c>1</c> — the ministry accepted the notification — and 3,029 carry
    /// <c>-1</c>, a rejection. The remaining 2,621,657 were never submitted. No other value occurs,
    /// and an unrecognised one is treated as not sent rather than guessed at: claiming a
    /// notification reached the ministry when it did not is the more expensive mistake.
    /// </para>
    /// </summary>
    private static IbysSubmissionStatus IbysStatusOf(string? status)
        => status switch
        {
            "1" => IbysSubmissionStatus.Approved,
            "-1" => IbysSubmissionStatus.Failed,
            _ => IbysSubmissionStatus.NotSent,
        };

    // ------------------------------------------------------------------ helpers

    private static int? Lookup(Dictionary<int, int> map, int? legacyId)
        => legacyId is int id && map.TryGetValue(id, out var modernId) ? modernId : null;

    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

    private static string? Text(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index)?.ToString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int? Int(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToInt32(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static bool Bit(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetValue(index));
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
