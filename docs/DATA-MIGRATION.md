# Data migration — legacy `DemoOsgbDb` into the rebuilt schema

The legacy database holds **169 tables and roughly 40 million rows**: 32,509 companies, 275,323
employees, 1.7 million visits, 334,794 e-prescriptions, 29,703 invoices. This document records
what is being carried across, what is not, and why — the decisions, not the code.

The tool is `src/Ensa.DataMigrator`.

```
dotnet run --project src/Ensa.DataMigrator -- --list
dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv --dry-run
dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv
dotnet run --project src/Ensa.DataMigrator -- --confirm EnsaDbDEv --step locations
```

## Four rules the tool is built on

**1. Name the destination out loud.** `--confirm EnsaDbDEv` is not ceremony. The development and
production databases differ by three characters, sit on the same server and answer to the same
credentials. Without a match between what the caller names and what the configuration resolves to,
nothing runs. A data migration is not something you undo.

**2. Every step is re-runnable.** A migration of this size is not one command that works or fails;
it is run, inspected, corrected and run again. `migration.IdMap` remembers which legacy row became
which modern row, so a second pass recognises its own output instead of inserting twins. Without
it the only safe re-run is one that starts by emptying the destination.

**3. Reconcile, do not duplicate.** The destination is not empty — `ReferenceSeeder` has already
put the province list, the permission catalogue and the organization types there. Inserting the
legacy rows on top would produce two Ankaras and leave every company pointing at whichever won.
Where the modern system seeds a catalogue, that catalogue is authoritative and the migration
records which legacy id refers to which seeded row.

**4. Counting rows proves nothing.** "29,024 written" proves 29,024 inserts succeeded, not that the
right values landed. The `verify` step joins both databases through the id map and compares the
actual values under a **binary** collation — both databases are `Turkish_CI_AS`, under which `Ş`
and `ş` compare equal, and a check that cannot see that difference cannot see a mangled character
either.

### What `verify` judges, and how

A migrated row got its value one of two ways, and the id map records which:

| Resolution | Meaning | Rule applied |
|---|---|---|
| `I` | the migration inserted it from a legacy row | must be **byte-identical** to that row |
| `M` | it already existed; the migration only recorded the reference | value comes from the seed and is *expected* to differ |

The flag is **sticky**: a row this migration once inserted stays `I` even when a later run finds it
already present. It records where the row came from, not the outcome of the most recent pass.

Without that distinction the first verification run failed 894 correct rows — the province the
catalogue spells *Hakkâri* and the legacy data spells *Hakkari*, and 893 districts like it. A check
that cries wolf is a check nobody reads.

## Progress

| Step | Legacy source | Destination | Status |
|---|---|---|---|
| `locations` | `Sehir_T`, `Ilce_T`, `Mahalle_T` | `City`, `District`, `Neighborhood` | **done, verified** |
| `tenancy` | `Firma_T` (`Kurum=1`), `Ofisler_T`, `Kullanici_T` | `Organization`, `Office`, `User` | **done, verified** |
| `companies` | `Firma_T`, `IsyeriBolum_T`, `FirmaPersonel_T` | `Company`, `WorkplaceDepartment`, `CompanyEmployee` | **done, verified** |
| `operations` | `Cihaz_T`, `FirmaIlgilenen_T` | `Equipment`, `AssignedSpecialist` | **done, verified** |

### `locations` — what actually happened

```
read 31,043   written 29,902   skipped 1,141
  cities:         81 matched the seed, 0 inserted
  districts:   1,060 matched, 878 inserted
  neighbourhoods: 29,024 inserted

verify: districts 878/878 byte-identical
        neighbourhoods 29,022/29,024 byte-identical, 2 trimmed of stray CR/LF
```

Three findings worth keeping:

- **The circumflex.** The seeded catalogue spells the province *Hakkâri*; the legacy data spells it
  *Hakkari*. On a plain comparison the migration created an 82nd province and hung one province's
  districts off it. Place-name matching now folds circumflexed vowels, alongside the dotted/dotless
  *i* that a Turkish dataset needs anyway.
- **1,938 legacy districts became 1,108.** The legacy table carries about 830 duplicate rows for the
  same province and district name. All of them map onto the one modern row, so nothing is lost and
  nothing points at a twin.
- **Two neighbourhood names contained a stray CR/LF.** `.NET`'s `Trim()` removed it; SQL's
  `LTRIM/RTRIM` does not, which is why the verification reports those two separately rather than as
  failures. The migration cleaned the data rather than losing it.

### `tenancy` — what actually happened

```
read 5,874   written 5,874
  organizations: 1,040
  offices:         956   (14 demoted from head office)
  users:         3,878   (1,399 user names derived, 606 duplicate national ids dropped)

verify: national ids 2,827/2,865 read back as 11 digits, 0 still ciphertext
```

