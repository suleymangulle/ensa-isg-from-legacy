#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Fails if a tracked file carries a live credential.

This repository is public, and a credential committed to it is a published credential --
rotating it is the only remedy, because the commit stays reachable long after the file is
fixed. It has happened: a connection string for the shared SQL Server went into
``appsettings.json`` and ``appsettings.Development.json``, both of which say in their own
comments not to do exactly that.

Comments do not stop it happening again; a check that fails does. Run it before pushing, and
in CI:

    python tools/repo-check/check_no_secrets.py

What counts as a finding:

* a ``Password=`` or ``Pwd=`` with a real value -- an empty one or an obvious placeholder
  (``***``, ``<password>``, ``${...}``, ``__PASSWORD__``) is fine
* a ``User Id=sa`` / ``Uid=sa``, because the shared administrator login should never be named
  in source even without its password
* a ``Server=`` pointing at a bare IPv4 address, which is a real host rather than an example

Deliberately **not** a finding: the development encryption key in
``appsettings.Development.json``. It is published on purpose -- the file says so, and every
other environment refuses to start until ``Encryption__Key`` is supplied from outside source
control.

The real connection strings belong in ``appsettings.{Environment}.local.json``, which
``.gitignore`` excludes, or in ``ConnectionStrings__Default`` at deployment.
"""

from __future__ import annotations

import os
import re
import subprocess
import sys

REPOSITORY = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

# Values that are plainly not a credential.
PLACEHOLDER = re.compile(
    r"^\s*$|^\*+$|^<[^>]*>$|^\$\{[^}]*\}$|^__[A-Z_]+__$|^your[-_ ]|^changeme$|^x+$",
    re.IGNORECASE,
)

# A password only counts when it sits in something shaped like a connection string. Without
# this the check reports every `password = value` in C# and TypeScript, which is noise, and a
# check that cries wolf gets switched off.
CONNECTION_STRING = re.compile(
    r"\b(?:server|data\s*source|initial\s*catalog)\s*=", re.IGNORECASE
)

FINDINGS = (
    (
        "password",
        re.compile(r"\b(?:password|pwd)\s*=\s*([^;\"'\r\n]*)", re.IGNORECASE),
        "a connection-string password",
    ),
    (
        "sa-login",
        re.compile(r"\b(?:user\s*id|uid)\s*=\s*(sa)\s*(?:;|\"|'|$)", re.IGNORECASE),
        "the sa administrator login",
    ),
    (
        "host",
        re.compile(r"\bserver\s*=\s*((?:\d{1,3}\.){3}\d{1,3})", re.IGNORECASE),
        "a real server address",
    ),
)

# Text files only; a match inside a binary is noise.
SKIP_SUFFIXES = (
    ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".pdf", ".zip", ".dll", ".exe",
    ".pfx", ".woff", ".woff2", ".ttf", ".eot", ".snk",
)

# This file names the patterns it looks for, so it would report itself.
SKIP_PATHS = ("tools/repo-check/check_no_secrets.py",)


def git(*arguments: str) -> list[str]:
    result = subprocess.run(
        ["git", "-C", REPOSITORY, *arguments],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if result.returncode != 0:
        print(f"git {' '.join(arguments)} failed:\n{result.stderr}")
        sys.exit(2)
    return [line for line in result.stdout.splitlines() if line.strip()]


def main() -> int:
    tracked = git("ls-files")
    if not tracked:
        print("no tracked files; nothing to check.")
        return 0

    findings: list[tuple[str, int, str, str]] = []

    for path in tracked:
        if path.endswith(SKIP_SUFFIXES) or path in SKIP_PATHS:
            continue

        full = os.path.join(REPOSITORY, path)
        if not os.path.isfile(full):
            continue

        try:
            with open(full, encoding="utf-8-sig", errors="strict") as handle:
                lines = handle.read().splitlines()
        except (UnicodeDecodeError, OSError):
            continue

        for number, line in enumerate(lines, start=1):
            in_connection_string = CONNECTION_STRING.search(line) is not None

            for name, pattern, description in FINDINGS:
                if name == "password" and not in_connection_string:
                    continue

                match = pattern.search(line)
                if match and not PLACEHOLDER.match(match.group(1)):
                    findings.append((path, number, description, match.group(1)))

    if not findings:
        print(f"no credentials in {len(tracked)} tracked files.")
        return 0

    print()
    print("-- credentials in tracked files --")
    for path, number, description, value in findings:
        masked = value[:2] + "..." if len(value) > 3 else "..."
        print(f"   {path}:{number}  {description} ({masked})")

    print()
    print("This repository is public, so anything here is published. Remove the value, then")
    print("ROTATE it: the commit stays reachable even after the file is fixed.")
    print("Real connection strings go in appsettings.{Environment}.local.json, which .gitignore")
    print("excludes, or in the ConnectionStrings__Default environment variable at deployment.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
