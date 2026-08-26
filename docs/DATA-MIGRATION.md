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
