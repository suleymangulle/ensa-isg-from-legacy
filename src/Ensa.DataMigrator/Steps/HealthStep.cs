using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Health;
using Ensa.Domain.Lookups;
using Ensa.Domain.Shared.Enums;
using Ensa.EntityFrameworkCore.ValueConverters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The medication and diagnosis catalogues, and 334,794 e-prescriptions with their 1.4 million lines.
/// <para>
/// <b>The parent cannot go in bulk, and that is the right trade.</b> A prescription's medications and
/// diagnoses point back at it, so its identities have to be known — which means the DbContext, at
/// about 340 rows a second, so roughly seventeen minutes. The 1.4 million child rows, which are
/// leaves, go through <see cref="BulkWriter"/> in a fraction of that. Buying the parent's identities
/// with seventeen minutes is what makes the rest cheap.
/// </para>
/// <para>
/// <b>Two child columns are encrypted and are converted by hand.</b>
/// <c>EPrescriptionMedication.MedicationDescription</c> carries the physician's instructions to the
/// patient. It is written in bulk with the converter applied here, and named in the guard's
/// <c>preConverted</c> list so the exception is visible rather than silent.
/// </para>
/// <para>
/// <b>The patient is linked where the identity number allows it.</b> The legacy prescription records
/// only the patient's national id, and the rebuilt schema also has a
/// <c>PatientCompanyEmployeeId</c>. Matching them is what turns a prescription from a loose document
/// into part of an employee's health record — but only within one organization, because an identity
/// number is unique per tenant and matching across them would attach one provider's prescription to
/// another provider's employee.
/// </para>
/// </summary>
public sealed class HealthStep : IMigrationStep
{
    public int Order => 80;

    public string Name => "health";

    public string Description => "Medication and ICD-10 catalogues, e-prescriptions and their lines";

    private const int ParentBatchSize = 500;
    private const int LineChunkSize = 200_000;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        await using (var model = context.CreateDbContext())
        {
            BulkWriter.EnsureNoConverters(model, "EPrescriptionMedication", ["MedicationDescription"]);
            BulkWriter.EnsureNoConverters(model, "EPrescriptionDiagnosis");
        }

        var read = 0;
        var written = 0;
        var skipped = 0;
        var notes = new List<string>();

        var medications = await MigrateMedicationsAsync(context, cancellationToken);
        var icd10 = await MigrateIcd10Async(context, cancellationToken);
        var prescriptions = await MigratePrescriptionsAsync(context, cancellationToken);
        var lines = await MigrateMedicationLinesAsync(context, prescriptions.Map, medications.Map, cancellationToken);
        var diagnoses = await MigrateDiagnosesAsync(context, prescriptions.Map, icd10.Map, cancellationToken);

        foreach (var result in new[]
                 { medications.Result, icd10.Result, prescriptions.Result, lines, diagnoses })
        {
            read += result.Read;
            written += result.Written;
            skipped += result.Skipped;
            notes.Add(result.Note!);
        }

        return new StepResult(read, written, skipped, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ medication catalogue

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigrateMedicationsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();
        var already = await context.IdMap.LoadAsync("SKRS_Ilac_T", cancellationToken);

        var rows = new List<(int LegacyId, Medication Entity)>();
        var mergedPairs = new List<(int, int)>();
        var read = 0;
        var merged = 0;

        // Barcode is unique in the rebuilt schema and is not in the legacy catalogue. Seeded from
        // the destination too: the first attempt stopped after 14,000 rows.
        var byBarcode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var existing in await db.Set<Medication>()
                     .Where(m => m.Barcode != null)
                     .Select(m => new { m.Id, m.Barcode })
                     .ToListAsync(cancellationToken))
        {
            byBarcode[existing.Barcode!] = existing.Id;
        }

