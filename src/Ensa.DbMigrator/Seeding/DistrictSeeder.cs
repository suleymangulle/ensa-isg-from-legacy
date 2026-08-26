using Ensa.Domain.Lookups;
using Ensa.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ensa.DbMigrator.Seeding;

/// <summary>
/// District reference data.
/// <para>
/// <b>Scope:</b> every district of the three most populous provinces (İstanbul, Ankara, İzmir),
/// and the central district(s) of the remaining 78 provinces. This is the <i>minimum</i> set
/// needed to keep the system usable where an address has to be selected.
/// </para>
/// <para>
/// <b>Before going to production:</b> the full district list (roughly 970 rows) must be imported
/// from the legacy <c>Ilce_T</c> table or from the NVİ/TÜİK open data set. Because this seeder is
/// idempotent it is still safe to run after that import — it never re-inserts a (city, district
/// name) pair that already exists.
/// </para>
/// </summary>
public sealed class DistrictSeeder(EnsaDbContext context, ILogger<DistrictSeeder> logger) : IDataSeeder
{
    public int Order => 150;

    public string Name => "Districts (starter set)";

    /// <summary>Plate code → district names.</summary>
    private static readonly Dictionary<int, string[]> DistrictList = new()
    {
        // --- Complete lists ---
        [34] = // İstanbul
        [
            "Adalar", "Arnavutköy", "Ataşehir", "Avcılar", "Bağcılar", "Bahçelievler",
            "Bakırköy", "Başakşehir", "Bayrampaşa", "Beşiktaş", "Beykoz", "Beylikdüzü",
            "Beyoğlu", "Büyükçekmece", "Çatalca", "Çekmeköy", "Esenler", "Esenyurt",
            "Eyüpsultan", "Fatih", "Gaziosmanpaşa", "Güngören", "Kadıköy", "Kağıthane",
            "Kartal", "Küçükçekmece", "Maltepe", "Pendik", "Sancaktepe", "Sarıyer",
            "Silivri", "Sultanbeyli", "Sultangazi", "Şile", "Şişli", "Tuzla",
            "Ümraniye", "Üsküdar", "Zeytinburnu"
        ],
        [6] = // Ankara
        [
            "Akyurt", "Altındağ", "Ayaş", "Bala", "Beypazarı", "Çamlıdere", "Çankaya",
            "Çubuk", "Elmadağ", "Etimesgut", "Evren", "Gölbaşı", "Güdül", "Haymana",
            "Kahramankazan", "Kalecik", "Keçiören", "Kızılcahamam", "Mamak", "Nallıhan",
            "Polatlı", "Pursaklar", "Sincan", "Şereflikoçhisar", "Yenimahalle"
        ],
        [35] = // İzmir
        [
            "Aliağa", "Balçova", "Bayındır", "Bayraklı", "Bergama", "Beydağ", "Bornova",
            "Buca", "Çeşme", "Çiğli", "Dikili", "Foça", "Gaziemir", "Güzelbahçe",
            "Karabağlar", "Karaburun", "Karşıyaka", "Kemalpaşa", "Kınık", "Kiraz",
            "Konak", "Menderes", "Menemen", "Narlıdere", "Ödemiş", "Seferihisar",
            "Selçuk", "Tire", "Torbalı", "Urla"
        ],

        // --- Other metropolitan provinces, which have no district called "Merkez" ---
        [1] = ["Seyhan", "Çukurova", "Yüreğir", "Sarıçam"],                 // Adana
        [7] = ["Muratpaşa", "Kepez", "Konyaaltı", "Döşemealtı", "Aksu"],    // Antalya
        [16] = ["Osmangazi", "Nilüfer", "Yıldırım", "Gürsu", "Kestel"],     // Bursa
        [21] = ["Bağlar", "Kayapınar", "Sur", "Yenişehir"],                 // Diyarbakır
        [25] = ["Yakutiye", "Palandöken", "Aziziye"],                       // Erzurum
        [26] = ["Odunpazarı", "Tepebaşı"],                                  // Eskişehir
        [27] = ["Şahinbey", "Şehitkamil", "Oğuzeli"],                       // Gaziantep
        [31] = ["Antakya", "Defne", "İskenderun", "Payas"],                 // Hatay
        [33] = ["Akdeniz", "Mezitli", "Toroslar", "Yenişehir"],             // Mersin
        [38] = ["Kocasinan", "Melikgazi", "Talas", "Hacılar"],              // Kayseri
        [41] = ["İzmit", "Gebze", "Darıca", "Körfez", "Gölcük"],            // Kocaeli
        [42] = ["Selçuklu", "Meram", "Karatay"],                            // Konya
        [44] = ["Battalgazi", "Yeşilyurt"],                                 // Malatya
        [45] = ["Şehzadeler", "Yunusemre"],                                 // Manisa
        [46] = ["Dulkadiroğlu", "Onikişubat"],                              // Kahramanmaraş
        [48] = ["Menteşe", "Bodrum", "Fethiye", "Marmaris", "Milas"],       // Muğla
        [52] = ["Altınordu"],                                               // Ordu
        [54] = ["Adapazarı", "Serdivan", "Erenler", "Arifiye"],             // Sakarya
        [55] = ["İlkadım", "Atakum", "Canik", "Tekkeköy"],                  // Samsun
        [59] = ["Süleymanpaşa", "Çorlu", "Çerkezköy", "Kapaklı"],           // Tekirdağ
        [61] = ["Ortahisar", "Akçaabat", "Yomra"],                          // Trabzon
        [63] = ["Eyyübiye", "Haliliye", "Karaköprü", "Siverek"],            // Şanlıurfa
        [65] = ["İpekyolu", "Edremit", "Tuşba", "Erciş"],                   // Van
        [72] = ["Batman Merkez"],                                           // Batman
        [77] = ["Yalova Merkez"]                                            // Yalova
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var cities = await context.Set<City>()
            .AsNoTracking()
            .Select(s => new { s.Id, s.PlateCodeCode, s.CityName })
            .ToListAsync(cancellationToken);

        if (cities.Count == 0)
        {
            throw new InvalidOperationException(
                "No city rows found — ReferenceSeeder must run first.");
        }

        var current = (await context.Set<District>()
                .AsNoTracking()
                .Select(i => new { i.CityId, i.DistrictName })
                .ToListAsync(cancellationToken))
            .Select(i => (i.CityId, i.DistrictName))
            .ToHashSet();

        var toInsert = new List<District>();

        foreach (var city in cities)
        {
            var districtNames = DistrictList.TryGetValue(city.PlateCodeCode, out var list)
                ? list
                // Not in the list yet, so only the central district; the full list is imported later.
                : ["Merkez"];

            foreach (var districtName in districtNames)
            {
                if (current.Contains((city.Id, districtName)))
                {
                    continue;
                }

                toInsert.Add(new District
                {
                    CityId = city.Id,
                    DistrictName = districtName,
                    IlCode = city.PlateCodeCode
                });
            }
        }

        if (toInsert.Count == 0)
        {
            return;
        }

        context.Set<District>().AddRange(toInsert);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Count} district(s) inserted. NOTE: this is a starter set; the full district list " +
            "must be imported from the legacy Ilce_T table or from the NVİ open data set.",
            toInsert.Count);
    }
}
