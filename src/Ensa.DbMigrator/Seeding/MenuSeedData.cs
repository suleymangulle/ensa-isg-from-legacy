// ---------------------------------------------------------------------------
// GENERATED FILE - do not edit by hand.
//
// Mirrors the SPA's own navigation: every nav entry declared in
// react/ensa-web/src/pages/*/module.tsx, with its label taken from the merged English
// bundle. Regenerate after adding or moving a screen with:
//
//     python tools/gen-enums/gen_menu.py
//
// The SPA sidebar renders from code and does not read these rows; they are the legacy
// menu module's administration surface and what GET api/menu/my-menu returns.
// tools/api-tests/frontend_menu.py fails if the two ever drift apart.
//
// Each entry also carries the permission that governs it. my-menu renders an entry only for
// a user whose effective permissions contain it (ADR-041); an entry with no permission is
// not governed. This is visibility, never access -- PermissionEndpoint decides access.
// ---------------------------------------------------------------------------

namespace Ensa.DbMigrator.Seeding;

/// <summary>One entry of the seeded main menu.</summary>
/// <param name="Code">Stable <see cref="Ensa.Domain.Menus.MenuItem.Code"/>, derived from the route.</param>
/// <param name="Name">Display text, the English label the SPA shows.</param>
/// <param name="Url">SPA route, leading slash included.</param>
/// <param name="Icon">The glyph the SPA renders. The sidebar uses emoji rather than an icon font.</param>
/// <param name="Group">Sidebar section the entry belongs to.</param>
/// <param name="SortOrder">Order within the section.</param>
/// <param name="Permission">
/// <c>PermissionTarget</c> of the permission that governs the entry, or <c>null</c> when the
/// entry is governed by none. Where the screen replaced a legacy one this is the legacy page
/// target, so the migrated grants decide what a user sees; where it did not, it is the
/// permission the SPA declares. Visibility only - see ADR-041.
/// </param>
public sealed record MenuSeedEntry(
    string Code,
    string Name,
    string Url,
    string Icon,
    string Group,
    int SortOrder,
    string? Permission);

/// <summary>The seeded main menu, generated from the SPA navigation.</summary>
public static class MenuSeedData
{
    /// <summary>Layout type code of the main menu.</summary>
    public const string MainMenuTypeCode = "MAIN";

    /// <summary>Sidebar sections, in the order the SPA renders them.</summary>
    public static readonly (string Group, string Name)[] Groups =
    [
        ("overview", "Overview"),
        ("workplace", "Workplace management"),
        ("ohs", "OHS processes"),
        ("finance", "Finance"),
        ("records", "Records and documents"),
        ("admin", "Administration"),
    ];

