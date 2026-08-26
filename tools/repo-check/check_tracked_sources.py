# -*- coding: utf-8 -*-
"""
Derlemeye giren her kaynak dosyasi gercekten depoda mi?

Bu betik bir hatanin ardindan yazildi. `.gitignore` icindeki `documents/` satiri - calisma
zamaninda yuklenen dosyalarin klasoru icin yazilmisti - basinda `/` olmadigi icin agacin HER
seviyesindeki o isimli klasorle eslesti, ve Git Windows'ta varsayilan olarak buyuk/kucuk harf
duyarsiz oldugu icin `src/Ensa.Domain/Documents/`, `src/Ensa.Application/Documents/` ve dort
klasoru daha yuttu. Kirk kaynak dosyasi hic commit'lenmedi.

Hicbir sey uyarmadi. Yerel derleme geciyordu, cunku dosyalar diskteydi; testler geciyordu; gizli
malzeme taramasi temizdi. Hata yalnizca depoyu klonlayan birinde ortaya cikti:

    The type or namespace name 'Documents' does not exist in the namespace 'Ensa.Domain'

Eksik dosya, fazladan dosyadan cok daha sessiz bir hatadir: fazladan dosya listede gorunur,
eksik dosya gorunmez. Bu yuzden fazlaligi degil, EKSIKLIGI olcen bir kontrol gerekiyor.

Betik su uc soruyu sorar:

  1. Diskteki hangi kaynak dosyalari Git tarafindan yok sayiliyor?
  2. Hangileri henuz izlenmiyor (eklenmesi unutulmus)?
  3. Bir kural, olmasi gerekenden daha genis mi esliyor?

Calistirma:

    python tools/repo-check/check_tracked_sources.py
"""
import os
import subprocess
import sys

# Derlemeye ya da pakete giren her sey. Bir uzanti burada yoksa kimse onu korumuyor demektir.
SOURCE_SUFFIXES = (
    ".cs", ".csproj", ".sln", ".props", ".targets",
    ".ts", ".tsx", ".js", ".jsx", ".json", ".css", ".scss", ".html",
    ".resx", ".py", ".md", ".sql",
)

# Uretilen ya da makineye ozel oldugu icin depoda olmamasi GEREKEN yollar. Buradaki her girdi
# bilincli bir karardir; listeye bir sey eklemek "bu dosya kaynak degildir" demektir.
EXPECTED_ABSENT = (
    "/bin/",
    "/obj/",
    "/node_modules/",
    "/__pycache__/",
    "react/ensa-web/dist/",
    "react/ensa-web/tsconfig.tsbuildinfo",
    "src/Ensa.HttpApi.Host/App_Data/",   # calisma zamaninda yuklenen belgeler
    "src/Ensa.HttpApi.Host/Logs/",       # calisma zamani gunlukleri
    "tools/api-tests/ensa-dev-cert.pem", # makineye ozel, disa aktarilan sertifika
    ".claude/",                          # editor yerel ayarlari
    "received_mail.jsonl",               # sahte SMTP sunucusunun ciktisi
)


def git(*arguments):
    result = subprocess.run(
        ("git",) + arguments,
        capture_output=True, text=True, encoding="utf-8", errors="replace")

    if result.returncode != 0:
        print("git %s basarisiz: %s" % (" ".join(arguments), result.stderr.strip()))
        sys.exit(2)

    return [line for line in result.stdout.splitlines() if line.strip()]


def is_source(path):
    return path.endswith(SOURCE_SUFFIXES)


def expected_absent(path):
    return any(marker in path for marker in EXPECTED_ABSENT)


def main():
    if not os.path.isdir(".git"):
        print("Bu bir Git deposu degil; kontrol atlandi.")
        return 0

    tracked = set(git("ls-files"))
    ignored = [p for p in git("ls-files", "--others", "--ignored", "--exclude-standard")]
    untracked = [p for p in git("ls-files", "--others", "--exclude-standard")]

    # Yok sayilan ya da hic eklenmemis KAYNAK dosyalari - beklenenler dusuldukten sonra.
    ignored_sources = sorted(p for p in ignored if is_source(p) and not expected_absent(p))
    untracked_sources = sorted(p for p in untracked if is_source(p) and not expected_absent(p))

    print("=== DEPO BUTUNLUGU ===")
    print("  izlenen dosya            : %d" % len(tracked))
    print("  izlenen kaynak dosyasi   : %d" % sum(1 for p in tracked if is_source(p)))
    print("  YOK SAYILAN kaynak       : %d" % len(ignored_sources))
    print("  EKLENMEMIS kaynak        : %d" % len(untracked_sources))

    if ignored_sources:
        print("\n-- .gitignore bu kaynak dosyalarini yutuyor --")
        for path in ignored_sources[:40]:
            rule = subprocess.run(
                ("git", "check-ignore", "-v", path),
                capture_output=True, text=True, encoding="utf-8", errors="replace")
            detail = rule.stdout.strip().split("\t")[0] if rule.stdout.strip() else "?"
            print("   %-60s <- %s" % (path, detail))
        if len(ignored_sources) > 40:
            print("   ... ve %d tane daha" % (len(ignored_sources) - 40))

    if untracked_sources:
        print("\n-- depoya hic eklenmemis kaynak dosyalari --")
        for path in untracked_sources[:40]:
            print("   %s" % path)
        if len(untracked_sources) > 40:
            print("   ... ve %d tane daha" % (len(untracked_sources) - 40))

    if ignored_sources or untracked_sources:
        print("\nBu dosyalar olmadan klonlanmis bir kopya derlenmez.")
        print("Kural gercekten dogruysa yolu EXPECTED_ABSENT listesine ekleyin;")
        print("degilse .gitignore kalibini bir yola sabitleyin (basina '/' koyun).")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
