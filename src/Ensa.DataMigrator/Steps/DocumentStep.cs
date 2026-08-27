using System.Security.Cryptography;
using System.Text;
using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Documents;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The central document store: 35 categories and 109,883 file records.
/// <para>
/// <b>The metadata moves; the payload does not.</b> <c>Dosya_T.Dosya</c> holds 132 GB of
/// <c>varbinary(max)</c> — one column larger than every other table in this migration put
/// together. Streaming that through a row-by-row INSERT over a remote connection is not a
/// migration step, it is a week of copying, and the destination model already says where those
/// bytes belong: <see cref="Document.Content"/> is for small files, and anything larger lives
/// under <see cref="Document.StoragePath"/> in the document storage.
/// </para>
/// <para>
/// So each row is written with <c>Content</c> null and a <c>StoragePath</c> that names exactly
/// where <c>FileSystemDocumentStorage</c> will look for it — <c>{tenant}/{aa}/{bb}/{storageName}</c>.
/// The payloads are copied into that layout by <c>--export-documents</c>, which streams them one
/// at a time and can be stopped and resumed. Until it has run, a document row is a record of a
/// file whose content is not yet on disk, which is precisely the case
/// <see cref="IDocumentStorage.OpenAsync"/> already returns <c>null</c> for.
/// </para>
/// <para>
/// <b>The storage name is derived, not random.</b> <c>DocumentAppService</c> uses
/// <c>Guid.NewGuid()</c>, which is right for an upload and wrong here: the export runs as a
/// separate command, possibly on a different machine, and has to arrive at the same name for the
/// same legacy file. It is therefore a SHA-256 of a fixed namespace and the legacy id, cut to 16
/// bytes and stamped as a version 5 UUID — the same input always gives the same key, and
/// <c>Guid.TryParseExact(.., "N", ..)</c> accepts it.
/// </para>
/// <para>
/// <b>No SHA-256 of the content.</b> Filling <see cref="Document.Sha256"/> means reading all
/// 132 GB to hash it. The column exists for duplicate detection on upload, nothing reads it for
/// migrated rows, and the export can fill it later if that ever changes.
/// </para>
/// </summary>
public sealed class DocumentStep : IMigrationStep
{
    public int Order => 90;

    public string Name => "documents";

    public string Description => "Document categories and 109,883 file records (metadata; payloads via --export-documents)";

    private const int BatchSize = 500;

    /// <summary>
    /// Namespace for the derived storage keys. A fixed, arbitrary string: its only job is to make
    /// the derivation specific to this migration, so a legacy id can never collide with a key
    /// generated some other way.
    /// </summary>
    private const string StorageNamespace = "ensa.migration.document.v1";

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var categories = await CategoriesAsync(context, cancellationToken);
        var documents = await DocumentsAsync(context, cancellationToken);