        const string sql = """
            SELECT IlacId, IlacAdi, Barkodu, FirmaAdi, ATC_Kodu, ATC_Adi, AyaktanOdenmeSarti,
                   YatanOdenmeSarti, ReceteTuru, PasifeAlmaTarihi, Aktif
            FROM SKRS_Ilac_T ORDER BY IlacId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "IlacId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var barcode = Fit(context, "Medication", "Barcode", Text(reader, "Barkodu"));

                if (barcode is not null)
                {
                    if (byBarcode.TryGetValue(barcode, out var existingId))
                    {
                        // The same product under a second catalogue id. Both ids point at the one
                        // row, so a prescription naming either still finds its medicine.
                        if (existingId > 0)
                        {
                            mergedPairs.Add((legacyId, existingId));
                        }

                        merged++;
                        continue;
                    }

                    // Reserved before the insert, so two rows in one batch cannot both pass.
                    byBarcode[barcode] = 0;
                }

                rows.Add((legacyId, new Medication
                {
                    MedicationName = Fit(context, "Medication", "MedicationName", Text(reader, "IlacAdi"))
                                     ?? $"Medication {legacyId}",
                    Barcode = barcode,
                    GeneratorCompanyName = Fit(context, "Medication", "GeneratorCompanyName", Text(reader, "FirmaAdi")),
                    AtcCode = Fit(context, "Medication", "AtcCode", Text(reader, "ATC_Kodu")),
                    AtcName = Fit(context, "Medication", "AtcName", Text(reader, "ATC_Adi")),
                    OutpatientReimbursementCondition = Fit(context, "Medication",
                        "OutpatientReimbursementCondition", Text(reader, "AyaktanOdenmeSarti")),
                    InpatientReimbursementCondition = Fit(context, "Medication",
                        "InpatientReimbursementCondition", Text(reader, "YatanOdenmeSarti")),
                    PrescriptionType = Fit(context, "Medication", "PrescriptionType", Text(reader, "ReceteTuru")),
                    DeactivationDate = Date(reader, "PasifeAlmaTarihi"),
                    IsActive = Bit(reader, "Aktif"),
                }));
            }
        }

        var written = await SaveAsync(db, context, "SKRS_Ilac_T", rows, cancellationToken);

        if (mergedPairs.Count > 0 && !context.DryRun)
        {
            await context.IdMap.SaveAsync("SKRS_Ilac_T", mergedPairs, 'M', cancellationToken);
        }

        var map = await MapAfterSaveAsync(context, "SKRS_Ilac_T", already, rows, cancellationToken);

        var note = $"medications: {written} written";
        if (merged > 0)
        {
            note += $", {merged} merged into an existing entry with the same barcode";
        }

        return (new StepResult(read, written, merged, note), map);
    }

    // ------------------------------------------------------------------ ICD-10 catalogue

    private static async Task<(StepResult Result, Dictionary<string, int> Map)> MigrateIcd10Async(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();
        var already = await context.IdMap.LoadAsync("SKRS_ICD10_T", cancellationToken);

        var rows = new List<(int LegacyId, Icd10 Entity)>();
        var read = 0;
        var unnamed = 0;
        var merged = 0;

        // Code is unique in the rebuilt schema and is not in the legacy catalogue. Seeded from the
        // destination too, because the previous attempt stopped after 12,000 rows.
        var seenCodes = new HashSet<string>(
            await db.Set<Icd10>().Select(item => item.Code).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT ICD10Id, ICD10_ADi, ICD10_Kodu, ICD10_UstKodu, ICD10_Seviye, Aktif FROM SKRS_ICD10_T ORDER BY ICD10Id",
            connection) { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;
                var legacyId = Required(reader, "ICD10Id");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var code = Text(reader, "ICD10_Kodu");
                if (code is null)
                {
                    // A diagnosis without its code cannot be referenced by a prescription, which
                    // stores the code rather than an id.
                    unnamed++;
                    continue;
                }

                if (!seenCodes.Add(code))
                {
                    // The same diagnosis under a second catalogue id. Nothing is lost: a
                    // prescription references a diagnosis by its code, not by an id.
                    merged++;
                    continue;
                }

                rows.Add((legacyId, new Icd10
                {
                    Name = Fit(context, "Icd10", "Name", Text(reader, "ICD10_ADi")) ?? code,
                    Code = Fit(context, "Icd10", "Code", code)!,
                    ParentCode = Fit(context, "Icd10", "ParentCode", Text(reader, "ICD10_UstKodu")),
                    Level = Int(reader, "ICD10_Seviye"),
                    IsActive = Bit(reader, "Aktif"),
                }));
            }
        }

        var written = await SaveAsync(db, context, "SKRS_ICD10_T", rows, cancellationToken);

        // Prescriptions reference a diagnosis by its code, not by an id, so the map this step hands
        // on is keyed by code.
        var byCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (!context.DryRun)
        {
            await using var lookup = context.CreateDbContext();
            foreach (var entry in await lookup.Set<Icd10>()
                         .Select(item => new { item.Id, item.Code })
                         .ToListAsync(cancellationToken))
            {
                byCode[entry.Code] = entry.Id;
            }
        }

        var note = $"ICD-10: {written} written";
        if (merged > 0)
        {
            note += $", {merged} duplicate code(s) collapsed";
        }

        if (unnamed > 0)
        {
            note += $", {unnamed} SKIPPED (no code)";
        }

        return (new StepResult(read, written, unnamed + merged, note), byCode);
    }

    // ------------------------------------------------------------------ prescriptions

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> MigratePrescriptionsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var already = await context.IdMap.LoadAsync("ERecete_T", cancellationToken);

        // The patient is named by identity number only. Matching it to an employee is what makes a
        // prescription part of a health record - within one organization, because the number is
        // unique per tenant and matching across them would attach it to the wrong person entirely.
        var employeesByNationalId = new Dictionary<(int Tenant, string NationalId), int>();

        foreach (var employee in await db.Set<Ensa.Domain.Companies.CompanyEmployee>()
                     .Where(e => e.NationalId != null && e.TenantId != null)
                     .Select(e => new { e.Id, e.TenantId, e.NationalId })
                     .ToListAsync(cancellationToken))
        {
            employeesByNationalId[(employee.TenantId!.Value, employee.NationalId!)] = employee.Id;
        }

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var matched = 0;
        var unknownNoteType = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, EPrescription Entity)>();

        const string sql = """
            SELECT EReceteId, EReceteKodu, ProtokolNo, HastaTcKimlikNo, AciklamaTuru, Aciklama,
                   Iptal, GonderimTarihi, SonucKodu, SonucMesaji, UyariMesaji, EklemeTarihi, KurumId
            FROM ERecete_T ORDER BY EReceteId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "EReceteId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                // NOT fitted. Shortening an identity number produces eleven digits that look valid
                // and belong to somebody else - the same rule the company step settled, and a
                // prescription attached to the wrong patient is the worst version of that mistake.
                var nationalId = LegacyCrypt.TryDecrypt(Text(reader, "HastaTcKimlikNo"));

