#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Fails when a screen hand-rolls UI that `rich-react-component` already provides.

The SPA's user interface comes from one component library (ADR-038). That rule is easy to state
and easy to forget: a `<button className="btn btn-primary">` looks right, compiles, renders, and
is only wrong in the sense that it is the thirtieth private copy of a component we already own.
Nothing in `tsc` can see that, so it is checked here.

The second half is subtler and matters more. Three of the library's components render English
words of their own -- `DataGrid` says "Loading..." and "No data", `Pagination` says
"Previous"/"Next", `Modal` labels its close button "Close" -- and none of them takes a prop to
change that. This product ships Turkish and English, so those three may not be imported into a
screen directly; they are reached through `@/components/DataTable` and `@/components/Form`, which
pass the translated text in. A screen that imports them straight from the package puts English on
a Turkish page, and no translation check catches it, because the string is not in our bundle.

    python tools/repo-check/check_ui_library.py
"""

from __future__ import annotations

import io
import os
import re
import sys

REPOSITORY = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PAGES = os.path.join(REPOSITORY, "react", "ensa-web", "src", "pages")
SOURCE = os.path.join(REPOSITORY, "react", "ensa-web", "src")

# Components whose visible text the library hardcodes in English. They are allowed only in the
# two wrappers that translate them.
TRANSLATED_ONLY = ("DataGrid", "Pagination", "Modal")
WRAPPERS = (
    os.path.join(SOURCE, "components", "DataTable.tsx"),
    os.path.join(SOURCE, "components", "Form.tsx"),
)

LIBRARY_IMPORT = re.compile(
    r"import\s*\{([^}]*)\}\s*from\s*'rich-react-component'", re.S)

# Markup with a library counterpart. `<Link className="btn ...">` is deliberately absent: the
# library's Button renders a real <button> and would break routing (MODULES.md).
RAW_MARKUP = (
    (re.compile(r"badge-light-"), "the old Metronic badge class", "<Badge variant=...>"),
    (re.compile(r"<button[^>]*className=\"[^\"]*\bbtn\b"), "a raw Bootstrap button", "<Button>"),
    # Tag-agnostic on purpose: `<article className="card">` is still a hand-built card.
    (re.compile(r"className=\"card[ \"]"), "a hand-built card", "<Card>"),
    (re.compile(r"className=\"[^\"]*\bspinner-border\b"), "a hand-built spinner", "<Spinner>"),
    (re.compile(r"className=\"[^\"]*\bnav-tabs\b"), "a hand-built tab strip", "<Tabs>"),
    (re.compile(r"<table[\s>]"), "a hand-built table", "DataTable from @/components/DataTable"),
    (re.compile(r"<select[\s>]"), "a raw select", "<Select options={...}>"),
    (re.compile(r"<textarea[\s>]"), "a raw textarea", "<TextArea>"),
    (re.compile(r"className=\"[^\"]*\bform-control\b"), "a raw text input", "<Input>"),
    (re.compile(r"className=\"[^\"]*\bform-check-input\b"), "a raw checkbox", "<CheckBox>"),
)

# Two screens keep their own <table>, deliberately. Both are the same kind of thing: a fixed
# layout whose shape is the content, not a list of rows fetched from an endpoint. They are named
# here rather than silently skipped, so the exception stays a decision somebody made instead of a
# hole in the check.
TABLE_EXCEPTIONS = {
    "react/ensa-web/src/pages/finance/InvoicePrintPage.tsx":
        "a paper layout - the markup itself is the deliverable",
    "react/ensa-web/src/pages/health/components/ClinicalSections.tsx":
        "clinical worksheets - one fixed row per enum member, an editable control in every cell",
}

# Screens that keep a raw form control, each for a reason the library cannot answer today. The
# list is short and every line names the constraint, so a reader can tell an engineering limit
# from an avoided conversion -- which is the whole difference between an exception and a hole.
CARD_EXCEPTIONS = {
    "react/ensa-web/src/pages/finance/InvoicePrintPage.tsx":
        "the print sheet's own root element - a paper layout, like its table",
}

CONTROL_EXCEPTIONS = {
    "react/ensa-web/src/pages/membership/UserFormModal.tsx":
        "a native colour swatch - the library has no colour input",
    "react/ensa-web/src/pages/health/components/ClinicalSections.tsx":
        "compact controls in worksheet cells - FieldShell always adds mb-3 and full-size padding",
    "react/ensa-web/src/pages/membership/PermissionMatrixPage.tsx":
        "per-row override select inside a grid cell - same FieldShell margin",
    "react/ensa-web/src/pages/settings/ParameterListPage.tsx":
        "inline edit inside a table row - same FieldShell margin",
    "react/ensa-web/src/pages/settings/LookupListPage.tsx":
        "size={12} column browser - Select is a collapsed dropdown, there is no listbox mode",
    "react/ensa-web/src/pages/finance/PenaltySurveyLineForm.tsx":
        "size={6} listbox - same reason",
    "react/ensa-web/src/pages/documents/DocumentFormModal.tsx":
        "file upload plus a monospace hash field - Input's className cannot reach the control",
    "react/ensa-web/src/pages/LoginPage.tsx":
        "form-control-lg sign-in fields - the library's Input has no size",
    # Pending, not permanent: FilterSelect takes `children` (raw <option>s) and has 32 call
    # sites. Converting it to the library's `options` API is a follow-up task, not a limit.
    "react/ensa-web/src/pages/finance/components.tsx":
        "FilterSelect's children API - 32 call sites, conversion pending",
    "react/ensa-web/src/pages/observations/components.tsx":
        "FilterSelect's children API and a contrast-fixed alert panel - conversion pending",
    "react/ensa-web/src/pages/reports/components.tsx":
        "FilterSelect's children API - conversion pending",
}

CONTROL_RULES = ("a raw select", "a raw textarea", "a raw text input", "a raw checkbox")


# The library's `Spinner` defaults its label to the English "Loading...", and that label is only
# ever read by a screen reader — so a missing one is invisible in review and audible to exactly
# the user who cannot afford it. `@/components/DataTable`'s own `Spinner` takes no props and
# supplies the translation itself, so only the library's is checked.
SPINNER = re.compile(r"<Spinner(?P<attributes>[^>]*?)/?>")


def sources(root):
    for directory, subdirectories, names in os.walk(root):
        subdirectories[:] = [d for d in subdirectories if d != "node_modules"]
        for name in names:
            if name.endswith((".ts", ".tsx")):
                yield os.path.join(directory, name)


def relative(path):
    return os.path.relpath(path, REPOSITORY).replace("\\", "/")


def main():
    direct_imports = []
    raw_markup = []
    silent_spinners = []

    for path in sources(SOURCE):
        text = io.open(path, encoding="utf-8").read()

        if path not in WRAPPERS:
            for match in LIBRARY_IMPORT.finditer(text):
                names = [n.strip().replace("type ", "") for n in match.group(1).split(",")]
                for name in names:
                    bare = name.split(" as ")[0].strip()
                    if bare in TRANSLATED_ONLY:
                        direct_imports.append((relative(path), bare))

        imports_library_spinner = any(
            "Spinner" in [n.strip() for n in match.group(1).split(",")]
            for match in LIBRARY_IMPORT.finditer(text))

        for match in SPINNER.finditer(text) if imports_library_spinner else ():
            if "label=" not in match.group("attributes"):
                line = text.count("\n", 0, match.start()) + 1
                silent_spinners.append((relative(path), line))

        # Markup rules read `.tsx` only: a doc comment in a `.ts` helper may well mention
        # `<select>` while containing no markup at all.
        if path.startswith(PAGES) and path.endswith(".tsx"):
            for pattern, what, instead in RAW_MARKUP:
                if what == "a hand-built table" and relative(path) in TABLE_EXCEPTIONS:
                    continue
                if what in CONTROL_RULES and relative(path) in CONTROL_EXCEPTIONS:
                    continue
                if what == "a hand-built card" and relative(path) in CARD_EXCEPTIONS:
                    continue
                for match in pattern.finditer(text):
                    line = text.count("\n", 0, match.start()) + 1
                    raw_markup.append((relative(path), line, what, instead))

    print("=== SPA COMPONENT LIBRARY ===")
    print("  screens scanned            : %d" % len(list(sources(PAGES))))
    print("  English-carrying imports   : %d" % len(direct_imports))
    print("  hand-rolled markup         : %d" % len(raw_markup))
    print("  spinners with no label     : %d" % len(silent_spinners))
    print("  documented exceptions      : %d table, %d card, %d control"
          % (len(TABLE_EXCEPTIONS), len(CARD_EXCEPTIONS), len(CONTROL_EXCEPTIONS)))
    for path, reason in sorted(TABLE_EXCEPTIONS.items()):
        print("      table   %-46s %s" % (path.split("src/pages/")[-1], reason))
    for path, reason in sorted(CARD_EXCEPTIONS.items()):
        print("      card    %-46s %s" % (path.split("src/pages/")[-1], reason))
    for path, reason in sorted(CONTROL_EXCEPTIONS.items()):
        print("      control %-46s %s" % (path.split("src/pages/")[-1], reason))

    if direct_imports:
        print("\n-- IMPORTED STRAIGHT FROM THE PACKAGE (renders English text) --")
        for path, name in direct_imports:
            through = "@/components/Form" if name == "Modal" else "@/components/DataTable"
            print("  %-58s %s -> use %s" % (path, name, through))

    if raw_markup:
        print("\n-- HAND-ROLLED MARKUP --")
        for path, line, what, instead in raw_markup[:40]:
            print("  %s:%d  %s -> %s" % (path, line, what, instead))
        if len(raw_markup) > 40:
            print("  ... and %d more" % (len(raw_markup) - 40))

    if silent_spinners:
        print("\n-- SPINNER WITHOUT A TRANSLATED LABEL --")
        for path, line in silent_spinners:
            print("  %s:%d  pass label={t('common.loading')}" % (path, line))

    return 1 if direct_imports or raw_markup or silent_spinners else 0


if __name__ == "__main__":
    sys.exit(main())
