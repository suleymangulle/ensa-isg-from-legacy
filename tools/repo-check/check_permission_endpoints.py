#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Fails when an API endpoint has no row in the endpoint permission map.

Controllers no longer name the permission that guards them. `[Authorize]` is parameterless
everywhere, and which permission applies is answered at request time from
`PermissionEndpoint` -- seeded from `PermissionEndpointSeedData.cs`. That is the same
arrangement the legacy application had, where `Yetki_T.YetkiHedefi` named the page or method
and no controller mentioned a permission.

The cost of that arrangement is that adding an endpoint and forgetting the map is silent in
the code. It is not silent at runtime -- an unmapped endpoint is refused, exactly as the legacy
code refused a method with no matching row -- but "nobody can use the new screen" is a poor way
to find out. This check turns it into a build failure instead.

It also enforces the other half: no `[Authorize]` may carry a permission identifier again.

    python tools/repo-check/check_permission_endpoints.py
"""

from __future__ import annotations

import glob
import io
import os
import re
import sys

REPOSITORY = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SEED = os.path.join(
    REPOSITORY, "src", "Ensa.DbMigrator", "Seeding", "PermissionEndpointSeedData.cs")

HTTP_VERB = re.compile(r"\[Http(Get|Post|Put|Delete|Patch)")
ENTRY = re.compile(r'new\("(?P<controller>\w+)",\s*"(?P<action>\w+)",')

# [Authorize(...)] carrying a permission name. AuthenticationSchemes is a different thing and
# is allowed: it says which token to read, not what the caller may do.
GUARDED_ATTRIBUTE = re.compile(r"\[Authorize\(\s*EnsaPermissions\.")


def seeded() -> set[tuple[str, str]]:
    if not os.path.isfile(SEED):
        sys.exit(f"the seed file is missing: {os.path.relpath(SEED, REPOSITORY)}")

    text = io.open(SEED, encoding="utf-8-sig").read()
    return {(m.group("controller"), m.group("action")) for m in ENTRY.finditer(text)}


def endpoints() -> list[tuple[str, str, str]]:
    """Every controller action the API exposes, named as ASP.NET Core dispatches it."""
    found = []

    for path in sorted(glob.glob(
            os.path.join(REPOSITORY, "src", "Ensa.HttpApi*", "**", "*Controller.cs"),
            recursive=True)):
        text = io.open(path, encoding="utf-8-sig").read()

        match = re.search(
            r"public\s+(?:sealed\s+)?(?:abstract\s+)?class\s+(\w+)Controller\b", text)
        if not match or "abstract class" in text[:match.end()]:
            continue

        controller = match.group(1)
        relative = os.path.relpath(path, REPOSITORY).replace(os.sep, "/")

        for action_match in re.finditer(
                r"((?:[ \t]*\[[^\]]*\]\s*)+)public\s+(?:async\s+)?[\w<>,\[\]\?\s]+?\s+(\w+)\s*\(",
                text):
            block, action = action_match.group(1), action_match.group(2)
            if not HTTP_VERB.search(block):
                continue

            # SuppressAsyncSuffixInActionNames is on by default: CreateAsync dispatches as Create.
            if action.endswith("Async") and len(action) > len("Async"):
                action = action[: -len("Async")]

            found.append((controller, action, relative))

    return found


def guarded_attributes() -> list[str]:
    hits = []
    for path in glob.glob(os.path.join(REPOSITORY, "src", "**", "*.cs"), recursive=True):
        text = io.open(path, encoding="utf-8-sig", errors="replace").read()
        # Documentation is allowed to mention the old form when explaining why it is gone.
        code = re.sub(r"^\s*///.*$", "", text, flags=re.M)
        for _ in GUARDED_ATTRIBUTE.finditer(code):
            hits.append(os.path.relpath(path, REPOSITORY).replace(os.sep, "/"))
    return hits


def main() -> int:
    mapped = seeded()
    actions = endpoints()

    missing = [(c, a, f) for c, a, f in actions if (c, a) not in mapped]
    orphaned = sorted(mapped - {(c, a) for c, a, _ in actions})
    attributes = guarded_attributes()

    if not missing and not orphaned and not attributes:
        print(f"all {len(actions)} endpoints are mapped, and no attribute names a permission.")
        return 0

    if missing:
        print()
        print("-- endpoints with no row in PermissionEndpointSeedData.cs --")
        for controller, action, path in missing:
            print(f"   {controller}.{action}   ({path})")
        print()
        print("   These are refused at runtime, the same way the legacy code refused a method")
        print("   with no Yetki_T row. Add a row naming the permission, or null when a valid")
        print("   token is deliberately enough.")

    if orphaned:
        print()
        print("-- rows in the seed file for endpoints that no longer exist --")
        for controller, action in orphaned:
            print(f"   {controller}.{action}")

    if attributes:
        print()
        print("-- [Authorize] carrying a permission identifier --")
        for path in sorted(set(attributes)):
            print(f"   {path}")
        print()
        print("   The controller must not know which permission it needs. Remove the argument")
        print("   and put the mapping in PermissionEndpointSeedData.cs.")

    return 1


if __name__ == "__main__":
    sys.exit(main())
