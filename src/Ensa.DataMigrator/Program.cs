using Ensa.DataMigrator.Infrastructure;
using Ensa.EntityFrameworkCore.ValueConverters;
using Ensa.DataMigrator.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// ---------------------------------------------------------------------
// Ensa.DataMigrator
// Carries the legacy application's data into the rebuilt schema.
//
//   dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv
//   dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv --dry-run
//   dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv --step locations
//   dotnet run --project src/Ensa.DataMigrator -- --list
//
// --confirm is not a formality. The development and production databases differ by three
// characters, sit on the same server and answer to the same credentials; naming the destination
// out loud is what stops this tool from writing to the wrong one.
// ---------------------------------------------------------------------

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        EnvironmentName =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    });

    // Same settings files as the API and the schema migrator, local overrides last. The legacy
    // connection string carries a password, so it lives only in appsettings.*.local.json.
    var hostSettings = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Ensa.HttpApi.Host"));

    if (Directory.Exists(hostSettings))
    {
        builder.Configuration
            .AddJsonFile(Path.Combine(hostSettings, "appsettings.json"), optional: true)
            .AddJsonFile(
                Path.Combine(hostSettings, $"appsettings.{builder.Environment.EnvironmentName}.json"),
                optional: true)
            .AddJsonFile(
                Path.Combine(hostSettings, $"appsettings.{builder.Environment.EnvironmentName}.local.json"),
                optional: true);
    }

    builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
    builder.Services.AddSerilog();

    var steps = new IMigrationStep[]
    {
        new LocationStep(),
        new CatalogueStep(),
        new TenancyStep(),
        new PasswordStep(),
        new UserSplitStep(),
        new CompanyStep(),
        new OperationsStep(),
        new VisitStep(),
        new PlanStep(),
        new RiskStep(),
        new HealthStep(),
        new PlanLineMapStep(),
        new DocumentStep(),
        new DocumentLinkStep(),
        new OperationsExtraStep(),
        new FinanceStep(),
        new HealthFormStep(),
        new IbysStep(),
        new TrainingExamStep(),
        new ReportStep(),
        new LookupExtrasStep(),
        new CommercialStep(),
        new LogStep
        {
            IncludeApplicationLog = args.Contains("--include-legacy-log", StringComparer.OrdinalIgnoreCase),
        },
        new EmployeeDocumentStep(),
        new ReencryptStep(),
        new UserIdentityVerifyStep(),
        new UserColumnClassifyStep(),
        new VerifyStep(),
    };

    if (args.Contains("--list", StringComparer.OrdinalIgnoreCase))
    {
        Log.Information("Steps, in order:");
        foreach (var step in steps.OrderBy(s => s.Order))
        {
            Log.Information("  {Order:D2} {Name,-16} {Description}", step.Order, step.Name, step.Description);
        }

        return 0;
    }

    // Column encryption is a process-wide static that EF model building reads, normally set by
    // AddEnsaEntityFrameworkCore. This tool builds its DbContext by hand, so without this the
    // converter falls back to the published development key and everything written to an encrypted
    // column is unreadable by the application - silently, because the migrator would then read it
    // back with the same wrong key and find it perfectly healthy.
    var encryption = new EnsaEncryptionOptions();
    builder.Configuration.GetSection(EnsaEncryptionOptions.SectionName).Bind(encryption);
    encryption.EnsureUsable(builder.Environment.EnvironmentName);
    EnsaEncryptionOptions.SetCurrent(encryption);

    Log.Information(
        "Column encryption: {State}",
        encryption.IsConfigured ? "configured" : "development fallback");

    var target = MigrationTarget.Resolve(
        builder.Configuration.GetConnectionString("Legacy"),
        builder.Configuration.GetConnectionString("Default"),
        Value(args, "--confirm"));

    // Reading the legacy ciphertext is the one part of this tool that cannot be checked by
    // inspection: either the key, the block size and the padding are all right and identity numbers
    // come out, or something is subtly wrong and plausible rubbish comes out. --probe-crypt decrypts
    // a sample and says which.
    if (args.Contains("--probe-crypt", StringComparer.OrdinalIgnoreCase))
    {
        return await ProbeLegacyCryptAsync(target.LegacyConnectionString);
    }

    // Whether a migrated user can actually sign in. The sample check inside the password step
    // proves a hash matches its plaintext; this proves the whole round trip against the running
    // API, which is the only thing that answers "can these people log in".
    if (args.Contains("--probe-login", StringComparer.OrdinalIgnoreCase))
    {
        return await ProbeLoginAsync(
            target.LegacyConnectionString,
            target.ModernConnectionString,
            Values(args, "--api").FirstOrDefault() ?? "https://localhost:7001");
    }

    // Which coded answers a legacy encrypted column actually holds. Mapping "Evet"/"Hayir" onto an
    // enum cannot be done by reading the schema - the values are ciphertext - and guessing them is
    // how a migration quietly inverts a medical answer.
    if (args.Contains("--probe-codes", StringComparer.OrdinalIgnoreCase))
    {
        return await ProbeCodesAsync(
            target.LegacyConnectionString,
            Values(args, "--table").FirstOrDefault(),
            Values(args, "--column").ToArray());
    }

    // The 132 GB of file payloads the document step deliberately leaves behind. Separate because
    // it is a different kind of operation: it needs disk rather than a database, it runs for hours,
    // and it is the one part of this migration that may have to be pointed at a different machine.
    if (args.Contains("--export-documents", StringComparer.OrdinalIgnoreCase))
    {
        return await ExportDocumentsAsync(
            target.LegacyConnectionString,
            Values(args, "--to").FirstOrDefault(),
            target.ModernConnectionString);
    }

    var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
    var only = Values(args, "--step");

    using var host = builder.Build();
    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

    var idMap = new IdMap(target.ModernConnectionString);
    await idMap.EnsureCreatedAsync();

    var context = new MigrationContext(
        target, idMap, loggerFactory.CreateLogger("Ensa.DataMigrator"), dryRun);

    // Column limits come from the destination itself, so they cannot drift from the schema; the
    // encrypted ones are then corrected to their plaintext capacity, from the EF model.
    await context.Fitter.LoadAsync(target.ModernConnectionString);

    await using (var model = context.CreateDbContext())
    {
        context.Fitter.ApplyEncryptedColumnLimits(model);
    }

    var runner = new MigrationRunner(steps, context, loggerFactory.CreateLogger<MigrationRunner>());

    return await runner.RunAsync(only);
}
catch (Exception exception)
{
    Log.Fatal(exception, "The data migration run failed.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Decrypts a sample of legacy values and reports whether they came out as identity numbers.
/// </summary>
static async Task<int> ProbeLegacyCryptAsync(string legacyConnectionString)
{
    await using var connection = new Microsoft.Data.SqlClient.SqlConnection(legacyConnectionString);
    await connection.OpenAsync();

    await using var command = new Microsoft.Data.SqlClient.SqlCommand(
        "SELECT TOP 200 TCKimlikNo FROM Kullanici_T WHERE TCKimlikNo IS NOT NULL", connection);

    var encrypted = 0;
    var eleven = 0;
    var failed = 0;
    var plain = 0;
    var odd = new List<string>();
    string? example = null;

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var value = reader.GetString(0);

        if (!LegacyCrypt.LooksEncrypted(value))
        {
            plain++;
            continue;
        }

        encrypted++;
        var decrypted = LegacyCrypt.TryDecrypt(value);

        if (decrypted is null)
        {
            failed++;
            continue;
        }

        if (decrypted.Length == 11 && decrypted.All(char.IsDigit))
        {
            eleven++;
            example ??= decrypted[..3] + "********";
        }
        else
        {
            odd.Add($"{decrypted.Length} char(s)");
        }
    }

    Log.Information("Legacy ciphertext prefix : {Prefix}", LegacyCrypt.CipherPrefix);
    Log.Information("sampled                  : {Total}", encrypted + plain);
    Log.Information("  already plain          : {Plain}", plain);
    Log.Information("  encrypted              : {Encrypted}", encrypted);
    Log.Information("  decrypted to 11 digits : {Eleven}", eleven);
    Log.Information("  would not decrypt      : {Failed}", failed);
    Log.Information("  example                : {Example}", example ?? "(none)");

    if (odd.Count > 0)
    {
        Log.Information("  decrypted, not 11 digits: {Odd}", string.Join(", ", odd.Take(10)));
    }

    // Two separate questions. Whether the DECRYPTION works is answered by "nothing failed and the
    // output is well-formed text" - eleven digits appearing at all proves the key, the block size
    // and the padding are right together, because no near-miss produces that. Whether the SOURCE
    // is clean is a different question: a field somebody typed a passport number into decrypts
    // perfectly and is still not an identity number, and that is not the decryption's fault.
    var trustworthy = encrypted > 0 && failed == 0 && eleven > encrypted * 0.9;

    Log.Information(trustworthy
        ? "The decryption is trustworthy: nothing failed and {Eleven}/{Encrypted} are well-formed "
          + "identity numbers. The rest decrypted cleanly into something else, which is the source "
          + "data, not the key."
        : "NOT trustworthy - do not migrate encrypted columns until this reads clean.",
        eleven, encrypted);

    return trustworthy ? 0 : 1;
}

static string? Value(string[] args, string name)
{
    var index = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static List<string> Values(string[] args, string name)
{
    var found = new List<string>();
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            found.Add(args[index + 1]);
        }
    }

    return found;
}

