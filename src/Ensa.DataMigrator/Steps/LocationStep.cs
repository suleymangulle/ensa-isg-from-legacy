using Ensa.DataMigrator.Infrastructure;
using Microsoft.Data.SqlClient;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// Provinces, districts and neighbourhoods.
/// <para>
/// This is the first step because every address in the system points at it, and it is the one
/// place where the destination is <b>not</b> empty: <c>ReferenceSeeder</c> already put 81
/// provinces and a 230-district starter set there. Inserting the legacy rows on top would produce
/// two Ankaras, and every company would then point at whichever one happened to win.
/// </para>
/// <para>
/// So the rule here is <b>reconcile, do not duplicate</b>: match what is already there by name,
/// record the translation, and insert only what is genuinely missing. That is also what turns the
/// district starter set into the real list — the legacy table has 1,938 of them.
/// </para>
/// <para>
/// Matching is by name, normalised for case and for the Turkish dotted/dotless i, because the two
/// sides were typed by different people a decade apart.
/// </para>
/// </summary>
public sealed class LocationStep : IMigrationStep
{
    public int Order => 10;

    public string Name => "locations";

    public string Description => "Provinces, districts and neighbourhoods (reconciled against the seed)";

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        var read = 0;
        var written = 0;
        var skipped = 0;
        var notes = new List<string>();

        // The three stages are threaded together in memory rather than through the id map,
        // because a dry run does not persist one - and a dry run whose later stages report every
        // row as an orphan is not a rehearsal, it is noise.
        var (cities, cityIds) = await ReconcileCitiesAsync(context, cancellationToken);
        read += cities.Read;
        written += cities.Written;
        skipped += cities.Skipped;
        notes.Add(cities.Note!);

        var (districts, districtIds) = await ReconcileDistrictsAsync(context, cityIds, cancellationToken);
        read += districts.Read;
        written += districts.Written;
        skipped += districts.Skipped;
        notes.Add(districts.Note!);

        var neighbourhoods = await CopyNeighbourhoodsAsync(context, districtIds, cancellationToken);
        read += neighbourhoods.Read;
        written += neighbourhoods.Written;
        skipped += neighbourhoods.Skipped;
        notes.Add(neighbourhoods.Note!);

