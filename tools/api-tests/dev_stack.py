# -*- coding: utf-8 -*-
"""
Gelistirme yiginini butun olarak dogrular: SPA -> Vite proxy -> API.

Diger betikler API'yi dogrudan https://localhost:7001 uzerinden konusur. Bu betik ayni islemleri
tarayicinin gectigi yoldan, yani http://localhost:5173 uzerinden yapar. Aradaki fark onemsiz
degil: proxy yanlis yapilandirilmissa API kusursuz calisiyor olsa bile uygulama acilmaz, ve bu
yalnizca burada gorulur.

Onkosul: `npm run dev --prefix react/ensa-web` ile SPA, `dotnet run --project src/Ensa.HttpApi.Host`
ile API ayakta olmali.
"""
import json
import sys
import urllib.error
import urllib.parse
import urllib.request

SPA = "http://localhost:5173"
ADMIN_USER = "admin"
ADMIN_PASSWORD = "Ensa!2026"
SCOPE = "openid profile email roles offline_access ensa"

# Panonun ve ana ekranlarin dayandigi uclar.
PROBES = [
    "/api/company?maxResultCount=1",
    "/api/company-employee?maxResultCount=1",
    "/api/equipment/overdue-inspections",
    "/api/corrective-action/overdue",
    "/api/risk-assessment-report/expiring",
    "/api/visit?maxResultCount=1",
    "/api/support-ticket?maxResultCount=1",
]


def call(path, token=None, form=None):
    data, headers = None, {}
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded"

    request = urllib.request.Request(SPA + path, data=data, headers=headers)
    if token:
        request.add_header("Authorization", "Bearer " + token)

    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return response.status, response.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")
    except Exception as error:                       # noqa: BLE001 - teshis amacli
        return 0, str(error)


def main():
    failures = 0

    print("=== GELISTIRME YIGINI (SPA -> proxy -> API) ===")

    code, body = call("/")
    ok = code == 200 and 'id="root"' in body
    failures += 0 if ok else 1
    print("  %-44s HTTP %-3s %s" % ("SPA kabugu", code, "GECTI" if ok else "KALDI"))
    if code == 0:
        print("\n  SPA ayakta degil. Once: npm run dev --prefix react/ensa-web")
        return 1

    code, body = call("/connect/token", form={
        "grant_type": "password", "username": ADMIN_USER,
        "password": ADMIN_PASSWORD, "scope": SCOPE})
    ok = code == 200
    failures += 0 if ok else 1
    print("  %-44s HTTP %-3s %s" % ("proxy -> /connect/token", code, "GECTI" if ok else "KALDI"))
    if not ok:
        print("     %s" % body[:200])
        return 1

    token = json.loads(body)["access_token"]

    for path in PROBES:
        code, _ = call(path, token=token)
        ok = code == 200
        failures += 0 if ok else 1
        print("  %-44s HTTP %-3s %s" % ("proxy -> " + path.split("?")[0], code,
                                        "GECTI" if ok else "KALDI"))

    # Proxy, yetkilendirmeyi kendi basina gecersiz kilmamali.
    code, _ = call("/api/company")
    ok = code == 401
    failures += 0 if ok else 1
    print("  %-44s HTTP %-3s %s" % ("tokensiz istek 401 dondurur", code,
                                    "GECTI" if ok else "KALDI"))

    total = 2 + len(PROBES) + 1
    print("\n%d kontrolden %d tanesi gecti" % (total, total - failures))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
