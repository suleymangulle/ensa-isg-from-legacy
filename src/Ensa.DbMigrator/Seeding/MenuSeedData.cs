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
// ---------------------------------------------------------------------------

namespace Ensa.DbMigrator.Seeding;

/// <summary>One entry of the seeded main menu.</summary>
/// <param name="Code">Stable <see cref="Ensa.Domain.Menus.MenuItem.Code"/>, derived from the route.</param>
/// <param name="Name">Display text, the English label the SPA shows.</param>
/// <param name="Url">SPA route, leading slash included.</param>
/// <param name="Icon">The glyph the SPA renders. The sidebar uses emoji rather than an icon font.</param>
/// <param name="Group">Sidebar section the entry belongs to.</param>
/// <param name="SortOrder">Order within the section.</param>
public sealed record MenuSeedEntry(
    string Code,
    string Name,
    string Url,
    string Icon,
    string Group,
    int SortOrder);

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
        new("HOME", "Dashboard", "/", "▤", "overview", 10),
        new("COMPANIES", "Companies", "/companies", "▦", "workplace", 10),
        new("EMPLOYEES", "Employees", "/employees", "☰", "workplace", 20),
        new("DEPARTMENTS", "Workplace departments", "/departments", "◫", "workplace", 30),
        new("WORK-PLANS", "Work Plans", "/work-plans", "▤", "workplace", 30),
        new("ACTIVITIES", "Activity Catalogue", "/activities", "◇", "workplace", 40),
        new("EQUIPMENT", "Equipment", "/equipment", "⚙", "workplace", 40),
        new("TRAINING-PLANS", "Training plans", "/training-plans", "◈", "ohs", 10),
        new("RISK-ASSESSMENTS", "Risk assessments", "/risk-assessments", "⚠", "ohs", 20),
        new("TRAININGS", "Training Catalogue", "/trainings", "▣", "ohs", 20),
        new("EMERGENCY-PLANS", "Emergency Plans", "/emergency-plans", "⚑", "ohs", 25),
        new("MEDICAL-EXAMINATIONS", "Health surveillance", "/medical-examinations", "✚", "ohs", 30),
        new("TRAINING-PROGRESS", "Training Progress", "/training-progress", "◉", "ohs", 30),
        new("EPRESCRIPTIONS", "E-prescriptions", "/eprescriptions", "℞", "ohs", 40),
        new("INCIDENTS", "Incidents", "/incidents", "⚡", "ohs", 40),
        new("CORRECTIVE-ACTIONS", "Corrective Actions", "/corrective-actions", "✔", "ohs", 50),
        new("FIELD-OBSERVATIONS", "Field Observations", "/field-observations", "◎", "ohs", 60),
        new("INVOICES", "Invoices", "/invoices", "🧾", "finance", 10),
        new("CASH-REGISTER", "Cash Register", "/cash-register", "💰", "finance", 20),
        new("PENALTIES", "Penalties", "/penalties", "⚖", "finance", 30),
        new("FINANCE-BALANCES", "Company Balances", "/finance/balances", "📊", "finance", 40),
        new("IBYS", "IBYS submissions", "/ibys", "⇪", "records", 10),
        new("DOCUMENTS", "Documents", "/documents", "🗎", "records", 20),
        new("FORMS", "Forms", "/forms", "🗒", "records", 30),
        new("ARCHIVE", "Module archive", "/archive", "🗄", "records", 40),
        new("REPORTS-OHS", "OHS Control Report", "/reports/ohs", "◈", "records", 60),
        new("REPORTS-ACTIVITIES", "Activity Reports", "/reports/activities", "▤", "records", 70),
        new("REPORTS-YEAR-END", "Year-End Review", "/reports/year-end", "◎", "records", 80),
        new("VISITS", "Visits", "/visits", "🗓", "records", 100),
        new("SUPPORT-TICKETS", "Support tickets", "/support-tickets", "🛟", "records", 110),
        new("MESSAGES", "Messages", "/messages", "✉", "records", 120),
        new("MAIL", "Mail", "/mail", "📧", "records", 130),
        new("USERS", "Users", "/users", "◉", "admin", 10),
        new("ROLES", "Roles", "/roles", "◈", "admin", 20),
        new("PERMISSIONS", "Permissions", "/permissions", "⚿", "admin", 30),
        new("ORGANIZATIONS", "Organisations", "/organizations", "⌂", "admin", 40),
        new("OFFICES", "Offices", "/offices", "▤", "admin", 50),
        new("SETTINGS-PARAMETERS", "Parameters", "/settings/parameters", "⚙", "admin", 60),
        new("SETTINGS-MENUS", "Menus", "/settings/menus", "☰", "admin", 70),
        new("SETTINGS-LOOKUPS", "Reference data", "/settings/lookups", "⛁", "admin", 80),
    ];
}