/// <summary>
/// Signs a migrated user in against the running API, to prove the password migration end to end.
/// <para>
/// The sample check inside <c>PasswordStep</c> proves a hash matches the plaintext it was made
/// from. That is not the same as proving the application accepts it: the hash format, the security
/// stamp, the user lookup and the token endpoint all sit between the two. This walks the whole path.
/// </para>
/// <para>
/// <b>No password is printed.</b> The plaintext is decrypted, posted and dropped; only the user
/// name and the HTTP status come out.
/// </para>
/// </summary>
static async Task<int> ProbeLoginAsync(string legacyConnectionString, string modernConnectionString, string api)
{
    const int Sample = 5;

    // Legacy id -> modern user, for users that actually received a hash.
    var candidates = new List<(int LegacyId, string UserName)>();

    await using (var modern = new Microsoft.Data.SqlClient.SqlConnection(modernConnectionString))
    {
        await modern.OpenAsync();
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(
            $"""
             SELECT TOP {Sample} m.LegacyId, u.UserName
             FROM migration.IdMap AS m
             JOIN ensa.[User] AS u ON u.Id = m.ModernId
             JOIN ensa.UserProfile AS p ON p.UserId = u.Id
             WHERE m.LegacyTable = 'Kullanici_T'
               AND u.PasswordHash IS NOT NULL AND LEN(u.PasswordHash) > 0
               AND p.IsActive = 1 AND p.IsDeleted = 0
             ORDER BY m.LegacyId;
             """, modern);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            candidates.Add((reader.GetInt32(0), reader.GetString(1)));
        }
    }

    if (candidates.Count == 0)
    {
        Log.Error("No migrated user has a password hash. Run the passwords step first.");
        return 1;
    }

    var secrets = new Dictionary<int, string>();

    await using (var legacy = new Microsoft.Data.SqlClient.SqlConnection(legacyConnectionString))
    {
        await legacy.OpenAsync();
        var ids = string.Join(",", candidates.Select(c => c.LegacyId));
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(
            $"SELECT KullaniciId, Sifre FROM Kullanici_T WHERE KullaniciId IN ({ids})", legacy);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var plaintext = LegacyCrypt.TryDecrypt(reader.GetString(1));
            if (!string.IsNullOrEmpty(plaintext))
            {
                secrets[reader.GetInt32(0)] = plaintext;
            }
        }
    }

    // The development certificate is self-signed; this probe is a local diagnostic, never a
    // component of the application.
    using var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    };
    using var client = new HttpClient(handler) { BaseAddress = new Uri(api) };

    var succeeded = 0;
    var attempted = 0;

    foreach (var (legacyId, userName) in candidates)
    {
        if (!secrets.TryGetValue(legacyId, out var password))
        {
            continue;
        }

        attempted++;

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "ensa-spa",
            ["username"] = userName,
            ["password"] = password,
            ["scope"] = "openid profile email roles offline_access ensa",
        });

        try
        {
            using var response = await client.PostAsync("/connect/token", form);
            var ok = response.IsSuccessStatusCode;
            if (ok)
            {
                succeeded++;
            }

            Log.Information(
                "  {UserName,-28} HTTP {Status} {Verdict}",
                userName, (int)response.StatusCode, ok ? "SIGNED IN" : "REJECTED");
        }
        catch (HttpRequestException exception)
        {
            Log.Error("  {UserName,-28} could not reach {Api}: {Message}", userName, api, exception.Message);
            return 1;
        }
    }

    Log.Information("Migrated users signed in: {Succeeded}/{Attempted}", succeeded, attempted);
    return succeeded == attempted && attempted > 0 ? 0 : 1;
}

