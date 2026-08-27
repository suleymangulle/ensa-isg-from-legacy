# -*- coding: utf-8 -*-
"""
SPA'nin kullandigi yetki adlari gercekten var mi?

Kenar cubugu, kullanicinin sahip olmadigi girdileri gizler. Bu yuzden yanlis yazilmis TEK bir
yetki sabiti bir ekrani herkesten kalici olarak gizler - ve hicbir hata vermez, hicbir test
kirilmaz. Sessizce kaybolan bir menu, gorunur bir hatadan cok daha zor fark edilir.

Bu betik:
  1. `PERMISSIONS` sabitlerinin hepsini API'nin yetki katalogunda arar,
  2. modullerin menu girdilerinde kullandigi yetkilerin gercek oldugunu dogrular,
  3. katalogda olup SPA'nin hic tanimadigi yetkileri bilgi olarak listeler.
"""
import io
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

import devcert

BASE = "https://localhost:7001"
CTX = devcert.ssl_context()
PERMISSIONS_FILE = "react/ensa-web/src/api/permissions.ts"
MODULES = "react/ensa-web/src/pages"

ADMIN_USER = "admin"
ADMIN_PASSWORD = "Ensa!2026"
SCOPE = "openid profile email roles offline_access ensa"

CONSTANT = re.compile(r"^    \w+: '([^']+)',$", re.M)
NAV_USE = re.compile(r"permission: PERMISSIONS\.(\w+)\.(\w+)")


def call(path, token=None, form=None):
    data, headers = None, {}
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded"

    request = urllib.request.Request(BASE + path, data=data, headers=headers)
    if token:
        request.add_header("Authorization", "Bearer " + token)

    try:
        with urllib.request.urlopen(request, context=CTX, timeout=60) as response:
            return response.status, response.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")


def main():
    code, body = call("/connect/token", form={
        "grant_type": "password", "client_id": "ensa-spa", "username": ADMIN_USER,
        "password": ADMIN_PASSWORD, "scope": SCOPE})
    if code != 200:
        print("Token alinamadi: HTTP %s" % code)
        return 1
    token = json.loads(body)["access_token"]

    # The catalogue the server seeded, which is what the policies are built from.
    code, body = call("/api/permission?maxResultCount=1000", token=token)
    if code != 200:
        print("Yetki katalogu okunamadi: HTTP %s" % code)
        return 1

    catalogue = {item["permissionTarget"] for item in json.loads(body)["items"]}

    source = io.open(PERMISSIONS_FILE, encoding="utf-8").read()
    declared = set(CONSTANT.findall(source))
    # GroupName is the shared prefix, not a permission.
    declared.discard("Ensa")

    # Which constants the sidebar actually relies on.
    used = set()
    for module in sorted(os.listdir(MODULES)):
        path = os.path.join(MODULES, module, "module.tsx")
        if not os.path.exists(path):
            continue
        module_source = io.open(path, encoding="utf-8").read()
        for group, member in NAV_USE.findall(module_source):
            match = re.search(
                r"^  %s: \{(?:[^}]*?)\n    %s: '([^']+)'," % (re.escape(group), re.escape(member)),
                source, re.M | re.S)
            if match:
                used.add(match.group(1))
            else:
                used.add("%s.%s (COZULEMEDI)" % (group, member))

    unknown_declared = sorted(declared - catalogue)
    unknown_used = sorted(u for u in used if u not in catalogue)
    unused = sorted(catalogue - declared)

    print("=== SPA YETKI SABITLERI ===")
    print("  API katalogu           : %d" % len(catalogue))
    print("  SPA'da tanimli sabit   : %d" % len(declared))
    print("  menude kullanilan      : %d" % len(used))
    print("  KATALOGDA OLMAYAN sabit: %d" % len(unknown_declared))
    print("  KATALOGDA OLMAYAN menu : %d" % len(unknown_used))
    print("  SPA'nin tanimadigi     : %d (bilgi)" % len(unused))

    if unknown_declared:
        print("\n-- SPA'da var, API'de yok --")
        for name in unknown_declared[:20]:
            print("   %s" % name)

    if unknown_used:
        print("\n-- MENUDE KULLANILIYOR ama API'de yok --")
        for name in unknown_used:
            print("   %s" % name)

    if unused:
        print("\n-- API'de var, SPA tanimiyor (bilgi) --")
        for name in unused[:10]:
            print("   %s" % name)

    return 1 if (unknown_declared or unknown_used) else 0


if __name__ == "__main__":
    sys.exit(main())