        return new StepResult(
            categories.Read + documents.Read,
            categories.Written + documents.Written,
            categories.Skipped + documents.Skipped,
            string.Join("; ", new[] { categories.Note, documents.Note }.Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ categories

    /// <summary>
    /// <c>DosyaKategori_T</c> to <see cref="DocumentCategory"/>.
    /// <para>
    /// The legacy table has no tenant column: the 35 categories are the same list for everybody,
    /// so they are written host-level and every organization reads them.
    /// </para>
    /// </summary>
    private static async Task<StepResult> CategoriesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var already = await context.IdMap.LoadAsync("DosyaKategori_T", cancellationToken);

        var read = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, DocumentCategory Entity)>();

        const string sql = """
            SELECT DosyaKategoriId, KategoriKodu, KategoriAdi, RaporlamaMaddeGrubu, EklemeTarihi
            FROM DosyaKategori_T ORDER BY DosyaKategoriId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 120 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "DosyaKategoriId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                batch.Add((legacyId, new DocumentCategory
                {
                    CategoryCode = Fit(context, "DocumentCategory", "CategoryCode", Text(reader, "KategoriKodu"))
                        ?? $"kategori-{legacyId}",
                    CategoryName = Fit(context, "DocumentCategory", "CategoryName", Text(reader, "KategoriAdi"))
                        ?? $"Kategori {legacyId}",
                    ReportingArticleGroup = Int(reader, "RaporlamaMaddeGrubu"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = null,
                }));
            }
        }

        var written = batch.Count == 0
            ? 0
            : context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "DosyaKategori_T", batch, pairs, cancellationToken);

        return new StepResult(read, written, read - written, $"document categories: {written} written");
    }

    // ------------------------------------------------------------------ documents

    private static async Task<StepResult> DocumentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);
        var categoryMap = await context.IdMap.LoadAsync("DosyaKategori_T", cancellationToken);
        var already = await context.IdMap.LoadAsync("Dosya_T", cancellationToken);

        var read = 0;
        var written = 0;
        var noCompany = 0;
        var noTenant = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, Document Entity)>();

        // DATALENGTH rather than the column: the size is wanted, the 132 GB is not. Reading
        // Dosya itself here would pull every payload across the wire to be thrown away.
        const string sql = """
            SELECT DosyaId, DosyaKategoriId, DosyaAdi, DosyaTuru, KurumId, FirmaId,
                   Aktif, Silindi, EkleyenKullanici, EklemeTarihi,
                   GuncelleyenKullanici, GuncellemeTarihi,
                   CAST(DATALENGTH(Dosya) AS bigint) AS Boyut
            FROM Dosya_T ORDER BY DosyaId;
            """;

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, "DosyaId");
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                // The tenant is required; a document whose organization did not survive has no
                // reader, and writing it host-level would show it to everybody.
                if (!organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    noTenant++;
                    continue;
                }

                // The company is optional in the legacy table and stays optional here, but a
                // company that was named and did not survive is a broken reference, not an
                // unattached document: the link is dropped and counted rather than guessed at.
                int? companyId = null;
                if (Int(reader, "FirmaId") is int legacyCompanyId)
                {
                    if (companyMap.TryGetValue(legacyCompanyId, out var mapped))
                    {
                        companyId = mapped;
                    }
                    else
                    {
                        noCompany++;
                    }
                }

                var name = Text(reader, "DosyaAdi") ?? $"belge-{legacyId}";
                var storageName = DeriveStorageName(legacyId);

                batch.Add((legacyId, new Document
                {
                    DocumentCategoryId = categoryMap.TryGetValue(Required(reader, "DosyaKategoriId"), out var category)
                        ? category
                        : null,
                    CompanyId = companyId,
                    DocumentName = Fit(context, "Document", "DocumentName", name) ?? $"belge-{legacyId}",
                    StorageName = storageName,
                    StoragePath = BuildStoragePath(storageName, tenantId),
                    Extension = Fit(context, "Document", "Extension", ExtensionOf(name)),
                    ContentType = Fit(context, "Document", "ContentType", Text(reader, "DosyaTuru")),
                    SizeBytes = Long(reader, "Boyut") ?? 0,
                    Content = null,
                    Sha256 = null,

                    // Dosya_T names only the company. Which record inside that company a file
                    // belongs to is recorded by the link tables (FirmaPersonelDosya_T,
                    // IsyeriBolumEvrak_T and the rest), which become tables of their own; a
                    // guess here would be a claim the legacy schema does not make.
                    OwnerType = companyId is null ? DocumentOwnerType.Unspecified : DocumentOwnerType.Company,
                    OwnerRecordId = companyId,

                    IsActive = Bit(reader, "Aktif"),
                    IsDeleted = Bit(reader, "Silindi"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = tenantId,
                }));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, "Dosya_T", batch, pairs, cancellationToken);

                    if (written % 10_000 == 0)
                    {
                        context.Logger.LogInformation("    documents: {Written} written so far", written);
                    }
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, "Dosya_T", batch, pairs, cancellationToken);
        }

        var note = $"documents: {written} written (metadata only, payloads via --export-documents)";
        if (noTenant > 0)
        {
            note += $", {noTenant} SKIPPED (organization missing)";
        }

        if (noCompany > 0)
        {
            note += $", {noCompany} company link(s) DROPPED (company missing)";
        }

        return new StepResult(read, written, read - written, note);
    }

    // ------------------------------------------------------------------ storage keys

    /// <summary>
    /// The stable storage key for a legacy file id: a version 5 UUID over
    /// <see cref="StorageNamespace"/> and the id, rendered the way the storage expects it.
    /// </summary>
    public static string DeriveStorageName(int legacyDocumentId)
    {
        var digest = SHA256.HashData(
            Encoding.UTF8.GetBytes($"{StorageNamespace}:{legacyDocumentId}"));

        var bytes = digest.AsSpan(0, 16).ToArray();

        // Version 5, RFC 4122 variant. Not decoration: it keeps the value inside the shape a GUID
        // is allowed to have, so nothing downstream can reject it as malformed.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        // Big-endian, so the hex text is the digest's own order rather than Guid's field layout.
        // The export command and this step have to agree, and a byte order is one fewer thing to
        // get wrong than a struct layout.
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Exactly what <c>FileSystemDocumentStorage.BuildRelativePath</c> produces. Duplicated rather
    /// than referenced because the migrator does not take a dependency on the host, and the two
    /// are tied together by <c>--export-documents</c>, which writes the files at this path.
    /// </summary>
    public static string BuildStoragePath(string storageName, int? tenantId)
        => string.Join('/', tenantId?.ToString() ?? "host", storageName[..2], storageName[2..4], storageName);

    // ------------------------------------------------------------------ helpers

    private static async Task<Dictionary<int, int>> LoadCompanyMapAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, int>(await context.IdMap.LoadAsync("Firma_T", cancellationToken));

        foreach (var (legacyId, modernId) in
                 await context.IdMap.LoadAsync("Firma_T:KurumSirket", cancellationToken))
        {
            map[legacyId] = modernId;
        }

        return map;
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

    private static string? ExtensionOf(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        if (dot < 0 || dot == fileName.Length - 1)
        {
            return null;
        }

        var extension = fileName[(dot + 1)..].Trim().ToLowerInvariant();
        return extension.Length is > 0 and <= 16 ? extension : null;
    }

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

    private static long? Long(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToInt64(reader.GetValue(index));
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
