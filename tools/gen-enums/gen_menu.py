# -*- coding: utf-8 -*-
"""
Generates the menu seed table from the SPA's own navigation.

The sidebar is defined in code — each `src/pages/<module>/module.tsx` declares its own nav
entries, and `moduleNavigation()` filters them by permission (ADR-023, ADR-031). That is the
renderer, and it stays the renderer: navigation must not wait on a round trip.

The `Menu` module is the other half — the legacy, configurable menu that an administrator
inspects and that `GET api/menu/my-menu` renders per user, honouring the per-user
`UserMenuOverride` rows. Its tables shipped empty, so the administration screen showed nothing
and `my-menu` answered "no menu is defined for this layout type" to everyone.

Seeding it by hand would create a second navigation definition that drifts from the first within
a release. Generating it from the SPA means there is one source of truth and the drift is
impossible; `tools/api-tests/frontend_menu.py` gates it.

Run after adding or moving a screen:

    python tools/gen-enums/gen_menu.py
"""
import io
import json
import os
import re
import sys

MODULES = "react/ensa-web/src/pages"
CORE_LOCALE = "react/ensa-web/src/i18n/locales/en.json"
TARGET = "src/Ensa.DbMigrator/Seeding/MenuSeedData.cs"

# NAV_GROUPS in src/modules/registry.ts, in the order the sidebar renders them.
GROUPS = ["overview", "workplace", "ohs", "finance", "records", "admin"]

# The dashboard declares an empty path - it is the index route - so the path group must accept
# zero characters, or the landing page silently drops out of the seeded menu.
ENTRY = re.compile(r"\{(?P<body>[^{}]*?path:\s*'(?P<path>[^']*)'[^{}]*?)\}", re.S)
FIELD = re.compile(r"(\w+):\s*'([^']*)'")
ORDER = re.compile(r"order:\s*(\d+)")

# `permission: PERMISSIONS.Company.Default` -- a constant reference, not a quoted string, so
# FIELD above does not see it.
PERMISSION = re.compile(r"permission:\s*PERMISSIONS\.([A-Za-z0-9_.]+)")

PERMISSIONS_TS = "react/ensa-web/src/api/permissions.ts"

# The legacy screen behind each modern one, keyed by the SPA route. The value is the
# `Yetki_T.YetkiHedefi` of the legacy page, which is what `PermissionStep` migrated into
# `Permission.PermissionTarget`.
#
# WHY the legacy target rather than the seeded one: this column decides what a user SEES, and the
# migrated grants -- 940 user-type rows, 1,106 organization-type rows -- are the customer's own
# decisions about who sees what. Reproducing them is the point of the migration. Access is decided
# elsewhere and is unaffected: see ADR-041 and PermissionEndpoint (ADR-033).
#
# Keyed by route rather than by permission: three report screens share one permission
# (Ensa.Report) but replaced three different legacy screens, so the permission cannot tell them
# apart. Written out one by one rather than derived by rule -- no name rule survives the
# Turkish-to-English renaming (FirmaListController is Company, EK_2_FormuController is
# MedicalExaminationForm).
LEGACY_PERMISSION = {
    "companies":             "ENSA_ISG.FirmaListController",
    "employees":             "ENSA_ISG.FirmaPersonelListController",
    "departments":           "ENSA_ISG.FirmaBolumListController",
    "work-plans":            "ENSA_ISG.CalismaPlaniListController",
    "activities":            "ENSA_ISG.aktivite_ekle",
    "equipment":             "ENSA_ISG.FirmaCihazListController",
    "training-plans":        "ENSA_ISG.EgitimPlaniListController",
    "risk-assessments":      "ENSA_ISG.RiskAnalizRaporuListController",
    "trainings":             "ENSA_ISG.EgitimListController",
    "emergency-plans":       "ENSA_ISG.AcilDurumEylemPlaniListController",
    "medical-examinations":  "ENSA_ISG.EK_2_FormuController",
    "training-progress":     "ENSA_ISG.Controllers.EgitimKatilimSertifikasiController",
    "eprescriptions":        "ENSA_ISG.Controllers.EReceteListesiController",
    "incidents":             "ENSA_ISG.Controllers.OlayKayitlariController",
    "corrective-actions":    "ENSA_ISG.Controllers.DOFController",
    "field-observations":    "ENSA_ISG.SahaGozlemListController",
    "invoices":              "ENSA_ISG.SatisFaturalariController",
    "cash-register":         "ENSA_ISG.MuhasebeModulu.CariHareketlerController",
    "penalties":             "ENSA_ISG.CezaAnketiController",
    "finance/balances":      "ENSA_ISG.MuhasebeModulu.CariHareketlerController",
    "ibys":                  "ENSA_ISG.Controllers.IBYSController",
    "documents":             "ENSA_ISG.DosyaController",
    "forms":                 "ENSA_ISG.Controllers.IsgDokumantasyonController",
    "archive":               "ENSA_ISG.ModulArsiviController",
    "reports/ohs":           "ENSA_ISG.ISGKontrolRaporuController",
    "reports/activities":    "ENSA_ISG.Controllers.FirmaRaporlamaController",
    "reports/year-end":      "ENSA_ISG.Controllers.YilSonuDegerlendirmeRaporuController",
    "visits":                "ENSA_ISG.ZiyaretTakvimiController",
    "support-tickets":       "ENSA_ISG.UserRequestController",
    "users":                 "ENSA_ISG.KullaniciListController",
    "roles":                 "ENSA_ISG.YetkilendirmeController",
    "permissions":           "ENSA_ISG.YetkilendirmeController",
    "offices":               "ENSA_ISG.OfisListesiController",
    "settings/menus":        "ENSA_ISG.MenuSettingsController",
}

