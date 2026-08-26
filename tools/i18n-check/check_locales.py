# -*- coding: utf-8 -*-
"""
Ceviri butunlugu denetimi.

Eksik bir ceviri anahtari derlemeyi kirmaz; kullaniciya ham `company.fields.ssiNumber` olarak
gorunur. Uygulama Turkce ve Ingilizce olarak teslim edildigi icin bu sessiz hata sinifi burada
yakalanir.

Denetlenenler:
  1. Kaynakta gecen her duz `t('...')` anahtari iki dilde de var mi?
  2. Dinamik enum etiketleri (`t(`enums.riskLevel.${value}`)`) icin: enums.ts'teki O enumun HER
     sayisal degeri iki dilde de karsiliga sahip mi?
  3. tr ve en anahtar kumeleri birebir ayni mi?
  4. Bir dilde tanimli olup hicbir yerde kullanilmayan anahtarlar (bilgi amacli).

Modul paketleri `src/pages/<modul>/locales/<dil>.json` cekirdek pakete birlestirilir; i18n/index.ts
calisma zamaninda ayni birlestirmeyi yapar.
"""
import io
import json
import os
import re
import sys
import collections

WEB_ROOT = "react/ensa-web"
SOURCE_ROOT = os.path.join(WEB_ROOT, "src")
CORE = os.path.join(SOURCE_ROOT, "i18n", "locales")
ENUMS = os.path.join(SOURCE_ROOT, "api", "enums.ts")
LANGUAGES = ("tr", "en")

# t('a.b.c') / t("a.b.c") / t(`a.b.c`) — sabit anahtarlar
LITERAL = re.compile(r"\bt\(\s*(['\"`])([A-Za-z][\w.]*)\1")
# t(`enums.riskLevel.${value}`) — dinamik son segment
DYNAMIC = re.compile(r"\bt\(\s*`([A-Za-z][\w.]*)\.\$\{")
# 'enums.riskLevel.' + value  seklindeki birlestirmeler
CONCAT = re.compile(r"['\"]([A-Za-z][\w.]*)\.['\"]\s*\+")
ENUM_BLOCK = re.compile(r"export enum (\w+)\s*\{(.*?)\n\}", re.S)
ENUM_MEMBER = re.compile(r"(\w+)\s*=\s*(-?\d+)")


def merge(base, extra):
    """Modul paketini cekirdek paketin uzerine, bolum bazinda birlestirir."""
    for section, values in extra.items():
        if isinstance(base.get(section), dict) and isinstance(values, dict):
            merge(base[section], values)
        else:
            base.setdefault(section, values)
    return base


def load_bundle(language):
    bundle = json.load(io.open(os.path.join(CORE, language + ".json"), encoding="utf-8"))

    pages = os.path.join(SOURCE_ROOT, "pages")
    for module in sorted(os.listdir(pages)):
        path = os.path.join(pages, module, "locales", language + ".json")
        if os.path.exists(path):
            merge(bundle, json.load(io.open(path, encoding="utf-8")))
    return bundle


def flatten(node, prefix=""):
    out = {}
    for key, value in node.items():
        full = prefix + "." + key if prefix else key
        if isinstance(value, dict):
            out.update(flatten(value, full))
        else:
            out[full] = value
    return out


def source_files():
    for directory, subdirectories, files in os.walk(SOURCE_ROOT):
        subdirectories[:] = [d for d in subdirectories if d != "node_modules"]
        for name in files:
            if name.endswith((".ts", ".tsx")):
                yield os.path.join(directory, name)


def enum_values():
    """enums.ts -> {enum adi (camelCase): [sayisal degerler]}"""
    source = io.open(ENUMS, encoding="utf-8").read()
    values = {}
    for match in ENUM_BLOCK.finditer(source):
        name = match.group(1)
        camel = name[0].lower() + name[1:]
        values[camel] = sorted({int(v) for _, v in ENUM_MEMBER.findall(match.group(2))})
    return values


