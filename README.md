# Ensa — Occupational Health & Safety Management System

A rewrite of a legacy .NET Framework / Entity Framework 6 application (`D:\EnsaProject`) on
**.NET 10**, **EF Core 10**, **SQL Server**, **OpenIddict** and **React 19**.

The solution mirrors the ABP.IO layered template **exactly**, but does not reference any
`Volo.Abp.*` package — the concepts are reimplemented in-repo.

---

## Solution Layout

```
Ensa.sln
├── src/
│   ├── Ensa.Domain.Shared/          enums · constants · exceptions · localization   (no dependencies)
│   ├── Ensa.Domain/                 entities · navigation entities · IRepository · domain services
│   ├── Ensa.Application.Contracts/  DTOs · navigation DTOs · IAppService · permissions
│   ├── Ensa.Application/            application services · AutoMapper profiles
│   ├── Ensa.EntityFrameworkCore/    DbContext · configurations · repositories · migrations
│   ├── Ensa.HttpApi/                controllers · exception filter
│   ├── Ensa.HttpApi.Host/           Program.cs · OpenIddict server · appsettings
│   └── Ensa.DbMigrator/             migration + seed runner
├── test/
│   ├── Ensa.TestBase/
│   ├── Ensa.Domain.Tests/           business-rule tests
│   ├── Ensa.Application.Tests/
│   └── Ensa.EntityFrameworkCore.Tests/   model-contract tests
├── react/ensa-web/                  React 19 · Vite · Bootstrap 5 (Metronic palette) · i18n
└── docs/
    ├── ARCHITECTURE.md              the binding contract
    └── DECISIONS.md                 architecture decision records
```

### Layer dependencies

```
Domain.Shared         ->  (none)
Domain                ->  Domain.Shared
Application.Contracts ->  Domain.Shared
Application           ->  Domain, Application.Contracts
EntityFrameworkCore   ->  Domain
HttpApi               ->  Application.Contracts
HttpApi.Host          ->  HttpApi, Application, EntityFrameworkCore
DbMigrator            ->  EntityFrameworkCore, Application
```

`Application.Contracts` cannot see Domain. `HttpApi` cannot see Domain or EF Core.
`Application` has **no EF Core reference** — persistence goes through repositories only.

---

## Core Rules

| Rule | Detail |
|---|---|
| **No navigation properties** | Entities and DTOs hold no class-typed properties; relationships are `int`/`int?` FKs |
| **Navigation entity / DTO** | Combined reads use `[NotMapped]` `{Entity}Navigation` and `{Entity}NavigationDto`; never a `DbSet` |
| **Multi-tenant** | `TenantId int?`; `null` = host. A global query filter is installed by reflection in `EnsaDbContext` |
| **Enums** | Legacy magic strings/ints became enums under `Ensa.Domain.Shared/Enums/`; stored as `int` |
| **Normalization** | Repeating column groups became child tables; all `byte[]` payloads moved to a central `Document` table |
| **Money** | `decimal` everywhere, never `double` |
| **Configuration** | One `IEntityTypeConfiguration` per entity, Fluent API only. **No configuration means no table** — the DbContext declares no `DbSet`s |
| **Keys** | PK is always `Id`; FKs are `{Entity}Id` |
| **Language** | Code, comments and XML docs are English; user-facing text is localized (tr/en) |

---

## Getting Started

### Prerequisites
- .NET SDK 10
- SQL Server (LocalDB is fine for development)
- Node.js 20+

### Database

Development uses LocalDB by default
(`src/Ensa.HttpApi.Host/appsettings.Development.json`):

```
Server=(localdb)\MSSQLLocalDB;Database=EnsaDb;Trusted_Connection=True;TrustServerCertificate=True
```

```bash
sqllocaldb start MSSQLLocalDB           # if it is not running
dotnet tool install --global dotnet-ef  # first time only

# Regenerate the schema only if you changed the model:
dotnet ef migrations add <Name> -p src/Ensa.EntityFrameworkCore -s src/Ensa.HttpApi.Host

# Apply migrations and load seed data (idempotent — safe to re-run):
DOTNET_ENVIRONMENT=Development dotnet run --project src/Ensa.DbMigrator
```