/// <summary>
/// Copies the legacy file payloads onto disk, in the layout the document storage reads.
/// <para>
/// The document steps write metadata and no bytes; this writes the bytes. They are 132 GB, so it
/// is a separate command: it is run once, it takes hours, and it needs a disk rather than a
/// database. Each file is streamed straight from the legacy column to its final path without being
/// held in memory, which is what makes an 88 MB row unremarkable.
/// </para>
/// <para>
/// <b>Three sources, not one.</b> Most files are in <c>Dosya_T</c>, but the legacy schema also
/// kept observation photographs and evacuation plans inline in the tables that use them. All three
/// became <see cref="Ensa.Domain.Documents.Document"/> rows, so all three are placed here.
/// </para>
/// <para>
/// <b>Resumable, because it will be interrupted.</b> A file already on disk at the right size is
/// left alone, so a second run continues rather than starts again. Every file is written to a
/// temporary name first and moved into place, so an interrupted copy can never be mistaken for a
/// complete one.
/// </para>
/// </summary>
static async Task<int> ExportDocumentsAsync(
    string legacyConnectionString,
    string? destination,
    string modernConnectionString)
{
    if (string.IsNullOrWhiteSpace(destination))
    {
        Log.Error("--export-documents needs --to <directory>, the document storage root.");
        return 1;
    }

    // (id map key, legacy table, key column, payload column)
    (string MapKey, string Table, string KeyColumn, string BlobColumn)[] sources =
    [
        ("Dosya_T", "Dosya_T", "DosyaId", "Dosya"),
        (OperationsExtraStep.FieldObservationBlobs,
            "SahaGozlemRaporuSatirlari_T", "SahaGozlemSatiriId", "Dosya"),
        (OperationsExtraStep.EvacuationPlanBlobs,
            "AcilDurumEylemPlani_T", "AcilDurumEylemPlaniId", "TahliyePlani"),
        (CommercialStep.ModuleArchiveBlobs,
            "ModulArsivDetay_T", "ModulArsivDetayId", "Dosya"),
        (CommercialStep.PenaltySurveyLogoBlobs,
            "CezaAnketi_T", "CezaAnketId", "Logo"),
    ];

    var root = Path.GetFullPath(destination);
    Directory.CreateDirectory(root);
    Log.Information("Exporting document payloads to {Root}", root);

    var totalWritten = 0;
    var totalAlready = 0;
    var totalEmpty = 0;
    var totalMissing = 0;
    long totalBytes = 0;

    foreach (var source in sources)
    {
        // The path is the destination's, not the legacy table's: it is built from the tenant the
        // document step resolved. Reading it back from ensa.Document keeps the two in step even
        // if the derivation ever changes, and a document the step skipped is skipped here too.
        var targets = new Dictionary<int, (string Path, long Size)>();

        await using (var modern = new Microsoft.Data.SqlClient.SqlConnection(modernConnectionString))
        {
            await modern.OpenAsync();
            await using var command = new Microsoft.Data.SqlClient.SqlCommand(
                """
                SELECT m.LegacyId, d.StoragePath, d.SizeBytes
                FROM migration.IdMap AS m
                JOIN ensa.Document AS d ON d.Id = m.ModernId
                WHERE m.LegacyTable = @key AND d.StoragePath IS NOT NULL
                ORDER BY m.LegacyId;
                """, modern) { CommandTimeout = 600 };
            command.Parameters.AddWithValue("@key", source.MapKey);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                targets[reader.GetInt32(0)] = (reader.GetString(1), reader.GetInt64(2));
            }
        }

        if (targets.Count == 0)
        {
            Log.Warning("  {Key}: nothing mapped, skipping", source.MapKey);
            continue;
        }

        Log.Information("  {Key}: {Count} payload(s) to place", source.MapKey, targets.Count);

        var written = 0;
        var already = 0;
        var empty = 0;
        var missing = 0;
        long bytes = 0;

        await using (var legacy = new Microsoft.Data.SqlClient.SqlConnection(legacyConnectionString))
        {
            await legacy.OpenAsync();

            foreach (var (legacyId, target) in targets)
            {
                var fullPath = Path.Combine(root, target.Path.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(fullPath) && new FileInfo(fullPath).Length == target.Size)
                {
                    already++;
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                await using var command = new Microsoft.Data.SqlClient.SqlCommand(
                    $"SELECT {source.BlobColumn} FROM {source.Table} WHERE {source.KeyColumn} = @id",
                    legacy) { CommandTimeout = 1800 };
                command.Parameters.AddWithValue("@id", legacyId);

                // SequentialAccess is the whole point: without it the provider buffers the entire
                // 88 MB value before the first read, and the export needs as much memory as the
                // largest file rather than as much as one buffer.
                await using var reader = await command.ExecuteReaderAsync(
                    System.Data.CommandBehavior.SequentialAccess);

                if (!await reader.ReadAsync())
                {
                    missing++;
                    continue;
                }

                if (await reader.IsDBNullAsync(0))
                {
                    empty++;
                    continue;
                }

                var temporaryPath = fullPath + ".partial";

                await using (var payload = reader.GetStream(0))
                await using (var file = new FileStream(
                                 temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                                 bufferSize: 1 << 20, useAsync: true))
                {
                    await payload.CopyToAsync(file, 1 << 20);
                }

                File.Move(temporaryPath, fullPath, overwrite: true);

                written++;
                bytes += new FileInfo(fullPath).Length;

                if (written % 500 == 0)
                {
                    Log.Information(
                        "    {Written} placed, {Skipped} already there, {Gigabytes:F1} GB copied",
                        written, already, bytes / 1073741824d);
                }
            }
        }

        Log.Information(
            "  {Key}: {Written} placed ({Gigabytes:F1} GB), {Already} already present, "
            + "{Empty} empty in the legacy table, {Missing} legacy row(s) gone",
            source.MapKey, written, bytes / 1073741824d, already, empty, missing);

        totalWritten += written;
        totalAlready += already;
        totalEmpty += empty;
        totalMissing += missing;
        totalBytes += bytes;
    }

    Log.Information(
        "Document payloads: {Written} placed ({Gigabytes:F1} GB), {Already} already present, "
        + "{Empty} empty, {Missing} legacy row(s) gone",
        totalWritten, totalBytes / 1073741824d, totalAlready, totalEmpty, totalMissing);

    return 0;
}