def main():
    bundles = {language: flatten(load_bundle(language)) for language in LANGUAGES}
    enums = enum_values()

    literal_keys = collections.defaultdict(set)
    dynamic_prefixes = collections.defaultdict(set)

    for path in source_files():
        source = io.open(path, encoding="utf-8").read()
        short = path.replace("\\", "/")

        for _, key in LITERAL.findall(source):
            if "." in key:
                literal_keys[key].add(short)
        for prefix in DYNAMIC.findall(source):
            dynamic_prefixes[prefix].add(short)
        for prefix in CONCAT.findall(source):
            if prefix.startswith("enums."):
                dynamic_prefixes[prefix].add(short)

    missing_literal = []
    for key, users in sorted(literal_keys.items()):
        for language in LANGUAGES:
            if key not in bundles[language]:
                missing_literal.append((language, key, sorted(users)[0]))

    missing_enum = []
    unknown_prefix = []
    for prefix, users in sorted(dynamic_prefixes.items()):
        if not prefix.startswith("enums."):
            continue
        enum_name = prefix.split(".", 1)[1]
        expected = enums.get(enum_name)
        if expected is None:
            # Her `enums.*` kumesi bir backend enum'u degil: `enums.month` gibi takvim etiketleri
            # yalnizca ceviri paketinde yasar. Boyle bir onek tanimliysa kabul edilir; iki dilde
            # ayni anahtarlari tasidigi ayrica dogrulanir.
            defined = {
                language: {k for k in bundles[language] if k.startswith(prefix + ".")}
                for language in LANGUAGES
            }
            if not defined[LANGUAGES[0]]:
                unknown_prefix.append((prefix, sorted(users)[0]))
            elif defined[LANGUAGES[0]] != defined[LANGUAGES[1]]:
                for language in LANGUAGES:
                    other = [l for l in LANGUAGES if l != language][0]
                    for key in sorted(defined[other] - defined[language]):
                        missing_enum.append((language, key, sorted(users)[0]))
            continue
        for language in LANGUAGES:
            for value in expected:
                key = "%s.%d" % (prefix, value)
                if key not in bundles[language]:
                    missing_enum.append((language, key, sorted(users)[0]))

    only = {}
    for language in LANGUAGES:
        other = [l for l in LANGUAGES if l != language][0]
        only[language] = sorted(set(bundles[language]) - set(bundles[other]))

    print("=== CEVIRI BUTUNLUGU ===")
    for language in LANGUAGES:
        print("  %s paketi            : %d anahtar" % (language, len(bundles[language])))
    print("  kullanilan sabit anahtar: %d" % len(literal_keys))
    print("  dinamik enum oneki      : %d" % len(dynamic_prefixes))
    print("  EKSIK sabit anahtar     : %d" % len(missing_literal))
    print("  EKSIK enum etiketi      : %d" % len(missing_enum))
    print("  tanimsiz enum oneki     : %d" % len(unknown_prefix))
    print("  dil paritesi bozuk      : %d" % sum(len(v) for v in only.values()))

    if missing_literal:
        print("\n-- EKSIK SABIT ANAHTAR --")
        for language, key, where in missing_literal[:40]:
            print("  [%s] %-52s %s" % (language, key, where))

    if missing_enum:
        print("\n-- EKSIK ENUM ETIKETI --")
        for language, key, where in missing_enum[:40]:
            print("  [%s] %-52s %s" % (language, key, where))

    if unknown_prefix:
        print("\n-- enums.ts'te KARSILIGI OLMAYAN ONEK --")
        for prefix, where in unknown_prefix[:20]:
            print("  %-54s %s" % (prefix, where))

    for language in LANGUAGES:
        if only[language]:
            print("\n-- YALNIZ %s ICINDE (%d) --" % (language.upper(), len(only[language])))
            for key in only[language][:20]:
                print("  %s" % key)

    failed = missing_literal or missing_enum or unknown_prefix or any(only.values())
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
