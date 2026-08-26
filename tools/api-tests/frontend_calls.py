# -*- coding: utf-8 -*-
"""
SPA'nin cagirdigi HER API yolunu canli Swagger dokumanina karsi dogrular.

frontend_routes.py yalnizca ENDPOINTS sabitindeki kaynak adlarina bakar. Modul basina ekran
yazildikca cagrilarin cogu artik modul klasorlerindeki api.ts dosyalarinda template literal
olarak duruyor:

    http.get(`/${DOCUMENT}/${id}/detail`)

Bu betik kaynagi tarar, ayni dosyada tanimli sabitleri yerine koyar, parametreleri {} ile
normalize eder ve olusan yolu Swagger'daki gercek yollarla karsilastirir. Var olmayan bir uca
bakan ekran, kullanicinin gordugu ilk anda 404 olur; burada derleme zamaninda yakalanir.
"""
import io
import json
import os
import re
import sys
import urllib.request

import devcert

BASE = "https://localhost:7001"
SOURCE_ROOT = "react/ensa-web/src"

# Tip argumani ic ice generic olabilir: http.get<ListResult<InvoiceLineDto>>(...).
# Tek seviyeli bir <[^>]*> deseni bu cagrilari sessizce atlar - yani tarayici tam da
# yakalamasi gereken yerleri gormez. Bir seviye ic ice destekleniyor.
# http.get/post/put/delete/patch(`...`) veya ('...') icindeki ilk argüman.
CALL = re.compile(
    r"http\.(get|post|put|delete|patch)\s*(?:<(?:[^<>]|<[^<>]*>)*>)?\s*\(\s*([`'\"])(.*?)\2",
    re.S)

# const NAME = 'value'  |  NAME: 'value'
CONST = re.compile(r"(?:const\s+(\w+)\s*=\s*|(\w+)\s*:\s*)['\"]([^'\"]+)['\"]")

PLACEHOLDER = re.compile(r"\$\{([^}]*)\}")


def swagger_paths():
    url = BASE + "/swagger/v1/swagger.json"
    with urllib.request.urlopen(url, context=devcert.ssl_context(), timeout=60) as response:
        document = json.load(response)

    paths = {}
    for path, operations in document["paths"].items():
        normalized = re.sub(r"\{[^}]+\}", "{}", path).rstrip("/")
        paths.setdefault(normalized, set()).update(m.lower() for m in operations)
    return paths


def source_files():
    for directory, subdirectories, files in os.walk(SOURCE_ROOT):
        subdirectories[:] = [d for d in subdirectories if d != "node_modules"]
        for name in files:
            if name.endswith((".ts", ".tsx")):
                yield os.path.join(directory, name)


def normalize(raw, constants):
    """Sabitleri yerine koyar, kalan her ifadeyi {} yapar."""
    def replace(match):
        expression = match.group(1).strip()
        # `${RESOURCES.equipment}` gibi nitelikli erisimlerde son parcayi dene.
        key = expression.split(".")[-1]
        if expression in constants:
            return constants[expression]
        if key in constants:
            return constants[key]
        return "{}"

    path = PLACEHOLDER.sub(replace, raw)
    path = path.split("?")[0].rstrip("/")
    return path


def match_methods(candidate, paths):
    """
    Aday yola uyan Swagger yollarinin destekledigi metotlarin birlesimini dondurur; hicbiri
    uymuyorsa None.

    SPA tarafindaki `{}` bir route parametresi olabilecegi gibi calisma zamaninda sabit bir
    segment de olabilir (or. `${set}` -> "exposed-groups"), bu yuzden joker sayilir. Joker birden
    fazla yola uyabildigi icin tek bir eslesmede durmak yaniltir: `/{id}/{set}` hem GET-only
    `/active/{companyId}` yoluna hem de PUT olan `/{id}/exposed-groups` yoluna uyar.
    """
    if candidate in paths:
        return set(paths[candidate])

    wanted = candidate.split("/")
    methods = set()
    for path, verbs in paths.items():
        actual = path.split("/")
        if len(actual) != len(wanted):
            continue
        if all(w == "{}" or a == "{}" or a == w for a, w in zip(actual, wanted)):
            methods |= verbs

    return methods or None


def main():
    paths = swagger_paths()

    checked = 0
    unknown = []
    wrong_method = []

    for file_path in source_files():
        source = io.open(file_path, encoding="utf-8").read()
        constants = {}
        for match in CONST.finditer(source):
            name = match.group(1) or match.group(2)
            constants[name] = match.group(3)

        for match in CALL.finditer(source):
            method, _, raw = match.groups()
            if "${" not in raw and not raw.startswith("/"):
                continue

            path = normalize(raw, constants)
            if not path.startswith("/"):
                continue

            # http.ts baseURL'i '/api' — kaynak icindeki yollar onun uzerine biner.
            full = "/api" + path if not path.startswith("/api") else path
            checked += 1

            # Kaynak adinin kendisi degiskense (ör. `/${resource}/${id}`) bu genel bir
            # yardimcidir; hangi controller'a gidecegi calisma zamaninda belli olur.
            segments = full.split("/")
            if len(segments) > 2 and segments[2] == "{}":
                checked -= 1
                continue

            allowed = match_methods(full, paths)
            if allowed is None:
                unknown.append((file_path.replace("\\", "/"), method.upper(), full, raw))
            elif method not in allowed:
                wrong_method.append(
                    (file_path.replace("\\", "/"), method.upper(), full, sorted(allowed)))

    print("=== SPA -> API CAGRI DOGRULAMASI ===")
    print("  dogrulanan cagri        : %d" % checked)
    print("  API'de bulunamayan yol  : %d" % len(unknown))
    print("  yanlis HTTP metodu      : %d" % len(wrong_method))

    if unknown:
        print("\n-- BULUNAMAYAN --")
        for file_path, method, full, raw in unknown:
            print("  %-6s %-52s %s" % (method, full, file_path))
            print("         kaynak: %s" % raw)

    if wrong_method:
        print("\n-- YANLIS METOT --")
        for file_path, method, full, allowed in wrong_method:
            print("  %-6s %-52s izinli: %s  (%s)" % (method, full, ", ".join(allowed), file_path))

    return 1 if (unknown or wrong_method) else 0


if __name__ == "__main__":
    sys.exit(main())
