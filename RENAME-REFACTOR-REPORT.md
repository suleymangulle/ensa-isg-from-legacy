# Rename Refactor Report

**Date:** 2026-08-29
**Branch:** `DATA-AKTARIM-ANALYZE`
**Scope:** backend/database naming refactor only. No behaviour change, no schema redesign, no
frontend edits.

> **Revision, 2026-08-29 — corrected after an independent review of the implementation.** Two
> claims in the first draft were wrong and are fixed below: `FieldFitter.Fit` does **not** fail
> loudly on an unknown column ([Needs Review #8](#needs-review)), and the `ensa.[User]` carry
> columns are **not** part of the current schema ([Needs Review #7](#needs-review)). The
> verification section now also records an EF test run that failed, and a
> [pre-merge checklist](#recommendation-before-merge) was added. Treat this revision as the
> current statement of what was done and what is still open.
>
> **2026-08-30 — the checklist was executed as far as it safely could be.** The migration was
> applied, rolled back and re-applied on a throwaway LocalDB database with representative data;
> results are in [Pre-merge verification run](#pre-merge-verification-run--2026-08-30). No shared
> database was modified.

---

## Initial Worktree Status

`git status --short` at the start printed **nothing** — the worktree was clean.

The previous session's work (the `SAGLAMOSGB` company-scope analysis and fix) had already been
committed as `947d331 devam edecek`, so **no pre-existing user changes were at risk and nothing was
reverted, deleted or overwritten**. Every file listed under [Files Changed](#files-changed) was
modified by this refactor alone.

---

## Applied Renames

All entity properties are mapped by convention (`grep HasColumnName` over `Configurations/` returns
nothing), so every entity-level rename is also a database column rename.

| Old name | New name | Main affected files | DB column? | Migration |
|---|---|---|---|---|
| `RiskAssessmentReport.WorkplaceTelefonu` | `WorkplacePhoneNumber` | `Risks/RiskAssessmentReport.cs`, `RiskAssessmentReportConfiguration.cs`, `RiskAssessmentReportDtos.cs`, `RiskStep.cs` | yes | RenameColumn |
| `RiskAssessmentReport.MachinesVeEquipments` | `MachineryAndEquipment` | same as above | yes | RenameColumn |
| `Incident.IsPerDate` | `ReturnToWorkDate` | `Risks/Incident.cs`, `IncidentManager.cs`, `IncidentDtos.cs` | yes | RenameColumn |
| `UserMedulaCredential.BranchCode` | `MedicalSpecialtyCode` | `Membership/UserMedulaCredential.cs`, `UserMedulaCredentialConfiguration.cs`, `UserDtos.cs`, `TenancyStep.cs`, `UserColumnClassifyStep.cs` | yes | RenameColumn |
| `Company.TaxTaxOffice` | `TaxOffice` | `Companies/Company.cs`, `CompanyConfiguration.cs`, `CompanyDtos.cs`, `CompanyStep.cs` | yes | RenameColumn |
| `Organization.TaxTaxOffice` | `TaxOffice` | `Tenancy/Organization.cs`, `OrganizationConfiguration.cs`, `OrganizationDtos.cs`, `TenancyStep.cs` | yes | RenameColumn |
| `PenaltySurvey.TaxTaxOffice` | `TaxOffice` | `Finance/PenaltySurvey.cs`, `PenaltySurveyConfiguration.cs`, `PenaltyDtos.cs`, `CommercialStep.cs` | yes | RenameColumn |
| `Company.OrganizationTypeVerified` | `IsHazardClassVerified` | `Company.cs`, `CompanyManager.cs` (incl. `cref`) | yes | RenameColumn |
| `Company.SolutionPartner` | `IsSolutionPartner` | `Company.cs` | yes | RenameColumn |
| `Company.PasswordSent` | `AreEmployeePasswordsSent` | `Company.cs`, `CompanyStep.cs` | yes | RenameColumn |
| `Company.QuoteVatIncluded` | `IsQuoteVatIncluded` | `Company.cs`, `CompanyStep.cs` | yes | RenameColumn |
| `Company.UserLimit` | `IsDistanceLearningUserLimitEnabled` | `Company.cs` | yes | RenameColumn |
| `Company.VisitSpecialist` | `SpecialistVisitMinutes` | `Company.cs`, `CompanyStep.cs` | yes | RenameColumn |
| `Company.VisitPhysician` | `PhysicianVisitMinutes` | `Company.cs`, `CompanyStep.cs` | yes | RenameColumn |
| `Company.InvoiceAmountKh` | `UnofficialInvoiceAmount` | `Company.cs`, `CompanyConfiguration.cs`, `CompanyStep.cs` | yes | RenameColumn |
| `Company.GrContractAmount` | `GroupContractAmount` | same as above | yes | RenameColumn |
| `Company.PayableDigit` | `ExpectedPaymentAmount` | same as above | yes | RenameColumn |
| `Visit.Completed` | `IsCompleted` | `Communication/Visit.cs`, `VisitDtos.cs`, `VisitAppService.cs`, `CommunicationAutoMapperProfile.cs`, `VisitStep.cs` | yes | RenameColumn |
| `WorkPlan.Transferred` | `IsTransferred` | `Plans/WorkPlan.cs`, `WorkPlanConfiguration.cs`, `WorkPlanDtos.cs`, `WorkPlanAppService.cs`, `PlanStep.cs` | yes | RenameColumn + RenameIndex |
| `TrainingPlan.Transferred` | `IsTransferred` | `Trainings/TrainingPlan.cs`, `TrainingPlanConfiguration.cs`, `TrainingPlanDtos.cs`, `TrainingPlanAppService.cs` | yes | RenameColumn + RenameIndex |
| `OrganizationContract.Paid` | `IsPaid` | `Tenancy/OrganizationContract.cs`, `OrganizationNavigationDto.cs`, `CommercialStep.cs` | yes | RenameColumn |
| `ProspectOrganization.Paid` | `IsPaid` | `Tenancy/ProspectOrganization.cs`, `CommercialStep.cs` | yes | RenameColumn |
| `ProspectOrganization.MailSent` | `IsMailSent` | same as above | yes | RenameColumn |
| `ProspectOrganization.PhysicianExists` | `HasPhysician` | same as above | yes | RenameColumn |
| `Office.HeadquarterOffice` | `IsHeadquarterOffice` | `Tenancy/Office.cs`, `OfficeConfiguration.cs`, `OfficeDtos.cs`, `MyOfficeDtos.cs`, `OfficeAppService.cs`, `AccountAppService.cs`, `OfficeRepository.cs`, `OrganizationRepository.cs`, `TenancyStep.cs`, `MembershipSeeder.cs`, `OfficeAccessTests.cs` | yes | RenameColumn + DropIndex/CreateIndex |
| `CashRegister.HeadquarterCashRegister` | `IsHeadquarterCashRegister` | `Finance/CashRegister.cs`, `CashRegisterConfiguration.cs`, `CashRegisterDtos.cs`, `CashRegisterAppService.cs`, `CashRegisterRepository.cs`, `FinanceStep.cs` | yes | RenameColumn + RenameIndex |
| `SystemSetting.Encrypted` | `IsEncrypted` | `Lookups/SystemSetting.cs`, `LookupExtrasStep.cs` | yes | RenameColumn |
| `TreeNode.MainItem` | `IsMainItem` | `Lookups/TreeNode.cs`, `LookupExtrasStep.cs` | yes | RenameColumn |
| `WorkplaceDepartment.Deletable` | `IsDeletable` | `Companies/WorkplaceDepartment.cs`, `WorkplaceDepartmentDtos.cs`, `WorkplaceDepartmentAppService.cs`, `CompanyStep.cs` | yes | RenameColumn |
| `Equipment.Deletable` | `IsDeletable` | `Risks/Equipment.cs`, `EquipmentDtos.cs`, `EquipmentAppService.cs`, `OperationsStep.cs` | yes | RenameColumn |
| `UserPermission.Authorized` | `IsAuthorized` | `Membership/UserPermission.cs`, `PermissionRepository.cs`, `PermissionAppService.cs`, `IPermissionRepository.cs`, `PermissionManager.cs` | yes | RenameColumn |
| `UserProfile.ContractApproved` | `IsContractApproved` | `Membership/UserProfile.cs`, `ProfileDto.cs`, `UserDtos.cs`, `UserAppService.cs`, `TenancyStep.cs`, `UserColumnClassifyStep.cs` | yes | RenameColumn |
| `EmailSettings.SslUse` | `UseSsl` | `Communication/EmailSettings.cs`, `EmailSettingsDtos.cs`, `EmailSettingsAppService.cs`, `SmtpMailSender.cs`, `LookupExtrasStep.cs` | yes | RenameColumn |
| `YearEndReviewLine.PersonVeTitle` | `PersonAndTitle` | `Reports/YearEndReviewLine.cs`, `YearEndReviewLineConfiguration.cs`, `ReportDtos.cs`, `ReportStep.cs` | yes | RenameColumn |
| `YearEndReviewLine.ResultVeComment` | `ResultAndComment` | same as above | yes | RenameColumn |
| `OhsReport.TotalMonthlyFazlaOvertimeDuration` | `TotalMonthlyOvertimeDuration` | `Reports/OhsReport.cs`, `ReportDtos.cs`, `ReportStep.cs` | yes | RenameColumn |

**36 column renames, 3 index renames, 1 index drop/create.** DTO-only occurrences
(`EmployeeTrainingProgressDtos.Completed` to `IsCompleted`) carry no column.

### Deliberately not renamed

Per the out-of-scope list, or because the identifier means something else:

- `Company.BranchNo` / `BranchName` / `BranchContact` / `BranchContactGsm`, `Office`, `OfficeId`,
  `HeadquarterCompanyId`, `WorkplaceType.Branch`, `Bank.BranchName` — untouched.
- `PlanLineStatus.Completed` and `CompanyCheckStatus.Completed` enum members and all their usages —
  an enum member, not a boolean flag.
- `OrganizationNavigation.HeadquarterOffice` (`Office?`) and
  `OrganizationNavigationDto.HeadquarterOffice` (`LookupDto?`) — these are the head office
  *reference*, not a boolean; only the flag they read (`o.IsHeadquarterOffice`) was renamed.
- Legacy source column names in migrator SQL: `Bit(reader, "Deletable")`,
  `SELECT ... Deletable ...` and the `(Legacy: <c>Deletable</c>)` XML docs — all of these name
  columns of the legacy database, verified to exist in the read-only source at `D:\EnsaProject`.
- `u.BranchCode` / `u.ContractApproved` in `UserColumnClassifyStep.cs:48,59` and
  `UserSplitStep.cs:515`. **These are not columns of the current schema.** They were dropped from
  `ensa.[User]` by `20260827185256_DropMovedUserColumns` (`:44-47`, `:59-62`); the step that reads
  them is documented to run *before* that drop (`UserColumnClassifyStep.cs:7-21` — "Run it
  immediately before the migration that drops anything"), so it addresses the **pre-drop schema**.
  The resulting asymmetry is deliberate and correct: the source side keeps `u.BranchCode` /
  `u.ContractApproved`, while the destination side follows the rename to
  `m.MedicalSpecialtyCode` / `p.IsContractApproved`.

---

## Migration

- **Created:** yes — `20260829195006_RenameLegacyLeftoverColumns` under
  `src/Ensa.EntityFrameworkCore/Migrations/`, plus its `.Designer.cs`. The model snapshot was
  regenerated; `dotnet ef migrations has-pending-model-changes` reports
  *"No changes have been made to the model since the last migration."*
- **Operations:** 36 `RenameColumn` + 3 `RenameIndex` in `Up()`, and the exact inverse in `Down()`
  (verified programmatically: every `Down` pair is the reverse of an `Up` pair, no duplicate
  sources).
- **No `DropColumn`, no `AddColumn`, no `AlterColumn`, no raw `Sql`.** No data is moved or lost.
- **One drop/add, and it is an index, not a column:** `IX_Office_TenantId_HeadquarterOffice` is
  dropped and recreated as `IX_Office_TenantId_IsHeadquarterOffice`, because its filter predicate
  (`[HeadquarterOffice] = 1 AND [IsDeleted] = 0`) names the renamed column. A filtered index holds
  no data of its own and is rebuilt from the table. `Down()` restores the old name and predicate.

### Blocker found and handled — review this closely

The scaffolder's first output was **wrong and would have corrupted data**: within a table EF pairs
dropped and added columns positionally, not by identity, which produced

```
PasswordSent             -> IsHazardClassVerified
OrganizationTypeVerified -> AreEmployeePasswordsSent
GrContractAmount         -> ExpectedPaymentAmount
InvoiceAmountKh          -> GroupContractAmount
PayableDigit             -> UnofficialInvoiceAmount
MailSent                 -> HasPhysician        (ProspectOrganization)
Paid                     -> IsMailSent
PhysicianExists          -> IsPaid
```

Each of those swaps two columns' values. **The new migration file only** was rewritten (never an old
one) from the authoritative property-level mapping, so each old column now maps to the column it
belongs to; the reasoning is recorded in the migration's own XML doc. The `.Designer.cs` and the
model snapshot are EF-generated and untouched.

---

## Files Changed

99 paths: 96 modified, 3 new (the migration, its designer file, and this report).

| Area | Files |
|---|---|
| `src/Ensa.Domain` | 28 |
| `src/Ensa.Application.Contracts` | 19 |
| `src/Ensa.EntityFrameworkCore` | 17 (+2 new migration files) |
| `src/Ensa.Application` | 17 |
| `src/Ensa.DataMigrator` | 11 |
| `src/Ensa.DbMigrator` | 1 |
| `src/Ensa.HttpApi.Host` | 1 |
| `test/Ensa.EntityFrameworkCore.Tests` | 1 |
| `tools/api-tests` | 2 |
| `docs` | 1 (`DATA-MIGRATION.md`, two column references) |

**No frontend files were changed.**
`git status --short | grep -c "react/\|public/\|resources/"` returns `0`. No UI text, page or style
was touched.

`git diff --stat` totals **304 insertions / 304 deletions** — line-for-line replacements only, no
structural edits. (An earlier pass had added a UTF-8 BOM to 73 files that had none; that was
detected against `HEAD` and reverted, so the diff carries no byte-order-mark noise.)

The two `tools/api-tests` edits are backend verification scripts that assert on the JSON contract:
`api_office_switch.py` (`headquarterOffice` to `isHeadquarterOffice` in the office DTO shape
assertion) and `api_mail.py` (`sslUse` to `useSsl` in the SMTP settings payload). Without them those
suites would fail against the renamed contract.

---

## Verification

| Check | Result |
|---|---|
| `dotnet build` | **Build succeeded — 0 warnings, 0 errors** |
| `dotnet test test\Ensa.EntityFrameworkCore.Tests\Ensa.EntityFrameworkCore.Tests.csproj --no-restore` | **61 passed, 0 failed** |
| `dotnet test --no-restore` (full) | **141 passed, 0 failed** in 4 of 5 runs (DataMigrator 18, Application 10, Domain 52, EntityFrameworkCore 61) — see the flakiness note below |
| `dotnet ef migrations has-pending-model-changes` | *No changes have been made to the model since the last migration* |
| `git diff --check` | exit 0, no whitespace errors |
| Migration op inventory (whole file, `Up` + `Down`) | `RenameColumn` 72, `RenameIndex` 6, `DropIndex` 2, `CreateIndex` 2; **zero** `DropColumn` / `AddColumn` / `AlterColumn` / `Sql` |
| Migration pairs vs. model snapshot | all 36 `(table, newName)` pairs resolve to a declared property on that entity; no old name survives in the snapshot; `Down` is the exact inverse of `Up` |
| Leftover-name scan over `src/`, `test/` (excluding `Migrations/`) | only the deliberate keeps listed above |

**EF test flakiness (observed once, unexplained).** One full-suite run reported **7 failures** in
`Ensa.EntityFrameworkCore.Tests` (54/61, 47 s); the four other full runs and every targeted run of
that project were green (61/61, 11–15 s). Failure detail was not captured. That suite runs against a
real LocalDB with `databaseCreate: true` while `dotnet test` executes assemblies in parallel, so
database contention is the likeliest cause rather than anything in this refactor — but it is not
proven, and "141 passed" should not be read as fully deterministic.

**Not run:** the migration was **not applied to any database**, and no API-level
(`tools/api-tests/*.py`) run was performed, because those need the schema change applied to the
shared `EnsaDbDEv` instance — a live-schema decision that was not part of this task.

### Independently confirmed safe

Re-verified after this report's first draft, against the tree rather than against the draft:

- **Data-loss safety.** The migration performs rename operations only. No `DropColumn`,
  `AddColumn`, `AlterColumn` or raw `Sql` anywhere in the file, and `Down()` reverses `Up()` pair
  for pair.
- **The Office filtered-index rebuild is justified.** `EnsaDbContextModelSnapshot.cs:9817-9820`
  shows the index as `HasIndex("TenantId").IsUnique()` with
  `HasDatabaseName("IX_Office_TenantId_IsHeadquarterOffice")` and
  `HasFilter("[IsHeadquarterOffice] = 1 AND [IsDeleted] = 0")`. Both the explicit name and the
  filter predicate changed, and a filtered predicate cannot be altered in place — so drop/create is
  the correct expression. It rebuilds an index on a ~1k-row table and touches no row data.
- **Property name equals column name.** `HasColumnName` appears nowhere under
  `src/Ensa.EntityFrameworkCore/Configurations/`, so the migration had to cover every renamed
  property — and it does.
- **Nothing else depends on the old column names.** No `FromSql` / `ExecuteSql` in any repository,
  and no views, computed columns or check constraints exist in the model, so `sp_rename` leaves no
  stale dependent object behind.
- **Legacy source names are genuinely legacy.** `Deletable`, `IsPaid`, `IsMailSent`, `IsDoctor`,
  `Sifreli` and `MainTreeItem` were each found in the read-only legacy source at `D:\EnsaProject`,
  confirming the reader keys were correctly left alone while their assignment targets were renamed.

---

## Needs Review

1. **HIGH — the API JSON contract changed.** `src/Ensa.HttpApi/EnsaHttpApiModule.cs:96` sets
   `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` and no DTO carries `[JsonPropertyName]`, so
   each of the 16 DTO-level renames changes the wire name one-for-one, on responses **and** on
   request binding, and the failure mode is silent rather than an error: a reader gets a missing
   field, a writer's value is dropped.

   Because DTO property names changed and System.Text.Json uses camelCase, any existing frontend or
   external API consumer that still expects the old JSON names will need a coordinated update or a
   temporary compatibility strategy. **Frontend impact is inferred from the API contract change;
   frontend files were not reviewed as part of this scope.**

   On compatibility: a temporary `[JsonPropertyName("<old name>")]` on the **read** DTOs would hold
   the old contract open if the backend has to ship before its consumers; putting it on input DTOs
   would re-freeze the old vocabulary for writers and defeat the refactor. If one coordinated
   release is possible, prefer no attributes at all. The renamed wire names are the 16 pairs listed
   in [Applied Renames](#applied-renames), camelCased.
2. **The corrected migration pairing is the highest-value thing to review.** If the scaffolder's
   original output had been kept, `Company` and `ProspectOrganization` flags and amounts would have
   been silently swapped. Worth an independent read of `Up()` against the entity definitions.
3. **`OrganizationNavigation.HeadquarterOffice` / `OrganizationNavigationDto.HeadquarterOffice` kept
   their names** — they hold an `Office` / `LookupDto`, not a flag, so the boolean standard does not
   apply. If the intent was "every `HeadquarterOffice` identifier", this is the one place the rule
   was not followed.
4. **`Company.UserLimit` to `IsDistanceLearningUserLimitEnabled`**: the property is `bool?`, so it
   has three states and the new name reads binary. Behaviour is unchanged; the naming may deserve a
   second look.
5. **`Company.PayableDigit` to `ExpectedPaymentAmount`** and **`InvoiceAmountKh` to
   `UnofficialInvoiceAmount`**: renamed exactly as specified, but both readings come from the
   existing XML docs ("Amount expected to be paid according to the ledger", "Off-the-books
   (unofficial) invoice amount") rather than from verified business rules.
6. **`docs/IDENTITY-MIGRATION-REPORT-TR.md` left untouched.** It names `BranchCode` and
   `ContractApproved` while describing the *legacy* `User` columns that were split; it is a
   historical record of a past run, so rewriting it would misstate what happened.
   `docs/DATA-MIGRATION.md` was updated where it named current columns.
7. **Migrator carry-column asymmetry (pre-drop schema).** In `UserColumnClassifyStep.cs:48,59` and
   `UserSplitStep.cs:515`, `u.BranchCode` / `u.ContractApproved` keep their names while
   `m.MedicalSpecialtyCode` / `p.IsContractApproved` follow the rename. This is correct: those
   `ensa.[User]` columns no longer exist in the current schema — they were dropped by
   `20260827185256_DropMovedUserColumns` — and the step exists to prove the move *before* that drop
   runs. It reads oddly, so it is called out rather than left to be rediscovered.
8. **`Fit(context, "Table", "Column", ...)` lookups fail *silently*, not loudly** — corrected from
   an earlier draft of this report, which claimed the opposite.
   `FieldFitter.LoadAsync` (`src/Ensa.DataMigrator/Infrastructure/FieldFitter.cs:32-56`) reads
   column limits from the **live destination** `sys.columns` and keys them `table.column`.
   `Fit` (`FieldFitter.cs:120-138`) returns the value unchanged when the key is missing:

   ```csharp
   if (!_maxLengths.TryGetValue(key, out var maxLength) || value.Length <= maxLength)
   {
       return value;   // unknown column -> silent pass-through
   }
   ```

   All eight call sites now pass the **new** column names (`RiskStep.cs:239,243`,
   `CompanyStep.cs:162,335`, `CommercialStep.cs:595`, `TenancyStep.cs:199,538`,
   `ReportStep.cs:359,362`), and each was checked against the model snapshot — all eight resolve to
   a declared property. But against a database where this migration has **not** been applied, every
   one of those keys misses, length-fitting quietly stops protecting exactly those columns, and the
   only signal is a later INSERT failure — and only for a value that actually exceeds the column.
   `WorkplaceTelefonu` is the documented case that motivated the fitter (`docs/DATA-MIGRATION.md`).

   > **Operational ordering requirement:** apply migration
   > `20260829195006_RenameLegacyLeftoverColumns` **before** running the DataMigrator. Running the
   > migrator first degrades silently. (`VisitStep.cs:41`'s bulk-copy column list, which now names
   > `"IsCompleted"`, is the one place that would fail loudly in that situation.)

---

## Recommendation before merge

In this order:

1. **Apply the migration to a restored copy of the development database first.**
   `dotnet ef database update -p src/Ensa.EntityFrameworkCore -s src/Ensa.HttpApi.Host`. Nothing has
   executed this migration yet, so `sp_rename`, the three index renames and the Office index rebuild
   are all unexercised. Afterwards, check row counts and spot-check values on `Company`,
   `ProspectOrganization` and `Office` — those are the tables where the scaffolder's original
   positional mis-pairing would have shown up.
2. **Run the DataMigrator only after that migration is applied.** `FieldFitter` resolves column
   limits by name against the live schema and degrades silently when a name is missing (Needs
   Review #8). Running the migrator against a pre-rename database loses length protection on the
   eight renamed text columns without saying so.
3. **Re-run the full test suite and keep the logs.** `dotnet test --no-restore`. If the EF failures
   reappear, capture them with
   `dotnet test test\Ensa.EntityFrameworkCore.Tests\Ensa.EntityFrameworkCore.Tests.csproj --logger "console;verbosity=detailed"`
   before treating the suite as a merge gate — the one failing run in this work was never explained.
4. **Decide the API compatibility strategy** (Needs Review #1): one coordinated release of the
   backend together with its consumers, or temporary `[JsonPropertyName]` attributes on read DTOs
   with a ticket to remove them. Consumer-side work is outside this review's scope and has to be
   planned by whoever owns those clients. This is the only finding that reaches users, and it fails
   quietly.
5. **Then run the API-level suites** — `api_office_switch.py`, `api_mail.py` and the rest — which
   need the migrated schema and a running API, and which were not executed here.

Status of this checklist after the verification run below: **1 done on a throwaway database,
2 blocked pending a decision, 3 done, 4 blocked, 5 blocked.**

---

## Pre-merge verification run — 2026-08-30

No source file was modified during this run. The repository diff is unchanged at 96 files,
304 insertions / 304 deletions.

### Which database `dotnet ef database update` actually targets

Worth stating before any command: `dotnet ef` resolves the connection through
`EnsaDbContextFactory.ResolveConnectionString` (`src/Ensa.EntityFrameworkCore/EnsaDbContextFactory.cs:62-90`),
in this order — (1) the `ConnectionStrings__Default` environment variable, (2)
`src/Ensa.HttpApi.Host/appsettings.json`, (3) a LocalDB fallback. **It never reads
`appsettings.Development.local.json`**, which is what the running application uses.

| Path | Resolves to |
|---|---|
| `dotnet ef database update` with no environment variable | `Server=localhost;Database=EnsaDb` (from `appsettings.json`) — and `sqlcmd -S localhost` fails with *"Named Pipes Provider: Could not open a connection"*, i.e. there is no SQL Server on `localhost` at all |
| The application at runtime (`DOTNET_ENVIRONMENT=Development`) | `EnsaDbDEv` on `213.159.30.211,1433` (from `appsettings.Development.local.json`) |

So the command as written in the checklist would **fail rather than migrate the development
database**. Reaching `EnsaDbDEv` requires setting `ConnectionStrings__Default` explicitly.

### The shared development database was NOT migrated

`EnsaDbDEv` (`213.159.30.211,1433`) is shared and holds the real migrated legacy data, so nothing
was applied to it. Read-only inspection only:

```
applied_migrations_tail            20260828101024_MenuItemPermission
company_has_TaxTaxOffice(old)      1
company_has_TaxOffice(new)         0
office_has_HeadquarterOffice(old)  1
```

Two facts follow. It is still on the pre-rename schema — and its migration history contains
**`20260828101024_MenuItemPermission`, which does not exist on this branch** (the branch ends at
`20260827230602_RelaxMedicationDoseUnitCodeIndex` before the rename). The database is one migration
ahead of, and divergent from, this branch's model snapshot. See [Remaining risks](#remaining-risks-after-this-run).

### What was verified instead: a throwaway LocalDB proof

A disposable database `EnsaRenameProof` was created on `(localdb)\MSSQLLocalDB`, migrated to the
commit *before* the rename, populated with deliberately mirrored values, migrated, inspected, rolled
back, and re-applied. It was dropped afterwards (`SELECT COUNT(*) … = 0`).

```
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "CREATE DATABASE EnsaRenameProof;"
ConnectionStrings__Default='Server=(localdb)\MSSQLLocalDB;Database=EnsaRenameProof;…' \
  dotnet ef database update RelaxMedicationDoseUnitCodeIndex -p src/Ensa.EntityFrameworkCore -s src/Ensa.HttpApi.Host
  -> Done.   (schema at the pre-rename state)
… INSERT two rows each into ensa.Company, ensa.ProspectOrganization, ensa.Office …
ConnectionStrings__Default='…EnsaRenameProof…' dotnet ef database update …
  -> Applying migration '20260829195006_RenameLegacyLeftoverColumns'. Done.
```

**Value preservation — the eight pairs the scaffolder originally mis-paired.** The two rows carry
mirrored flags precisely so a swap cannot hide. Read through the old names before the migration and
through the new names after it:

| Row | before (old columns) | after (new columns) |
|---|---|---|
| `PROOF-A` | `1\|0\|1\|0\|1\|111\|222\|1111.11\|2222.22\|3333.33\|TAXOFFICE-A` | `1\|0\|1\|0\|1\|111\|222\|1111.11\|2222.22\|3333.33\|TAXOFFICE-A` |
| `PROOF-B` | `0\|1\|0\|1\|0\|333\|444\|4444.44\|5555.55\|6666.66\|TAXOFFICE-B` | `0\|1\|0\|1\|0\|333\|444\|4444.44\|5555.55\|6666.66\|TAXOFFICE-B` |
| `PROSPECT-A` | `PhysicianExists=1, Paid=0, MailSent=1` | `HasPhysician=1, IsPaid=0, IsMailSent=1` |
| `PROSPECT-B` | `PhysicianExists=0, Paid=1, MailSent=0` | `HasPhysician=0, IsPaid=1, IsMailSent=0` |
| `HQ Office` / `Branch Office` | `HeadquarterOffice=1` / `0` | `IsHeadquarterOffice=1` / `0` |

Column order in the Company rows is
`OrganizationTypeVerified, PasswordSent, SolutionPartner, QuoteVatIncluded, UserLimit, VisitSpecialist, VisitPhysician, InvoiceAmountKh, GrContractAmount, PayableDigit, TaxTaxOffice`
before, and the corresponding new names after. **The two readings are identical, field for field** —
`PasswordSent → AreEmployeePasswordsSent`, `OrganizationTypeVerified → IsHazardClassVerified`, the
money triplet and the three `ProspectOrganization` flags all landed where intended.

**Schema after migration** (same database):

```
new columns present: 36 of 36
old columns still present: 0 (must be 0)
index: IX_Office_TenantId_IsHeadquarterOffice | filter: ([IsHeadquarterOffice]=(1) AND [IsDeleted]=(0))
index: IX_WorkPlan_TenantId_IsTransferred | IX_TrainingPlan_TenantId_IsTransferred |
       IX_CashRegister_TenantId_IsHeadquarterCashRegister
rows Company=2 ProspectOrganization=2 Office=2
```

**Reversibility.** `dotnet ef database update RelaxMedicationDoseUnitCodeIndex` reverted the
migration on the same database: the old column names came back holding the same values
(`PROOF-A|1|0|1111.11|2222.22|3333.33|TAXOFFICE-A`), and the index returned as
`IX_Office_TenantId_HeadquarterOffice | ([HeadquarterOffice]=(1) AND [IsDeleted]=(0))`. Re-applying
produced the post-rename reading again, unchanged. `Down()` is not merely the inverse on paper.

### Static re-check of the migration (task 1)

```
forbidden op count (DropColumn|AddColumn|AlterColumn|Sql): 0
all ops: RenameColumn 72, RenameIndex 6, DropIndex 2, CreateIndex 2   (Up + Down)
Up: 36 column renames, 3 index renames ; Down: 36 / 3 ; Down-is-exact-inverse: YES
duplicate sources: none | duplicate targets: none | target collides with an un-renamed source: none
all eight previously dangerous pairs: OK
```

The `Office` `DropIndex`/`CreateIndex` is justified and now demonstrated: the index carries both a
changed name and a filter predicate naming the renamed column, and the round-trip above shows it
rebuilt correctly in each direction. An index holds no row data.

### DataMigrator ordering risk (task 3)

`FieldFitter.LoadAsync`'s own query was run against the **migrated** proof schema. All eight
`Fit(context, "Table", "Column", …)` targets resolve, with their limits:

```
RiskAssessmentReport.WorkplacePhoneNumber -> 128     Company.TaxOffice              -> 128
RiskAssessmentReport.MachineryAndEquipment -> 4000   PenaltySurvey.TaxOffice        -> 128
UserMedulaCredential.MedicalSpecialtyCode -> 32      Organization.TaxOffice         -> 128
YearEndReviewLine.PersonAndTitle -> 256              YearEndReviewLine.ResultAndComment -> 2000
```

Against the **un-migrated** `EnsaDbDEv` those same keys would miss, and `Fit` would silently stop
fitting (Needs Review #8). The ordering requirement is therefore confirmed by measurement, not only
by reading the code.

**The DataMigrator itself was not run.** It reads the shared legacy database `DemoOsgbDb` and writes
to the destination; that needs explicit confirmation and a destination that has this migration
applied.

### Tests (task 4)

```
dotnet build            -> 0 warnings, 0 errors
dotnet test --no-restore -> 141 passed, 0 failed
                            DataMigrator 18 | Application 10 | Domain 52 | EntityFrameworkCore 61 (11 s)
```

The EF failure seen once earlier did **not** reappear in this run, and the suite finished in its
normal time. It remains unexplained rather than fixed; detailed logging was not needed here.

### API scripts (task 5) — not run, and why

`tools/api-tests/api_office_switch.py` and `api_mail.py` need a running API on a migrated schema.
Neither is available without crossing a line this run was asked not to cross:

1. **The API cannot be pointed at the throwaway database.** `src/Ensa.HttpApi.Host/Program.cs:23-26`
   loads `appsettings.{Environment}.local.json` **after** the default configuration providers, so it
   overrides the `ConnectionStrings__Default` environment variable. Under `Development` the API
   always connects to `EnsaDbDEv`. Redirecting it would mean editing that file — a gitignored file
   holding real credentials, out of scope here.
2. **`EnsaDbDEv` is still on the pre-rename schema**, so an API built from this branch would fail on
   every query touching a renamed column. Running the scripts against it would only reproduce that
   known state.
3. The throwaway database had schema but no seed data (no administrator, no permissions), so even a
   redirected API could not have authenticated.

They should be run immediately after a real database is migrated — see the checklist above.

---

## Remaining risks after this run

1. **No real database has been migrated.** The rename is proven on a synthetic two-row database.
   Volume-related behaviour on `EnsaDbDEv` (1,791 companies, ~1.7 M visits) — chiefly the `Office`
   unique filtered-index rebuild — has not been observed. Expected to be trivial; not measured.
2. **`EnsaDbDEv` has diverged from this branch.** It carries `20260828101024_MenuItemPermission`,
   which this branch does not contain. Applying the rename there is mechanically fine (EF ignores
   history rows it does not know), but this branch's model snapshot does not describe that
   migration's schema, so a later `migrations add` **on this branch** would try to revert it. Merge
   or rebase onto the branch that owns `MenuItemPermission` before migrating a shared database.
3. **Migrating a shared database breaks other branches.** Any checkout still using the old property
   names would fail against renamed columns. `EnsaDbDEv` is shared, so this is a coordination
   decision, not a technical one.
4. **The API contract risk is unchanged** (Needs Review #1) and untested end to end, because the API
   scripts could not run.
5. **The DataMigrator is unexercised against the new names**, and must run only after the
   destination has this migration applied.

---

## Final Git Status

```
 M docs/DATA-MIGRATION.md
 M src/Ensa.Application.Contracts/Communication/Dtos/EmailSettingsDtos.cs
 M src/Ensa.Application.Contracts/Communication/Dtos/VisitDtos.cs
 M src/Ensa.Application.Contracts/Companies/Dtos/CompanyDtos.cs
 M src/Ensa.Application.Contracts/Companies/Dtos/WorkplaceDepartmentDtos.cs
 M src/Ensa.Application.Contracts/Finance/Dtos/CashRegisterDtos.cs
 M src/Ensa.Application.Contracts/Finance/Dtos/PenaltyDtos.cs
 M src/Ensa.Application.Contracts/Membership/Dtos/MyOfficeDtos.cs
 M src/Ensa.Application.Contracts/Membership/Dtos/ProfileDto.cs
 M src/Ensa.Application.Contracts/Membership/Dtos/UserDtos.cs
 M src/Ensa.Application.Contracts/Plans/Dtos/WorkPlanDtos.cs
 M src/Ensa.Application.Contracts/Reports/Dtos/ReportDtos.cs
 M src/Ensa.Application.Contracts/Risks/Dtos/EquipmentDtos.cs
 M src/Ensa.Application.Contracts/Risks/Dtos/IncidentDtos.cs
 M src/Ensa.Application.Contracts/Risks/Dtos/RiskAssessmentReportDtos.cs
 M src/Ensa.Application.Contracts/Tenancy/Dtos/Navigations/OrganizationNavigationDto.cs
 M src/Ensa.Application.Contracts/Tenancy/Dtos/OfficeDtos.cs
 M src/Ensa.Application.Contracts/Tenancy/Dtos/OrganizationDtos.cs
 M src/Ensa.Application.Contracts/Trainings/Dtos/EmployeeTrainingProgressDtos.cs
 M src/Ensa.Application.Contracts/Trainings/Dtos/TrainingPlanDtos.cs
 M src/Ensa.Application/Communication/CommunicationAutoMapperProfile.cs
 M src/Ensa.Application/Communication/EmailSettingsAppService.cs
 M src/Ensa.Application/Communication/VisitAppService.cs
 M src/Ensa.Application/Companies/WorkplaceDepartmentAppService.cs
 M src/Ensa.Application/Companies/WorkplaceDepartmentAutoMapperProfile.cs
 M src/Ensa.Application/Finance/CashRegisterAppService.cs
 M src/Ensa.Application/Membership/AccountAppService.cs
 M src/Ensa.Application/Membership/PermissionAppService.cs
 M src/Ensa.Application/Membership/UserAppService.cs
 M src/Ensa.Application/Plans/PlansAutoMapperProfile.cs
 M src/Ensa.Application/Plans/WorkPlanAppService.cs
 M src/Ensa.Application/Risks/EquipmentAppService.cs
 M src/Ensa.Application/Risks/EquipmentAutoMapperProfile.cs
 M src/Ensa.Application/Tenancy/OfficeAppService.cs
 M src/Ensa.Application/Trainings/EmployeeTrainingProgressAppService.cs
 M src/Ensa.Application/Trainings/TrainingPlanAppService.cs
 M src/Ensa.Application/Trainings/TrainingsAutoMapperProfile.cs
 M src/Ensa.DataMigrator/Steps/CommercialStep.cs
 M src/Ensa.DataMigrator/Steps/CompanyStep.cs
 M src/Ensa.DataMigrator/Steps/FinanceStep.cs
 M src/Ensa.DataMigrator/Steps/LookupExtrasStep.cs
 M src/Ensa.DataMigrator/Steps/OperationsStep.cs
 M src/Ensa.DataMigrator/Steps/PlanStep.cs
 M src/Ensa.DataMigrator/Steps/ReportStep.cs
 M src/Ensa.DataMigrator/Steps/RiskStep.cs
 M src/Ensa.DataMigrator/Steps/TenancyStep.cs
 M src/Ensa.DataMigrator/Steps/UserColumnClassifyStep.cs
 M src/Ensa.DataMigrator/Steps/VisitStep.cs
 M src/Ensa.DbMigrator/Seeding/MembershipSeeder.cs
 M src/Ensa.Domain/Communication/EmailSettings.cs
 M src/Ensa.Domain/Communication/Visit.cs
 M src/Ensa.Domain/Companies/Company.cs
 M src/Ensa.Domain/Companies/CompanyManager.cs
 M src/Ensa.Domain/Companies/WorkplaceDepartment.cs
 M src/Ensa.Domain/Finance/CashRegister.cs
 M src/Ensa.Domain/Finance/PenaltySurvey.cs
 M src/Ensa.Domain/Lookups/SystemSetting.cs
 M src/Ensa.Domain/Lookups/TreeNode.cs
 M src/Ensa.Domain/Membership/IPermissionRepository.cs
 M src/Ensa.Domain/Membership/PermissionManager.cs
 M src/Ensa.Domain/Membership/UserMedulaCredential.cs
 M src/Ensa.Domain/Membership/UserPermission.cs
 M src/Ensa.Domain/Membership/UserProfile.cs
 M src/Ensa.Domain/Plans/WorkPlan.cs
 M src/Ensa.Domain/Reports/OhsReport.cs
 M src/Ensa.Domain/Reports/YearEndReviewLine.cs
 M src/Ensa.Domain/Risks/Equipment.cs
 M src/Ensa.Domain/Risks/Incident.cs
 M src/Ensa.Domain/Risks/IncidentManager.cs
 M src/Ensa.Domain/Risks/RiskAssessmentReport.cs
 M src/Ensa.Domain/Tenancy/IOfficeRepository.cs
 M src/Ensa.Domain/Tenancy/Navigations/OrganizationNavigation.cs
 M src/Ensa.Domain/Tenancy/Office.cs
 M src/Ensa.Domain/Tenancy/Organization.cs
 M src/Ensa.Domain/Tenancy/OrganizationContract.cs
 M src/Ensa.Domain/Tenancy/ProspectOrganization.cs
 M src/Ensa.Domain/Trainings/TrainingPlan.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Companies/CompanyConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Finance/CashRegisterConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Finance/PenaltySurveyConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Membership/UserMedulaCredentialConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Plans/WorkPlanConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Reports/YearEndReviewLineConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Risks/RiskAssessmentReportConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Tenancy/OfficeConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Tenancy/OrganizationConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Configurations/Trainings/TrainingPlanConfiguration.cs
 M src/Ensa.EntityFrameworkCore/Migrations/EnsaDbContextModelSnapshot.cs
 M src/Ensa.EntityFrameworkCore/Repositories/Finance/CashRegisterRepository.cs
 M src/Ensa.EntityFrameworkCore/Repositories/Membership/PermissionRepository.cs
 M src/Ensa.EntityFrameworkCore/Repositories/Tenancy/OfficeRepository.cs
 M src/Ensa.EntityFrameworkCore/Repositories/Tenancy/OrganizationRepository.cs
 M src/Ensa.HttpApi.Host/Mailing/SmtpMailSender.cs
 M test/Ensa.EntityFrameworkCore.Tests/OfficeAccessTests.cs
 M tools/api-tests/api_mail.py
 M tools/api-tests/api_office_switch.py
?? RENAME-REFACTOR-REPORT.md
?? src/Ensa.EntityFrameworkCore/Migrations/20260829195006_RenameLegacyLeftoverColumns.Designer.cs
?? src/Ensa.EntityFrameworkCore/Migrations/20260829195006_RenameLegacyLeftoverColumns.cs
```
