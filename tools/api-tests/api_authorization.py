# -*- coding: utf-8 -*-
"""
Yetkilendirme dogrulamasi.

api_coverage.py tokensiz istegin 401 dondugunu kanitlar - yani kimlik dogrulamayi. Bu betik bir
adim otesini olcer: kimligi dogrulanmis ama HICBIR yetkisi olmayan bir kullanici korumali uclara
eristiginde 403 almali. Ikisi ayri seydir; yalnizca [Authorize] konulup yetki adi unutulmus bir
controller birinci testi gecer, bu testi gecemez.

Kullanici her kosuda benzersiz adla olusturulur ve sonunda silinir.
"""
import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

import devcert

BASE = "https://localhost:7001"
CTX = devcert.ssl_context()

ADMIN_USER = "admin"
ADMIN_PASSWORD = "Ensa!2026"
SCOPE = "openid profile email roles offline_access ensa"


def call(path, token=None, form=None, body=None, method=None):
    data, headers = None, {}

    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded"

    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"

    request = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    if token:
        request.add_header("Authorization", "Bearer " + token)

    try:
        with urllib.request.urlopen(request, context=CTX, timeout=60) as response:
            return response.status, response.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")
    except Exception as error:                       # noqa: BLE001 - teshis amacli
        return 0, str(error)


def token_for(username, password):
    code, body = call("/connect/token", form={
        "grant_type": "password", "username": username,
        "password": password, "scope": SCOPE})
    if code != 200:
        print("Token alinamadi (%s): HTTP %s %s" % (username, code, body[:300]))
        raise SystemExit(1)
    return json.loads(body)["access_token"]


def error_code(body):
    try:
        return json.loads(body).get("error", {}).get("code") or ""
    except Exception:                                # noqa: BLE001
        return ""


def main():
    admin = token_for(ADMIN_USER, ADMIN_PASSWORD)

    username = "authcheck" + str(int(time.time()))[-6:]
    password = "AuthCheck!2026"

    code, body = call("/api/user", token=admin, body={
        "userName": username, "password": password,
        "name": "Authorization", "lastName": "Probe", "roles": []})
    if code != 200:
        print("Yetkisiz kullanici olusturulamadi: HTTP %s %s" % (code, body[:300]))
        raise SystemExit(1)

    user_id = json.loads(body)["id"]
    print("yetkisiz kullanici olusturuldu: %s (Id=%s)" % (username, user_id))

    try:
        limited = token_for(username, password)

        # Korumali uclar: yetkisi olmayan kullanici 403 gormeli, 200 de 401 de degil.
        forbidden = [
            ("GET", "/api/company", None),
            ("GET", "/api/user", None),
            ("GET", "/api/risk-assessment-report", None),
            ("GET", "/api/medical-examination-form", None),
            ("POST", "/api/company", {"companyName": "Probe", "hazardClass": 1,
                                      "workplaceType": 1, "cityId": 34, "districtId": 1}),
        ]

        # Kendi kimligi: yalnizca oturum ister, yetki istemez.
        allowed = [("GET", "/connect/userinfo", None)]

        failures = 0
        print("\n=== yetki gerektiren uclar (beklenen 403) ===")
        for method, path, body_payload in forbidden:
            code, response = call(path, token=limited, body=body_payload,
                                  method=method if body_payload is None else None)
            ok = code == 403
            failures += 0 if ok else 1
            print("  %-5s %-36s HTTP %-3s %s %s"
                  % (method, path, code, "GECTI" if ok else "KALDI", error_code(response)))

        print("\n=== yalnizca oturum isteyen uclar (beklenen 200) ===")
        for method, path, body_payload in allowed:
            code, _ = call(path, token=limited, body=body_payload)
            ok = code == 200
            failures += 0 if ok else 1
            print("  %-5s %-36s HTTP %-3s %s" % (method, path, code, "GECTI" if ok else "KALDI"))

        total = len(forbidden) + len(allowed)
        print("\n%d kontrolden %d tanesi gecti" % (total, total - failures))
        return 1 if failures else 0
    finally:
        code, _ = call("/api/user/%s" % user_id, token=admin, method="DELETE")
        print("yetkisiz kullanici silindi: HTTP %s" % code)


if __name__ == "__main__":
    sys.exit(main())
