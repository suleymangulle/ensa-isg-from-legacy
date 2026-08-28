# Architecture Decision Records

Decisions taken while rewriting the legacy `D:\EnsaProject` system that cannot be inferred from
the code itself.

---

## ADR-001 — ABP.IO layer template, without ABP libraries

**Decision.** The solution mirrors ABP.IO's layered template exactly (`Domain.Shared`, `Domain`,
`Application.Contracts`, `Application`, `EntityFrameworkCore`, `HttpApi`, `HttpApi.Host`,
`DbMigrator` plus four test projects), but no `Volo.Abp.*` package is referenced.

**Why.** ABP's layer separation and dependency direction are proven, but the framework brings a
version/licence dependency and a learning cost. The concepts we actually wanted (`Entity<TKey>`,
`IRepository<,>`, `ICurrentTenant`, `ICurrentUser`, `IDataFilter`, `AuditedEntity`,
`PagedResultDto`, `ICrudAppService`, per-layer DI modules) were reimplemented under
`Ensa.Domain.Common` and `Ensa.Application.Contracts.Common`.

**Consequence.** A developer who knows ABP can navigate this repository; there is no runtime
dependency on it.

---

## ADR-002 — No navigation properties in entities or DTOs

**Decision.** No entity or DTO has a class-typed property. Relationships are `int` / `int?`
foreign keys. Combined reads use `[NotMapped]` **navigation entities** (`{Entity}Navigation`) and
**navigation DTOs** (`{Entity}NavigationDto`), which are never `DbSet`s and never reach
`ModelBuilder`.

**Why.** The product owner required it. Side benefits: lazy-loading N+1 becomes impossible,
serialization cycles disappear, and entities stay pure data carriers.

**Cost.** Referential integrity is expressed through explicit `HasIndex` plus database FK
constraints rather than EF conventions, and joins are written by hand in the repository layer.
A model-level test (`ModelValidationTests`) asserts that no navigation ever creeps back in.

---

## ADR-003 — Tenant discriminator `KurumId` → `TenantId`

**Decision.** The legacy `KurumId` column present on almost every table becomes
`IMultiTenant.TenantId` (`int?`). `TenantId = null` means a **host** record shared by all tenants.
An `Organization` entity was introduced — the legacy system had no tenant table; it stored the
organization as the `Firma_T` row where `FirmaId == KurumId`.

**Implementation.** `EnsaDbContext` installs a global query filter by reflection:
`TenantId == currentTenant.Id || TenantId == null`. `TenantId` is stamped on insert by the
`SaveChanges` interceptor. `ICurrentTenant.Change()` lets host administration screens act on
another tenant.

**Note.** Identity's default **global** unique indexes on `NormalizedUserName` / `NormalizedName`
are wrong in a multi-tenant model; they were replaced with `(TenantId, NormalizedUserName)` and
`(TenantId, NormalizedName)`.

---

## ADR-004 — Central document store

**Decision.** The `byte[] Dosya` + name + type triple, repeated on ~15 legacy tables, is gone.
There is one `Documents.Document` table; owners reference it with `DocumentId`. Polymorphic cases
use `OwnerType` (`DocumentOwnerType` enum) + `OwnerRecordId`.

**Why.** Normalization, plus `Sha256` de-duplication, one place to enforce size/quota, and the
option to move large files out to `StoragePath` on disk.

---

## ADR-005 — Encrypted columns vs. typed columns

**Context.** In legacy `PeriyodikMuayeneFormu_T` over 150 columns were `[EncryptColumn]` and **all
of them were `string`** — including dates, height, weight and blood pressure.

**Decision.** Those fields now use their real types (`DateTime`, `int`, `decimal`). Consequently
`EncryptedStringConverter` (which is `string` → `string`) cannot apply to them.

**Open item — needs a product decision.** Health data is special-category personal data under KVKK.
Three options:
1. **SQL Server TDE** — the whole database is encrypted at rest, application code is unchanged,
   queryability is unaffected. **Recommended.**
2. **Always Encrypted** — per-column, the server never sees plaintext; deterministic mode supports
   equality but not range queries.
3. Reverting numeric fields to `string` to keep application-level encryption — loses type safety
   and queryability. **Not recommended.**

Text fields (`OpinionNotes`, `Recommendations`, `PatientNationalId`, `XmlData`, `SignedData`,
`MedulaPassword`, `ESignatureLicense.License`) remain protected by `EncryptedStringConverter`.

**Note.** `EncryptedStringConverter` is **deterministic** (fixed IV). This is required so that a
unique index and equality predicates work on `NationalId`. The cost is that equal plaintexts
produce equal ciphertexts — the known leak of deterministic encryption. Encryption sits *beside*
access control, not instead of it.

---

## ADR-006 — Repeating column groups normalized into child tables

The legacy habit of spreading one concept across N columns was removed systematically:

| Legacy | Current |
|---|---|
| `Ceza_T`'s 9 amount columns (3 hazard classes × 3 headcount bands) | `PenaltyAmount` (+ per-year tracking, which legacy lacked) |
| `Egitim_T.AzTehlikeliSure/TehlikeliSure/CokTehlikeliSure` | `TrainingDuration` |
| `RiskAnalizRaporu_T`'s `TMK*` (10), `MKO*` (7), `IO*` (7) booleans | three child tables keyed by enum |
| four identical `Risk*Kayit_T` tables | one `RiskAssessmentHistoryRecord` + `RiskHistoryRecordType` |
| `AcilDurumEylemPlani_T`'s 9 free-text columns | `EmergencyPlanSection` |
| the examination form's 23 complaint / 12 physical / 8 lab columns | three child tables + enums |
| `FirmaPersonel_T`'s health columns | `EmployeeHealthInfo` / `EmployeeImmunization` / `EmployeeFamilyHistory` |
| repeating `EskiIs*1/2/3` group | `EmployeeWorkHistory` |
| `YSDRSatirlari_T.AltCalismalarJson` | self-referencing `ParentLineId` hierarchy |
| `Mail_T.BagliDosyalar` (CSV) | `MailAttachment` |
| `FirmaHareket_T.Borc` + `Alacak` | `LedgerEntryType` enum + a single `Amount` |

---

## ADR-007 — Authentication: OpenIddict + ASP.NET Core Identity

**Decision.** The legacy `Kullanici_T.Sifre` mechanism (AES, reversible) is gone. `User :
IdentityUser<int>` uses ASP.NET Core Identity (PBKDF2 hashing, lockout, token providers). The
token server is **OpenIddict 7.x** with `password`, `refresh_token` and `client_credentials`.

**Carrying tenant and permissions.** The access token carries `ensa:tenantId` and one
`ensa:permission` claim per effective permission. `TenantResolutionMiddleware` feeds
`ICurrentTenantAccessor`.

The legacy authorization tables were kept, and `PermissionManager` reproduces the legacy
`YetkiKontrolu.Authorize` algorithm exactly:

1. system administrator → all permissions
2. subscription-plan **gate**
3. organization-type **gate**
4. user-type permissions ∪ permissions granted directly to the user
5. an explicit denial overrides everything
6. restriction allow/deny list

**Critical.** Steps 2 and 3 are *gates*: a permission granted directly to a user is still
ineffective if the organization's plan or type does not allow it.