                if (nationalId is null || nationalId.Length > 11)
                {
                    // A prescription whose patient cannot be identified identifies nobody.
                    orphaned++;
                    continue;
                }

                var tenantId = MapId(organizationMap, Int(reader, "KurumId"));

                int? employeeId = null;
                if (tenantId is { } tenant
                    && employeesByNationalId.TryGetValue((tenant, nationalId), out var found))
                {
                    employeeId = found;
                    matched++;
                }

                batch.Add((legacyId, new EPrescription
                {
                    EPrescriptionCode = Fit(context, "EPrescription", "EPrescriptionCode", Text(reader, "EReceteKodu")),
                    ProtocolNo = Fit(context, "EPrescription", "ProtocolNo", Text(reader, "ProtokolNo")),
                    PatientNationalId = nationalId,
                    PatientCompanyEmployeeId = employeeId,
                    Description = Fit(context, "EPrescription", "Description", Text(reader, "Aciklama")),
                    DescriptionType = NoteType(Int(reader, "AciklamaTuru"), ref unknownNoteType),
                    Cancelled = Bit(reader, "Iptal"),
                    SubmissionDate = Date(reader, "GonderimTarihi"),
                    ResultCode = Fit(context, "EPrescription", "ResultCode", Text(reader, "SonucKodu")),
                    ResultMessage = Fit(context, "EPrescription", "ResultMessage", Text(reader, "SonucMesaji")),
                    WarningMessage = Fit(context, "EPrescription", "WarningMessage", Text(reader, "UyariMesaji")),
                    TenantId = tenantId,
                }));

