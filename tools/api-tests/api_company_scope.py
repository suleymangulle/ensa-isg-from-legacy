# -*- coding: utf-8 -*-
"""
Kurum kullanicilari ve firma kapsami.

Bu betik uc soruyu ayri ayri yanitlar:

  1. Host yonetici bir kullaniciyi bir kuruma baglayabiliyor mu?
     Cekirdek yonetici kasitli olarak host kullanicisidir (`TenantId = null`) - her kurumu yonetir.
     `CreateUserDto` bir kurum alani tasimazsa olusturdugu her kullanici da host kullanicisi olur;
     yetki hesabi kurumu olmayan kullaniciya bos kume dondurdugu icin urun tek bir calisan uzman
     bile uretemez.

  2. Kurum kullanicisinin jetonu gercekten yetki tasiyor mu?
     Jeton uretilirken hicbir kiraci cozulmemistir - kiraci bilgisini tasiyacak jeton zaten o an
     uretilmektedir. Kullanicinin kendi kiraci baglami acilmazsa yetki satirlari genel sorgu
     suzgecinin disinda kalir ve jeton hic yetki iddiasi tasimadan cikar: kullanici basariyla
     giris yapar, her uctan 403 alir.

  3. Bir firmaya bagli kullanici o firmanin disina cikabiliyor mu?
     Kiraci suzgeci OSGB'leri birbirinden ayirir; bir OSGB'nin MUSTERILERI arasinda ayrim yapmaz.
     Firma kapsami suzgeci (`ICompanyScoped` / `ICompanyRecord`) bu ayrimi yapar ve firmaya bagli
     olmayan herkes icin - yani OSGB'nin kendi personeli icin - etkisizdir.
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
SCOPE = "openid profile email roles offline_access ensa"

ADMIN_USER = "admin"
ADMIN_PASSWORD = "Ensa!2026"
TEST_PASSWORD = "Kapsam!2026"

# The organization every test user is bound to. The seeder creates exactly one.
ORGANIZATION_ID = 1

# Ensa.Domain.Shared.Enums.StaffRole
SAFETY_SPECIALIST = 1
CUSTOMER = 5
ORGANIZATION_ADMINISTRATOR = 7

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


def ids_of(body):
    return [item["id"] for item in json.loads(body)["items"]]


def main():
    admin, code = login(ADMIN_USER, ADMIN_PASSWORD)
    if admin is None:
        print("Yonetici girisi basarisiz: HTTP %s" % code)
        return 1

    stamp = str(int(time.time()))[-6:]
    users, companies = [], []

    def create_user(label, staff_role, company_id=None, tenant_id=ORGANIZATION_ID):
        payload = {
            "userName": label + stamp, "password": TEST_PASSWORD,
            "name": label, "lastName": "Kapsam",
            "staffRole": staff_role, "roles": [],
        }
        if tenant_id is not None:
            payload["tenantId"] = tenant_id
        if company_id is not None:
            payload["companyId"] = company_id

        status, payload_body = call("/api/user", token=admin, body=payload)
        if status == 200:
            users.append(json.loads(payload_body)["id"])
        return status, payload_body

    try:
        # -- 1. Kuruma baglama -------------------------------------------------
        print("\n=== 1. HOST YONETICI KULLANICIYI KURUMA BAGLIYOR ===")

        status, body = create_user("uzman", SAFETY_SPECIALIST)
        specialist = json.loads(body) if status == 200 else {}
        check("kurum kullanicisi olusturuldu", status == 200, "HTTP %s" % status)
        check("kullanici kuruma bagli", specialist.get("tenantId") == ORGANIZATION_ID,
              "tenantId=%s" % specialist.get("tenantId"))

        status, _ = call("/api/user", token=admin, body={
            "userName": "yok" + stamp, "password": TEST_PASSWORD, "name": "Yok",
            "lastName": "Kapsam", "staffRole": SAFETY_SPECIALIST, "tenantId": 999999, "roles": []})
        check("olmayan kuruma baglama reddedildi", status == 404, "HTTP %s" % status)

        status, body = create_user("host", SAFETY_SPECIALIST, tenant_id=None)
        host_user = json.loads(body) if status == 200 else {}
        check("kurum verilmezse host kullanicisi", host_user.get("tenantId") is None,
              "tenantId=%s" % host_user.get("tenantId"))

        # -- 2. Jeton yetki tasiyor mu ----------------------------------------
        print("\n=== 2. KURUM KULLANICISININ JETONU YETKI TASIYOR ===")

        token, code = login("uzman" + stamp, TEST_PASSWORD)
        check("kurum kullanicisi giris yapti", code == 200, "HTTP %s" % code)

        status, body = call("/api/account/permissions", token=token)
        count = len(json.loads(body)["items"]) if status == 200 else 0
        check("etkin yetki kumesi bos degil", count > 0, "%d yetki" % count)

        status, _ = call("/api/company?maxResultCount=1", token=token)
        check("goruntuleme ucu calisiyor", status == 200, "GET /api/company HTTP %s" % status)

        status, _ = call("/api/company", token=token, body={
            "companyName": "Izinsiz " + stamp, "ssiNumber": "1" * 16,
            "hazardClass": 1, "workplaceType": 1, "cityId": 34, "districtId": 1})
        check("yazma ucu reddedildi (yalnizca goruntuleme)", status == 403,
              "POST /api/company HTTP %s" % status)

        status, body = create_user("yonetici", ORGANIZATION_ADMINISTRATOR)
        admin_token, _ = login("yonetici" + stamp, TEST_PASSWORD)
        status, body = call("/api/account/permissions", token=admin_token)
        admin_count = len(json.loads(body)["items"]) if status == 200 else 0
        check("kurum yoneticisi tam yetkili", admin_count > count,
              "%d yetki (uzman: %d)" % (admin_count, count))

        # -- 3. Firma kapsami --------------------------------------------------
        print("\n=== 3. FIRMAYA BAGLI KULLANICI FIRMASININ DISINA CIKAMIYOR ===")

        for index in (1, 2):
            status, body = call("/api/company", token=admin_token, body={
                "companyName": "Kapsam %s-%d" % (stamp, index),
                "ssiNumber": "6" * 10 + stamp + str(index),
                "hazardClass": 1, "workplaceType": 1, "cityId": 34, "districtId": 1})
            companies.append(json.loads(body)["id"] if status == 200 else None)

        check("iki firma olusturuldu", all(companies), "id=%s" % companies)

        employees = []
        for index, company in enumerate(companies, start=1):
            status, body = call("/api/company-employee", token=admin_token, body={
                "companyId": company, "name": "Calisan%d" % index, "lastName": "Kapsam",
                "gender": 1, "educationLevel": 0, "maritalStatus": 0, "isActive": True})
            employees.append(json.loads(body)["id"] if status == 200 else None)

        check("her firmaya bir calisan eklendi", all(employees), "id=%s" % employees)

        status, body = create_user("musteri", CUSTOMER, company_id=companies[0])
        customer = json.loads(body) if status == 200 else {}
        check("musteri kullanicisi firmaya bagli", customer.get("companyId") == companies[0],
              "companyId=%s" % customer.get("companyId"))

        customer_token, code = login("musteri" + stamp, TEST_PASSWORD)
        check("musteri giris yapti", code == 200, "HTTP %s" % code)

        status, body = call("/api/company?maxResultCount=50", token=customer_token)
        listed = ids_of(body) if status == 200 else []
        check("musteri yalnizca kendi firmasini listeliyor", listed == [companies[0]],
              "gorulen=%s" % listed)

        status, _ = call("/api/company/%s" % companies[1], token=customer_token)
        check("baska firmayi dogrudan okuyamiyor", status == 404, "HTTP %s" % status)

        status, _ = call("/api/company/%s" % companies[0], token=customer_token)
        check("kendi firmasini okuyabiliyor", status == 200, "HTTP %s" % status)

        status, body = call("/api/company-employee?maxResultCount=50", token=customer_token)
        listed = ids_of(body) if status == 200 else []
        check("musteri yalnizca kendi calisanini goruyor", listed == [employees[0]],
              "gorulen=%s" % listed)

        status, _ = call("/api/company-employee/%s" % employees[1], token=customer_token)
        check("baska firmanin calisanini okuyamiyor", status == 404, "HTTP %s" % status)

        # -- 4. Personel etkilenmiyor -----------------------------------------
        print("\n=== 4. OSGB PERSONELI KAPSAMDAN ETKILENMIYOR ===")

        status, body = call("/api/company?maxResultCount=50", token=admin_token)
        listed = ids_of(body) if status == 200 else []
        check("kurum yoneticisi butun firmalari goruyor",
              all(company in listed for company in companies), "gorulen=%d" % len(listed))

        status, body = call("/api/company-employee?maxResultCount=50", token=admin_token)
        listed = ids_of(body) if status == 200 else []
        check("kurum yoneticisi butun calisanlari goruyor",
              all(employee in listed for employee in employees), "gorulen=%d" % len(listed))

    finally:
        # The companies and employees belong to organization 1, and the host administrator cannot
        # see a tenant's rows: the tenant filter is `TenantId == CurrentTenantId || TenantId ==
        # null`, and a host caller's CurrentTenantId is null. Deleting them needs the organization
        # administrator this run created; the users themselves are host-visible.
        cleaner = locals().get("admin_token") or admin

        for employee in [e for e in locals().get("employees", []) if e]:
            call("/api/company-employee/%s" % employee, token=cleaner, method="DELETE")
        for company in [c for c in companies if c]:
            call("/api/company/%s" % company, token=cleaner, method="DELETE")
        for user in users:
            call("/api/user/%s" % user, token=admin, method="DELETE")

    print("\n%d kontrolden %d tanesi gecti" % (passed + failed, passed))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