# Screens with no legacy counterpart keep the permission the SPA declares. Inventing a legacy
# target for functionality the legacy system never had would be fabrication, not migration.
# (Mail, Message, Organization, Parameter, Lookup -- and the dashboard, which is governed by
# nothing because every user needs a landing page.)


def permission_targets():
    """`Company.Default` -> `Ensa.Company`, read from the generated SPA constants."""
    source = io.open(PERMISSIONS_TS, encoding="utf-8").read()
    targets, group = {}, None

    for line in source.splitlines():
        opening = re.match(r"\s*(\w+):\s*\{\s*$", line)
        if opening:
            group = opening.group(1)
            continue
        pair = re.match(r"\s*(\w+):\s*'([^']*)'", line)
        if pair and group:
            targets["%s.%s" % (group, pair.group(1))] = pair.group(2)

    return targets


def nav_extent(source):
    """Start and end offsets of the nav array body, found by matching brackets."""
    start = source.index("nav: [") + len("nav: [")
    depth = 1

    for position in range(start, len(source)):
        if source[position] == "[":
            depth += 1
        elif source[position] == "]":
            depth -= 1
            if depth == 0:
                return start, position

    raise ValueError("unterminated nav array")


def merged_english():
    """The core bundle with every module bundle merged over it, the way i18n does it."""
    def merge(target, addition):
        for key, value in addition.items():
            if isinstance(value, dict) and isinstance(target.get(key), dict):
                merge(target[key], value)
            else:
                target[key] = value

    bundle = json.load(io.open(CORE_LOCALE, encoding="utf-8"))

    for module in sorted(os.listdir(MODULES)):
        path = os.path.join(MODULES, module, "locales", "en.json")
        if os.path.exists(path):
            merge(bundle, json.load(io.open(path, encoding="utf-8")))

    return bundle


def label(bundle, key):
    node = bundle
    for part in key.split("."):
        if not isinstance(node, dict) or part not in node:
            return None
        node = node[part]
    return node if isinstance(node, str) else None


def code_of(path):
    """`settings/parameters` -> `SETTINGS-PARAMETERS`. Stable, so a re-run is a no-op."""
    return re.sub(r"[^A-Z0-9]+", "-", path.upper()).strip("-") or "HOME"


def collect():
    bundle = merged_english()
    targets = permission_targets()
    entries, missing, unresolved = [], [], []

    for module in sorted(os.listdir(MODULES)):
        path = os.path.join(MODULES, module, "module.tsx")
        if not os.path.exists(path):
            continue

        source = io.open(path, encoding="utf-8").read()
        if "nav: [" not in source:
            continue

        start, end = nav_extent(source)

        for match in ENTRY.finditer(source[start:end]):
            fields = dict(FIELD.findall(match.group("body")))
            order = ORDER.search(match.group("body"))

            text = label(bundle, fields.get("labelKey", ""))
            if text is None:
                missing.append("%s -> %s" % (module, fields.get("labelKey")))
                text = fields.get("labelKey", match.group("path"))

            # The legacy page permission when the screen replaced one; otherwise whatever the SPA
            # declares; otherwise nothing, and the entry is not governed.
            permission = LEGACY_PERMISSION.get(match.group("path"))
            if permission is None:
                declared = PERMISSION.search(match.group("body"))
                if declared:
                    permission = targets.get(declared.group(1))
                    if permission is None:
                        unresolved.append("%s -> PERMISSIONS.%s" % (module, declared.group(1)))

            entries.append({
                "path": match.group("path"),
                "code": code_of(match.group("path")),
                "name": text,
                "icon": fields.get("icon", ""),
                "group": fields.get("group", "overview"),
                "order": int(order.group(1)) if order else 0,
                "permission": permission,
            })

    return entries, missing, unresolved, bundle