/// <summary>
/// Prints the coded answers a legacy encrypted column holds, with their frequencies.
/// <para>
/// The medical examination form keeps 122 of its 135 columns as ciphertext, and most of them are
/// closed-ended: an answer picked from a list, stored encrypted like everything else. Mapping
/// those onto the destination's enums needs to know what the list was, and nothing in the schema
/// says - <c>BalgamliOksuruk</c> is <c>nvarchar(320)</c> whether it holds "Evet" or an essay.
/// </para>
/// <para>
/// <b>Only repeated values are printed.</b> A value that appears at least
/// <see cref="MinimumOccurrences"/> times across the table is a code, not somebody's medical
/// history: a name, an address or a diagnosis does not repeat twenty times. Anything rarer is
/// counted and not shown. Nothing is written to disk.
/// </para>
/// </summary>
static async Task<int> ProbeCodesAsync(string legacyConnectionString, string? table, string[] columns)
{
    const int MinimumOccurrences = 20;
    const int MaximumLength = 60;

    if (string.IsNullOrWhiteSpace(table) || columns.Length == 0)
    {
        Log.Error("--probe-codes needs --table <name> and one or more --column <name>.");
        return 1;
    }

    // The table and column names go straight into the text of the query, so they are checked
    // rather than trusted: this is a developer tool, but a developer tool that concatenates SQL
    // is still a developer tool that concatenates SQL.
    static bool IsIdentifier(string name)
        => name.Length is > 0 and <= 128 && name.All(c => char.IsLetterOrDigit(c) || c == '_');

    if (!IsIdentifier(table) || !columns.All(IsIdentifier))
    {
        Log.Error("Table and column names must be plain identifiers.");
        return 1;
    }

    await using var connection = new Microsoft.Data.SqlClient.SqlConnection(legacyConnectionString);
    await connection.OpenAsync();

    foreach (var column in columns)
    {
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(
            $"SELECT [{column}] FROM [{table}] WHERE [{column}] IS NOT NULL", connection)
        {
            CommandTimeout = 1800,
        };

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var unreadable = 0;
        var total = 0;

        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                total++;

                var raw = reader.GetValue(0)?.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var value = LegacyCrypt.LooksEncrypted(raw) ? LegacyCrypt.TryDecrypt(raw) : raw;

                if (value is null)
                {
                    unreadable++;
                    continue;
                }

                value = value.Trim();
                if (value.Length == 0)
                {
                    continue;
                }

                counts[value] = counts.GetValueOrDefault(value) + 1;
            }
        }

        var codes = counts
            .Where(pair => pair.Value >= MinimumOccurrences && pair.Key.Length <= MaximumLength)
            .OrderByDescending(pair => pair.Value)
            .Take(25)
            .ToList();

        var withheld = counts.Count - codes.Count;

        Log.Information(
            "{Table}.{Column}: {Total} row(s), {Distinct} distinct, {Unreadable} would not decrypt",
            table, column, total, counts.Count, unreadable);

        foreach (var (value, count) in codes)
        {
            Log.Information("    {Count,7} x {Value}", count, value);
        }

        if (withheld > 0)
        {
            Log.Information(
                "    ({Withheld} value(s) too rare or too long to be a code, not shown)", withheld);
        }
    }

    return 0;
}