    /// <summary>Every navigable screen the SPA declares.</summary>
    public static readonly MenuSeedEntry[] Entries =
    [
        new("HOME", "Dashboard", "/", "▤", "overview", 10, null),
        new("COMPANIES", "Companies", "/companies", "▦", "workplace", 10, "ENSA_ISG.FirmaListController"),
        new("EMPLOYEES", "Employees", "/employees", "☰", "workplace", 20, "ENSA_ISG.FirmaPersonelListController"),
        new("DEPARTMENTS", "Workplace departments", "/departments", "◫", "workplace", 30, "ENSA_ISG.FirmaBolumListController"),
        new("WORK-PLANS", "Work Plans", "/work-plans", "▤", "workplace", 30, "ENSA_ISG.CalismaPlaniListController"),
        new("ACTIVITIES", "Activity Catalogue", "/activities", "◇", "workplace", 40, "ENSA_ISG.aktivite_ekle"),
        new("EQUIPMENT", "Equipment", "/equipment", "⚙", "workplace", 40, "ENSA_ISG.FirmaCihazListController"),
        new("TRAINING-PLANS", "Training plans", "/training-plans", "◈", "ohs", 10, "ENSA_ISG.EgitimPlaniListController"),
        new("RISK-ASSESSMENTS", "Risk assessments", "/risk-assessments", "⚠", "ohs", 20, "ENSA_ISG.RiskAnalizRaporuListController"),
        new("TRAININGS", "Training Catalogue", "/trainings", "▣", "ohs", 20, "ENSA_ISG.EgitimListController"),
        new("EMERGENCY-PLANS", "Emergency Plans", "/emergency-plans", "⚑", "ohs", 25, "ENSA_ISG.AcilDurumEylemPlaniListController"),
        new("MEDICAL-EXAMINATIONS", "Health surveillance", "/medical-examinations", "✚", "ohs", 30, "ENSA_ISG.EK_2_FormuController"),
        new("TRAINING-PROGRESS", "Training Progress", "/training-progress", "◉", "ohs", 30, "ENSA_ISG.Controllers.EgitimKatilimSertifikasiController"),
        new("EPRESCRIPTIONS", "E-prescriptions", "/eprescriptions", "℞", "ohs", 40, "ENSA_ISG.Controllers.EReceteListesiController"),
        new("INCIDENTS", "Incidents", "/incidents", "⚡", "ohs", 40, "ENSA_ISG.Controllers.OlayKayitlariController"),
        new("CORRECTIVE-ACTIONS", "Corrective Actions", "/corrective-actions", "✔", "ohs", 50, "ENSA_ISG.Controllers.DOFController"),
        new("FIELD-OBSERVATIONS", "Field Observations", "/field-observations", "◎", "ohs", 60, "ENSA_ISG.SahaGozlemListController"),
        new("INVOICES", "Invoices", "/invoices", "🧾", "finance", 10, "ENSA_ISG.SatisFaturalariController"),
        new("CASH-REGISTER", "Cash Register", "/cash-register", "💰", "finance", 20, "ENSA_ISG.MuhasebeModulu.CariHareketlerController"),
        new("PENALTIES", "Penalties", "/penalties", "⚖", "finance", 30, "ENSA_ISG.CezaAnketiController"),
        new("FINANCE-BALANCES", "Company Balances", "/finance/balances", "📊", "finance", 40, "ENSA_ISG.MuhasebeModulu.CariHareketlerController"),
        new("IBYS", "IBYS submissions", "/ibys", "⇪", "records", 10, "ENSA_ISG.Controllers.IBYSController"),
        new("DOCUMENTS", "Documents", "/documents", "🗎", "records", 20, "ENSA_ISG.DosyaController"),
        new("FORMS", "Forms", "/forms", "🗒", "records", 30, "ENSA_ISG.Controllers.IsgDokumantasyonController"),
        new("ARCHIVE", "Module archive", "/archive", "🗄", "records", 40, "ENSA_ISG.ModulArsiviController"),
        new("REPORTS-OHS", "OHS Control Report", "/reports/ohs", "◈", "records", 60, "ENSA_ISG.ISGKontrolRaporuController"),
        new("REPORTS-ACTIVITIES", "Activity Reports", "/reports/activities", "▤", "records", 70, "ENSA_ISG.Controllers.FirmaRaporlamaController"),
        new("REPORTS-YEAR-END", "Year-End Review", "/reports/year-end", "◎", "records", 80, "ENSA_ISG.Controllers.YilSonuDegerlendirmeRaporuController"),
        new("VISITS", "Visits", "/visits", "🗓", "records", 100, "ENSA_ISG.ZiyaretTakvimiController"),
        new("SUPPORT-TICKETS", "Support tickets", "/support-tickets", "🛟", "records", 110, "ENSA_ISG.UserRequestController"),
        new("MESSAGES", "Messages", "/messages", "✉", "records", 120, "Ensa.Message"),
        new("MAIL", "Mail", "/mail", "📧", "records", 130, "Ensa.Mail"),
        new("USERS", "Users", "/users", "◉", "admin", 10, "ENSA_ISG.KullaniciListController"),
        new("ROLES", "Roles", "/roles", "◈", "admin", 20, "ENSA_ISG.YetkilendirmeController"),
        new("PERMISSIONS", "Permissions", "/permissions", "⚿", "admin", 30, "ENSA_ISG.YetkilendirmeController"),
        new("ORGANIZATIONS", "Organisations", "/organizations", "⌂", "admin", 40, "Ensa.Tenant"),
        new("OFFICES", "Offices", "/offices", "▤", "admin", 50, "ENSA_ISG.OfisListesiController"),
        new("SETTINGS-PARAMETERS", "Parameters", "/settings/parameters", "⚙", "admin", 60, "Ensa.Lookups"),
        new("SETTINGS-MENUS", "Menus", "/settings/menus", "☰", "admin", 70, "ENSA_ISG.MenuSettingsController"),
        new("SETTINGS-LOOKUPS", "Reference data", "/settings/lookups", "⛁", "admin", 80, "Ensa.Lookups"),
    ];
}
