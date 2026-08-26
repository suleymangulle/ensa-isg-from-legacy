# -*- coding: utf-8 -*-
"""
Generates the frontend permission constants from EnsaPermissions.

The same reasoning as the enum generator: these strings are the `ensa:permission` claim values
carried in the token AND the authorization policy names on the server. A hand-copied constant
that drifts by one character does not fail a build — it silently hides a menu entry from someone
who should see it, or shows one to someone who cannot use it.

Run after changing EnsaPermissions:

    python tools/gen-enums/gen_permissions.py
"""
import io
import re
import sys

SOURCE = "src/Ensa.Application.Contracts/Permissions/EnsaPermissions.cs"
TARGET = "react/ensa-web/src/api/permissions.ts"

# Only the NESTED groups. Without the leading indentation the outer EnsaPermissions class also
# matches, and its non-greedy body then swallows the first nested class whole.
GROUP = re.compile(r"^    public static class (\w+)\s*\n    \{(.*?)\n    \}", re.S | re.M)
MEMBER = re.compile(r'public const string (\w+)\s*=\s*(.+?);')
SUMMARY = re.compile(r"///\s*<summary>(.*?)</summary>", re.S)


def resolve(expression, group_default):
    """Turns `Default + ".Create"` into a literal string."""
    parts = [part.strip() for part in expression.split("+")]
    out = []

    for part in parts:
        if part.startswith('"') and part.endswith('"'):
            out.append(part[1:-1])
        elif part == "GroupName":
            out.append("Ensa")
        elif part == "Default":
            out.append(group_default or "")
        else:
            return None

    return "".join(out)


def main():
    source = io.open(SOURCE, encoding="utf-8-sig").read()

    blocks = []
    total = 0

    for match in GROUP.finditer(source):
        name, body = match.group(1), match.group(2)

        members = []
        group_default = None

        for member in MEMBER.finditer(body):
            key, expression = member.group(1), member.group(2)
            value = resolve(expression, group_default)
            if value is None:
                continue
            if key == "Default":
                group_default = value
            members.append((key, value))
            total += 1

        if not members:
            continue

        head = source[:match.start()]
        doc = SUMMARY.findall(head[head.rfind("public static class") if False else 0:])
        summary = ""
        # Only the doc comment immediately above this class.
        preceding = source[:match.start()].rstrip().splitlines()
        comment_lines = []
        for line in reversed(preceding):
            stripped = line.strip()
            if stripped.startswith("///"):
                comment_lines.append(stripped.lstrip("/").strip())
            elif stripped.startswith("//") or stripped == "":
                if comment_lines:
                    break
            else:
                break
        if comment_lines:
            summary = " ".join(reversed(comment_lines))
            summary = re.sub(r"<[^>]+>", "", summary)
            summary = re.sub(r"\s+", " ", summary).strip()
            summary = summary.replace("*/", "* /")

        lines = []
        if summary:
            lines.append("  /** %s */" % summary)
        lines.append("  %s: {" % name)
        for key, value in members:
            lines.append("    %s: '%s'," % (key, value))
        lines.append("  },")
        blocks.append("\n".join(lines))

    header = (
        "// ---------------------------------------------------------------------------\n"
        "// GENERATED FILE - do not edit by hand.\n"
        "//\n"
        "// Mirrors src/Ensa.Application.Contracts/Permissions/EnsaPermissions.cs. These strings are\n"
        "// both the `ensa:permission` claim values in the token and the authorization policy names\n"
        "// on the server, so a copy that drifts by one character silently hides a screen from\n"
        "// someone entitled to it. Regenerate with:\n"
        "//\n"
        "//     python tools/gen-enums/gen_permissions.py\n"
        "//\n"
        "// Hiding a link is a courtesy, never a control: every endpoint enforces its own\n"
        "// permission and answers 403 regardless of what the interface shows.\n"
        "// ---------------------------------------------------------------------------\n"
    )

    io.open(TARGET, "w", encoding="utf-8", newline="\n").write(
        header + "\nexport const PERMISSIONS = {\n" + "\n".join(blocks) + "\n} as const\n")

    print("%d izin sabiti yazildi -> %s" % (total, TARGET))
    return 0


if __name__ == "__main__":
    sys.exit(main())
