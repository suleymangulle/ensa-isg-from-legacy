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
    entries, missing = [], []

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

            entries.append({
                "path": match.group("path"),
                "code": code_of(match.group("path")),
                "name": text,
                "icon": fields.get("icon", ""),
                "group": fields.get("group", "overview"),
                "order": int(order.group(1)) if order else 0,
            })

    return entries, missing, bundle


def escape(value):
    return value.replace("\\", "\\\\").replace('"', '\\"')


def main():
    entries, missing, bundle = collect()

    if missing:
        print("Cevirisi bulunamayan menu etiketi:")
        for item in missing:
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
        "public sealed record MenuSeedEntry(",
        "    string Code,",
        "    string Name,",
        "    string Url,",
        "    string Icon,",
        "    string Group,",
        "    int SortOrder);",
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
        lines.append('        new("%s", "%s", "/%s", "%s", "%s", %d),' % (
            escape(entry["code"]), escape(entry["name"]), escape(entry["path"]),
            escape(entry["icon"]), escape(entry["group"]), entry["order"]))

    lines += ["    ];", "}", ""]

    io.open(TARGET, "w", encoding="utf-8", newline="\n").write("\n".join(lines))

    print("%d menu girdisi, %d grup yazildi -> %s" % (len(entries), len(used_groups), TARGET))
    return 0


if __name__ == "__main__":
    sys.exit(main())