**The legacy database encrypts some columns, and this nearly went unnoticed.** `Kullanici_T`'s
`TCKimlikNo` (3,468 rows), `MedulaKullanici`, `MedulaSifre` and the whole of
`PeriyodikMuayeneFormu_T` hold ciphertext that reads like ordinary text. The first run carried it
across as-is and encrypted it a second time — unreadable by anything, and nothing objects. It
surfaced only because the doubly-encrypted values no longer fitted the destination column and the
truncation report said so.

`LegacyCrypt` now reads them: Rijndael with a **256-bit block** (not AES — .NET cannot do this at
all, hence BouncyCastle), CBC, PKCS7, PBKDF2 over a fixed salt. Two of the first 200 sampled values
decrypted into *more ciphertext*: the legacy application encrypted some rows twice, so decryption
repeats until it stops finding cipher. `--probe-crypt` proves the key before anything is written,
and `verify` reads every migrated identity number back through the model, which is the only check
that can tell a healthy value from a doubly-encrypted one.

**`FirmaPersonel_T.TCKimlikNo` is not encrypted** — 0 of 275,323 rows — so the largest table is
unaffected. That was checked against the data rather than assumed from the legacy attributes, which
turned out to be misleading.

**Passwords are not migrated.** `Kullanici_T.Sifre` is reversibly encrypted, not hashed. There is
nothing to convert into a PBKDF2 hash, and converting would be wrong anyway: credentials stored in
a form somebody can decrypt should be treated as already exposed. Every migrated user arrives with
`MustChangePassword` and no usable password. **Operationally this means no migrated user can sign
in until their password is reset** — that needs planning before the production run.

### Other decisions this step forced

| Finding | Decision |
|---|---|
| An organization's "authorised person's telephone" holds an e-mail address, too long for the column | Truncate, and report every shortened column by name and count. One bad row must not stop 1,039 good ones, and quiet truncation is worse than either. |
| Two organizations flag several offices as head office; the schema allows one | The earliest keeps the flag, the rest are carried across as ordinary offices. 14 demoted, reported. |
| The same national id appears on several accounts of one organization | The first keeps it; later accounts are written without it. The account is real, the identifier is ambiguous, and an ambiguous statutory identifier is worse than an absent one. 606 dropped. |
| `Kullanici_T` has no user name — the legacy app signed people in by e-mail, but 600 accounts have none and 208 addresses are shared | E-mail when free, else e-mail with the legacy id appended, else `legacy.{id}`. Every fallback keeps the id visible so the account traces back. 1,399 derived. |
| Deduplicating user names with an ordinal comparison failed 23 seconds in | The unique index lives in a `Turkish_CI_AS` database, where `I` and `ı` are the same letter. Deduplication now folds the way the collation compares. |
| The step wrote the id map once at the end; a mid-run failure left 200 organizations with nothing pointing at them, and the next run collided | The map is written with the chunk that produced it. A run that dies halfway leaves a prefix the next run recognises. |

### Assumptions on the record

