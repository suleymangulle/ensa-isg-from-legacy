"""Ensa API uçtan uca doğrulama betiği."""
import json
import ssl
import urllib.error
import urllib.parse
import urllib.request

BASE = "https://localhost:7001"
# TLS doğrulaması AÇIK. ASP.NET Core geliştirme sertifikası kendinden imzalı
# olduğu için, onu CA olarak açıkça yüklüyoruz (doğrulamayı kapatmak yerine).
import os

import devcert


CTX = devcert.ssl_context()
CTX.check_hostname = True


def istek(yol, yontem="GET", token=None, govde=None, form=None):
    url = BASE + yol
    data, headers = None, {}
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded"
    elif govde is not None:
        data = json.dumps(govde).encode()
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token

    req = urllib.request.Request(url, data=data, headers=headers, method=yontem)
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            raw = r.read().decode("utf-8")
            return r.status, (json.loads(raw) if raw.strip() else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8")
        try:
            return e.code, json.loads(raw)
        except json.JSONDecodeError:
            return e.code, raw


def kontrol(ad, kosul, ayrinti=""):
    print(("  [GECTI] " if kosul else "  [KALDI] ") + ad + (" — " + ayrinti if ayrinti else ""))
    return kosul


import time

DAMGA = str(int(time.time()))[-9:]          # her calistirmada benzersiz veri
sonuclar = []

print("=== KIMLIK ===")
kod, tok = istek("/connect/token", "POST", form={
    "grant_type": "password", "client_id": "ensa-spa", "username": "admin",
    "password": "Ensa!2026", "scope": "openid profile email roles offline_access ensa",
})
sonuclar.append(kontrol("password grant", kod == 200 and "access_token" in tok, f"HTTP {kod}"))
token = tok["access_token"]
refresh = tok.get("refresh_token")

kod, _ = istek("/api/company")
sonuclar.append(kontrol("tokensiz istek reddedilir", kod == 401, f"HTTP {kod}"))

kod, ui = istek("/connect/userinfo", token=token)
sonuclar.append(kontrol("userinfo", kod == 200 and ui.get("sub") == "1", f"HTTP {kod}"))

kod, yeni = istek("/connect/token", "POST", form={
    "grant_type": "refresh_token", "client_id": "ensa-spa", "refresh_token": refresh,
})
sonuclar.append(kontrol("refresh_token grant", kod == 200 and "access_token" in yeni, f"HTTP {kod}"))

print("\n=== FIRMA CRUD ===")
sgk = "777666555" + DAMGA + "0000"
kod, olusan = istek("/api/company", "POST", token, {
    "companyName": f"CRUD Test Tekstil {DAMGA}", "ssiNumber": sgk, "hazardClass": 2,
    "workplaceType": 1, "cityId": 16, "districtId": 110, "contactPerson": "Zeynep Ak",
})
olustu = kod == 200 and olusan.get("id")
sonuclar.append(kontrol("olustur", bool(olustu), f"HTTP {kod} Id={olusan.get('id') if olustu else olusan}"))
fid = olusan["id"] if olustu else None

if fid:
    kod, tek = istek(f"/api/company/{fid}", token=token)
    sonuclar.append(kontrol("tekil oku", kod == 200 and tek["id"] == fid, f"HTTP {kod}"))

    kod, nav = istek(f"/api/company/{fid}/detail", token=token)
    sonuclar.append(kontrol(
        "navigation DTO", kod == 200 and nav.get("company", {}).get("id") == fid,
        f"HTTP {kod} sehir={(nav.get('city') or {}).get('displayName') if kod == 200 else '-'}"))

    kod, guncel = istek(f"/api/company/{fid}", "PUT", token, {
        "companyName": f"CRUD Test Tekstil GUNCEL {DAMGA}", "ssiNumber": sgk, "hazardClass": 3,
        "workplaceType": 1, "cityId": 16, "districtId": 110, "isActive": True,
    })
    sonuclar.append(kontrol(
        "guncelle", kod == 200 and guncel.get("hazardClass") == 3,
        f"HTTP {kod} ad={guncel.get('companyName') if kod == 200 else guncel}"))

    kod, _ = istek(f"/api/company/{fid}", "DELETE", token=token)
    sonuclar.append(kontrol("sil (soft delete)", kod in (200, 204), f"HTTP {kod}"))

    kod, _ = istek(f"/api/company/{fid}", token=token)
    sonuclar.append(kontrol("silinen kayit gorunmez", kod == 404, f"HTTP {kod}"))

print("\n=== IS KURALLARI ===")
# Silinmeyen kalici kayit: hem mukerrer SGK hem de arama filtresi testinin dayanagi.
# Onceki surumde bu testler bir onceki kosunun artiklarina guveniyordu, bu yuzden
# taze bir veritabaninda yanlis basarisizlik veriyorlardi.
sgk_kalici = "777666555" + DAMGA + "0001"
kod, kalici = istek("/api/company", "POST", token, {
    "companyName": f"Kalici Test Firma {DAMGA}", "ssiNumber": sgk_kalici,
    "hazardClass": 1, "workplaceType": 1, "cityId": 34, "districtId": 1,
})
sonuclar.append(kontrol("kalici kayit olustur", kod == 200 and bool(kalici.get("id")), f"HTTP {kod}"))

kod, h = istek("/api/company", "POST", token, {
    "companyName": "Mukerrer SGK", "ssiNumber": sgk_kalici,
    "hazardClass": 1, "workplaceType": 1, "cityId": 34, "districtId": 1,
})
sonuclar.append(kontrol(
    "mukerrer SGK reddedilir", kod == 400 and "SsiNumberAlreadyRegistered" in (h.get("error", {}).get("code") or ""),
    f"HTTP {kod} kod={h.get('error', {}).get('code')}"))

kod, h = istek("/api/company", "POST", token, {
    "companyName": "Merkezsiz Sube", "hazardClass": 1,
    "workplaceType": 2, "cityId": 34, "districtId": 1,
})
sonuclar.append(kontrol(
    "merkezsiz sube reddedilir", kod == 400 and "HeadquarterRequiredForBranch" in (h.get("error", {}).get("code") or ""),
    f"HTTP {kod} kod={h.get('error', {}).get('code')}"))

kod, h = istek("/api/company", "POST", token, {"companyName": "", "cityId": 0})
hatalar = (h.get("error", {}).get("validationErrors") or []) if kod == 400 else []
sonuclar.append(kontrol(
    "zorunlu alan dogrulamasi", kod == 400 and len(hatalar) >= 2,
    f"HTTP {kod} {len(hatalar)} hata"))

print("\n=== LISTELEME ===")
kod, liste = istek("/api/company?MaxResultCount=5&Sorting=CompanyName%20ASC", token=token)
sonuclar.append(kontrol("sayfali liste", kod == 200 and "totalCount" in liste,
                        f"HTTP {kod} toplam={liste.get('totalCount')}"))

kod, ara = istek(f"/api/company?Filter={DAMGA}", token=token)
sonuclar.append(kontrol("arama filtresi", kod == 200 and ara["totalCount"] >= 1,
                        f"HTTP {kod} bulunan={ara.get('totalCount')}"))

kod, lk = istek("/api/company/lookup?filter=Test", token=token)
sonuclar.append(kontrol("lookup", kod == 200 and "items" in lk,
                        f"HTTP {kod} adet={len(lk.get('items', []))}"))

print("\n" + "=" * 46)
gecen, toplam = sum(sonuclar), len(sonuclar)
print(f"  SONUC: {gecen}/{toplam} test gecti")
print("=" * 46)
raise SystemExit(0 if gecen == toplam else 1)
