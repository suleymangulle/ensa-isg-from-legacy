#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Bir kullanici kendi yetkisini yukseltebiliyor mu?

Controller'lar artik hangi izne ihtiyac duyduklarini bilmiyor: `[Authorize]` parametresiz ve
karar, istek aninda `PermissionEndpoint` haritasindan cozuluyor. Bu duzenlemenin tek riski,
kararin gercekten calismamasidir -- calismazsa attribute'u kaldirilmis her endpoint, kimligi
dogrulanmis herkese acilir.

En kotu ihtimal `PUT /api/permission/user/{userId}`: izin dagitan endpoint. Orada bir bosluk,
kullanicinin kendine istedigi yetkiyi vermesi demektir. Bu betik tam olarak onu deniyor.

    python tools/api-tests/api_privilege_escalation.py
"""

import json
import os
import time
import sys
import urllib.error
import urllib.parse
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import devcert                                                    # noqa: E402

BASE = "https://localhost:7001"
SCOPE = "openid profile email roles offline_access ensa"
CLIENT = "ensa-spa"

# The development certificate, trusted properly rather than by switching verification off --
# see tools/api-tests/README.md for the one-time export.
ssl_context = devcert.ssl_context()


def call(path, token=None, body=None, method=None, form=None):
    url = BASE + path
    data = None
    headers = {}

    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded"
    elif body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"

    if token:
        headers["Authorization"] = "Bearer " + token

    request = urllib.request.Request(url, data=data, headers=headers,
                                     method=method or ("POST" if data else "GET"))
    try:
        with urllib.request.urlopen(request, context=ssl_context, timeout=60) as response:
            raw = response.read().decode("utf-8", "replace")
            return response.status, (json.loads(raw) if raw else {})
    except urllib.error.HTTPError as error:
        raw = error.read().decode("utf-8", "replace")
        try:
            return error.code, json.loads(raw)
        except json.JSONDecodeError:
            return error.code, {}
    except Exception as error:                                   # noqa: BLE001
        print("  API'ye ulasilamadi: %s" % error)
        sys.exit(2)


def token_for(username, password):
    code, body = call("/connect/token", form={
        "grant_type": "password", "client_id": CLIENT,
        "username": username, "password": password, "scope": SCOPE})
    if code != 200:
        print("  giris basarisiz (%s): HTTP %s" % (username, code))
        sys.exit(2)
    return body["access_token"]


results = []


def check(label, passed, detail=""):
    print("  [%s] %-52s %s" % ("GECTI" if passed else "KALDI", label, detail))
    results.append(passed)
    return passed


print("=== YETKI YUKSELTME DENEMESI ===")

admin = token_for("admin", "Ensa!2026")

# A fresh name each run: deleting a user marks the profile, and the account row -- with
# its user name -- stays for the audit trail, exactly as it should.
stamp = str(int(time.time()))
username = "escalation.probe." + stamp
password = "Escalate!2026"

code, created = call("/api/user", token=admin, body={
    "userName": username, "password": password,
    "name": "Escalation", "lastName": "Probe",
    "email": username + "@example.invalid", "isActive": True,
})

if code not in (200, 201):
    print("  izinsiz kullanici olusturulamadi: HTTP %s" % code)
    sys.exit(2)

user_id = created.get("id") or created.get("Id")
print("  izinsiz kullanici olusturuldu: %s (Id=%s)" % (username, user_id))

try:
    limited = token_for(username, password)

    # Once gercekten izinsiz oldugunu dogrula; aksi halde asagidaki 403'ler bir sey kanitlamaz.
    code, permissions = call("/api/account/permissions", token=limited)
    held = permissions.get("items", [])
    check("kullanicinin hic izni yok", code == 200 and not held,
          "HTTP %s, %d izin" % (code, len(held)))

    # 1) Kendine izin vermeye calis. Bu endpoint acik olsaydi her sey biterdi.
    code, _ = call("/api/permission/user/%s" % user_id, token=limited, method="PUT", body={
        "userId": user_id,
        "permissions": [{"permissionId": 1, "authorized": True}],
    })
    check("kendine izin veremiyor (PUT /api/permission/user)", code == 403,
          "HTTP %s" % code)

    # 2) Kullanici tipine izin vermeye calis -- ayni tabloya giden diger kapi.
    code, _ = call("/api/permission/usertype/1", token=limited, method="PUT", body={
        "userTypeId": 1, "permissions": [{"permissionId": 1, "authorized": True}],
    })
    check("kullanici tipine izin veremiyor", code == 403, "HTTP %s" % code)

    # 3) Izin katalogunu okuyamaz (Ensa.Permission gerekir).
    code, _ = call("/api/permission", token=limited)
    check("izin katalogunu okuyamiyor", code == 403, "HTTP %s" % code)

    # 4) Baska bir kullanici olusturamaz.
    code, _ = call("/api/user", token=limited, body={
        "userName": "escalation.second." + stamp, "password": password,
        "name": "Second", "lastName": "Probe", "isActive": True})
    check("baska kullanici olusturamiyor", code == 403, "HTTP %s" % code)

    # 5) Rol atayamaz.
    code, _ = call("/api/user/%s/roles" % user_id, token=limited, method="PUT",
                   body={"userId": user_id, "roleIds": [1]})
    check("kendine rol atayamiyor", code in (403, 404), "HTTP %s" % code)

    # 6) Kendi profilini okuyabilmeli -- izinsiz de olsa. Aksi halde reddetme
    #    politikasi dogru degil, sadece her seyi kapatmis olurduk.
    code, _ = call("/api/account/profile", token=limited)
    check("kendi profilini okuyabiliyor", code == 200, "HTTP %s" % code)

finally:
    code, _ = call("/api/user/%s" % user_id, token=admin, method="DELETE")
    print("  izinsiz kullanici silindi: HTTP %s" % code)

print()
print("%d kontrolden %d tanesi gecti" % (len(results), sum(results)))
sys.exit(0 if all(results) else 1)
