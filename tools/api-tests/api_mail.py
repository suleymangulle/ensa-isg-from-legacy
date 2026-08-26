# -*- coding: utf-8 -*-
"""
Posta kuyrugu ve arka plan gondericisi.

Kuyruga alinan bir posta kendiliginden gitmez: onu gonderen bir arka plan iscisi vardir. Bu
betik o zincirin tamamini surer - hesap tanimla, posta olustur, kuyruga al, iscinin gondermesini
bekle - ve mesajin SMTP sunucusuna gercekten ulastigini dogrular.

Onkosul: sahte SMTP sunucusu ayakta olmali.

    python tools/api-tests/fake_smtp.py received_mail.jsonl

Iscinin yoklama araligi varsayilan 30 saniyedir; testi hizlandirmak icin API'yi
MailDelivery__PollSeconds=5 ile baslatin.
"""
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

import devcert

BASE = "https://localhost:7001"
CTX = devcert.ssl_context()
RECEIVED = sys.argv[1] if len(sys.argv) > 1 else "received_mail.jsonl"


def call(path, token=None, form=None, body=None, method=None):
    data, hdr = None, {}
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        hdr["Content-Type"] = "application/x-www-form-urlencoded"
    if body is not None:
        data = json.dumps(body).encode()
        hdr["Content-Type"] = "application/json"
    request = urllib.request.Request(BASE + path, data=data, headers=hdr, method=method)
    if token:
        request.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(request, context=CTX, timeout=60) as response:
            return response.status, response.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")


def received():
    if not os.path.exists(RECEIVED):
        return []
    with open(RECEIVED, encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def main():
    _, body = call("/connect/token", form={
        "grant_type": "password", "username": "admin", "password": "Ensa!2026",
        "scope": "openid profile email roles offline_access ensa"})
    token = json.loads(body)["access_token"]

    failures = 0

    def check(label, ok, detail=""):
        nonlocal failures
        failures += 0 if ok else 1
        print("  [%s] %-44s %s" % ("GECTI" if ok else "KALDI", label, detail))

    # Hesap: sahte SMTP sunucusu
    code, _ = call("/api/email-settings", token=token, method="PUT", body={
        "email": "isg@ensa.local", "password": "irrelevant",
        "pop3Server": "127.0.0.1", "smtpServer": "127.0.0.1",
        "port": 2525, "sslUse": False, "isActive": True})
    check("SMTP hesabi tanimlandi", code == 200, "HTTP %s" % code)

    before = len(received())

    code, body = call("/api/mail", token=token, body={
        "sender": "isg@ensa.local",
        "recipient": "uzman@firma.local;hekim@firma.local",
        "topic": "Periyodik muayene hatirlatmasi",
        "content": "Sayin yetkili, periyodik muayene tarihi yaklasiyor.",
        "contentFormat": 0, "mailPriority": 2, "mailType": 0})
    mail_id = json.loads(body).get("id") if code == 200 else None
    check("posta olusturuldu", code == 200, "HTTP %s id=%s" % (code, mail_id))

    code, body = call("/api/mail/%s/queue" % mail_id, token=token, method="POST")
    status = json.loads(body).get("mailStatus") if code == 200 else None
    check("kuyruga alindi", code == 200 and status == 1, "HTTP %s status=%s" % (code, status))

    # Isci en gec 5 saniyede bir yokluyor.
    deadline = time.time() + 45
    delivered = None
    while time.time() < deadline:
        time.sleep(2)
        code, body = call("/api/mail/%s" % mail_id, token=token)
        if code == 200 and json.loads(body).get("mailStatus") == 2:
            delivered = json.loads(body)
            break

    check("isci gonderdi (status=Sent)", delivered is not None,
          "attemptCount=%s submissionDate=%s" % (
              (delivered or {}).get("attemptCount"), (delivered or {}).get("submissionDate")))

    arrived = received()[before:]
    check("SMTP sunucusu mesaji aldi", len(arrived) == 1, "%d mesaj" % len(arrived))

    if arrived:
        message = arrived[0]
        check("iki alici da cozuldu", len(message["recipients"]) == 2,
              ", ".join(message["recipients"]))
        check("konu iletildi", "Periyodik" in message["raw"].replace("=\n", ""),
              message["raw"].split("\n")[0][:50])
        check("yuksek oncelik basligi var", "X-Priority" in message["raw"] or "Importance" in message["raw"],
              "oncelik basligi")

    # Temizlik
    call("/api/mail/%s" % mail_id, token=token, method="DELETE")
    call("/api/email-settings", token=token, method="DELETE")

    total = 7
    print("\n%d kontrolden %d tanesi gecti" % (total, total - failures))
    return 1 if failures else 0


sys.exit(main())