---

## ADR-008 — `double` → `decimal`

Every monetary field in the legacy system was `double`. In the current model money is
**always `decimal`** (`HasPrecision(18,2)`). Risk scores are `decimal(9,2)`, coordinates
`decimal(9,6)`.

**Why.** `double` is binary floating point and accumulates rounding error across sums — not
acceptable for invoice and cash reconciliation.

---

## ADR-009 — Primary key naming `{Entity}Id` → `Id`

Every entity's primary key is `Id` (from `Entity<TKey>`). Foreign-key names keep the
`{Entity}Id` form (`CompanyId`, `UserId`, `DocumentId`).

**Migration impact.** Moving legacy data requires an explicit column mapping
(`Firma_T.FirmaId` → `Company.Id`).

---

## ADR-010 — Data transformations required when migrating legacy rows

- `MenuDetail_T.PrentMenuDetailId`: root rows `0` → `NULL`
- `MenuItem_T.ConnectedModule`: `-1` → `NULL`
- `Menu_T` + `YeniMenu_T` collapse into a single `Menu` (the latter was unused)
- every string-coded enum (`"AZ TEHLİKELİ"`, `"Satış"`, `"ser-admin"`, …) maps to its numeric enum
  value; case and Turkish-character variants must be normalized during the mapping
- `Kullanici_T.Sifre` is decrypted and re-hashed by Identity; rows that cannot be decrypted are
  flagged `MustChangePassword = true` and pushed through password reset
- `IbysQuery` and `CompanyModule` had no `KurumId` in legacy — derive `TenantId` via `CompanyId`
- derived string columns such as `AyYazi` are not migrated (recomputed from month/year)
- **`Bank` gained `IMultiTenant`.** Legacy had no `KurumId` on it. If migration leaves
  `TenantId = null`, every tenant sees those collection accounts — verify this explicitly.

---

## ADR-011 — Disabling the tenant filter during sign-in

**Problem.** `User` implements `IMultiTenant`, so the global filter is
`TenantId == currentTenant.Id || TenantId == null`. During the `password` grant the user is not yet
authenticated, so `CurrentTenant.Id` is `null` and the filter matches only host users — **no
tenant-bound user could ever sign in.**

**Decision.** In `AuthorizationController`, user lookups (`FindByNameAsync`, `FindByEmailAsync`,
`FindByIdAsync`) run inside `IDataFilter.Disable<IMultiTenant>()`. The filter is off for **that
single lookup only**; the tenant id is then read from the record itself and written to the token as
`ensa:tenantId`. Every subsequent request runs under the normal filter via
`TenantResolutionMiddleware`.

**Alternatives rejected.**
- *Exempting `User` from the tenant filter entirely* — would leak users across tenants everywhere.
- *Resolving the tenant before sign-in from a subdomain or organization code* — safer, but requires
  the user to supply organization context at the login screen; legacy had no such field.

The same pattern appears in `UserRepository.GetPermissionsAsync` and in `OrganizationRepository`'s
host-administration queries. Every use is justified in its XML doc, and inner queries are then
constrained explicitly with `TenantId == id`.

**Warning.** Any new code inside `IDataFilter.Disable<IMultiTenant>()` loses tenant isolation for
that scope. These call sites deserve extra scrutiny in review.

---

## ADR-012 — Concurrency for document numbering

The next document number is taken with one atomic statement:

```sql
UPDATE n SET LastNumber = LastNumber + 1
OUTPUT INSERTED.LastNumber
FROM ensa.NumberSequence AS n WITH (UPDLOCK, HOLDLOCK)
WHERE n.TenantId = @tenantId AND n.CompanyId = @companyId AND n.Type = @type;
```

If no row exists, an `INSERT ... OUTPUT` runs in the same transaction. `HOLDLOCK` takes a key-range
lock on the `(TenantId, CompanyId, Type)` unique index, so the insert-if-missing race cannot create
duplicate counters.

A read-then-increment-then-write pattern is **not acceptable**: two concurrent requests would get
the same number and violate the invoice-number unique constraint.

This raw SQL bypasses the global query filter, so the tenant predicate is written by hand — the
single deliberate, documented exception to the rule in ARCHITECTURE §9.

---

## ADR-013 — Turkish → English rename of the entire codebase

**Decision.** Identifiers, file names, folder names, table names, permission names and error codes
are English. The legacy Turkish domain vocabulary is preserved only as a mapping table in
`CLAUDE.md`.

**How it was done.** A glossary-driven transformer split each PascalCase identifier into tokens and
applied the longest matching translation. Comment prose was deliberately **excluded** from the
mechanical pass (only `cref` / `<c>` references were rewritten) because word-by-word translation of
prose produces half-Turkish sentences; comments were rewritten separately.

**Collisions found and resolved.**
- `KullaniciTuru` and Identity's `KullaniciRol` both mapped to `UserRole` → the domain entity became
  `UserType`, Identity's join table kept `UserRole`.
- `Ortak` would have collided with the existing `Common` folder → it became `Lookups`.
- `Kimlik` was renamed `Membership` rather than `Identity` to avoid ambiguity with
  `Microsoft.AspNetCore.Identity`.
- Turkish identifiers that translate onto C# keywords (`yeni` → `new`, `eski` → `previous`) were
  given non-keyword names.

**Verification.** `dotnet build` at 0 errors / 0 warnings, 39 unit and model tests green, migration
regenerated from scratch, database recreated and seeded, and the API smoke test re-run end to end.

---

## ADR-014 — Localization

**Decision.** User-facing text is resolved from resource files, not from the throw site.

- `Ensa.Domain.Shared\Localization\EnsaResource.resx` — English (fallback)
- `EnsaResource.tr.resx` — Turkish
- **The resource key is the error code** (`Ensa:Company:HeadquarterNotFound`), so no lookup table
  is needed between exceptions and translations.
- `BusinessException` carries an English developer fallback message plus named values added with
  `WithData(name, value)`; `EnsaExceptionFilter` substitutes them into the localized template.
- A missing key degrades to the English fallback rather than to an empty string.

**Culture resolution.** `?culture=en-US` → `Accept-Language` → default `tr-TR`. Supported cultures
are `tr-TR` and `en-US`; anything else silently falls back to the default.

**Frontend.** The SPA uses `react-i18next` with `tr` and `en` bundles and sends its active language
as `Accept-Language`, so server-side messages arrive already translated.

**Rule.** Every new error code must be added to **both** resx files in the same change.

## ADR-015 — Turkish data and identifiers that survived the bulk rename

**Context.** The mechanical rename of ADR-013 translated whitespace-free string literals and
PascalCase tokens. That is correct for identifiers and wrong for two other things, and both
classes of damage were silent — the code still compiled and the tests still passed.

**Damage found.**

1. **Turkish data translated into English.** `InvoiceManager`'s Turkish number-word tables were
   rewritten (`iki` → `two`, `altı` → `childı`, `dokuz` → `nine`, `elli` → `fifty`), so every
   invoice whose total contained a 2, 6, 9 or 50 printed nonsense in the amount-in-words field.
   Six district names were hit the same way (`Şile` → `Şwith`, `Altındağ` → `Childındağ`,
   `Yenişehir` → `Newşehir`, `Altınordu` → `Childınordu`, `Eskişehir` → `Previousşehir`).
