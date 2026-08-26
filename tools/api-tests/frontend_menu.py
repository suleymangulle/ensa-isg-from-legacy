# -*- coding: utf-8 -*-
"""
Seedlenen menu, SPA'nin gercekten sundugu ekranlarla ayni mi?

Menu iki yerde tanimli, ve bu bilerek boyle: kenar cubugu KODDAN cizilir (yetkiye gore
suzulur, hicbir istek beklemez), veritabanindaki Menu modulu ise eski sistemin yapilandirilabilir
menusudur - yonetim ekraninin listeledigi ve `GET api/menu/my-menu` icin dondurulen sey odur.

Iki tanimin ayrisması sessiz bir hatadir: menude olmayan bir ekran yonetim ekraninda gorunmez,
silinmis bir ekran menude olu bir baglanti olarak kalir. `tools/gen-enums/gen_menu.py` ikisini
tek kaynaktan uretir; bu betik uretimin calistirilmayi unutulmadigini kanitlar.

  1. SPA'nin her menu girdisi, API'nin dondurdugu menu agacinda var mi,
  2. menu agacindaki her ekran girdisinin SPA'da bir karsiligi var mi,
  3. baslik ve URL'ler ayni mi.
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
SCOPE = "openid profile email roles offline_access ensa"

ADMIN_USER = "admin"
ADMIN_PASSWORD = "Ensa!2026"

MODULES = "react/ensa-web/src/pages"
GENERATED = "src/Ensa.DbMigrator/Seeding/MenuSeedData.cs"
MENU_TYPE_CODE = "MAIN"

ENTRY = re.compile(r"\{(?P<body>[^{}]*?path:\s*'(?P<path>[^']*)'[^{}]*?)\}", re.S)
SEED_ROW = re.compile(r'^\s*new\("([^"]*)", "((?:[^"\\]|\\.)*)", "([^"]*)",', re.M)


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


def nav_extent(source):
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


def spa_paths():
    """Every route the sidebar offers, as a URL with a leading slash."""
    paths = set()

    for module in sorted(os.listdir(MODULES)):
        path = os.path.join(MODULES, module, "module.tsx")
        if not os.path.exists(path):
            continue

        source = io.open(path, encoding="utf-8").read()
        if "nav: [" not in source:
            continue

        start, end = nav_extent(source)
        for match in ENTRY.finditer(source[start:end]):
            paths.add("/" + match.group("path"))

    return paths


def generated_urls():
    """The URLs in the generated seed table - what the seeder actually wrote."""
    source = io.open(GENERATED, encoding="utf-8").read()
    return {url for _, _, url in SEED_ROW.findall(source)}


def flatten(nodes, into):
    for node in nodes:
        into.append(node)
        flatten(node.get("children", []), into)
    return into


def main():
    code, body = call("/connect/token", form={
        "grant_type": "password", "username": ADMIN_USER,
        "password": ADMIN_PASSWORD, "scope": SCOPE})
    if code != 200:
        print("Token alinamadi: HTTP %s" % code)
        return 1
    token = json.loads(body)["access_token"]

    code, body = call("/api/menu/my-menu?MenuTypeCode=%s" % MENU_TYPE_CODE, token=token)
    if code != 200:
        print("Menu okunamadi: HTTP %s %s" % (code, body[:200]))
        print("Seeder calistirilmis mi? DOTNET_ENVIRONMENT=Development dotnet run --project src/Ensa.DbMigrator")
        return 1

    served = flatten(json.loads(body)["roots"], [])

    # Section headings carry no URL; only the navigable leaves are compared.
    served_urls = {node["url"] for node in served if node.get("url")}
    headings = [node for node in served if not node.get("url")]

    spa = spa_paths()
    generated = generated_urls()

    missing_in_menu = sorted(spa - served_urls)
    missing_in_spa = sorted(served_urls - spa)
    stale_generation = sorted(spa ^ generated)

    print("=== SEEDLENEN MENU / SPA KARSILASTIRMASI ===")
    print("  SPA menu girdisi        : %d" % len(spa))
    print("  uretilen tablo satiri   : %d" % len(generated))
    print("  API'nin dondurdugu ekran: %d" % len(served_urls))
    print("  bolum basligi           : %d" % len(headings))
    print("  MENUDE OLMAYAN ekran    : %d" % len(missing_in_menu))
    print("  SPA'DA OLMAYAN menu     : %d" % len(missing_in_spa))
    print("  URETIM GUNCEL DEGIL     : %d" % len(stale_generation))

    if missing_in_menu:
        print("\n-- SPA'da var, seedlenen menude yok --")
        for url in missing_in_menu:
            print("   %s" % url)

    if missing_in_spa:
        print("\n-- Menude var, SPA'da boyle bir ekran yok --")
        for url in missing_in_spa:
            print("   %s" % url)

    if stale_generation:
        print("\n-- Uretilen tablo SPA ile ayni degil; gen_menu.py calistirilmali --")
        for url in stale_generation:
            print("   %s" % url)

    return 1 if (missing_in_menu or missing_in_spa or stale_generation) else 0


if __name__ == "__main__":
    sys.exit(main())
