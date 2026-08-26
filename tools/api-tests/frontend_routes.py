# -*- coding: utf-8 -*-
"""Frontend'in ENDPOINTS sabitindeki her kaynagi canli API'ye karsi surer."""
import io, json, os, re, ssl, sys, urllib.request, urllib.error, urllib.parse

import devcert


CTX = devcert.ssl_context()


def call(path, token=None, form=None):
    data = urllib.parse.urlencode(form).encode() if form else None
    request = urllib.request.Request(
        "https://localhost:7001" + path, data=data,
        headers={"Content-Type": "application/x-www-form-urlencoded"} if form else {})
    if token:
        request.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(request, context=CTX, timeout=60) as response:
            return response.status, response.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8", "replace")
    except Exception as error:                       # noqa: BLE001
        return 0, str(error)


_, body = call("/connect/token", form={
    "grant_type": "password", "username": "admin", "password": "Ensa!2026",
    "scope": "openid profile email roles offline_access ensa"})
token = json.loads(body)["access_token"]

source = io.open("react/ensa-web/src/api/endpoints.ts", encoding="utf-8").read()
block = source[source.index("export const ENDPOINTS"):source.index("} as const")]
resources = re.findall(r"(\w+):\s*'([^']+)'", block)

failures = 0
for name, resource in resources:
    code, body = call("/api/%s?skipCount=0&maxResultCount=1" % resource, token=token)
    ok = code == 200 and '"totalCount"' in body
    failures += 0 if ok else 1
    total = json.loads(body).get("totalCount") if ok else "-"
    print("  %-24s /api/%-22s HTTP %-3s %s  toplam=%s"
          % (name, resource, code, "GECTI" if ok else "KALDI", total))

print("\n%d kaynaktan %d tanesi calisiyor" % (len(resources), len(resources) - failures))
sys.exit(1 if failures else 0)