2. **Identifiers half-translated, changing their meaning.** `IsVeren` (the employer) became
   `IsProvider`, `AnaAdi` (the mother's name) became `MainName`, `EvAdresi` became `EvAddress`,
   `IsSektor` became `IsSector`, and `sınır` (a limit) became the adjective `nervous` in
   20 files. `weightKg` gained a second unit and became `weightKgKg`.

**Decision.**

- Turkish **data** is never translated. This covers the number-word tables, city and district
  names, `TurkishSearch`'s character map, and every value quoted from the legacy schema. The
  number-word tables carry a comment saying so.
- Legacy references inside `(Legacy: <c>...</c>)` are never translated either — they are the only
  traceability link back to `D:\EnsaProject`, and translating them was already reverted once.
- Boolean members drop the Turkish question suffix and take the `Is` / `Has` / `Show` prefix
  (`SubcontractorMu` → `IsSubcontractor`, `EditableMi` → `IsEditable`).

**Guard.** `BusinessRulesTests.Spells_an_amount_out_in_Turkish` pins the exact Turkish wording for
every digit that was corrupted. The previous test only asserted a non-empty string, which is why
the bug survived; an assertion that cannot fail is not a test.

## ADR-016 — Caller-supplied years are validated at the application boundary

**Context.** `year` is an `int` query parameter, so an omitted value binds to `0`. Repositories
build their range with `new DateTime(year, 1, 1)`, which throws `ArgumentOutOfRangeException`.
Three endpoints (`work-plan/active`, `training-plan/active`, `invoice/next-number`) answered a
missing parameter with HTTP 500.

**Decision.** `EnsaAppService.ValidateCalendarYear` rejects anything outside `1..9999` with the
localized `Ensa:InvalidYear` code, and every entry point that forwards a year to a repository
calls it before the repository. Unvalidated input is a 400, never a 500.

**Verification.** A coverage sweep drives every parameterless GET endpoint twice — once
anonymously, once authenticated — and fails on any 5xx or any endpoint that does not answer 401
without a token. It is what surfaced these three.

## ADR-017 — Invoice numbers come from an atomic counter

**Context.** `GenerateInvoiceNumberAsync` read the highest existing invoice number and added one.
Two concurrent callers read the same maximum and produced the same number; the unique index then
rejected whichever invoice saved second. A `NumberSequence` counter with an atomic
`UPDATE ... OUTPUT INSERTED` implementation already existed for document numbering — and had no
callers at all.

**Decision.** Invoice numbering goes through `INumberSequenceRepository.GetNextNumberAsync`.
The counter row is `(TenantId, ScopeId, "INVOICE-{year}")`, so each office restarts its series
every year, which is what the printed `{office:D2}-{year}-{order:D6}` format already implied.

`NumberSequence.CompanyId` was renamed to `ScopeId`: the column holds the company for document
series and the office for invoice series, and the old name asserted something untrue. The
now-dead `IInvoiceRepository.GetLatestInvoiceNoAsync` was deleted rather than left in the
contract, where it would invite the racy pattern back.

**Verification.** 24 concurrent calls to `/api/invoice/next-number` return 24 distinct numbers
forming an unbroken 1..24 sequence.

**Migrating existing data.** A database that already holds invoices needs its counter rows
seeded from the current maximum per office and year before the first call, otherwise the
sequence restarts at 1 and the unique index rejects the insert.

## ADR-018 — One approval workflow for every plan line

**Context.** Work plan lines and training plan lines run the identical
Draft → SubmittedForApproval → Approved/Rejected machine. It was written twice: once in
`WorkPlanManager`, once inline in `TrainingPlanAppService`. Both copies were correct at the time,
but keeping two hand-maintained copies of one rule in step is not a strategy.

**Decision.** `IPlanApprovalManager` in `Ensa.Domain/Common` owns the transition table and the
field writes, over the `IApprovablePlanLine` interface that both line entities implement. The
caller passes its own resource code, so the localized message stays module-specific while the
behaviour cannot diverge. `WorkPlanManager.ApplyApprovalTransition` delegates to it.

Rejections write to a dedicated `RejectionReason` column on both line entities. Previously the
reason was appended to the author's `Description`, which permanently rewrote user-authored text
and appended again on every re-rejection. Leaving the rejected state clears the reason.

**Guard.** `PlanApprovalTests` pins the allowed edges, the field clearing on resubmission and
approval, and asserts that a work plan line and a training plan line come out identical.

## ADR-019 — Time comes from IClock, never from DateTime.Now

**Context.** `HealthSurveillanceManager`, `WorkPlanManager` and `TrainingPlanningManager` read the
ambient clock directly, and disagreed with each other: two used `DateTime.UtcNow`, one used
`DateTime.Now`. Rules that compare against "today" were therefore untestable and inconsistent.

**Decision.** Every domain service takes `IClock`, as `IncidentManager` already did. Tests inject
`FixedClock`, so a date-dependent rule cannot pass or fail depending on the day it runs.

## ADR-020 — Verification scripts live in the repository

**Decision.** `tools/api-tests/` holds the black-box checks: the Company module end to end, a
sweep of every parameterless `GET` in the live Swagger document (anonymous must be 401,
authenticated must not be 5xx), and a check that every resource the SPA names actually resolves.

They found what unit tests structurally cannot: three endpoints answering 500 for a missing query
parameter, and a frontend page calling a controller that does not exist. Routes are read from
Swagger at run time, so new endpoints join the sweep without anyone remembering to add them.

TLS verification stays on; the development certificate is pinned as the certificate authority and
is never committed.

## ADR-021 — Tenant isolation and encryption are proved, not asserted

**Context.** `ModelValidationTests` checked that a global query filter *exists* on every
tenant-scoped entity. That is a statement about metadata: it cannot tell whether the filter
actually keeps one customer's rows away from another, and in a multi-tenant OHS system that
difference is the difference between a working product and a cross-customer breach. Nothing
exercised the filters against real data.

**Decision.** `TenantIsolationTests` and `EncryptedColumnTests` run against a real LocalDB
database created and dropped per test class. They prove, by writing and reading rows:

- `TenantId` is stamped from the ambient tenant on insert;
- one tenant cannot read another tenant's rows, by id or by list;
- a host row (`TenantId == null`) is visible to every tenant, which is how shared reference data
  works;
- `IDataFilter.Disable<IMultiTenant>()` reveals every tenant's rows **and closes again on
  dispose** — this is the single deliberate hole in the isolation (ADR-011), so it has to be
  scoped;
- disabling the soft-delete filter does not widen the tenant filter — the two are independent;
- a soft delete hides the row but leaves it in the table, flagged and timestamped;
- an encrypted column round-trips, is stored as ciphertext (asserted with raw SQL, not just by
  reading it back through the converter), supports equality lookups, and still enforces its
  unique index — which is what the deterministic AES of ADR-005 buys.

**Rule.** A test that cannot fail is not a test. `Spells_an_amount_out_and_never_returns_empty`
passed happily while invoices printed English digits (ADR-015); an assertion has to be specific
enough to catch the failure it exists for.

## ADR-022 — Authorization is verified separately from authentication

**Decision.** `tools/api-tests/api_authorization.py` creates a user with no roles and no
permissions, signs in as that user, and asserts 403 on protected endpoints — while
`/connect/userinfo`, which needs a session rather than a permission, still answers 200. The probe
user is deleted afterwards.

`api_coverage.py` only proves that an anonymous caller is rejected. A controller that carries
`[Authorize]` without a permission name passes that check and fails this one, which is precisely
the mistake worth catching.

## ADR-023 — Screen modules register themselves

**Context.** The SPA covered seven screens while the API served 39 controllers. Building out the
rest meant several people writing screens at the same time, and three files stood in the way:
`App.tsx` (the router), `Sidebar.tsx` (the menu) and the two shared locale bundles. Every new
screen would have touched all three — the classic contention point, where a merge silently drops
a route or a translation key and the loss shows up as a blank page or raw `some.key` text.

**Decision.** A module owns one folder and registers itself:

```
src/pages/<module>/
  module.tsx            # routes + sidebar entries
  locales/{tr,en}.json  # its own labels
  api.ts                # its DTO types and hooks
```

`src/modules/registry.ts` collects every `module.tsx` with `import.meta.glob`, and
`src/i18n/index.ts` merges every module locale bundle onto the core one. Dropping a folder in adds
its routes and menu entries; nothing shared is edited. The contract is `react/ensa-web/MODULES.md`.

The frontend enum bundle (`src/api/enums.ts`) is **generated** from
`src/Ensa.Domain.Shared/Enums/*.cs` by `tools/gen-enums/gen_enums.py`, because the API serialises
enums as numbers and a hand-copied value is a silent contract break. Generating it immediately
surfaced one: `FitnessForWorkOpinion` had drifted to `FitForWork`/`UnfitForWork` in the SPA while
the backend had renamed them to `Fit`/`Unfit`.

**Result.** 16 modules, 65 routes, 40 sidebar entries.

## ADR-024 — The SPA's API calls and translations are verified, not trusted

**Context.** A screen pointed at an endpoint that does not exist compiles perfectly and fails only
when a user opens it. A missing translation key does the same and shows raw `company.fields.x`.
Both were found by hand during the build-out — which does not scale.

**Decision.** Two checks, run against the live system:

- `tools/api-tests/frontend_calls.py` resolves **every** `http.get/post/put/delete` in the SPA
  source against the live Swagger document — substituting module constants, treating route
  parameters as wildcards — and verifies the path **and** the HTTP method. It currently covers 164
  calls.
- `tools/i18n-check/check_locales.py` verifies every literal `t('...')` key in both languages,
  every **dynamic** enum label (each numeric member of the backend enum must have a label), and
  tr/en parity. It currently covers 2 378 keys per language.

`tools/api-tests/dev_stack.py` completes the set by driving the SPA shell and the API **through
the Vite proxy** — a misconfigured proxy leaves a perfectly working API unreachable, and nothing
else notices.

**What they caught.** Ten wrong endpoint names in the module briefs, including
`medical-examination-form/expiring` (really `/company/{id}/expiring`, and the company is required)
and `corrective-action/complete` (really `/close`). Both checkers had blind spots of their own that
had to be closed: the call scanner skipped nested generics such as `http.get<PagedResult<T>>`,
which hid nearly half the calls (91 → 164), and the locale checker skipped brace-less class
declarations. A checker that silently sees less than it claims is worse than none.

## ADR-025 — Gaps found while wiring the screens

Building the screens exercised the API the way a client does, and that surfaced work the backend
was missing rather than merely wrong:

- **No `AccountController`.** `AccountAppService` was fully implemented but never exposed, so a
  user could not read their own profile or **change their own password** — while the seeder flags
  the administrator to change it on first sign-in. Now `api/account`, authenticated but not
  permission-guarded: a user with no permissions still has to be able to change their password.
- **No way to configure outgoing mail.** `EmailSettings` existed as an entity with an encrypted
  password column and no service, so the mail queue had nothing to send with. Now
  `api/email-settings`; the password is write-only in both directions — never returned, and an
  empty value on update keeps the stored one.
- **`UpdateUserDto.NationalId` erased data.** The national id is an encrypted, uniquely indexed
  identifier that `UserDto` deliberately does not return, so a client editing a phone number sent
  nothing for it and the absolute map wiped it. An omitted national id now means "keep the current
  one". A scan of all 44 update DTOs found no other instance.
- **Three missing lookups** (`organization-types`, `subscription-plans`, `user-types`) forced forms
  to ask for raw numeric identifiers for required fields.
- **List DTOs without a company name** (`ActivityReport`, `YearEndReviewReport`, `Office`) forced
  screens to build an id-to-name map from a capped lookup and fall back to "Company #12". The name
  is now resolved server-side with one batched query per page.
- **17 controllers documented their own route wrongly** (`api/correctiveaction` instead of the
  kebab-case `api/corrective-action` the route transformer actually produces).

## ADR-026 — Document payloads

**Context.** An OHS system is a document system: risk assessment reports, training certificates,
examination forms, equipment inspection records. The `Document` table normalised the
`byte[] + name + type` triple that legacy repeated across dozens of tables — but there was no way
to put bytes into it or get them out. Several screens rendered a disabled download button.

**Decision.** `IDocumentStorage` abstracts where the bytes live; `FileSystemDocumentStorage` in the
host implements it. Payloads at or below 256 KB stay in the row (a logo is not worth a second
storage round trip, and a database backup stays self-contained); larger ones go to
`{root}/{tenant}/{aa}/{bb}/{guid}`, sharded because one directory holding hundreds of thousands of
files is slow on every common file system.

The transfer is `POST api/document/upload` (multipart) and `GET api/document/{id}/content`.

**What the design promises, and what enforces it:**

| Promise | Mechanism |
|---|---|
| A file name can never become a path | The only path key is a generated GUID, validated as one; the name is stripped to its leaf and used solely for `Content-Disposition`. Every resolved path is re-checked to be inside the root. |
| Size and digest cannot be claimed by the client | Both are measured while reading; the declared `Content-Length` is not trusted. |
| An upload cannot fill the disk | The limit is enforced byte by byte as the stream arrives, and a partial write is deleted rather than left looking valid. |
| An uploaded HTML or SVG cannot run in this origin | Every download is an attachment, and executable content types are served as `application/octet-stream`. |
| One tenant cannot read another's files | The metadata row is fetched through the repository's tenant filter before the payload is opened; the storage layer knows nothing about tenants. |
| A failed insert leaves no orphan | The payload is deleted if the row does not commit. |

`tools/api-tests/api_documents.py` proves all six against the running system.

**The token cannot travel on an anchor.** A plain `<a href>` sends no `Authorization` header, and
putting the token in the query string would write it into browser history, proxy logs and the
`Referer` of anything the page loads next. The SPA fetches through the shared axios instance and
hands the result over as an object URL (`src/api/download.ts`).

**Deliberately not done.** A soft-deleted document keeps its payload: the row can be restored, and
a restore that finds its file gone is worse than the disk it saves. Reclaiming bytes belongs to a
sweep over rows deleted beyond recovery — a background job, not part of a request.

## ADR-027 — The outgoing mail queue has a sender

**Context.** `MailAppService` owned a queue and documented its own gap: *"that worker does not
exist yet. Until it is written, mails queued here stay in Queued forever."* With
`api/email-settings` (ADR-025) the account it needs is finally configurable, so the worker could
be written.

**Decision.** `MailDeliveryWorker`, a hosted service, polls the queue, sends through `IMailSender`
and records the outcome. `SmtpMailSender` implements the transport with the framework's
`SmtpClient` — no new dependency, because what this needs is exactly SMTP submission with
STARTTLS and a password; `IMailSender` is the seam if that ever stops being true.

**Why the worker does not use the application service.** That service is written for requests: it
checks permissions and runs inside the caller's tenant. A worker has neither a user to check nor a
single organization to belong to. It therefore goes through the repositories with the tenant filter
disabled — the same deliberate, narrow exception the sign-in path makes (ADR-011) — and resolves
each message's own organization to find the account to send with. Bending the app service to
accommodate a worker would mean weakening the checks that protect it from people.

**Why sending is not part of the request that queues the mail.** Delivery is slow and fails in ways
that need retrying with backoff; doing it inline would hold a database transaction open for the
length of a mail-server timeout. Worse, a request aborted mid-flight would leave a message sent but
unrecorded — and a duplicate notification is worse than a late one.

**Behaviour worth stating.** A missing account is not a delivery failure and does not consume an
attempt: it is logged and skipped, so the retries stay available for a real outage. Three failed
attempts move a message to `Failed`, where a person can see the error rather than a queue retrying
for ever. An attachment whose payload is gone is skipped with a warning instead of failing the
whole message — a notification without its attachment still tells the recipient something.

**Verification.** `tools/api-tests/api_mail.py` drives the chain against `fake_smtp.py` and asserts
the message arrived, with both recipients resolved from the semicolon-separated column. The retry
path was observed too: while the test server was mishandling the `AUTH LOGIN` initial response, the
worker retried three times and moved the message to `Failed`, exactly as designed.

## ADR-028 — Permissions are configurable per staff type, not only per user

**Context.** The permission model has five sources — the system-administrator shortcut, the
subscription-plan gate, the organization-type gate, **staff-type defaults**, and per-user
overrides. `UserTypePermission` carried the fourth and `IPermissionManager` honoured it, but no
endpoint could read or write it. The only writable surface was per user, so configuring what a
workplace physician can do meant granting the same permissions to every physician by hand — 171
permissions against 8 staff types, one user at a time.

**Decision.** `GET`/`PUT api/permission/user-type/{userTypeId}` manage the defaults of a staff
type. The list is absolute, not a delta: what is sent becomes the whole set, the same rule the
per-user endpoint already follows. There is no deny list here — a default is either given to the
type or not, and an exception for one person belongs on that person.

**A permission that would be dropped is refused, not stored.** `PermissionRestrictionMode` can bar
a permission from a staff type; storing it anyway would leave the screen showing something the
effective-permission calculation silently discards. `IsPermissionGrantableAsync` is checked first
and the save is rejected with `Ensa:Permission:NotGrantableToUserType`.

**Verified** end to end: assigning the six `Ensa.MedicalExamination` permissions to the physician
type, reading them back, replacing them with a subset (proving the list is absolute rather than
merged), and confirming another staff type is untouched.

## ADR-029 — Every required foreign key has a lookup

Forms were asking users to type raw numeric identifiers because the entity existed but nothing
listed it. Six lookups were added over two rounds — `organization-types`, `subscription-plans`,
`user-types` (ADR-025), then `payment-methods`, `service-items`, `menu-types`.

The rule this settles: **if a DTO field is a required foreign key, the API owes the client a way
to choose a value.** A `[Range(1, int.MaxValue)]` with no list behind it is not a contract a screen
can honour; it is a number box and a guess.

## ADR-030 — A contract with no endpoint behind it is a defect

`GetEmployeeTrainingProgressListInput` existed in the contracts assembly with no action bound to
it. The screen built against that surface had to make a specialist choose a company and then an
employee before showing anything — while the question the job actually asks is the other way
round: *who has not finished their training.*

`GET api/employee-training-progress` now answers it, filterable by workplace, training,
completion and active state.

**Filtering by workplace needed care.** A progress row carries the employee, not the workplace,
and the architecture allows no navigation property to join through. The first implementation
filtered the page in memory after it had been fetched — which silently reported the size of one
page as the total and broke paging. The company's employees are now resolved first, one extra
query, so the predicate stays in SQL and both the paging and the count stay correct.

**The rule:** an input DTO that nothing consumes is not a placeholder for future work, it is a
promise the API does not keep. Either wire it up or delete it.

## ADR-031 — The menu shows what the user can actually use

**Context.** `AuthContext` exposed a `hasPermission` helper that nothing called. Every signed-in
user saw all forty sidebar entries regardless of what their permissions allowed, and found out by
clicking: the API answered 403, correctly, and the screen showed an error. Authorization worked;
the interface simply did not reflect it.

**Decision.** A `NavEntry` may declare the permission required to see it, and the sidebar drops
what the user does not hold. Thirty-nine of the forty entries now do; the dashboard is deliberately
open, being the landing page for anyone with a session.

**Hiding a link is a courtesy, never a control.** Every endpoint still enforces its own permission
and answers 403 whatever the menu shows — proved by `api_authorization.py`. This change addresses
the opposite failure: a menu that promises thirty screens and refuses twenty-eight.

**The constants are generated, for the same reason the enums are.** These strings are both the
`ensa:permission` claim values in the token and the authorization policy names on the server.
A hand-copied constant that drifts by one character does not fail a build or a test — it hides a
screen from everyone entitled to it, permanently and silently. `tools/gen-enums/gen_permissions.py`
produces `src/api/permissions.ts` from `EnsaPermissions`, and
`tools/api-tests/frontend_permissions.py` gates it: all 171 constants and the 33 the menu relies on
are checked against the catalogue the server seeded. Both currently match exactly.


## ADR-032 — A user belongs to an organization, and somebody has to say which

**Context.** The seeded administrator is a host user on purpose: `TenantId = null`, documented in
`MembershipSeeder` as *"belongs to no tenant; it manages every organization."* That is right. What
was missing is the other half — `CreateUserDto` carried no organization field, so every user the
host administrator created inherited the host context, and `PermissionManager` returns an empty
set at its very first guard for a user with no organization:

```csharp
if (user.TenantId is not int organizationId)
{
    return [];
}
```

Out of the box the product could not produce a single working specialist, physician or customer.
Every such account signed in successfully and was refused by every endpoint — indistinguishable
from having been granted nothing.

**Decision.** `CreateUserDto.TenantId` states the organization the new user joins.

It is honoured **only for a host caller**. A caller inside an organization has the value ignored
and overwritten with their own organization, so a tenant can never place a user in another one.
The organization is verified to exist before the user is created. `UpdateUserDto` deliberately
does not carry the field: moving a user between organizations is not an edit, it is a migration.

The SPA shows the field only where it means something — create mode, host caller — and requires
it there, because the alternative it silently produced was an account nobody could use.

## ADR-033 — The token is built inside the user's own tenant

**Context.** ADR-011 established that the sign-in lookup runs with the tenant filter disabled: no
token has been issued yet, so `CurrentTenant.Id` is `null`, and with the filter on, only host
users could be found. That fix covered finding the user. It did not cover everything that happens
afterwards.

`CreatePrincipalAsync` resolves the user's roles, claims and effective permissions — all from
tenant-scoped tables, all through the global query filter, all still in the host context. For a
host user this is invisible. For a user inside an organization every one of those rows fell
outside the filter, and the access token came out carrying no `ensa:permission` claim at all.

The symptom was exactly the one ADR-032 describes, which is what made it hard to see: two
independent causes producing one indistinguishable failure. `/api/account/permissions` returned
the full set for the same user, because by then the token had established the tenant — the two
sources that are supposed to agree disagreed, and only the token mattered.

**Decision.** `CreatePrincipalAsync` runs inside `ICurrentTenant.Change(user.GetTenantId())`. The
token is built in the tenant the user actually belongs to, which is the same context every later
request runs in.

**Why a tenant switch rather than another filter disable.** Disabling the filter would have worked
and been wrong: it would read every tenant's rows to build one tenant's token. The switch reads
exactly what the user will be allowed to read.

## ADR-034 — The tenant filter separates providers, not their customers

**Context.** With ADR-032 and ADR-033 in place a customer contact could finally sign in and work —
and could then list every company their OHS provider serves, open any of them, and read those
companies' employees. The tenant filter had never claimed otherwise: it separates one provider
from another and says nothing about the customers inside a provider. Nothing else said anything
either. Every client of a provider could read every other client's file.

**Decision.** Company scope is a global query filter, installed by reflection in `EnsaDbContext`
alongside tenancy and soft delete:

- `ICompanyScoped` — the entity carries a `CompanyId` (35 entities: employees, examinations,
  invoices, risk assessments, documents, …). Reached through `EF.Property<int?>`, so both `int`
  and `int?` declarations work.
- `ICompanyRecord` — the entity *is* the workplace, so the scope key is its own `Id`. Only
  `Company`.

The scope key is `ICurrentUser.CompanyId`, carried in a new `ensa:companyId` access-token claim
written from the user record — never from the request. When it is null, which is the case for
every member of the provider's own staff and for every call with no user (sign-in, seeding,
background work), the filter is inert.