        return new StepResult(read, written, skipped, string.Join("; ", notes));
    }

    // ------------------------------------------------------------------ provinces

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> ReconcileCitiesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var legacy = new List<(int Id, string Name, int PlateCode)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT SehirId, SehirAdi, PlakaKodu FROM Sehir_T ORDER BY SehirId", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                legacy.Add((reader.GetInt32(0), reader.GetString(1).Trim(), reader.GetInt32(2)));
            }
        }

        var existing = new Dictionary<string, int>(StringComparer.Ordinal);

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        await using (var command = new SqlCommand("SELECT Id, CityName FROM ensa.City", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                existing[Normalise(reader.GetString(1))] = reader.GetInt32(0);
            }
        }

        // Matched and inserted rows are tracked apart, because the verification judges them by
        // different rules: an inserted value must equal its legacy row byte for byte, a matched one
        // takes the seeded catalogue's spelling and is supposed to differ.
        var matchedPairs = new List<(int, int)>();
        var insertedPairs = new List<(int, int)>();
        var missing = new List<(int Id, string Name, int PlateCode)>();

        foreach (var city in legacy)
        {
            if (existing.TryGetValue(Normalise(city.Name), out var modernId))
            {
                matchedPairs.Add((city.Id, modernId));
            }
            else
            {
                missing.Add(city);
            }
        }

        if (missing.Count > 0 && context.DryRun)
        {
            foreach (var city in missing)
            {
                insertedPairs.Add((city.Id, context.NextDryRunId()));
            }
        }
        else if (missing.Count > 0)
        {
            await using var connection = await context.OpenModernAsync(cancellationToken);

            foreach (var city in missing)
            {
                await using var command = new SqlCommand("""
                    INSERT INTO ensa.City (CityName, PlateCodeCode, CreationTime)
                    OUTPUT INSERTED.Id VALUES (@name, @plate, SYSDATETIME());
                    """, connection);

                command.Parameters.AddWithValue("@name", city.Name);
                command.Parameters.AddWithValue("@plate", city.PlateCode);

                var newId = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
                insertedPairs.Add((city.Id, newId));
            }
        }

        if (!context.DryRun)
        {
            await context.IdMap.SaveAsync("Sehir_T", matchedPairs, 'M', cancellationToken);
            await context.IdMap.SaveAsync("Sehir_T", insertedPairs, 'I', cancellationToken);
        }

        var map = matchedPairs.Concat(insertedPairs)
            .ToDictionary(pair => pair.Item1, pair => pair.Item2);

        return (new StepResult(
            legacy.Count, missing.Count, legacy.Count - missing.Count,
            $"cities: {legacy.Count - missing.Count} matched the seed, {missing.Count} inserted"), map);
    }

    // ------------------------------------------------------------------ districts

    private static async Task<(StepResult Result, Dictionary<int, int> Map)> ReconcileDistrictsAsync(
        MigrationContext context,
        Dictionary<int, int> cityMap,
        CancellationToken cancellationToken)
    {

        var legacy = new List<(int Id, string Name, int CityId, int? IlCode, int? DistrictCode)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT IlceId, IlceAdi, SehirId, IlKodu, IlceKodu FROM Ilce_T ORDER BY IlceId", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                legacy.Add((
                    reader.GetInt32(0), reader.GetString(1).Trim(), reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4)));
            }
        }

        // Existing districts are keyed by province + name: two provinces may both have a Merkez.
        var existing = new Dictionary<(int CityId, string Name), int>();

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT Id, DistrictName, CityId FROM ensa.District", connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                existing[(reader.GetInt32(2), Normalise(reader.GetString(1)))] = reader.GetInt32(0);
            }
        }

        var matchedPairs = new List<(int, int)>();
        var insertedPairs = new List<(int, int)>();
        var inserted = 0;
        var orphaned = 0;

        await using (var connection = await context.OpenModernAsync(cancellationToken))
        {
            foreach (var district in legacy)
            {
                if (!cityMap.TryGetValue(district.CityId, out var modernCityId))
                {
                    // A district whose province did not come across has nowhere to hang.
                    orphaned++;
                    continue;
                }

                var key = (modernCityId, Normalise(district.Name));

                if (existing.TryGetValue(key, out var modernId))
                {
                    matchedPairs.Add((district.Id, modernId));
                    continue;
                }

                if (context.DryRun)
                {
                    var placeholder = context.NextDryRunId();
                    existing[key] = placeholder;
                    insertedPairs.Add((district.Id, placeholder));
                    inserted++;
                    continue;
                }

                await using var command = new SqlCommand("""
                    INSERT INTO ensa.District (DistrictName, CityId, IlCode, DistrictCode, CreationTime)
                    OUTPUT INSERTED.Id VALUES (@name, @city, @il, @ilce, SYSDATETIME());
                    """, connection);

                command.Parameters.AddWithValue("@name", district.Name);
                command.Parameters.AddWithValue("@city", modernCityId);
                command.Parameters.AddWithValue("@il", (object?)district.IlCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@ilce", (object?)district.DistrictCode ?? DBNull.Value);

                var newId = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
                existing[key] = newId;
                insertedPairs.Add((district.Id, newId));
                inserted++;
            }
        }

        if (!context.DryRun)
        {
            await context.IdMap.SaveAsync("Ilce_T", matchedPairs, 'M', cancellationToken);
            await context.IdMap.SaveAsync("Ilce_T", insertedPairs, 'I', cancellationToken);
        }

        var matched = legacy.Count - inserted - orphaned;
        var note = $"districts: {matched} matched, {inserted} inserted";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (province missing)";
        }

        var map = matchedPairs.Concat(insertedPairs)
            .ToDictionary(pair => pair.Item1, pair => pair.Item2);

        return (new StepResult(legacy.Count, inserted, matched + orphaned, note), map);
    }

    // ------------------------------------------------------------------ neighbourhoods

    private static async Task<StepResult> CopyNeighbourhoodsAsync(
        MigrationContext context,
        Dictionary<int, int> districtMap,
        CancellationToken cancellationToken)
    {
        var alreadyMapped = await context.IdMap.LoadAsync("Mahalle_T", cancellationToken);

        var rows = new List<(int Id, string Name, int DistrictId)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
            "SELECT MahalleId, MahalleAdi, IlceId FROM Mahalle_T ORDER BY MahalleId", connection)
            { CommandTimeout = 600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetInt32(0), reader.GetString(1).Trim(), reader.GetInt32(2)));
            }
        }

        var pending = rows.FindAll(row => !alreadyMapped.ContainsKey(row.Id));
        var orphaned = 0;
        var inserted = 0;
        var pairs = new List<(int, int)>();

        if (!context.DryRun && pending.Count > 0)
        {
            await using var connection = await context.OpenModernAsync(cancellationToken);

            foreach (var batch in pending.Chunk(500))
            {
                await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

                foreach (var row in batch)
                {
                    if (!districtMap.TryGetValue(row.DistrictId, out var modernDistrictId))
                    {
                        orphaned++;
                        continue;
                    }

                    await using var command = new SqlCommand("""
                        INSERT INTO ensa.Neighborhood (NeighborhoodName, DistrictId)
                        OUTPUT INSERTED.Id VALUES (@name, @district);
                        """, connection, transaction);

                    command.Parameters.AddWithValue("@name", row.Name);
                    command.Parameters.AddWithValue("@district", modernDistrictId);

                    var newId = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
                    pairs.Add((row.Id, newId));
                    inserted++;
                }

                await transaction.CommitAsync(cancellationToken);
            }

            await context.IdMap.SaveAsync("Mahalle_T", pairs, 'I', cancellationToken);
        }
        else if (context.DryRun)
        {
            foreach (var row in pending)
            {
                if (districtMap.ContainsKey(row.DistrictId))
                {
                    inserted++;
                }
                else
                {
                    orphaned++;
                }
            }
        }

        var note = $"neighbourhoods: {inserted} inserted, {alreadyMapped.Count} already there";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (district missing)";
        }

        return new StepResult(rows.Count, inserted, rows.Count - inserted, note);
    }

    // ------------------------------------------------------------------ helpers



    /// <summary>
    /// Folds a place name to something two datasets can be compared on.
    /// <para>
    /// Two Turkish-specific hazards, both found here rather than reasoned about in advance:
    /// </para>
    /// <para>
    /// <b>The dotted and dotless i.</b> Invariant upper-casing turns "i" into "I", which in Turkish
    /// is a different letter from "İ". Without handling it, "Isparta" and "İsparta" compare
    /// unequal and the province is inserted a second time.
    /// </para>
    /// <para>
    /// <b>The circumflex.</b> The seeded catalogue spells the province "Hakkâri", the legacy data
    /// spells it "Hakkari" — both are in use, and on a plain comparison the migration quietly
    /// created an 82nd province and hung one province's districts off it. Circumflexed vowels fold
    /// to their bare form.
    /// </para>
    /// </summary>
    private static string Normalise(string value)
        => value.Trim()
            .Replace('İ', 'i').Replace('I', 'ı')
            .ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('â', 'a').Replace('î', 'i').Replace('û', 'u');
}