- Legacy `KurumTuru` → `OSGB`→`OSGB`, `Bireysel`→`BIREYSEL`, **`Kurumsal`→`ISGB`** (a corporate
  customer running its own in-house unit is what an İSGB is), `ensa`→`OSGB` (the vendor's own row).
- Legacy `PaketTuru` → `pro`→`PROFESYONEL`, `demo`→`DEMO`, `startup`→`BASLANGIC`, **`ensa`→`KURUMSAL`**
  (the vendor's unrestricted plan has no counterpart; it maps to the widest one that exists).
- 15 organizations have neither field set and fall back to `OSGB` / `DEMO`.

### `companies` — what actually happened

```
read 338,801   written 326,802
  organization company records:     990
  companies:                     31,393   (1,208 tenant references repaired)
  departments:                   30,874   (duplicates merged)
  employees:                    263,523
  links: 2,693 branches to headquarters, 3,511 group companies, 1,227 users to their company

verify: CompanyEmployee national ids  238,153/247,625 read back as 11 digits, 0 still ciphertext
```

A second run writes nothing.

### Two product defects the data found

The rebuilt schema had sized three columns from what the fields are called rather than from what
they hold. The migration's truncation report is what said so, and they are fixed in the product
(`WidenSsiNumberAndIbysCodeLists`) rather than worked around here:

| Column | Was | Longest real value |
|---|---|---|
| `Company.SsiNumber` | 32 | **37** — an SSI registration number identifies the workplace to the state; shortening it produces a number belonging to nobody |
| `CompanyEmployee.WorkMethodCode` | 32 | 82 |
| `CompanyEmployee.WorkEnvironmentCode` | 32 | 417 |

### Decisions this step forced

| Finding | Decision |
|---|---|
| **1,260 companies name a `KurumId` that is not an organization.** Dropping them costs 31,556 employees | For 1,208 the row named is an ordinary company whose *own* `KurumId* is a real organization — the reference went one level short, and is followed **one hop only**. Two hops is a guess, and a company placed in the wrong organization is worse than one left behind: the tenant filter would show one provider another provider's client. The remaining 52 point at a row that does not exist. |
| **2,706 employees belong to a `Kurum=1` row** — the organization's own staff | `Company.IsOrganizationRecord` exists for exactly this. Each organization now also has a company record, and 990 were created. Without it those employees were orphans. |
| **1,462 identity numbers are longer than eleven characters** | Dropped, not truncated. Shortening one produces eleven digits that look valid and belong to somebody else. |
| Duplicate SSI number within an organization | First keeps it, the rest are written without it — a registration number pointing at two workplaces identifies neither. |
| Duplicate department name within a company | **Merged**, not nulled: a department has nothing but its name, so both legacy ids point at the one modern row. An employee referencing either then lands on the same department. |
| A `LatLng` field parsing as latitude 11122 | Out of range is not a coordinate needing rounding; it means nothing and is dropped. |
| 11,800 employees point at a company that does not exist in the legacy database | Unrecoverable. Reported, not hidden. |

### Three mistakes of mine, and what they cost

- **Positional column reads.** `KurumId` is the 52nd column and was read as the 53rd, which is a
  `bit` — the run died on a type mismatch. Had the neighbour been another `int`, every one of
  31,469 companies would have taken a wrong value in silence. The step now reads **by name**.
- **The rewrite that fixed it inherited the same mistake.** The link stage builds its SQL inline
  rather than in a `const string sql` block, so it fell outside the rewrite's reach and got the
  employee query's column names. It failed loudly this time: a missing *name* throws, where a wrong
  *position* returns a plausible number.
- **The duplicate check did not know what earlier runs had written.** It was built from scratch each
  time, so the resumed run — the normal case at 263,523 rows — collided on the unique index. The
  company and user stages seed theirs from the destination; this one did not, and the difference
  only appears on the second run.

### Why the second pass is set-based

Back-filling `HeadquarterCompanyId`, `GroupCorporateId` and `User.CompanyId` first attached one
entity per row and called `SaveChanges`: about 30,000 round trips across a wide-area connection,
and EF treats a row that does not match as a concurrency failure and abandons the step. For
back-filling a foreign key neither is right — a row that is not there is a row to leave alone. A
join against a `VALUES` list does it in batches and reports what it touched. Raw SQL is safe there
because these are plain integer keys with no converter behind them.

### `operations` — what actually happened

```
read 117,805   written 113,384
  equipment:             53,480
  assigned specialists:  59,904   (4,138 repeat assignments collapsed)
```

Clean on the first attempt — the earlier steps had already found the classes of problem this one
would have hit.

| Finding | Decision |
|---|---|
| The same person assigned to the same workplace in the same capacity, recorded again each renewal | One assignment; the repeat legacy ids point at it, so anything referring to them still resolves. 4,138 collapsed. |
| Two legacy equipment categories against six in the rebuilt enum | Mapped one-to-one and nothing invented. A lifting appliance filed as `tesisat-techizat` stays installation equipment until somebody reclassifies it: guessing would put a machine under inspection rules that do not apply to it. |
| `Cihaz_T.PeriyotId` points at a catalogue that is not migrated yet | Left null rather than carried across as a number that would land on an unrelated row. |

**Worth telling the customer: the equipment inspection dates are almost entirely absent in the
source.** `SonrakiMuayeneTarihi` is empty in all 53,607 legacy rows and `MuayeneTarihi` is filled in
842. The migration is faithful — it copied what is there — but the overdue-inspection screen will
show nothing until that data is entered. This is a gap in the legacy records, not in the migration.

## Scope

### Carried across

Reference catalogues, tenancy, the core business records and the operational history: companies and
their employees, departments, equipment, visits, work and training plans, risk assessments,
corrective actions, field observations, incidents, emergency plans, medical examinations,
e-prescriptions, invoices, cash movements and documents.

### Deliberately not carried across

| Legacy table | Rows | Why not |
|---|---|---|
| `PersonelLoglamasi_T` | 19,819,018 | Page-level tracking of remote training sessions. It records how the old training player was used, not what anyone was trained in — the training results themselves are in `PersonelEgitimIlerlemeDurum_T`, which is migrated. Half the database for none of the meaning. |
| `Log_T` | 7,720,637 | The old application's own audit trail, in its own format. The new system writes its own; importing another application's log would leave one table whose entries nothing can interpret. |
| `Firma_T_NCE`, `OLDCOMPANIES`, `BazalFirmaTablosu`, `deneme`, `TemGosterAlan` | 31,000+ | Backup copies and scratch tables left in the schema. |

Together these account for **27.5 of the 40 million rows**. Excluding them is what makes the rest
tractable; each exclusion is listed here so it is a decision on the record rather than an omission.

## Where the credentials live

Both connection strings carry a password and this repository is public, so they live only in
`src/Ensa.HttpApi.Host/appsettings.Development.local.json`, which `.gitignore` excludes:

```json
{
  "ConnectionStrings": {
    "Default": "…EnsaDbDEv…",
    "Legacy":  "…DemoOsgbDb…"
  }
}
```

## Production

Not yet. Everything above targets **`EnsaDbDEv`**. The production run happens when the development
run is complete and inspected, against a database whose name has to be named on the command line
like any other.