**It fails closed, unlike the tenant filter.** A row with a null `TenantId` is shared reference
data and is visible to everyone; a row with a null `CompanyId` is provider-level data and is
hidden from a company-bound user. The asymmetry is deliberate: shared reference data is meant to
be shared, provider-level data is not.

**Why a global filter instead of scoping each app service.** The same reason tenancy is one: a
rule enforced in thirty-seven places is a rule that will be forgotten in the thirty-eighth. The
one place it cannot reach is a repository that writes raw SQL, and there is none.

**Verified by `tools/api-tests/api_company_scope.py`** — twenty checks covering all three ADRs:
the organization binding, the permission claims in the token, and the scope itself. A customer
sees one company and one employee; reading the neighbour's records answers 404, not 403, because
a record the caller may not see should not be confirmed to exist.


## ADR-035 — The menu is generated from the SPA, not written twice

**Context.** The `Menu` module shipped with empty tables. `GET api/menu` returned nothing, so the
menu administration screen listed no rows, and `GET api/menu/my-menu` answered *"No menu is
defined for this layout type"* to every user. A whole legacy module — entities, repository with
its per-user override rules, app service, controller, screen — present in all four layers and
inert in all four.

**Decision.** Seed it, and generate the seed from the SPA's own navigation.

`tools/gen-enums/gen_menu.py` reads every `src/pages/*/module.tsx` nav entry and the merged
English bundle, and writes `src/Ensa.DbMigrator/Seeding/MenuSeedData.cs`. `MenuSeeder` turns that
into one `MenuType`, one `Menu`, 46 `MenuItem` rows (6 section headings + 40 screens) and the
`MenuNode` tree.

