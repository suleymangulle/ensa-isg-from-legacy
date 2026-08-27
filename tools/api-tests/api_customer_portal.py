# -*- coding: utf-8 -*-
"""
Eski musteri portali (`MusteriArayuzu`) modern uygulamada karsiliksiz kalmis mi?

Eski sistemde musteri firmalar ayri bir web uygulamasina giriyordu. Bunun sebebi bir urun karari
degildi: ana uygulamada satir duzeyi kapsam yoktu, yani bir musteriyi ana uygulamaya sokmak onu
butun musterilerin verisinin icine sokmak demekti. Ayri uygulama, olmayan bir suzgecin yerine
gecen bir cozumdu.

Firma kapsami suzgeci (ADR-034) o suzgeci getirdiginde ayri uygulamanin gerekcesi kalmadi:
musteri kullanicisi ana SPA'ya girer ve ayni ekranlarda yalnizca kendi firmasinin verisini gorur.

Bu betik o iddiayi kanitlar. Eski portalin HER sayfasi icin modern karsiligini gercek bir musteri
kullanicisiyla cagirir; hem calistigini hem de kapsamin disina tasmadigini olcer.

| Eski sayfa          | Modern karsiligi                                   |
|---------------------|----------------------------------------------------|
| Login / Logout      | /connect/token (OpenIddict)                        |
| Default             | pano sayacilari + firma uyari ozeti                |
| FirmaPersonel       | /api/company-employee                              |
| IsyeriBolumleri     | /api/workplace-department                          |
| Cihazlar            | /api/equipment                                     |
| EksikEgitimler      | /api/employee-training-progress                    |
| DenetimEvraklari    | /api/document                                      |
| UserProfil          | /api/account/profile + /api/account/change-password |
| dosya               | /api/document/{id}/content                         |
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
TEST_PASSWORD = "Portal!2026"
CHANGED_PASSWORD = "Portal!2027"

ORGANIZATION_ID = 1
CUSTOMER = 5                    # StaffRole.Customer
ORGANIZATION_ADMINISTRATOR = 7  # StaffRole.OrganizationAdministrator

passed = 0
failed = 0


def call(path, token=None, form=None, body=None, method=None, raw=False):
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
            payload = response.read()
            return response.status, payload if raw else payload.decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        payload = error.read()
        return error.code, payload if raw else payload.decode("utf-8", "replace")


def upload(token, file_name, payload, company_id):
    """One multipart/form-data POST to /api/document/upload, hand-built (no third-party client)."""
    boundary = "----ensa-portal-boundary"
    crlf = "\r\n"
    parts = []

    def field(name, value):
        parts.append(("--" + boundary + crlf
                      + 'Content-Disposition: form-data; name="' + name + '"' + crlf
                      + crlf + str(value) + crlf).encode())

    parts.append(("--" + boundary + crlf
                  + 'Content-Disposition: form-data; name="file"; filename="'
                  + file_name + '"' + crlf
                  + "Content-Type: text/plain" + crlf + crlf).encode())
    parts.append(payload)
    parts.append(crlf.encode())

    field("companyId", company_id)
    field("ownerType", 0)

    parts.append(("--" + boundary + "--" + crlf).encode())
    data = b"".join(parts)

    request = urllib.request.Request(
        BASE + "/api/document/upload", data=data,
        headers={"Content-Type": "multipart/form-data; boundary=" + boundary,
                 "Authorization": "Bearer " + token})

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


def check(page, title, condition, detail=""):
    global passed, failed
    if condition:
        passed += 1
        print("  [GECTI]  %-18s %-40s %s" % (page, title, detail))
    else:
        failed += 1
        print("  [KALDI]  %-18s %-40s %s" % (page, title, detail))


def items(body):
    payload = json.loads(body)
    return payload.get("items", [])


def main():
    admin, code = login(ADMIN_USER, ADMIN_PASSWORD)
    if admin is None:
        print("Yonetici girisi basarisiz: HTTP %s" % code)
        return 1

    stamp = str(int(time.time()))[-6:]
    users, companies, employees, departments, equipment, documents = [], [], [], [], [], []

    def create_user(label, staff_role, company_id=None):
        payload = {
            "userName": label + stamp, "password": TEST_PASSWORD,
            "name": label, "lastName": "Portal", "staffRole": staff_role,
            "tenantId": ORGANIZATION_ID, "roles": [],
        }
        if company_id is not None:
            payload["companyId"] = company_id

        status, response = call("/api/user", token=admin, body=payload)
        if status == 200:
            users.append(json.loads(response)["id"])
        return status, response

    try:
        # ---------------------------------------------------------- kurulum
        status, body = create_user("kurumadmin", ORGANIZATION_ADMINISTRATOR)
        if status != 200:
            print("Kurum yoneticisi olusturulamadi: HTTP %s %s" % (status, body[:200]))
            return 1
        staff, _ = login("kurumadmin" + stamp, TEST_PASSWORD)

        for index in (1, 2):
            status, body = call("/api/company", token=staff, body={
                "companyName": "Portal %s-%d" % (stamp, index),
                "ssiNumber": "5" * 10 + stamp + str(index),
                "hazardClass": 1, "workplaceType": 1, "cityId": 34, "districtId": 1})
            companies.append(json.loads(body)["id"] if status == 200 else None)

        if not all(companies):
            print("Firmalar olusturulamadi.")
            return 1

        mine, theirs = companies

        for company, name in ((mine, "Benim"), (theirs, "Digeri")):
            status, body = call("/api/company-employee", token=staff, body={
                "companyId": company, "name": name, "lastName": "Calisan",
                "gender": 1, "educationLevel": 0, "maritalStatus": 0, "isActive": True})
            employees.append(json.loads(body)["id"] if status == 200 else None)

            status, body = call("/api/workplace-department", token=staff, body={
                "companyId": company, "departmentName": name + " Bolum", "isActive": True})
            departments.append(json.loads(body)["id"] if status == 200 else None)

            status, body = call("/api/equipment", token=staff, body={
                "companyId": company, "equipmentName": name + " Cihaz",
                "equipmentType": 1, "isActive": True})
            equipment.append(json.loads(body)["id"] if status == 200 else None)

        status, body = create_user("musteri", CUSTOMER, company_id=mine)
        if status != 200:
            print("Musteri kullanicisi olusturulamadi: HTTP %s %s" % (status, body[:200]))
            return 1

        customer, code = login("musteri" + stamp, TEST_PASSWORD)
        check("Login", "musteri portala girebiliyor", code == 200, "HTTP %s" % code)
        if customer is None:
            return 1

        # ---------------------------------------------------------- Default
        status, body = call("/api/company?maxResultCount=50", token=customer)
        listed = [c["id"] for c in items(body)] if status == 200 else []
        check("Default", "pano yalnizca kendi firmasini sayiyor", listed == [mine],
              "gorulen=%s" % listed)

        status, body = call("/api/company/%s/detail" % mine, token=customer)
        summary = json.loads(body).get("warningSummary") if status == 200 else None
        check("Default", "eksik evrak ozeti geliyor", status == 200 and summary is not None,
              "HTTP %s" % status)

        # ---------------------------------------------------------- FirmaPersonel
        status, body = call("/api/company-employee?maxResultCount=50", token=customer)
        listed = [e["id"] for e in items(body)] if status == 200 else []
        check("FirmaPersonel", "kendi calisanlarini listeliyor", listed == [employees[0]],
              "gorulen=%s" % listed)

        status, _ = call("/api/company-employee/%s" % employees[1], token=customer)
        check("FirmaPersonel", "baska firmanin calisanina erisemiyor", status == 404,
              "HTTP %s" % status)

        # ---------------------------------------------------------- IsyeriBolumleri
        status, body = call("/api/workplace-department?maxResultCount=50", token=customer)
        listed = [d["id"] for d in items(body)] if status == 200 else []
        check("IsyeriBolumleri", "kendi bolumlerini listeliyor", listed == [departments[0]],
              "gorulen=%s" % listed)

        # ---------------------------------------------------------- Cihazlar
        status, body = call("/api/equipment?maxResultCount=50", token=customer)
        listed = [e["id"] for e in items(body)] if status == 200 else []
        check("Cihazlar", "kendi cihazlarini listeliyor", listed == [equipment[0]],
              "gorulen=%s" % listed)

        status, body = call("/api/equipment/overdue-inspections", token=customer)
        check("Cihazlar", "gecikmis muayene listesi calisiyor", status == 200,
              "HTTP %s, %d kayit" % (status, len(items(body)) if status == 200 else -1))

        # ---------------------------------------------------------- EksikEgitimler
        status, body = call(
            "/api/employee-training-progress?maxResultCount=50&IsCompleted=false", token=customer)
        check("EksikEgitimler", "eksik egitim listesi calisiyor", status == 200,
              "HTTP %s, %d kayit" % (status, len(items(body)) if status == 200 else -1))

        status, body = call("/api/employee-training-progress/employee/%s" % employees[1],
                            token=customer)
        rows = items(body) if status == 200 else []
        check("EksikEgitimler", "baska firmanin calisani icin kayit donmuyor",
              status in (200, 404) and not rows, "HTTP %s, %d kayit" % (status, len(rows)))

        # ---------------------------------------------------------- DenetimEvraklari
        status, body = call("/api/document?maxResultCount=50", token=customer)
        visible = items(body) if status == 200 else []
        check("DenetimEvraklari", "evrak listesi calisiyor", status == 200,
              "HTTP %s, %d kayit" % (status, len(visible)))
        check("DenetimEvraklari", "baska firmanin evraki gorunmuyor",
              all(document.get("companyId") == mine for document in visible),
              "%d kayit incelendi" % len(visible))

        # ---------------------------------------------------------- UserProfil
        status, body = call("/api/account/profile", token=customer)
        profile = json.loads(body) if status == 200 else {}
        check("UserProfil", "profil okunabiliyor", status == 200 and profile.get("companyId") == mine,
              "companyId=%s" % profile.get("companyId"))

        status, _ = call("/api/account/change-password", token=customer, body={
            "currentPassword": TEST_PASSWORD,
            "newPassword": CHANGED_PASSWORD,
            "newPasswordRepeat": CHANGED_PASSWORD})
        check("UserProfil", "parolasini kendisi degistirebiliyor", status in (200, 204),
              "HTTP %s" % status)

        retoken, code = login("musteri" + stamp, CHANGED_PASSWORD)
        check("UserProfil", "yeni parolayla giris yapabiliyor", code == 200, "HTTP %s" % code)
        customer = retoken or customer

        # ---------------------------------------------------------- dosya
        payload = ("portal test %s" % stamp).encode()
        status, body = upload(staff, "portal-%s.txt" % stamp, payload, mine)
        if status == 200:
            documents.append(json.loads(body)["id"])

            status, downloaded = call("/api/document/%s/content" % documents[0],
                                      token=customer, raw=True)
            check("dosya", "kendi firmasinin dosyasini indirebiliyor",
                  status == 200 and downloaded == payload,
                  "HTTP %s, %d bayt" % (status, len(downloaded)))
        else:
            check("dosya", "test dosyasi yuklendi", False, "HTTP %s %s" % (status, body[:120]))

        status, _ = call("/api/document/%s/content" % 999999, token=customer)
        check("dosya", "olmayan dosya 404", status == 404, "HTTP %s" % status)

        # ---------------------------------------------------------- Logout
        status, _ = call("/connect/userinfo", token=customer)
        check("Logout", "oturum acikken userinfo 200", status == 200, "HTTP %s" % status)

        status, _ = call("/connect/userinfo")
        check("Logout", "jetonsuz istek 401", status == 401, "HTTP %s" % status)

    finally:
        # Tenant-owned rows are deleted by the tenant-scoped user that created them. The host
        # administrator cannot see them: the tenant filter is `TenantId == CurrentTenantId ||
        # TenantId == null`, and a host caller's CurrentTenantId is null, so every delete would
        # answer 404 and leave the record behind. Only the users are host-visible.
        cleaner = locals().get("staff") or admin

        for document in documents:
            call("/api/document/%s" % document, token=cleaner, method="DELETE")
        for item in equipment:
            if item:
                call("/api/equipment/%s" % item, token=cleaner, method="DELETE")
        for item in departments:
            if item:
                call("/api/workplace-department/%s" % item, token=cleaner, method="DELETE")
        for item in employees:
            if item:
                call("/api/company-employee/%s" % item, token=cleaner, method="DELETE")
        for item in companies:
            if item:
                call("/api/company/%s" % item, token=cleaner, method="DELETE")
        for item in users:
            call("/api/user/%s" % item, token=admin, method="DELETE")

    print("\n%d kontrolden %d tanesi gecti" % (passed + failed, passed))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
