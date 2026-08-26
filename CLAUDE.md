# Ensa — Project Instructions

## How to work here (STANDING RULE)

**Do not ask me questions.** When something is ambiguous, do not stop and ask; make the most
defensible call yourself, state the assumption in one line, and keep going. No approval requests,
no "shall I continue?", no menus of options. The decision is yours.

- Do not use the `AskUserQuestion` tool.
- No plan mode, no waiting for confirmation — implement directly.
- At the end of the work, summarise what you did and which assumptions you made — as information,
  not as a question.
- If something is genuinely going wrong, still do not ask: apply the best fix, then report it.

**Language:** all code, identifiers, comments, XML docs and repository documentation are **English**.
Chat replies to me are **Turkish**.

## Source Folder

`D:\EnsaProject` is **read-only**. Never create, modify or delete anything there.
Read it with `Read` / `Grep` / `Glob` only. All development happens under `D:\EnsaFromLegacyEnsa`.

## Architecture

Read these **before writing code**:
- `docs/ARCHITECTURE.md` — the binding architectural contract
- `docs/DECISIONS.md` — accepted architecture decision records (ADRs)

Rules in brief:
- ABP.IO layer template, **without** ABP libraries
- **No navigation properties in entities or DTOs** — relationships are `int` / `int?` FKs only
- Combined reads use `[NotMapped]` `{Entity}Navigation` / `{Entity}NavigationDto`
- Multi-tenant: `TenantId` (`int?`), `null` = host
- Magic strings / magic ints → **enums** (`Ensa.Domain.Shared/Enums/`)
- Money is **always `decimal`**
- One `IEntityTypeConfiguration` per entity (Fluent API); no data annotations on entities
- Identity: OpenIddict 7 + ASP.NET Core Identity
- User-facing error text lives in `Ensa.Domain.Shared/Localization/EnsaResource.resx` (English)
  and `EnsaResource.tr.resx` (Turkish); throw sites carry a stable code plus `WithData(...)`

## Commands

```
dotnet build                                  # repository root
dotnet ef migrations add <Name> -p src/Ensa.EntityFrameworkCore -s src/Ensa.HttpApi.Host
dotnet run --project src/Ensa.DbMigrator      # apply migrations + seed (see note below)
dotnet run --project src/Ensa.HttpApi.Host    # API  -> https://localhost:7001
npm run dev --prefix react/ensa-web           # web  -> http://localhost:5173
dotnet test                                   # unit tests
python tools/api-tests/api_test.py            # Company module end to end (API must be running)
python tools/api-tests/api_coverage.py        # every GET: anonymous 401, authenticated no 5xx
python tools/api-tests/api_authorization.py   # a permission-less user gets 403, not 200
python tools/api-tests/api_company_scope.py   # a customer sees their own workplace and no other
python tools/api-tests/api_customer_portal.py # every legacy customer-portal page has a counterpart
python tools/api-tests/api_documents.py       # document upload/download and its security claims
python tools/api-tests/fake_smtp.py &         # test SMTP server, then:
python tools/api-tests/api_mail.py            # queue -> background worker -> delivery
python tools/api-tests/frontend_routes.py     # every SPA endpoint resolves
python tools/api-tests/frontend_calls.py      # every SPA API call: path and method exist
python tools/api-tests/frontend_permissions.py # SPA permission constants match the catalogue
python tools/api-tests/frontend_menu.py       # seeded menu and SPA navigation still agree
python tools/api-tests/dev_stack.py           # SPA -> Vite proxy -> API, end to end
python tools/i18n-check/check_locales.py      # no missing or unpaired translation key
python tools/gen-enums/gen_enums.py           # regenerate the SPA enums from the backend
python tools/gen-enums/gen_permissions.py     # regenerate the SPA permission constants
python tools/gen-enums/gen_menu.py            # regenerate the menu seed from the SPA navigation
```

The API verification scripts need the development certificate exported once; see
`tools/api-tests/README.md`. On Windows run them with `PYTHONIOENCODING=utf-8`.

The migrator reads `Ensa.HttpApi.Host`'s settings files and resolves the environment from
`DOTNET_ENVIRONMENT` or `ASPNETCORE_ENVIRONMENT`. Its launch profile
(`src/Ensa.DbMigrator/Properties/launchSettings.json`) pins **Development**, so `dotnet run` and
Visual Studio's F5 both work from a fresh clone. Bypassing the profile (`--no-launch-profile`, a
published binary, a container) resolves **Production**, which looks for `Server=localhost` and
refuses to start without a column encryption key — both correct for a deployment. Set
`DOTNET_ENVIRONMENT=Development` explicitly in that case.

SQL Server must be reachable. On a developer machine LocalDB is used:
`sqllocaldb start MSSQLLocalDB`. The connection string lives in
`src/Ensa.HttpApi.Host/appsettings.Development.json`.

Seeded administrator: `admin` / `Ensa!2026` (flagged to change the password on first sign-in).

## Domain glossary (legacy Turkish → English)

The legacy system was Turkish. The current codebase is English; this table is the mapping used
when reading `D:\EnsaProject`:

| Legacy | Current | Legacy | Current |
|---|---|---|---|
| Firma | Company | Yetki | Permission |
| FirmaPersonel | CompanyEmployee | Kullanici | User |
| Kurum | Organization | Ofis | Office |
| TehlikeSinifi | HazardClass | Tehlike | Hazard |
| RiskAnalizRaporu | RiskAssessmentReport | DÖF | CorrectiveAction |
| SahaGozlem | FieldObservation | Olay | Incident |
| Egitim | Training | CalismaPlani | WorkPlan |
| SaglikMuayeneFormu | MedicalExaminationForm | ERecete | EPrescription |
| Dosya | Document | Ziyaret | Visit |
| Fatura | Invoice | Kasa | CashRegister |
| Ceza | Penalty | IsyeriBolum | WorkplaceDepartment |
