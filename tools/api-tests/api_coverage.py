# -*- coding: utf-8 -*-
"""
Uctan uca kapsam taramasi.

api_test.py tek bir modulu (Firma) derinlemesine dogrular. Bu betik ise yeni inen
tum modullerin gercekten ayakta oldugunu kanitlar: her parametresiz GET ucu icin
  1) tokensiz istek 401 dondurmeli   (yetkilendirme bosluklari)
  2) tokenli istek 5xx dondurmemeli  (DI, mapping ve sorgu hatalari)
"""
import json
import os
import ssl
import sys
import urllib.error
import urllib.parse
import urllib.request

import devcert


BASE = "https://localhost:7001"

CTX = devcert.ssl_context()


def istek(yol, token=None, form=None):
    url = BASE + yol
    data = headers = None
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers = {"Content-Type": "application/x-www-form-urlencoded"}
    request = urllib.request.Request(url, data=data, headers=headers or {})
    if token:
        request.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(request, context=CTX, timeout=60) as response:
            body = response.read().decode("utf-8", "replace")
            return response.status, body
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")
    except Exception as error:                      # noqa: BLE001 - teshis amacli
        return 0, str(error)


kod, govde = istek("/connect/token", form={
    "grant_type": "password", "username": "admin",
    "password": "Ensa!2026",
    "scope": "openid profile email roles offline_access ensa",
})
if kod != 200:
    print("Token alinamadi: HTTP %s %s" % (kod, govde[:300]))
    sys.exit(1)
token = json.loads(govde)["access_token"]

# Rotalar Swagger'dan canli olarak okunur; yan dosyaya bagimlilik yok, boylece yeni inen
# bir uc otomatik olarak taramaya dahil olur.
with urllib.request.urlopen(BASE + "/swagger/v1/swagger.json", context=CTX, timeout=60) as _swagger:
    _doc = json.load(_swagger)

rotalar = sorted(p for p, ops in _doc["paths"].items() if "get" in ops and "{" not in p)

# Bilerek herkese acik uclar.
#
# Liste kisa ve gerekcelidir: buraya bir yol eklemek, "kimlik dogrulamasi olmadan erisilebilir"
# demektir ve bilincli bir karardir. Listede olmayan her acik uc hata sayilir.
PUBLIC_PATHS = {
    "/health": "Yuk dengeleyici ve konteyner saglik yoklamasi; kimlik dogrulayamaz.",
}

anonim_sizinti, sunucu_hatasi, basarili, diger = [], [], [], []
for yol in rotalar:
    kod, _ = istek(yol)
    if kod != 401 and yol not in PUBLIC_PATHS:
        anonim_sizinti.append((yol, kod))

    kod, govde = istek(yol, token=token)
    if kod >= 500 or kod == 0:
        sunucu_hatasi.append((yol, kod, govde[:200]))
    elif kod == 200:
        basarili.append(yol)
    else:
        diger.append((yol, kod))

print("=== KAPSAM TARAMASI (%d parametresiz GET) ===" % len(rotalar))
print("  200 donen                : %d" % len(basarili))
print("  200 disi (4xx)           : %d" % len(diger))
print("  5xx / baglanti hatasi    : %d" % len(sunucu_hatasi))
print("  tokensiz 401 vermeyen    : %d" % len(anonim_sizinti))
print("  bilerek acik             : %d" % len(PUBLIC_PATHS))

if anonim_sizinti:
    print("\n-- YETKILENDIRME BOSLUGU --")
    for yol, kod in anonim_sizinti:
        print("  %-55s HTTP %s" % (yol, kod))

if sunucu_hatasi:
    print("\n-- SUNUCU HATASI --")
    for yol, kod, govde in sunucu_hatasi:
        print("  %-55s HTTP %s  %s" % (yol, kod, govde.replace("\n", " ")[:160]))

if diger:
    print("\n-- 200 DISI --")
    for yol, kod in diger:
        print("  %-55s HTTP %s" % (yol, kod))

sys.exit(1 if (anonim_sizinti or sunucu_hatasi) else 0)
