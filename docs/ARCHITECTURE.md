# Ensa — Architecture Contract (BINDING)

This document is the single source of truth for the project. Read it before writing code and
follow it without exception.

---

## 0. ABSOLUTE RULES

1. **Never write to `D:\EnsaProject`.** It is the read-only legacy source. Read it with
   `Read`/`Grep`/`Glob` only — never create, modify or delete anything there.
2. All development happens under **`D:\EnsaFromLegacyEnsa`**.
3. **Navigation properties (class-typed properties) are forbidden in entities and DTOs.**
   Relationships are expressed only with `int` / `int?` foreign-key fields.
4. When combined data is needed, use a **Navigation Entity** / **Navigation DTO** (§4).
5. Navigation entities are `[NotMapped]`, are **never a `DbSet`**, and never reach `ModelBuilder`.
6. Legacy magic strings / magic ints become **enums** (§6).
7. All code, identifiers, comments and XML docs are **English**. User-facing text is localized (§12).

---

## 1. Solution Layout (ABP.IO template, without ABP libraries)

```
D:\EnsaFromLegacyEnsa\
├── Ensa.sln
├── docs\ARCHITECTURE.md, docs\DECISIONS.md
├── src\
│   ├── Ensa.Domain.Shared\          enums, constants, exceptions, localization resources
│   ├── Ensa.Domain\                 entities, navigation entities, IRepository, domain services
│   ├── Ensa.Application.Contracts\  DTOs, navigation DTOs, app-service interfaces, permissions
│   ├── Ensa.Application\            app-service implementations, AutoMapper profiles
│   ├── Ensa.EntityFrameworkCore\    DbContext, IEntityTypeConfiguration, repositories, migrations
│   ├── Ensa.HttpApi\                controllers, exception filter
│   ├── Ensa.HttpApi.Host\           Program.cs, OpenIddict server, appsettings
│   └── Ensa.DbMigrator\             migration + seed runner
├── test\
│   ├── Ensa.TestBase\
│   ├── Ensa.Domain.Tests\
│   ├── Ensa.Application.Tests\
│   └── Ensa.EntityFrameworkCore.Tests\
└── react\ensa-web\                  React 19 + Vite + Bootstrap 5 (Metronic palette) + i18n
```

### Layer dependencies (never violate)

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

`Application.Contracts` **cannot see Domain**. `HttpApi` **cannot see Domain or EF Core**.
`Application` **has no EF Core reference** — no `ToListAsync`, `Include` or `EF.Functions` there;
all persistence goes through repository methods.

### ABP "module" equivalents

Instead of ABP's `XModule : AbpModule` classes, each layer exposes a static DI class:

| Layer | Class | Method |
|---|---|---|
| Domain | `EnsaDomainModule` | `AddEnsaDomain(this IServiceCollection)` |
| Application | `EnsaApplicationModule` | `AddEnsaApplication(...)` |
| EntityFrameworkCore | `EnsaEntityFrameworkCoreModule` | `AddEnsaEntityFrameworkCore(..., IConfiguration)` |
| HttpApi | `EnsaHttpApiModule` | `AddEnsaHttpApi(...)` |
| HttpApi.Host | `EnsaHttpApiHostModule` | `AddEnsaHttpApiHost(...)` |

---

## 2. Folders and Naming

### Domain
```
src\Ensa.Domain\
├── Common\                  Entity, NavigationEntity, ICurrentTenant, ICurrentUser, IClock
├── Repositories\            IRepository, IUnitOfWork
├── Services\                IDomainService
└── {Module}\                Companies, Trainings, Risks, Health, ...
    ├── Company.cs                     entity
    ├── ICompanyRepository.cs          module-specific repository interface
    ├── CompanyManager.cs              domain service
    └── Navigations\
        └── CompanyNavigation.cs       [NotMapped]
```

### Application.Contracts
```
src\Ensa.Application.Contracts\
├── Common\
├── Permissions\             EnsaPermissions (string constants)
└── {Module}\
    ├── Dtos\
    │   ├── CompanyDtos.cs             list / detail / create / update / list-input
    │   └── Navigations\CompanyNavigationDto.cs
    └── ICompanyAppService.cs
```

### EntityFrameworkCore
```
src\Ensa.EntityFrameworkCore\
├── EnsaDbContext.cs, EnsaDbContextFactory.cs
├── Ambient\                 Clock, CurrentTenant, DataFilter
├── Repositories\{Module}\   EfCoreRepository<T,TKey> + module repositories
├── Configurations\{Module}\ CompanyConfiguration : IEntityTypeConfiguration<Company>
├── ValueConverters\
└── Migrations\
```

