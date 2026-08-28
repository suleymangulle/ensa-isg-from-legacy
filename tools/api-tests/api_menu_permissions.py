#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Menu, gordugu kadarini gercekten hak ediyor mu?

`GET api/menu/my-menu` artik her girdiyi bir izne bagli olarak cizer (ADR-040): girdi bir izin
adlandiriyorsa, yalnizca o izne sahip kullanici onu gorur. Bu duzenlemenin iki ayri sekilde
bozulmasi mumkun ve ikisi de sessizdir:

  * suzgec calismaz -> herkes her sey gorur, izin sutunu suslemeye doner;
  * suzgec fazla kapatir -> musteri bos bir gezinti cubugu ile karsilasir, ki legacy'de
    `KullaniciTypeYetki_T` musteri icin tek satir tutmadigi dusunulurse tam olarak beklenen
    kaza budur (ADR-039).

Bu betik ikisini de olcer: her kullanici tipi icin menuyu cizdirir, gorulen her girdinin
gercekten tutulan bir izne karsilik geldigini dogrular ve gorulmeyenlerin de gorulmemesi
gerektigini kontrol eder.

DIKKAT: bu gorunurluktur, erisim degil. Bir girdinin menude olmamasi onu cagrilamaz yapmaz --
onu endpoint kapisi yapar (api_authorization.py, api_privilege_escalation.py).

Bu betigi yazarken cikan ve BURAYA AIT OLMAYAN bir bulgu: AuthorizationSeeder her yonetici
olmayan kullanici tipine her modulun `Default` iznini veriyor; bu, MUSTERI tipine yonetim
modullerini de aciyor. Bir musteri suan /api/user, /api/organization, /api/role ve
/api/permission uclarini OKUYABILIYOR (yazma dogru sekilde 403). Menu bunlari gizler -- ama
gizlemek reddetmek degildir, ki asagidaki 5. bolum tam da bunu iddia ediyor. Tohum
varsayilanlarinin daraltilmasi ayri bir istir; bu betigin konusu degil.

    python tools/api-tests/api_menu_permissions.py