def escape(value):
    return value.replace("\\", "\\\\").replace('"', '\\"')


def main():
    entries, missing, unresolved, bundle = collect()

    if missing:
        print("Cevirisi bulunamayan menu etiketi:")
        for item in missing:
            print("  ", item)
        return 1

    if unresolved:
        print("Karsiligi bulunamayan izin sabiti:")
        for item in unresolved:
            print("  ", item)
        return 1

    if not entries:
        print("Hicbir menu girdisi bulunamadi.")
        return 1

    used_groups = [g for g in GROUPS if any(e["group"] == g for e in entries)]

    lines = [
        "// ---------------------------------------------------------------------------",
        "// GENERATED FILE - do not edit by hand.",
        "//",
        "// Mirrors the SPA's own navigation: every nav entry declared in",
        "// react/ensa-web/src/pages/*/module.tsx, with its label taken from the merged English",
        "// bundle. Regenerate after adding or moving a screen with:",
        "//",
        "//     python tools/gen-enums/gen_menu.py",
        "//",
        "// The SPA sidebar renders from code and does not read these rows; they are the legacy",
        "// menu module's administration surface and what GET api/menu/my-menu returns.",
        "// tools/api-tests/frontend_menu.py fails if the two ever drift apart.",
        "//",
        "// Each entry also carries the permission that governs it. my-menu renders an entry only for",
        "// a user whose effective permissions contain it (ADR-041); an entry with no permission is",
        "// not governed. This is visibility, never access -- PermissionEndpoint decides access.",
        "// ---------------------------------------------------------------------------",
        "",
        "namespace Ensa.DbMigrator.Seeding;",
        "",
        "/// <summary>One entry of the seeded main menu.</summary>",
        "/// <param name=\"Code\">Stable <see cref=\"Ensa.Domain.Menus.MenuItem.Code\"/>, derived from the route.</param>",
        "/// <param name=\"Name\">Display text, the English label the SPA shows.</param>",
        "/// <param name=\"Url\">SPA route, leading slash included.</param>",
        "/// <param name=\"Icon\">The glyph the SPA renders. The sidebar uses emoji rather than an icon font.</param>",
        "/// <param name=\"Group\">Sidebar section the entry belongs to.</param>",
        "/// <param name=\"SortOrder\">Order within the section.</param>",
        "/// <param name=\"Permission\">",
        "/// <c>PermissionTarget</c> of the permission that governs the entry, or <c>null</c> when the",
        "/// entry is governed by none. Where the screen replaced a legacy one this is the legacy page",
        "/// target, so the migrated grants decide what a user sees; where it did not, it is the",
        "/// permission the SPA declares. Visibility only - see ADR-041.",
        "/// </param>",
        "public sealed record MenuSeedEntry(",
        "    string Code,",
        "    string Name,",
        "    string Url,",
        "    string Icon,",
        "    string Group,",
        "    int SortOrder,",
        "    string? Permission);",
        "",
        "/// <summary>The seeded main menu, generated from the SPA navigation.</summary>",
        "public static class MenuSeedData",
        "{",
        "    /// <summary>Layout type code of the main menu.</summary>",
        "    public const string MainMenuTypeCode = \"MAIN\";",
        "",
        "    /// <summary>Sidebar sections, in the order the SPA renders them.</summary>",
        "    public static readonly (string Group, string Name)[] Groups =",
        "    [",
    ]

    for group in used_groups:
        name = label(bundle, "nav.group." + group) or group
        lines.append('        ("%s", "%s"),' % (escape(group), escape(name)))

    lines += [
        "    ];",
        "",
        "    /// <summary>Every navigable screen the SPA declares.</summary>",
        "    public static readonly MenuSeedEntry[] Entries =",
        "    [",
    ]

    for entry in sorted(entries, key=lambda e: (GROUPS.index(e["group"]), e["order"], e["path"])):
        permission = ('"%s"' % escape(entry["permission"])) if entry["permission"] else "null"
        lines.append('        new("%s", "%s", "/%s", "%s", "%s", %d, %s),' % (
            escape(entry["code"]), escape(entry["name"]), escape(entry["path"]),
            escape(entry["icon"]), escape(entry["group"]), entry["order"], permission))

    lines += ["    ];", "}", ""]

    io.open(TARGET, "w", encoding="utf-8", newline="\n").write("\n".join(lines))

    print("%d menu girdisi, %d grup yazildi -> %s" % (len(entries), len(used_groups), TARGET))
    return 0


if __name__ == "__main__":
    sys.exit(main())
