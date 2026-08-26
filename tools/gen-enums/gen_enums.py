# -*- coding: utf-8 -*-
"""
Generates the frontend enum bundle from the backend enum definitions.

The API serialises enums as numbers (no JsonStringEnumConverter, see EnsaHttpApiModule), so the
SPA needs the numeric values. Hand-copying them is how a frontend silently drifts from the
contract, so they are generated instead.
"""
import io
import os
import re
import sys

SOURCE_DIR = "src/Ensa.Domain.Shared/Enums"
TARGET = "react/ensa-web/src/api/enums.ts"

ENUM = re.compile(r"^public enum (\w+)\s*$", re.M)
MEMBER = re.compile(r"^\s*(\w+)\s*=\s*(-?\d+)\s*,?\s*$", re.M)
SUMMARY = re.compile(r"///\s*<summary>(.*?)</summary>", re.S)


def parse(path):
    """Yields (enum name, doc line, [(member, value)]) for every enum in a file."""
    source = io.open(path, encoding="utf-8-sig").read()
    matches = list(ENUM.finditer(source))

    for index, match in enumerate(matches):
        name = match.group(1)
        start = source.index("{", match.end())
        depth, end = 0, start
        for position in range(start, len(source)):
            if source[position] == "{":
                depth += 1
            elif source[position] == "}":
                depth -= 1
                if depth == 0:
                    end = position
                    break

        body = source[start:end]
        members = [(m.group(1), m.group(2)) for m in MEMBER.finditer(body)]
        if not members:
            continue

        # The XML doc immediately above the declaration, flattened to one line.
        head = source[matches[index - 1].end() if index else 0:match.start()]
        doc = SUMMARY.findall(head)
        summary = ""
        if doc:
            summary = " ".join(
                line.strip().lstrip("/").strip()
                for line in doc[-1].splitlines()).strip()
            summary = re.sub(r"<[^>]+>", "", summary)
            summary = re.sub(r"\s+", " ", summary).strip()
            # A "*/" inside the doc text (e.g. "the Sigara*/Alkol* column group") would close
            # the generated block comment early and break the file.
            summary = summary.replace("*/", "* /")

        yield name, summary, members


def main():
    blocks = []
    total = 0

    for file_name in sorted(os.listdir(SOURCE_DIR)):
        if not file_name.endswith(".cs"):
            continue
        for name, summary, members in parse(os.path.join(SOURCE_DIR, file_name)):
            total += 1
            lines = []
            if summary:
                lines.append("/** %s */" % summary)
            lines.append("export enum %s {" % name)
            for member, value in members:
                lines.append("  %s = %s," % (member, value))
            lines.append("}")
            blocks.append("\n".join(lines))

    header = (
        "// ---------------------------------------------------------------------------\n"
        "// GENERATED FILE - do not edit by hand.\n"
        "//\n"
        "// Mirrors src/Ensa.Domain.Shared/Enums/*.cs. The API serialises enums as NUMBERS\n"
        "// (no JsonStringEnumConverter; see EnsaHttpApiModule.ConfigureJson), so the SPA needs the\n"
        "// numeric values and they must match the backend exactly. Regenerate with:\n"
        "//\n"
        "//     python tools/gen-enums/gen_enums.py\n"
        "//\n"
        "// Display labels are NOT here: they live in the locale bundles under `enums.*`, because\n"
        "// the UI ships in Turkish and English.\n"
        "// ---------------------------------------------------------------------------\n"
    )

    io.open(TARGET, "w", encoding="utf-8", newline="\n").write(
        header + "\n" + "\n\n".join(blocks) + "\n")

    print("%d enum yazildi -> %s" % (total, TARGET))
    return 0


if __name__ == "__main__":
    sys.exit(main())