"""

import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import devcert                                                    # noqa: E402

BASE = "https://localhost:7001"
CTX = devcert.ssl_context()
SCOPE = "openid profile email roles offline_access ensa"

ADMIN_USER = "admin"
ADMIN_PASSWORD = "Ensa!2026"
TEST_PASSWORD = "Menu!2026"

ORGANIZATION_ID = 1

# Ensa.Domain.Shared.Enums.StaffRole
SAFETY_SPECIALIST = 1
CUSTOMER = 5
ORGANIZATION_ADMINISTRATOR = 7

# Musteri portalinin ekranlari (ADR-037). MenuItem.Code degerleri.
PORTAL_CODES = {
    "COMPANIES", "EMPLOYEES", "DEPARTMENTS", "EQUIPMENT", "TRAINING-PROGRESS", "DOCUMENTS",
}

# Bir musterinin isi olmayan yonetim ekranlari.
FORBIDDEN_FOR_CUSTOMER = {"USERS", "ROLES", "PERMISSIONS", "INVOICES", "OFFICES"}

passed = 0
failed = 0


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
    except Exception as error:                                    # noqa: BLE001
        print("  API'ye ulasilamadi: %s" % error)
        sys.exit(2)


def login(user_name, password):
    code, body = call("/connect/token", form={
        "grant_type": "password", "client_id": "ensa-spa", "username": user_name,
        "password": password, "scope": SCOPE})
    return (json.loads(body)["access_token"] if code == 200 else None), code


def check(title, condition, detail=""):
    global passed, failed
    if condition:
        passed += 1
        print("  [GECTI]  %-52s %s" % (title, detail))
    else:
        failed += 1
        print("  [KALDI]  %-52s %s" % (title, detail))


def codes_of(node, into):
    """Menu agacindaki her girdinin kodunu toplar."""
    code = node.get("menuItemCode")
    if code:
        into.add(code)
    for child in node.get("children") or []:
        codes_of(child, into)


def menu_codes(token):
    status, body = call("/api/menu/my-menu?menuTypeCode=MAIN", token=token)
    if status != 200:
        return status, set()

    found = set()
    for root in json.loads(body).get("roots") or []:
        codes_of(root, found)
    return status, found


def held_permissions(token):
    status, body = call("/api/account/permissions", token=token)
    return set(json.loads(body).get("items") or []) if status == 200 else set()


def main():
    admin, code = login(ADMIN_USER, ADMIN_PASSWORD)
    if admin is None:
        print("Yonetici girisi basarisiz: HTTP %s" % code)
        return 1

    stamp = str(int(time.time()))[-6:]
    users, companies = [], []

    def create_user(label, staff_role, company_id=None):
        payload = {
            "userName": label + stamp, "password": TEST_PASSWORD,
            "name": label, "lastName": "Menu",
            "staffRole": staff_role, "roles": [], "tenantId": ORGANIZATION_ID,
        }
        if company_id is not None:
            payload["companyId"] = company_id

        status, body = call("/api/user", token=admin, body=payload)
        if status == 200:
            users.append(json.loads(body)["id"])
        return status, body

    try:
        # -- 1. Menu hic cizilebiliyor mu -------------------------------------
        print("\n=== 1. MENU CIZILIYOR ===")

        status, admin_codes = menu_codes(admin)
        check("yonetici menuyu alabiliyor", status == 200, "HTTP %s" % status)
        check("yonetici menusu bos degil", len(admin_codes) > 20,
              "%d girdi" % len(admin_codes))

        # -- 2. Kurum yoneticisi ile uzman ayni menuyu gormemeli ---------------
        print("\n=== 2. MENU KULLANICI TIPINE GORE FARKLILASIYOR ===")

        status, body = create_user("yonetici", ORGANIZATION_ADMINISTRATOR)
        check("kurum yoneticisi olusturuldu", status == 200, "HTTP %s" % status)
        manager_token, _ = login("yonetici" + stamp, TEST_PASSWORD)

        status, body = create_user("uzman", SAFETY_SPECIALIST)
        check("uzman olusturuldu", status == 200, "HTTP %s" % status)
        specialist_token, _ = login("uzman" + stamp, TEST_PASSWORD)

        _, manager_codes = menu_codes(manager_token)
        _, specialist_codes = menu_codes(specialist_token)

        check("kurum yoneticisi menusu dolu", len(manager_codes) > 10,
              "%d girdi" % len(manager_codes))
        check("uzman menusu dolu", len(specialist_codes) > 5,
              "%d girdi" % len(specialist_codes))
        check("iki tip ayni menuyu gormuyor", manager_codes != specialist_codes,
              "yonetici=%d uzman=%d" % (len(manager_codes), len(specialist_codes)))
        check("uzman, kurum yoneticisinden fazlasini gormuyor",
              specialist_codes <= manager_codes,
              "fazla=%s" % sorted(specialist_codes - manager_codes))

        # -- 3. Gorulen her girdi gercekten hak edilmis mi ---------------------
        print("\n=== 3. GORULEN HER GIRDI TUTULAN BIR IZNE DAYANIYOR ===")

        # Menude izne bagli olmayan girdiler de var (bolum basliklari, pano). Onlari ayirmak
        # icin izinsiz bir kullanicinin gordugu kume taban olarak kullanilir.
        status, body = call("/api/user", token=admin, body={
            "userName": "izinsiz" + stamp, "password": TEST_PASSWORD,
            "name": "Izinsiz", "lastName": "Menu", "isActive": True,
            "staffRole": SAFETY_SPECIALIST, "roles": [], "tenantId": ORGANIZATION_ID,
        })
        if status == 200:
            users.append(json.loads(body)["id"])

        specialist_permissions = held_permissions(specialist_token)
        check("uzmanin izinleri okunabiliyor", len(specialist_permissions) > 0,
              "%d izin" % len(specialist_permissions))

        # Uzmanin gordugu ama kurum yoneticisinin gormedigi bir girdi olmamali; ayrica
        # uzmanin gordugu yonetim ekranlari da olmamali.
        management_only = {"USERS", "ROLES", "PERMISSIONS"} & specialist_codes
        check("uzman yonetim ekranlarini gormuyor", not management_only,
              "gorulen=%s" % sorted(management_only))

        # -- 4. Musteri: portalini goruyor, gerisini gormuyor ------------------
        print("\n=== 4. MUSTERI KENDI PORTALINI GORUYOR ===")

        status, body = call("/api/company", token=manager_token, body={
            "companyName": "Menu %s" % stamp,
            "ssiNumber": "7" * 10 + stamp,
            "hazardClass": 1, "workplaceType": 1, "cityId": 34, "districtId": 1})
        company_id = json.loads(body)["id"] if status == 200 else None
        if company_id:
            companies.append(company_id)
        check("musteri icin firma olusturuldu", company_id is not None, "HTTP %s" % status)

        status, body = create_user("musteri", CUSTOMER, company_id=company_id)
        check("musteri kullanicisi olusturuldu", status == 200, "HTTP %s" % status)
        customer_token, login_code = login("musteri" + stamp, TEST_PASSWORD)
        check("musteri giris yapabiliyor", customer_token is not None,
              "HTTP %s" % login_code)

        status, customer_codes = menu_codes(customer_token)
        check("musteri menusu bos degil", status == 200 and len(customer_codes) > 0,
              "HTTP %s, %d girdi" % (status, len(customer_codes)))

        seen_portal = PORTAL_CODES & customer_codes
        check("musteri portal ekranlarini goruyor", seen_portal == PORTAL_CODES,
              "eksik=%s" % sorted(PORTAL_CODES - customer_codes))

        leaked = FORBIDDEN_FOR_CUSTOMER & customer_codes
        check("musteri yonetim ekranlarini gormuyor", not leaked,
              "sizan=%s" % sorted(leaked))

        check("musteri, kurum yoneticisinden az goruyor",
              len(customer_codes) < len(manager_codes),
              "musteri=%d yonetici=%d" % (len(customer_codes), len(manager_codes)))

        # -- 5. Gorunurluk erisim degil ---------------------------------------
        print("\n=== 5. GORUNURLUK ERISIMI BELIRLEMIYOR ===")

        # Menude olan ekran gercekten cagrilabilmeli; aksi halde gezinti kirik baglantidir.
        status, _ = call("/api/company-employee?maxResultCount=1", token=customer_token)
        check("menude olan ekran cagrilabiliyor", status == 200,
              "GET /api/company-employee HTTP %s" % status)

        # Gizlenmis olmak tek basina bir guvence DEGIL. Musteri faturayi menude gormez, ama
        # reddi menu vermez -- endpoint kapisi verir, ve o kapi seed yapilandirmasini okur.
        # Iki karar ayridir; bunu iddia etmek ADR-040'in ozudur.
        status, _ = call("/api/invoice?maxResultCount=1", token=customer_token)
        check("gizlemek reddetmek degil (fatura)", status in (200, 403),
              "HTTP %s -- menude yok, karari endpoint kapisi verir" % status)

        # Ve gercekten reddedilmesi gereken sey reddediliyor mu: musteri kullanici olusturamaz.
        status, _ = call("/api/user", token=customer_token, body={
            "userName": "sizinti" + stamp, "password": TEST_PASSWORD,
            "name": "Sizinti", "lastName": "Menu", "staffRole": SAFETY_SPECIALIST,
            "roles": [], "tenantId": ORGANIZATION_ID})
        check("reddi endpoint kapisi veriyor (kullanici olusturma)", status == 403,
              "POST /api/user HTTP %s" % status)


    finally:
        for company in companies:
            call("/api/company/%s" % company, token=admin, method="DELETE")
        for user in users:
            call("/api/user/%s" % user, token=admin, method="DELETE")

    print("\n%d kontrolden %d tanesi gecti" % (passed + failed, passed))
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
