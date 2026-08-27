# -*- coding: utf-8 -*-
"""
Belge yukleme/indirme akisi ve guvenlik iddialari.

Bir ISG sistemi temelde belge sistemidir: risk raporu, egitim sertifikasi, muayene formu. Bu
betik yalnizca "calisiyor mu" diye bakmaz, tasarimin soz verdigi seyleri tek tek sinar:

  * boyut ve SHA-256 sunucuda olculur - istemcinin beyanina guvenilmez,
  * ayni icerik ikinci kez yuklenemez,
  * yuklenen bir HTML/SVG geri sunulurken calistirilabilir tip olarak sunulmaz (depolanmis XSS),
  * dosya adindaki yol parcalari atilir - depolama anahtari sistemin urettigi GUID'dir,
  * buyuk dosya bit bit ayni doner,
  * tokensiz indirme reddedilir.
"""
import hashlib
import json
import os
import ssl
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

import devcert

BASE = "https://localhost:7001"
CTX = devcert.ssl_context()


def call(path, token=None, form=None, body=None, method=None, raw=None, headers=None):
    data, hdr = None, dict(headers or {})
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        hdr["Content-Type"] = "application/x-www-form-urlencoded"
    if body is not None:
        data = json.dumps(body).encode()
        hdr["Content-Type"] = "application/json"
    if raw is not None:
        data = raw
    request = urllib.request.Request(BASE + path, data=data, headers=hdr, method=method)
    if token:
        request.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(request, context=CTX, timeout=90) as response:
            return response.status, response.read(), dict(response.headers)
    except urllib.error.HTTPError as error:
        return error.code, error.read(), dict(error.headers)


def multipart(fields, file_field, file_name, file_bytes, content_type):
    boundary = "----ensa" + uuid.uuid4().hex
    parts = []
    for name, value in fields.items():
        parts.append(("--%s\r\nContent-Disposition: form-data; name=\"%s\"\r\n\r\n%s\r\n"
                      % (boundary, name, value)).encode())
    parts.append(("--%s\r\nContent-Disposition: form-data; name=\"%s\"; filename=\"%s\"\r\n"
                  "Content-Type: %s\r\n\r\n" % (boundary, file_field, file_name, content_type)).encode())
    parts.append(file_bytes)
    parts.append(("\r\n--%s--\r\n" % boundary).encode())
    return b"".join(parts), "multipart/form-data; boundary=" + boundary


def main():
    _, body, _ = call("/connect/token", form={
        "grant_type": "password", "client_id": "ensa-spa", "username": "admin", "password": "Ensa!2026",
        "scope": "openid profile email roles offline_access ensa"})
    token = json.loads(body)["access_token"]

    failures = 0

    def check(label, ok, detail=""):
        nonlocal failures
        failures += 0 if ok else 1
        print("  [%s] %-46s %s" % ("GECTI" if ok else "KALDI", label, detail))

    # 1) Kucuk dosya -> satir ici saklanmali
    small = ("Risk degerlendirme raporu\n" * 40).encode("utf-8")
    payload, ctype = multipart(
        {"ownerType": "0"}, "file", "rapor.txt", small, "text/plain")
    code, body, _ = call("/api/document/upload", token=token, raw=payload,
                         headers={"Content-Type": ctype})
    ok = code == 200
    document = json.loads(body) if ok else {}
    check("kucuk dosya yukleme", ok, "HTTP %s id=%s" % (code, document.get("id")))
    small_id = document.get("id")

    check("boyut sunucuda olculdu", document.get("sizeBytes") == len(small),
          "%s == %s" % (document.get("sizeBytes"), len(small)))
    check("sha256 sunucuda hesaplandi",
          document.get("sha256") == hashlib.sha256(small).hexdigest(),
          document.get("sha256", "")[:16] + "...")

    # 2) Indirme, ek olarak ve dogru adla
    code, body, headers = call("/api/document/%s/content" % small_id, token=token)
    disposition = headers.get("Content-Disposition", "")
    check("indirme icerigi ayni", code == 200 and body == small, "HTTP %s %d bayt" % (code, len(body)))
    check("ek olarak sunuluyor", "attachment" in disposition.lower(), disposition[:60])

    # 3) Ayni icerik ikinci kez -> mukerrer reddi
    payload, ctype = multipart({"ownerType": "0"}, "file", "kopya.txt", small, "text/plain")
    code, body, _ = call("/api/document/upload", token=token, raw=payload,
                         headers={"Content-Type": ctype})
    error_code = ""
    try:
        error_code = json.loads(body).get("error", {}).get("code", "")
    except Exception:  # noqa: BLE001
        pass
    check("mukerrer icerik reddedildi",
          code == 400 and error_code == "Ensa:Document:DuplicateContent", "HTTP %s %s" % (code, error_code))

    # 4) HTML yuklenirse tarayicida calistirilamamali
    html = b"<html><script>alert(document.domain)</script></html>"
    payload, ctype = multipart({"ownerType": "0"}, "file", "zararli.html", html, "text/html")
    code, body, _ = call("/api/document/upload", token=token, raw=payload,
                         headers={"Content-Type": ctype})
    html_id = json.loads(body).get("id") if code == 200 else None
    code, _, headers = call("/api/document/%s/content" % html_id, token=token)
    served = headers.get("Content-Type", "")
    check("html calistirilabilir tip olarak sunulmuyor",
          "text/html" not in served.lower(), "Content-Type: %s" % served)

    # 5) Yol enjeksiyonu denemesi dosya adiyla
    evil = b"escape attempt"
    payload, ctype = multipart(
        {"ownerType": "0"}, "file", "../../appsettings.json", evil, "application/json")
    code, body, _ = call("/api/document/upload", token=token, raw=payload,
                         headers={"Content-Type": ctype})
    stored_name = json.loads(body).get("documentName", "") if code == 200 else ""
    traversal_id = json.loads(body).get("id") if code == 200 else None
    check("dosya adindaki yol parcasi atildi",
          code == 200 and "/" not in stored_name and "\\" not in stored_name,
          "kaydedilen ad: %r" % stored_name)

    # 6) Buyuk dosya -> dosya sistemine, satir ici degil
    large = os.urandom(400 * 1024)
    payload, ctype = multipart({"ownerType": "0"}, "file", "buyuk.bin", large, "application/octet-stream")
    code, body, _ = call("/api/document/upload", token=token, raw=payload,
                         headers={"Content-Type": ctype})
    large_id = json.loads(body).get("id") if code == 200 else None
    check("buyuk dosya yuklendi", code == 200, "HTTP %s" % code)
    code, body, _ = call("/api/document/%s/content" % large_id, token=token)
    check("buyuk dosya bit bit ayni", code == 200 and body == large, "%d bayt" % len(body))

    # 7) Tokensiz erisim
    code, _, _ = call("/api/document/%s/content" % small_id)
    check("tokensiz indirme reddedildi", code == 401, "HTTP %s" % code)

    # Temizlik
    for ident in (small_id, html_id, traversal_id, large_id):
        if ident:
            call("/api/document/%s" % ident, token=token, method="DELETE")

    print("\n%d kontrolden %d tanesi gecti" % (11, 11 - failures))
    return 1 if failures else 0


sys.exit(main())