                if (batch.Count >= ParentBatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "ERecete_T", batch, pairs, cancellationToken);

                    if (written % 50_000 == 0)
                    {
                        context.Logger.LogInformation("    prescriptions: {Written} written so far", written);
                    }
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "ERecete_T", batch, pairs, cancellationToken);
        }

        var note = $"e-prescriptions: {written} written, {matched} linked to an employee by identity number";
        if (unknownNoteType > 0)
        {
            note += $", {unknownNoteType} unrecognised note type -> Unspecified";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (no patient identity number)";
        }

        var map = new Dictionary<int, int>(already);
        foreach (var (legacyId, modernId) in pairs)
        {
            map[legacyId] = modernId;
        }

        return (new StepResult(read, written, orphaned, note), map);
    }

    // ------------------------------------------------------------------ medications on a prescription

    private static async Task<StepResult> MigrateMedicationLinesAsync(
        MigrationContext context,
        Dictionary<int, int> prescriptionMap,
        Dictionary<int, int> medicationMap,
        CancellationToken cancellationToken)
    {
        string[] columns =
        [
            "EPrescriptionId", "MedicationId", "MedicationBarcode", "UsageMethodId", "UsageDoseUnitId",
            "UsagePeriodUnitId", "Box", "Dose", "DoseFraction", "Period", "MedicationDescription",
            "MedicationDescriptionType", "CreationTime", "IsDeleted", "TenantId",
        ];

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var ignored = 0;

        const string sql = """
            SELECT TOP (@take) EReceteIlacId, EReceteId, SKRS_IlacId, IlacBarkodu,
                   SKRS_KullanimSekliId, SKRS_IlacKullanimDozBirimiId,
                   SKRS_IlacKullanimPeriyoduBirimiId, Kutu, Doz, Doz2, Periyot,
                   IlacAciklama, IlacAciklamaTuru, KurumId
            FROM EReceteIlac_T
            WHERE EReceteIlacId > @after
            ORDER BY EReceteIlacId;
            """;

        return await CopyChunkedAsync(
            context, "EReceteIlac_T", "ensa.EPrescriptionMedication", columns, sql, "EReceteIlacId",
            (reader, orphan) =>
            {
                if (Int(reader, "EReceteId") is not { } legacyPrescriptionId
                    || !prescriptionMap.TryGetValue(legacyPrescriptionId, out var prescriptionId)
                    || !medicationMap.TryGetValue(Required(reader, "SKRS_IlacId"), out var medicationId))
                {
                    orphan();
                    return null;
                }

                return
                [
                    prescriptionId,
                    medicationId,
                    Fit(context, "EPrescriptionMedication", "MedicationBarcode", Text(reader, "IlacBarkodu")),
                    Required(reader, "SKRS_KullanimSekliId"),
                    Required(reader, "SKRS_IlacKullanimDozBirimiId"),
                    Required(reader, "SKRS_IlacKullanimPeriyoduBirimiId"),
                    Required(reader, "Kutu"),
                    Required(reader, "Doz"),
                    Number(reader, "Doz2"),
                    Required(reader, "Periyot"),
                    // Encrypted: the physician's instructions to the patient. Bulk copy does not run
                    // the converters, so it is applied here and the column named in the guard.
                    Encrypt(Fit(context, "EPrescriptionMedication", "MedicationDescription",
                        Text(reader, "IlacAciklama"))),
                    (int)NoteType(Int(reader, "IlacAciklamaTuru"), ref ignored),
                    DateTime.Now,
                    false,
                    MapId(organizationMap, Int(reader, "KurumId")),
                ];
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ diagnoses

    private static async Task<StepResult> MigrateDiagnosesAsync(
        MigrationContext context,
        Dictionary<int, int> prescriptionMap,
        Dictionary<string, int> icd10ByCode,
        CancellationToken cancellationToken)
    {
        string[] columns =
        [
            "EPrescriptionId", "Icd10Code", "Icd10Id", "CreationTime", "IsDeleted", "TenantId",
        ];

        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        const string sql = """
            SELECT TOP (@take) EReceteTaniId, EReceteId, ICD10_Kodu, KurumId
            FROM EReceteTani_T
            WHERE EReceteTaniId > @after
            ORDER BY EReceteTaniId;
            """;

        return await CopyChunkedAsync(
            context, "EReceteTani_T", "ensa.EPrescriptionDiagnosis", columns, sql, "EReceteTaniId",
            (reader, orphan) =>
            {
                var code = Text(reader, "ICD10_Kodu");

                if (code is null
                    || Int(reader, "EReceteId") is not { } legacyPrescriptionId
                    || !prescriptionMap.TryGetValue(legacyPrescriptionId, out var prescriptionId))
                {
                    orphan();
                    return null;
                }

                return
                [
                    prescriptionId,
                    Fit(context, "EPrescriptionDiagnosis", "Icd10Code", code),
                    // The catalogue link is a convenience; the code itself is what the prescription
                    // was issued with, and it stands whether or not the catalogue still lists it.
                    icd10ByCode.TryGetValue(code, out var icd10Id) ? icd10Id : (object?)null,
                    DateTime.Now,
                    false,
                    MapId(organizationMap, Int(reader, "KurumId")),
                ];
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ the chunked loop

    private static async Task<StepResult> CopyChunkedAsync(
        MigrationContext context,
        string legacyTable,
        string modernTable,
        string[] columns,
        string sql,
        string keyColumn,
        Func<SqlDataReader, Action, object?[]?> project,
        CancellationToken cancellationToken)
    {
        var watermark = await context.IdMap.GetWatermarkAsync(legacyTable, cancellationToken);
        var startedAt = watermark;

        var read = 0;
        var written = 0;
        var orphaned = 0;

        while (true)
        {
            var chunkRead = 0;
            var chunkOrphaned = 0;
            var lastId = watermark;
            var after = watermark;

            async IAsyncEnumerable<object?[]> RowsAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
            {
                await using var connection = await context.OpenLegacyAsync(token);
                await using var command = new SqlCommand(sql, connection) { CommandTimeout = 1800 };
                command.Parameters.AddWithValue("@take", LineChunkSize);
                command.Parameters.AddWithValue("@after", after);

                await using var reader = await command.ExecuteReaderAsync(token);

                while (await reader.ReadAsync(token))
                {
                    chunkRead++;
                    lastId = Required(reader, keyColumn);

                    var row = project(reader, () => chunkOrphaned++);
                    if (row is not null)
                    {
                        yield return row;
                    }
                }
            }

            if (context.DryRun)
            {
                await foreach (var _ in RowsAsync(cancellationToken))
                {
                }

                read += chunkRead;
                written += chunkRead - chunkOrphaned;
                orphaned += chunkOrphaned;
                break;
            }

            var chunkWritten = await context.Bulk.WriteAsync(
                modernTable, columns, RowsAsync(cancellationToken), cancellationToken);

            read += chunkRead;
            written += chunkWritten;
            orphaned += chunkOrphaned;

            if (chunkRead == 0)
            {
                break;
            }

            watermark = lastId;
            await context.IdMap.SetWatermarkAsync(legacyTable, watermark, cancellationToken);

            context.Logger.LogInformation(
                "    {Table}: {Written} written so far (legacy id {Watermark})",
                legacyTable, written, watermark);
        }

        var note = $"{legacyTable}: {written} written";
        if (startedAt > 0)
        {
            note += $" (resumed from {startedAt})";
        }

        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (prescription or catalogue entry missing)";
        }

        if (context.DryRun)
        {
            note += " — dry run stops after one chunk";
        }

        return new StepResult(read, written, orphaned, note);
    }

    // ------------------------------------------------------------------ shared

    /// <summary>
    /// The note type, from a legacy column with more values than the enum defines.
    /// <para>
    /// <c>PrescriptionNoteType</c> is Unspecified, Diagnosis and TreatmentDuration; the legacy
    /// column also holds 3, 4, 5 and 99 across 8,341 prescriptions. Casting the number straight in
    /// would store an enum value that renders as a number and behaves like nothing.
    /// </para>
    /// </summary>
    private static PrescriptionNoteType NoteType(int? value, ref int unknown)
    {
        switch (value)
        {
            case null or 0:
                return PrescriptionNoteType.Unspecified;
            case 1:
                return PrescriptionNoteType.Diagnosis;
            case 2:
                return PrescriptionNoteType.TreatmentDuration;
            default:
                unknown++;
                return PrescriptionNoteType.Unspecified;
        }
    }

    private static readonly EncryptedStringConverter Converter = new();

    /// <summary>Encrypts a value the way the model would, for a column written in bulk.</summary>
    private static string? Encrypt(string? plaintext)
        => plaintext is null ? null : (string?)Converter.ConvertToProvider(plaintext);

    private static async Task<int> SaveAsync<TEntity>(
        DbContext db,
        MigrationContext context,
        string legacyTable,
        List<(int LegacyId, TEntity Entity)> rows,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (rows.Count == 0 || context.DryRun)
        {
            return rows.Count;
        }

        foreach (var chunk in rows.Chunk(ParentBatchSize))
        {
            db.Set<TEntity>().AddRange(chunk.Select(item => item.Entity));
            await db.SaveChangesAsync(cancellationToken);

            await context.IdMap.SaveAsync(
                legacyTable,
                chunk.Select(item => (item.LegacyId, (int)db.Entry(item.Entity).Property("Id").CurrentValue!)).ToList(),
                'I', cancellationToken);

            db.ChangeTracker.Clear();
        }

        return rows.Count;
    }

    private static async Task<Dictionary<int, int>> MapAfterSaveAsync<TEntity>(
        MigrationContext context,
        string legacyTable,
        Dictionary<int, int> already,
        List<(int LegacyId, TEntity Entity)> rows,
        CancellationToken cancellationToken)
    {
        if (!context.DryRun)
        {
            return await context.IdMap.LoadAsync(legacyTable, cancellationToken);
        }

        var placeholder = new Dictionary<int, int>(already);
        foreach (var (legacyId, _) in rows)
        {
            placeholder[legacyId] = context.NextDryRunId();
        }

        return placeholder;
    }

    private static async Task<int> FlushAsync<TEntity>(
        DbContext db,
        MigrationContext context,
        string legacyTable,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        db.Set<TEntity>().AddRange(batch.Select(item => item.Entity));
        await db.SaveChangesAsync(cancellationToken);

        var chunkPairs = batch
            .Select(item => (item.LegacyId, (int)db.Entry(item.Entity).Property("Id").CurrentValue!))
            .ToList();

        await context.IdMap.SaveAsync(legacyTable, chunkPairs, 'I', cancellationToken);
        pairs.AddRange(chunkPairs);

        var count = batch.Count;
        batch.Clear();
        db.ChangeTracker.Clear();

        return count;
    }

    private static int DryRunFlush<TEntity>(
        MigrationContext context,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs)
    {
        pairs.AddRange(batch.Select(item => (item.LegacyId, context.NextDryRunId())));

        var count = batch.Count;
        batch.Clear();
        return count;
    }

    // ------------------------------------------------------------------ readers

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

    private static bool Bit(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static decimal? Number(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = Convert.ToDouble(reader.GetValue(index));
        return value is >= 0 and <= 100_000 ? Math.Round((decimal)value, 2) : null;
    }

    private static int Required(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index));
    }

    private static int? MapId(Dictionary<int, int> map, int? legacyId)
        => legacyId is { } id && map.TryGetValue(id, out var modernId) ? modernId : null;
}
