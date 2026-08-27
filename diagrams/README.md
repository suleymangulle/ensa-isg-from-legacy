# Database diagrams

Generated from the live schema, not drawn by hand:

```
python tools/gen-diagram/gen_schema_diagram.py
```

The tool reads its connection string from the same file the application does
(`src/Ensa.HttpApi.Host/appsettings.Development.local.json`, git-ignored), or from
`--connection` / the `ENSA_CONNECTION` environment variable. Re-run it after every migration;
the diagrams are output, and reviewing them is how a schema change gets a second look.

| File | What it shows |
|---|---|
| `ensa-database.svg` | All 188 tables with every column, grouped into the 16 modules, with all inferred relationships drawn. 2362 × 9522 — open it in a browser and zoom. |
| `ensa-modules.svg` | One card per module and the traffic between them. The overview to read first. |
| `modules/<module>.svg` | One module at readable size, plus the outside tables it points at, drawn faded. |
| `NASIL-OKUNUR.md` | Turkish reader's guide: what the markers mean and what the tooltips say. |

## Turkish hover notes

Every label drawn on the page is English, because the labels are real table and column names.
The explanations are Turkish, and they appear on hover -- **687 tooltips** on the full
diagram, **2252** across all eighteen files, written for the people who commissioned the
migration. See [NASIL-OKUNUR.md](NASIL-OKUNUR.md) for the reader's
guide in Turkish.

Hovering a table gives what a row is, why the table has the shape it has, its legacy table, its
scope, its size and how many relationships touch it. The *why* is the part worth having: it
records where the migration reshaped the legacy data -- a flat column group turned into rows, a
table created from nothing, a tenancy call -- which is exactly what a reader cannot recover from
the picture.

The prose lives in `tools/gen-diagram/table_notes_tr.py`, one entry per table. The generator
refuses to run if a table has no note, so the two cannot drift apart. Legacy table names are not
kept there: they are read from each entity's own XML documentation at generation time.

## How to read a table box

```
┌────────────────────────────────┐
│ CompanyEmployee          T  C  │  ← module colour; T = TenantId, C = CompanyId
├────────────────────────────────┤
│ ◆ Id                      int  │  ← primary key
│ ▸ AssignedDepartmentId    int  │  ← foreign key, drawn as an edge
│ ▹ OwnerRecordId           int  │  ← polymorphic: no fixed target table
│   FirstName·        nvarchar(64)│  ← the trailing · means nullable
├────────────────────────────────┤
│ created · modified · soft-delete│  263 523 rows
└────────────────────────────────┘
```

Every box carries a tooltip with its module, row count and column count; every polymorphic
column carries one naming the discriminator that decides its target.

## Where the relationships come from

The entities have no navigation properties (ADR: *no navigation properties in entities or
DTOs*), so the schema barely declares any foreign keys. The tool resolves each `<Something>Id`
column from three sources, in descending order of authority:

1. **The nine constraints the database actually declares** — Identity and OpenIddict brought
   their own, and where one exists it is the truth.
2. **The XML documentation on the property**, for columns whose name does not say where they
   point: `Incident.DepartmentId` documents itself as `FK → WorkplaceDepartment.Id`. These are
   listed one by one in `EXPLICIT_REFERENCES`, read out of the entity source and never guessed.
3. **The naming rule** — `CityId` → `City`; then by longest suffix, so `PhysicianUserId` and
   `ApproverUserId` both resolve to `User`, and `LogoDocumentId` to `Document`.

That yields **275 relationships**. Eleven more columns are genuinely polymorphic — a sibling
discriminator decides which table they point at — and are marked `▹` with no edge, because a
single edge would be a lie. Twenty-one `…Id` columns resolve to nothing on purpose: eight are
citizens' identity numbers, two are OpenIddict string identifiers, and the rest point at
tables that do not exist in this schema.

## What is left out

Audit columns (`CreationTime`, `CreatorId`, `LastModificationTime`, `LastModifierId`,
`DeletionTime`, `DeleterId`, `IsDeleted`) are identical on 173 tables. Drawing them 173 times
would add roughly 1 200 rows of noise, so each box summarises them in its footer instead.