The seeder creates 81 provinces, a starter district set, 171 permissions, 7 roles, a demo
organization, and the administrator account.

### API

```bash
dotnet run --project src/Ensa.HttpApi.Host
```

- API: `https://localhost:7001`
- Swagger: `https://localhost:7001/swagger`
- Health: `https://localhost:7001/health`

### Web

```bash
cd react/ensa-web
npm install
npm run dev        # http://localhost:5173
```

Vite proxies `/api` and `/connect` to `https://localhost:7001`.

---

## Authentication

OpenIddict 7 with `password`, `refresh_token` and `client_credentials` flows.

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&username=admin
&password=Ensa!2026
&scope=openid profile email roles offline_access ensa
```

Custom claims on the access token:

| Claim | Meaning |
|---|---|
| `ensa:tenantId` | The user's organization; `TenantResolutionMiddleware` feeds it into `ICurrentTenant` |
| `ensa:permission` | One claim per effective permission; each permission name is also an authorization policy |

Effective permissions are computed by `PermissionManager`, reproducing the legacy algorithm:
system administrator → subscription-plan gate → organization-type gate → user-type ∪ explicit
grants → explicit denial wins → restriction list. See `docs/DECISIONS.md` ADR-007.

> The seeded administrator is `admin` / `Ensa!2026` and is flagged to change the password on first
> sign-in. Set `Seed:AdminPassword` (or the `Seed__AdminPassword` environment variable) for any
> non-development environment.

---

## Localization

Server-side messages come from `src/Ensa.Domain.Shared/Localization/`:
`EnsaResource.resx` (English, fallback) and `EnsaResource.tr.resx` (Turkish).

The **error code is the resource key**, so an exception translates without a lookup table:

```csharp
throw new BusinessException(
        "SSI number is already registered to another workplace.",
        "Ensa:Company:SsiNumberAlreadyRegistered")
    .WithData("SsiNumber", company.SsiNumber);
```

Culture resolution: `?culture=en-US` → `Accept-Language` → default `tr-TR`.

```bash
curl -X POST https://localhost:7001/api/company?culture=en-US ...
# -> "A workplace marked as a branch must reference the headquarters it belongs to."
curl -X POST https://localhost:7001/api/company?culture=tr-TR ...
# -> "Şube olarak işaretlenen işyeri için bağlı olduğu merkez işyeri seçilmelidir."
```

The SPA uses `react-i18next` and sends its active language as `Accept-Language`.

**When adding an error code, add the key to both resx files.**

---

## Theme

Bootstrap 5 compiled over the Metronic palette
(`react/ensa-web/src/styles/metronic.scss`), exposed as both SCSS variables and CSS custom
properties:

```
--kt-primary #3E97FF   --kt-success #50CD89   --kt-info    #7239EA
--kt-warning #FFC700   --kt-danger  #F1416C   --kt-dark    #181C32
--kt-body-bg #F5F8FA   --kt-card-bg #FFFFFF   --kt-border-color #F1F1F2
```

Metronic's soft variants are generated too: `.badge-light-*`, `.btn-light-*`.

---

## Tests

```bash
dotnet test
```

- **`Ensa.Domain.Tests`** — statutory business rules: national-ID checksum, Fine-Kinney and
  L-Matrix risk scoring, training/examination/risk-assessment intervals from Law 6331, invoice VAT.
- **`Ensa.EntityFrameworkCore.Tests`** — model contract: the model builds, no navigation property
  exists on any entity, no navigation entity reached the model, every string column has a length,
  every tenant/soft-delete entity has a global filter, every domain entity is configured, every
  decimal has a precision.

---

## Documentation

- **`docs/ARCHITECTURE.md`** — layers, naming, entity/DTO rules, multi-tenancy, normalization
  decisions, the `IEntityTypeConfiguration` template
- **`docs/DECISIONS.md`** — ADRs: why ABP-without-ABP, the encryption/typing trade-off, the
  sign-in tenant-filter exception, document numbering concurrency, the Turkish→English rename
- **`CLAUDE.md`** — instructions for AI agents working in this repository, including the
  legacy Turkish → English domain glossary