**Why generated rather than written.** Writing the menu out by hand creates a second navigation
definition, and two definitions of the same thing drift within a release: a screen gets renamed
in one and not the other, a route moves and the menu keeps the dead link. Neither failure breaks
a build or a test. Generating means they cannot disagree, and
`tools/api-tests/frontend_menu.py` fails if the generator was not re-run.

**Why the sidebar still renders from code.** Navigation must not wait on a round trip, and the
code path is where the permission filter lives (ADR-031). These rows are the other half: the
configurable menu an administrator inspects, and the answer `my-menu` gives — including the
per-user `UserMenuOverride` rows the repository already honoured and nothing could exercise.

**It refreshes rather than preserves.** Unlike the staff-type defaults, an existing row's
generated fields are rewritten every run: a renamed screen must not keep its old label and a
moved one must not keep its dead URL. Rows an administrator added themselves are untouched.

## ADR-036 — The compliance panel had nothing behind it

**Context.** `CompanyComplianceSummary` carries the six figures the company detail screen shows
and the legacy customer portal put on its landing page: employees with no safety training, with
incomplete training, missing a pre-employment examination, equipment overdue for inspection. Its
own XML documentation said the values are *"recomputed periodically by a background job"*.

There was no such job. Nothing in the codebase ever wrote the table. `CompanyNavigation.Warning`
read it, `CompanyNavigationDto.WarningSummary` exposed it, the screen rendered it — and it was
null for every company that ever existed. The panel was not wrong, it was empty, which reads to a
user as *"nothing outstanding"*.

