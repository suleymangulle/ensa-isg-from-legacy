# -*- coding: utf-8 -*-
"""
Sube (office) baglami dogrulamasi.

Iki soruyu ayri ayri olcer.

BIRINCISI: `GET /api/account/offices` her oturumlu kullaniciya acik mi? Sube secici tam da ofis
yonetimi yetkisi OLMAYAN personel icin var - iki subede calisan bir uzman, `Ensa.Office` yetkisi
olmadan da subeler arasi gecebilmeli. api_authorization.py yetkili uclarin 403 dondugunu kanitlar;
burada kasten yetkisiz birakilmis bir ucun 200 DONDUGU kanitlanir. Ayni kullanici `api/office`
uclarinda 403 almaya devam etmeli: bu uc, ofis rehberi degil.

IKINCISI: `X-Ensa-OfficeId` basligi sunucuda gercekten dogrulaniyor mu? Bozuk deger 400, izinsiz
ya da var olmayan sube 403 dondurmeli - ve ucu de ayni koda dusmeli, cunku farkli cevaplar baska
kurumlarin sube kimliklerini haritalamaya yarar.

Kosmadan once API'nin ayakta olmasi gerekir; bkz. tools/api-tests/README.md.
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

OFFICE_HEADER = "X-Ensa-OfficeId"


def call(path, token=None, form=None, body=None, method=None, office=None):
    data, headers = None, {}

    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded"

    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"

    if office is not None:
        headers[OFFICE_HEADER] = office

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
        "grant_type": "password", "client_id": "ensa-spa", "username": username,
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


def check(results, label, ok, detail=""):
    results.append(ok)
    print("  %-58s %s %s" % (label, "GECTI" if ok else "KALDI", detail))


def main():
    admin = token_for(ADMIN_USER, ADMIN_PASSWORD)
    results = []

    # ---------------------------------------------------------------- kimlik dogrulama
    print("=== /api/account/offices erisimi ===")

    code, _ = call("/api/account/offices")
    check(results, "anonim istek 401 aliyor", code == 401, "HTTP %s" % code)

    code, body = call("/api/account/offices", token=admin)
    check(results, "yonetici listeyi alabiliyor", code == 200, "HTTP %s" % code)

    if code != 200:
        raise SystemExit(1)

    catalogue = json.loads(body)
    offices = catalogue.get("items", [])
    print("     sube sayisi=%d, varsayilan=%s, tumSubeler=%s"
          % (len(offices), catalogue.get("defaultOfficeId"), catalogue.get("allOfficesAllowed")))

    check(results, "yanit yalnizca gerekli alanlari tasiyor",
          all(set(o) == {"id", "name", "isHeadquarterOffice"} for o in offices),
          str(sorted(offices[0])) if offices else "(liste bos)")

    # -------------------------------------------------- yetkisiz kullanici da erisebilmeli
    username = "officecheck" + str(int(time.time()))[-6:]
    password = "OfficeCheck!2026"

    code, body = call("/api/user", token=admin, body={
        "userName": username, "password": password,
        "name": "Office", "lastName": "Probe", "roles": []})
    if code != 200:
        print("Yetkisiz kullanici olusturulamadi: HTTP %s %s" % (code, body[:300]))
        raise SystemExit(1)

    user_id = json.loads(body)["id"]
    print("\nyetkisiz kullanici olusturuldu: %s (Id=%s)" % (username, user_id))

    try:
        limited = token_for(username, password)

        print("\n=== yetkisiz kullanici ===")
        code, body = call("/api/account/offices", token=limited)
        check(results, "sube listesini yetkisiz de alabiliyor (200)", code == 200, "HTTP %s" % code)

        own = json.loads(body).get("items", []) if code == 200 else []
        check(results, "yalnizca kendi subelerini goruyor", own == [],
              "sube sayisi=%d" % len(own))

        code, body = call("/api/office/lookup", token=limited)
        check(results, "ofis yonetimi ucu hala 403", code == 403,
              "HTTP %s %s" % (code, error_code(body)))

        # ------------------------------------------------------------ baslik dogrulamasi
        print("\n=== X-Ensa-OfficeId dogrulamasi (yetkisiz kullanici) ===")
        for raw in ["0", "-1", "abc", " ", "1.5"]:
            code, body = call("/api/account/permissions", token=limited, office=raw)
            check(results, "bozuk deger %r -> 400" % raw,
                  code == 400 and error_code(body) == "Ensa:Office:InvalidHeader",
                  "HTTP %s %s" % (code, error_code(body)))

        code, body = call("/api/account/permissions", token=limited, office="999999")
        check(results, "var olmayan sube -> 403",
              code == 403 and error_code(body) == "Ensa:Office:NotPermitted",
              "HTTP %s %s" % (code, error_code(body)))

        code, body = call("/api/account/permissions", token=limited, office="all")
        check(results, "yetkisi yokken 'all' -> 403",
              code == 403 and error_code(body) == "Ensa:Office:AllOfficesNotPermitted",
              "HTTP %s %s" % (code, error_code(body)))

        code, _ = call("/api/account/offices", token=limited, office="999999")
        check(results, "sube listesi bayat secimle bile erisilebilir (200)", code == 200,
              "HTTP %s" % code)
    finally:
        call("/api/user/%s" % user_id, token=admin, method="DELETE")
        print("\nsonda temizlik: kullanici %s silindi" % user_id)

    # ------------------------------------------------------ yoneticinin sube baglami
    print("\n=== yonetici sube baglami ===")

    code, _ = call("/api/account/permissions", token=admin, office="all")
    check(results, "yonetici 'all' kapsamini alabiliyor", code == 200, "HTTP %s" % code)

    # Bu bolum kendi verisini kurar. Asil sorunun cevabi burada: sube degistirmek DONEN VERIYI
    # degistiriyor mu? Derlemenin gecmesi ya da baslik dogrulamasinin calismasi bunu kanitlamaz.
    stamp = str(int(time.time()))[-6:]
    created_offices, created_companies, created_visits = [], [], []

    try:
        for name in ("Sube A %s" % stamp, "Sube B %s" % stamp):
            code, body = call("/api/office", token=admin, body={"name": name})
            if code != 200:
                print("Sube olusturulamadi: HTTP %s %s" % (code, body[:300]))
                raise SystemExit(1)
            created_offices.append(json.loads(body)["id"])

        office_a, office_b = created_offices
        print("olusturulan subeler: A=%s, B=%s" % (office_a, office_b))

        for office_id, label in ((office_a, "A"), (office_b, "B")):
            code, body = call("/api/company", token=admin, body={
                "companyName": "Sube %s Isyeri %s" % (label, stamp),
                "hazardClass": 1, "workplaceType": 1,
                "cityId": 34, "districtId": 1, "officeId": office_id})
            if code != 200:
                print("Isyeri olusturulamadi: HTTP %s %s" % (code, body[:300]))
                raise SystemExit(1)
            created_companies.append(json.loads(body)["id"])

        def company_names(office):
            code_, body_ = call("/api/company?MaxResultCount=200&Filter=" + stamp,
                                token=admin, office=office)
            if code_ != 200:
                return code_, []
            return code_, sorted(row["companyName"] for row in json.loads(body_)["items"])

        code_a, names_a = company_names(str(office_a))
        code_b, names_b = company_names(str(office_b))
        code_all, names_all = company_names("all")

        check(results, "A subesi yalnizca kendi isyerini donduruyor",
              code_a == 200 and names_a == ["Sube A Isyeri %s" % stamp], str(names_a))
        check(results, "B subesi yalnizca kendi isyerini donduruyor",
              code_b == 200 and names_b == ["Sube B Isyeri %s" % stamp], str(names_b))
        check(results, "sube degisimi donen veriyi gercekten degistiriyor",
              names_a != names_b and code_a == 200 and code_b == 200,
              "%s != %s" % (names_a, names_b))
        check(results, "'all' kapsami ikisini birden donduruyor",
              code_all == 200 and names_all == sorted(names_a + names_b), str(names_all))

        # Ayni sorunun lookup ucu de ayni cevabi vermeli; aksi halde isyeri secici,
        # listenin gostermedigi bir kaydi onerir.
        code_, body_ = call("/api/company/lookup?filter=" + stamp, token=admin, office=str(office_a))
        lookup_names = sorted(row["displayName"] for row in json.loads(body_)["items"]) \
            if code_ == 200 else []
        check(results, "isyeri lookup'i da sube kapsaminda", lookup_names == names_a,
              str(lookup_names))

        # Ziyaret listesi de ayni kapsamda olmali. Ziyaretin kendi ofisi yoktur; isyerinin
        # ofisi uzerinden kapsanir, yani yuklem bir alt sorgudur. Bu uc ayrica olculuyor:
        # bir donem tam da o alt sorgu yuzunden HTTP 500 donuyordu ve hatayi yalnizca sube
        # baglami OLAN bir kullanici goruyordu - baglamsiz calisan her kontrol geciyordu.
        code_, body_ = call("/api/visit", token=admin, body={
            "companyId": created_companies[0],
            "visitDate": time.strftime("%Y-%m-%dT09:00:00"),
            "description": "Sube A Ziyaret %s" % stamp})

        if code_ != 200:
            print("Ziyaret olusturulamadi: HTTP %s %s" % (code_, body_[:300]))
            raise SystemExit(1)

        created_visits.append(json.loads(body_)["id"])

        def visit_descriptions(office):
            c_, b_ = call("/api/visit?MaxResultCount=200&Filter=" + stamp, token=admin, office=office)
            if c_ != 200:
                return c_, []
            return c_, sorted(row.get("description") or "" for row in json.loads(b_)["items"])

        code_va, visits_a = visit_descriptions(str(office_a))
        code_vb, visits_b = visit_descriptions(str(office_b))

        check(results, "ziyaret listesi sube baglaminda cevrilebiliyor (500 degil)",
              code_va == 200 and code_vb == 200, "A=HTTP %s B=HTTP %s" % (code_va, code_vb))
        check(results, "ziyaret yalnizca isyerinin subesinde gorunuyor",
              visits_a == ["Sube A Ziyaret %s" % stamp] and visits_b == [],
              "A=%s B=%s" % (visits_a, visits_b))

        # Sube filtresi baglamla celisirse istek reddedilir - biri sessizce kazanmaz.
        code_, body_ = call("/api/company?MaxResultCount=1&OfficeId=%d" % office_b,
                            token=admin, office=str(office_a))
        check(results, "celisen sube filtresi -> 400",
              code_ == 400 and error_code(body_) == "Ensa:Office:FilterConflict",
              "HTTP %s %s" % (code_, error_code(body_)))

        # Ayni subeyi hem baslikta hem filtrede vermek celiski degil, tekrardir.
        code_, _ = call("/api/company?MaxResultCount=1&OfficeId=%d" % office_a,
                        token=admin, office=str(office_a))
        check(results, "ayni subeyi tekrar etmek kabul ediliyor", code_ == 200, "HTTP %s" % code_)

        # Sube degistirmek kurumu degistirmemeli.
        code_, before = call("/api/account/profile", token=admin)
        _, after = call("/api/account/profile", token=admin, office=str(office_b))
        check(results, "sube degisimi kurumu degistirmiyor",
              code_ == 200 and json.loads(before).get("tenantId") == json.loads(after).get("tenantId"),
              "tenantId=%s" % json.loads(before).get("tenantId"))

        # Yeni subeler listede gorunmeli: secici, gerceklesen veriden besleniyor.
        code_, body_ = call("/api/account/offices", token=admin)
        listed = {row["id"] for row in json.loads(body_)["items"]} if code_ == 200 else set()
        check(results, "yeni subeler /account/offices listesine giriyor",
              {office_a, office_b} <= listed, "listede %d sube" % len(listed))
    finally:
        for visit_id in created_visits:
            call("/api/visit/%s" % visit_id, token=admin, method="DELETE")
        for company_id in created_companies:
            call("/api/company/%s" % company_id, token=admin, method="DELETE")
        for office_id in created_offices:
            call("/api/office/%s" % office_id, token=admin, method="DELETE")
        if created_offices:
            print("\nsonda temizlik: %d ziyaret, %d isyeri, %d sube silindi"
                  % (len(created_visits), len(created_companies), len(created_offices)))

    passed = sum(1 for r in results if r)
    print("\n%d kontrolden %d tanesi gecti" % (len(results), passed))
    return 0 if passed == len(results) else 1


if __name__ == "__main__":
    sys.exit(main())