### Naming rules
- The legacy `_T` suffix is gone: `Firma_T` → `Company`.
- Domain terms are **English** (see the glossary in `CLAUDE.md` for the legacy mapping).
- Primary key is always `Id`. Foreign keys are `{Entity}Id` (`CompanyId`, `UserId`).
- Table name is the singular entity name in the `ensa` schema: `ensa.Company`.
- Booleans read as `IsActive`, `IsDeleted`, `IsApproved`.
- Audit fields come from the base class — `CreationTime`, `CreatorId`, `LastModificationTime`,
  `LastModifierId`, `IsDeleted`, `DeletionTime`, `DeleterId`. **Never redeclare them.**

---

## 3. Entity Rules

```csharp
public class Company : FullAuditedTenantEntity   // Id = int
{
    public string CompanyName { get; set; } = string.Empty;
    public int CityId { get; set; }              // FK — no navigation property
    public int? DistrictId { get; set; }
    public HazardClass HazardClass { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### Choosing a base class
| Situation | Base class |
|---|---|
| Tenant-owned, deletable aggregate | `FullAuditedTenantEntity` |
| Tenant-owned ledger/log (never updated) | `CreationAuditedTenantEntity` |
| Tenant-owned, updated but never deleted | `AuditedTenantEntity` |
| Shared reference data (City, District, Icd10) | `AuditedEntity` / `Entity` |
| Join table | `CreationAuditedTenantEntity` |

### Forbidden
- `public City City { get; set; }` — navigation property
- `public List<CompanyEmployee> Employees { get; set; }` — collection navigation
- `public byte[] Logo { get; set; }` — file bytes never live on a domain entity; use `DocumentId`
- Static methods reaching into `DbContext` (the legacy code did this)
- `[Table]`, `[Column]`, `[MaxLength]` — **everything is Fluent API**

### Required
- Every `string` is `= string.Empty` or `string?`
- Enum-typed properties, never `int`/`string` stand-ins
- Money is `decimal` (never `double`)
- `DateTime` / `DateTime?`; use `DateOnly` when there is genuinely no time component

---

## 4. Navigation Entity / Navigation DTO

```csharp
// src\Ensa.Domain\Companies\Navigations\CompanyNavigation.cs
[NotMapped]
public class CompanyNavigation : NavigationEntity
{
    public Company Company { get; set; } = null!;
    public City? City { get; set; }
    public District? District { get; set; }
    public List<CompanyEmployee> Employees { get; set; } = [];
}
```

```csharp
// src\Ensa.Application.Contracts\Companies\Dtos\Navigations\CompanyNavigationDto.cs
public class CompanyNavigationDto : NavigationDto
{
    public CompanyDto Company { get; set; } = null!;
    public LookupDto? City { get; set; }
    public LookupDto? District { get; set; }
    public List<CompanyEmployeeDto> Employees { get; set; } = [];
}
```

These are filled by explicit projection in the module repository. **A fixed number of queries** —
never one query per collection element. `Include` is unavailable by design, so batch child loads
with `Where(x => parentIds.Contains(x.ParentId))` and group in memory.

---

## 5. Multi-Tenancy

- Tenant = the legacy `KurumId`, now **`TenantId`** (`int?`).
- The tenant table is `Tenancy.Organization`. `TenantId = null` means **host** (shared record).
- `EnsaDbContext` applies a global query filter by reflection to every `IMultiTenant` entity:
  `TenantId == currentTenant.Id || TenantId == null`.
- `TenantId` is assigned automatically on insert by the `SaveChanges` interceptor.
- `ICurrentTenant.Change(id)` switches tenant temporarily (host administration screens). The
  sign-in path uses it too: the access token is built inside the tenant of the user signing in,
  because nothing has resolved a tenant yet at that point (ADR-033).

### Company scope — the second dimension

Tenancy separates one OHS provider from another. It says nothing about the customers **inside** a
provider, and a customer contact must not read another customer's file. A second global query
filter, installed the same way, does that (ADR-034):

- `ICompanyScoped` — the entity carries a `CompanyId`; reached through `EF.Property<int?>`, so
  both `int` and `int?` declarations work.
- `ICompanyRecord` — the entity *is* the workplace, so the scope key is its own `Id`. Only
  `Company` implements it.
- The scope key is `ICurrentUser.CompanyId`, from the `ensa:companyId` access-token claim, which
  is written from the user record and never from the request.
- When it is null — every member of the provider's own staff, and every call with no user at all
  (sign-in, seeding, background work) — the filter is inert.
- Unlike tenancy it **fails closed**: a null `TenantId` is shared reference data and visible to
  all, a null `CompanyId` is provider-level data and hidden from a company-bound user.
- Suspend it with `IDataFilter.Disable<ICompanyScoped>()`, on the same terms as the tenant filter:
  deliberately, narrowly, and justified in the calling method's XML doc.
- **Host-only reference tables** (no `IMultiTenant`): `City`, `District`, `Neighborhood`,
  `OccupationCode`, SKRS tables, IBYS reference tables, `Icd10`, `Duty`, `Certificate`,
  `Penalty`, `Period`, `Permission`, `MenuItem`, `OrganizationType`, `SubscriptionPlan`.
- **Mixed host/tenant catalogues** (they *do* implement `IMultiTenant`; `TenantId = null` means
  shared): `Hazard`, `HazardCategory`, `Training`, `Activity`.

---

## 6. Enums

- All enums live under `Ensa.Domain.Shared\Enums\` (`CommonEnums.cs`, `OhsEnums.cs`, `BusinessEnums.cs`).
- Values are explicit (`= 1, = 2`); `0` is usually `Unspecified`.
- Stored as `int` (EF default — do not write `HasConversion<int>()`).
- Add new enums to one of the existing three files; do not create new enum files.

---

## 7. Normalization Decisions (mandatory)

| Legacy | Current |
|---|---|
| `byte[] Dosya` + name + type repeated on many tables | one `Document` table; `DocumentId` FK on the owner |
| `Firma.KurumTuru` / `PaketTuru` strings | `OrganizationTypeId` / `SubscriptionPlanId` FKs |
| `Firma.TehlikeSinifi` string | `HazardClass` enum |
| `Ceza_T` with 9 amount columns | `Penalty` + `PenaltyAmount` (hazard class × employee-count range × year) |
| `Egitim` with three duration columns | `TrainingDuration` (TrainingId, HazardClass, DurationMinutes) |
| `RiskAnalizRaporu` `TMK*`/`MKO*`/`IO*` boolean columns | three child tables keyed by enum |
| four identical `Risk*Kayit_T` tables | one `RiskAssessmentHistoryRecord` + `RiskHistoryRecordType` |
| `PeriyodikMuayeneFormu` (150+ flat columns) | `MedicalExaminationForm` + 6 normalized child tables |
| repeating `EskiIs1/2/3` group | `EmployeeWorkHistory` |
| `FirmaPersonel` health columns | `EmployeeHealthInfo` / `EmployeeImmunization` / `EmployeeFamilyHistory` |
| `YSDRSatirlari.AltCalismalarJson` | self-referencing `ParentLineId` hierarchy |
| `Mail.BagliDosyalar` CSV | `MailAttachment` |
| `FirmaHareket.Borc` + `Alacak` | `LedgerEntryType` enum + single `Amount` |
| `Kullanici.Sifre` | ASP.NET Core Identity `PasswordHash` |

---

## 8. Authentication / Authorization — OpenIddict

- **OpenIddict 7.x** (`OpenIddict.EntityFrameworkCore`, `OpenIddict.AspNetCore`).
- User store: ASP.NET Core Identity, `User : IdentityUser<int>`, `Role : IdentityRole<int>`.
- Flows: `password` (SPA sign-in), `refresh_token`, `client_credentials`.
- JWT; access token 1 hour, refresh token 30 days.
- **Tenant claim** `ensa:tenantId` — read by `TenantResolutionMiddleware` into `ICurrentTenant`.
- **Company claim** `ensa:companyId` — present only for a user bound to one client workplace;
  read into `ICurrentUser.CompanyId`, which drives the company-scope query filter.
- **Permission claims** `ensa:permission` — one per effective permission; each permission name is
  also an authorization policy.
- Permission names are constants on `EnsaPermissions` (`"Ensa.Company.Create"`).
- Effective permissions are computed by `PermissionManager`, reproducing the legacy algorithm:
  system administrator → subscription-plan gate → organization-type gate → user-type ∪ explicit
  grants → explicit denial wins → restriction list.
- **Sign-in disables the tenant filter for the user lookup only** — see ADR-011.

---

## 9. Repositories

- Generic: `IRepository<TEntity, TKey>` / `IRepository<TEntity>` (int).
- Module-specific queries get an interface in **Domain** (`ICompanyRepository : IRepository<Company>`)
  and an implementation in **EntityFrameworkCore\Repositories**.
- App services **never touch `DbContext`** — repositories only.
- Navigation-entity loaders live in the module repository.
- Registration is by assembly scan; never register a repository by hand.
- Trust the global query filter: never write `TenantId == ...` or `!IsDeleted` in a query. If a
  method must bypass it, use `IDataFilter.Disable<...>()` and justify it in the XML doc.

---

## 10. Application Services

```csharp
public class CompanyAppService(
    IServiceProvider serviceProvider,
    ICompanyRepository companyRepository,
    ICompanyManager companyManager)
    : EnsaAppService(serviceProvider), ICompanyAppService
{
    public async Task<CompanyDto> CreateAsync(CreateCompanyDto input, CancellationToken ct = default)
    {
        await CheckPermissionAsync(EnsaPermissions.Company.Create);
        var company = ObjectMapper.Map<CreateCompanyDto, Company>(input);

        // The manager validates AND persists. Do not call InsertAsync again.
        company = await companyManager.CreateAsync(company, ct);

        return ObjectMapper.Map<Company, CompanyDto>(company);
    }
}
```

- Every public method starts with `CheckPermissionAsync`.
- **Domain managers persist their own changes.** Calling `InsertAsync`/`UpdateAsync` after a manager
  call inserts the row twice and SQL Server fails with `IDENTITY_INSERT is set to OFF`.
- Never write `try/catch` — `EnsaExceptionFilter` shapes the response.
- Return `PagedResultDto<T>` / `ListResultDto<T>` / `TDto`.
- AutoMapper profiles: `Ensa.Application\{Module}\{Module}AutoMapperProfile.cs`; on create/update
  mappings, `Ignore()` `Id`, `TenantId` and all audit fields.

---

## 11. Frontend (`react\ensa-web`)

- Vite + React 19 + TypeScript + React Router + TanStack Query + Axios.
- **Bootstrap 5** compiled over the **Metronic palette**:

```
--kt-primary #3E97FF   --kt-success #50CD89   --kt-info    #7239EA
--kt-warning #FFC700   --kt-danger  #F1416C   --kt-dark    #181C32
--kt-body-bg #F5F8FA   --kt-card-bg #FFFFFF   --kt-border-color #F1F1F2
--kt-gray-100..900     #F9F9F9 … #181C32
```
Plus Metronic's soft variants: `.badge-light-*`, `.btn-light-*`.

- Sign-in uses the OpenIddict `password` grant; tokens in `localStorage`; 401 triggers one
  refresh attempt then redirects to `/login`.
- **No hard-coded user-facing strings** — everything goes through i18n (§12).
- Layout: `src/layout` (Sidebar + Header + Content), `src/pages/{module}`, `src/api`, `src/i18n`.

---

## 12. Localization

- Server resources: `src\Ensa.Domain.Shared\Localization\EnsaResource.resx` (English, fallback)
  and `EnsaResource.tr.resx` (Turkish).
- **The resource key is the error code**, e.g. `Ensa:Company:HeadquarterNotFound`.
- Throw sites carry an English developer fallback plus named data:

```csharp
throw new BusinessException(
        "SSI number is already registered to another workplace.",
        "Ensa:Company:SsiNumberAlreadyRegistered")
    .WithData("SsiNumber", company.SsiNumber);
```

- `EnsaExceptionFilter` resolves the localized template for the request culture and substitutes
  `{PlaceholderName}` from `WithData`. If the key has no resource entry, the fallback message is
  returned — a missing translation degrades to English, never to an empty string.
- Culture resolution order: `?culture=en-US` → `Accept-Language` → default `tr-TR`.
  Supported cultures: `tr-TR`, `en-US`.
- The SPA sends its active language as `Accept-Language` on every request.
- **When you add a new error code, add the key to BOTH resx files.**

---

## 13. Quality Bar

- `dotnet build` must be **0 errors, 0 warnings**.
- Nullable reference types are on; `= null!` only inside navigation entities/DTOs.
- Every entity has an `IEntityTypeConfiguration`. **No configuration means no table** — the
  DbContext declares no `DbSet`s and discovers entities purely through configurations.
- Every `string` column needs `HasMaxLength` or an explicit column type; every FK needs `HasIndex`;
  every unique index on a tenant entity is composite with `TenantId`.
- Comments and XML docs are English, and explain *why* rather than restating the code.