**Decision.** Write the job the entity promised, and put the rules where rules belong.

`ICompanyComplianceCalculator` (domain) defines what the six counts mean. What "missing training"
and "overdue inspection" mean is a statutory question, not a hosting concern; the equipment rule
is deliberately the same predicate `IEquipmentRepository.GetExaminationOverdueAsync` uses, so the
panel and the overdue list cannot disagree. `ComplianceSummaryWorker` (host) only decides when to
ask — every 30 minutes by default, `ComplianceSummary:IntervalMinutes` to change it, `0` to stop.

**A cache miss is computed, not shown empty.** A company created a minute ago has no row yet.
`CompanyAppService.GetWithNavigationAsync` computes and stores one on the first read rather than
leaving the panel blank until the next round. Both paths go through the same calculator, so the
first read and the job can never produce different numbers.

**A round that changes nothing writes nothing.** Rows whose figures did not move are skipped, so
`CalculatedTime` keeps meaning "when this last changed" and a quiet installation costs five
queries per round.

## ADR-037 — The customer portal is the same application

**Context.** The legacy system had a second web application, `MusteriArayuzu`, with ten pages:
sign-in, a dashboard, the company's employees, its workplace departments, its equipment, its
missing trainings, its inspection documents, a profile page and a file download.

It existed because the main application had no row-level scoping. Letting a customer into the
main application meant letting them into every customer's data, so they got their own building
instead. The separate app was a workaround for a filter that did not exist.

**Decision.** No second application. The company scope filter (ADR-034) is that filter, and with
it a customer contact signs into the same SPA and sees the same screens narrowed to their own
workplace.

Every legacy page has a counterpart, and `tools/api-tests/api_customer_portal.py` walks all ten
with a real customer user — 19 checks covering both halves of the claim: that the screen works,
and that it does not reach past the customer's own company.

| Legacy page | Modern counterpart |
|---|---|
| `Login` / `Logout` | `/connect/token`, OpenIddict |
| `Default` | dashboard counters + the compliance panel (ADR-036) |
| `FirmaPersonel` | `GET api/company-employee` |
| `IsyeriBolumleri` | `GET api/workplace-department` |
| `Cihazlar` | `GET api/equipment`, `.../overdue-inspections` |
| `EksikEgitimler` | `GET api/employee-training-progress` |
| `DenetimEvraklari` | `GET api/document` |
| `UserProfil` | `GET api/account/profile`, `POST api/account/change-password` |
| `dosya` | `GET api/document/{id}/content` |

**A finding worth recording.** Writing that test surfaced something about the tenant filter that
was not obvious: a **host administrator cannot see a tenant's rows at all**. The filter reads
`TenantId == CurrentTenantId || TenantId == null`, and a host caller's `CurrentTenantId` is null,
so a row owned by an organization is invisible to them — a host delete answers 404. This is
correct and deliberate: the operator of the platform does not casually read a customer's data,
and `ICurrentTenant.Change(id)` is there for the cases that genuinely need it. It is written down
here because two test scripts assumed the opposite and silently left their records behind.

## ADR-038 — The SPA's interface comes from one component library

**Context.** The SPA's user interface was Bootstrap markup written by hand, screen by screen. A
card was a `div.card` wrapping a `div.card-header` and a `div.card-body`; a status pill was a
`span` carrying a `badge-light-success` class; a table was a `table.table-hover` with its own
loading, empty and error branches. None of that is wrong in a single screen. Across 118 of them
it is 118 chances to get the contrast, the heading level or the `aria-selected` wrong, and no
compiler or test can see the difference between the copy that is right and the copy that drifted.

**Decision.** Every piece of interface comes from `rich-react-component` — the Base layer's
`Card`, `Button`, `Badge`, `Tabs`, `Input`, `Select`, `TextArea`, `CheckBox`, `Alert`, `Spinner`,
`Skeleton`, `Statistic`, `Modal`, `Toast` and the rest. A screen composes those; it does not
rebuild them. `tools/repo-check/check_ui_library.py` fails the build when a page hand-rolls a
card, a button, a badge, a spinner or a tab strip again.

**Three components are reached through a wrapper, not imported.** `DataGrid` renders the words
"Loading…" and "No data", `Pagination` renders "Previous" and "Next", `Modal` labels its close
button "Close" — all English literals, none of them a prop. This product ships Turkish and
English, so those three are imported only by `src/components/DataTable.tsx` and
`src/components/Form.tsx`, which supply the translated text and re-export the props the screens
already use. The same check fails a page that imports them straight from the package: an English
word on a Turkish page is invisible to `check_locales.py`, because the string was never in our
bundle to be missing from it.

**Why a narrow wrapper and not a facade over the whole library.** Wrapping every component would
put a second, private API in front of a library we own, and every screen would then be written
against the copy rather than the thing. The boundary is drawn at exactly one property: a
component is wrapped if it renders words, or if the app must supply an accessibility attribute
the library does not. Everything else — `Card`, `Badge`, `Button`, `Tabs`, `Statistic` — is
imported directly by the screen that uses it. That keeps the wrapper at two files, and the day
the library accepts label props, those two files shrink instead of thirty screens changing.

**What the library still owes us**, recorded so it is not rediscovered: label props for the
strings above; `tabIndex` and `aria-sort` on `DataGrid`'s sortable headers, which today are
mouse-only; an accessible name on `Modal`; an `aria-live` region around the toast stack; and a
way to unpad a `Card` body, which the SPA currently does with the `.ensa-card-flush` rule in
`src/styles/metronic.scss`. The package also declares `react ^18` as a peer while the SPA is on
React 19, which is why `react/ensa-web/.npmrc` sets `legacy-peer-deps=true`; that line is dated
and comes out the moment the peer range widens.

## ADR-039 — The shell follows the component library instead of working around it

**Context.** `rich-react-component` 0.2.0 gave the SPA a `Sidebar` that took a flat `sections`
array, knew nothing about routing, and had no collapsed state. The application filled all three
gaps itself: every entry carried an `onClick` calling `navigate()` instead of an `href`, each
entry carried its own `active` boolean computed from the URL, and "hiding" the menu meant
unmounting the whole component from `MainLayout`. Each of those was the right call against 0.2.0
and each cost something real — a menu of buttons has no middle-click, no open-in-new-tab and no
status-bar preview; several `active` flags can disagree; and an unmounted menu loses which groups
were open. The library's stylesheet was not imported at all, so the `rrc-*` layer it ships had no
effect, and the theme was a constant compiled into SCSS.

**Decision.** Take what 0.3.0 offers rather than keep the workarounds.

- The sidebar is built from the recursive `items` model with one authoritative `activeKey`;
  ancestors are derived by the library, not passed in. Entries carry a real `href`, and
  `renderLink` hands the library React Router's own `Link` — so the destinations are links again
  without the Base layer learning about routing.
- Hiding the menu is now two separate states, because they answer different questions:
  `collapsed` is the desktop rail (the menu is still there, reduced to its icons) and `mobileOpen`
  is the drawer, which only exists at a width that cannot hold the aside. Neither is derived from
  the other, and neither destroys the expansion state.
- `rich-react-component/style.css` is imported, between Bootstrap and this application's
  overrides. The application's own tokens moved out of `metronic.scss` into `ensa.scss` purely so
  that order could be expressed: one file cannot be both before and after the library.
- `AppearanceProvider` owns colour mode, sidebar presentation, sidebar tone and accent scheme,
  persisted under `ensa:appearance`. The accent is registered once as a complete colour scheme in
  `src/styles/appearance.ts`; the library rejects a partial one, which is the point — recolouring
  only the primary hue leaves buttons and menu states behind.

**Why a dark palette is ours to write.** The library ships light tokens and documents `--rrc-*` as
the integration point rather than shipping a second stylesheet. So dark mode is a block in
`src/styles/ensa.scss` that redefines the `--kt-*` names under Bootstrap 5.3's own
`data-bs-theme` attribute. That single attribute is what Bootstrap's components, the library's
components and this application's ~280 inline `var(--kt-gray-*)` styles all read, so one switch
moves all three. It also means the existing ban on hard-coded hex codes is now enforced by
appearance rather than by review: a literal colour stays light on a dark page.

**Why the built-in cell formatters are not used.** 0.3.0's `DataGrid` gained declarative columns —
`field` plus a `format` — which removes a callback from the common case. The formatters behind
`format` render US dollars and the English words "Yes"/"No", and read the browser's locale rather
than the language the user chose. So `@/components/DataTable` keeps the declarative column and
points it at `@/utils/format` through the library's `formatter` escape hatch. The same rule as
ADR-038: the library owns the markup, this repository owns the words.

**Still owed by the library**, unchanged from ADR-038: label props for `Pagination`'s
"Previous"/"Next" and `DataGrid`'s loading text, an `aria-live` region around the toast stack, and
a peer range that admits React 19.
## ADR-040 — The legacy permission tables governed nothing at runtime

**Context.** The legacy database carries a full authorization model: 419 permissions in
`Yetki_T`, 9,640 grants across `KullaniciTypeYetki_T`, `PaketTuruYetki_T` and `KurumTuruYetki_T`,
5,061 scope rows in `YetkiBaglanti_T`, and 362 restrictions in `YetkiKisit_T`. All of it is
migrated (`PermissionStep`). `Businness/Firmalar/YetkiKontrolu.cs` implements a four-gate
algorithm over those tables: package type AND organization type AND (user type OR user) AND not
explicitly denied, with `ser-admin` bypassing everything.

It was natural to read that as the legacy access-control layer and to bind the modern endpoint
gate to it. Before doing so, three facts were checked.

**What the legacy code actually does.**

1. `YetkiKontrolu.Authorize` has exactly one call site in the entire solution, and it is
   commented out. `ENSA_ISG/Attributes/LoginControlAttribute.cs` is an `ActionFilterAttribute`
   whose `OnActionExecuting` override — the whole body — is commented out. The attribute is
   applied to 62 controllers and does nothing.
2. None of the 101 controllers in `ENSA_ISG/Controllers` reference `YetkiKontrolu` at all.
3. The permission tables are read in two places only, and both are configuration screens:
   `Businness/Firmalar/YetkiIslemleri.cs`, reached from `YetkiAyarlamalariController`, and
   `Businness/Firmalar/MenuIslemleri.cs`, reached from `MenuIslemleriController`.

So the four-gate algorithm never ran, and the tables governed nothing at runtime — not the
endpoints and **not the menu either**. `Businness/Menu/MenuIslemleri.cs`, the builder the
dashboard actually calls, never mentions `Yetki_T`: it picks one of the 319 `Menu_T` rows by
matching `(MenuTypeCode, KullaniciType, KurumTuru, PaketTuru)`, drops the items whose
`ConnectedModule` the organization has not licensed, and applies the per-user `KullaniciMenu_T`
overrides. Access control at the controller was a login-session check plus hand-written branches
on `Kullanici_T.PersonelTuru` — `if (Kullanici.PersonelTuru != "Müşteri")` and its siblings
appear throughout `DefaultController`, `FirmaListController`, `DosyaController` and others.

`YetkiBaglanti_T` looks like the missing link and is not. Its `BaglantiType.MenuEleman` value —
a permission attached to a single menu entry — has **zero rows**, and its `BaglantiType.Menu`
value has 46, pointing at `YeniMenu_T`, an unfinished second menu model that only the
administration screen reads.

**The evidence that settles it.** `KullaniciYetki_T`, the per-user grant table and one of the two
halves of the third gate, is **empty** — zero rows. `Kullanici_T.YetkiGrubuId` is null or zero for
every one of the 3,901 users. And `KullaniciTypeYetki_T` has grants for six user types but none
for `Müşteri` (286 users), `Ofis personeli` (6) or the 172 users with no type at all. Under its
own algorithm the legacy system would have refused a customer every screen — including the
customer portal it demonstrably shipped (ADR-037). It did not refuse them, because the algorithm
was not running.

**Decision.** The endpoint permission map (`PermissionEndpoint`, ADR-033) keeps the permissions
designed for it. The migrated legacy permissions are retained as what they are — menu and
visibility configuration — and are not repurposed as an endpoint gate.

Binding the 333 guarded endpoints to legacy page permissions was implemented and measured before
this was understood. It cost the customer portal entirely: `api_customer_portal` fell from 19/19
to 7/19 and `api_company_scope` from 20/20 to 14/20, every failure a customer receiving 403,
because a customer holds no legacy grant and never did. Restoring the seeded map returned both to
green. That measurement is the reason this ADR exists rather than a paragraph of speculation.

**What this means for fidelity.** Any endpoint authorization in this system is new work; the
legacy system had none to be faithful to. The same turns out to be true of the menu — that
configuration was authored but nothing consumed it either. What it does record is the customer's
own decision about which user type, organization type and plan may reach which screen, which is
exactly the question a menu answers. ADR-041 puts it to that use.

## ADR-041 — The migrated permissions decide what the menu shows

**Context.** ADR-040 established that the legacy permission tables — 419 permissions and 9,640
grants across user type, organization type and subscription plan — were authored through an
administration screen and then consumed by nothing. They are the only surviving record of the
customer's own decision about which kind of user may reach which screen, and that is precisely
the question a navigation menu answers.

The modern menu already reproduced the legacy menu's real filter: `Menu` carries `UserTypeCode`,
`OrganizationTypeId` and `SubscriptionPlanId`, `MenuItem.ModuleId` is checked against the
company's licensed modules, and `UserMenuOverride` applies the per-user additions and removals.
What it had no notion of was a permission.

**Decision.** `MenuItem` gains a nullable `PermissionId`. `GET api/menu/my-menu` renders an entry
only when the caller's effective permissions contain it; an entry that names no permission is not
governed and is always rendered. `tools/gen-enums/gen_menu.py` carries the binding into
`MenuSeedData`, taking the legacy page permission where the screen replaced a legacy one — 34 of
the 40 entries — and the permission the SPA declares for the five that had no legacy counterpart
(Mail, Message, Organisations, Parameters, Reference data). The dashboard is governed by nothing,
because every user needs a landing page.

The effective set is resolved in `MenuAppService` through `IPermissionManager` and passed into
the repository rather than looked up there, so the one implementation of the legacy four-gate
algorithm stays in the domain service and a repository does not depend on it.

The result is a menu that differs by user type for the first time, from the customer's own
configuration rather than from ours:

| User type | Entries rendered |
|---|---|
| Organization administrator | 28 |
| Safety specialist | 22 |
| Customer | 16 |

**This is visibility, not access.** The endpoint gate (ADR-033) decides what a request may do and
decides it independently: it reads `PermissionEndpoint`, not this column. The two can therefore
disagree, and one case is worth naming — a customer holds the seeded `Ensa.Invoice` permission
and so may call `GET api/invoice`, while the menu entry is bound to the legacy
`SatisFaturalariController` permission they do not hold, and stays hidden. Hiding an entry the
user could reach is a cosmetic defect; showing one they cannot use is a cosmetic defect too.
Neither is a way in, which is why an unmapped menu entry stays visible while an unmapped endpoint
is refused. `tools/api-tests/api_menu_permissions.py` asserts the independence rather than
papering over it.

**Two places where legacy recorded nothing, and what was done instead.**

*The customer user type has no grants at all.* `KullaniciTypeYetki_T` holds rows for six user
types and none for `Müşteri` — 286 users — because legacy decided customer access with a
hand-written `PersonelTuru == "Müşteri"` branch. Migrating faithfully carries over nothing, and a
customer would have signed in to a navigation bar holding the dashboard and four screens that are
not theirs. `AuthorizationSeeder` grants that user type exactly the six customer-portal screens of
ADR-037 and nothing else. It is configuration, it is written out one line at a time, and it says
so.

*Two of those six screens are explicitly forbidden to them.* `FirmaListController` and
`EgitimKatilimSertifikasiController` carry `PermissionRestrictionMode.OnlySelected` with the
customer absent from the list — the sixth gate drops the grant. Taken literally that removes a
customer's own workplace and their missing trainings, two screens the legacy product served every
day through the branch above. The restriction was authored in the same screen that governed
nothing, so the seeder adds the customer to those two allow lists and leaves the rule intact for
every other user type. A `SelectedExcept` exclusion is reported instead of edited: a deny list
naming the customer is a deliberate act, not an unfinished one.
