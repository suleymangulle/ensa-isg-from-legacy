Add the following requirements to the existing architecture contract. These requirements are mandatory and override any ambiguous wording in the previous plan.

## 14. IDENTITY TABLE AND COLUMN NAMING — IMPORTANT CLARIFICATION

Do NOT attempt to rename the User table or its columns to match OpenIddict.

OpenIddict has no User entity/table and therefore defines no User-table naming convention.

The User store belongs exclusively to **ASP.NET Core Identity**.

The final User entity, columns, keys, indexes, constraints and relationships must follow ASP.NET Core Identity conventions unless an existing project-wide table naming convention can be retained without changing Identity semantics.

The only explicitly approved application-specific User property remains:

`TenantId`

Do not add `CompanyId` or any other domain-specific field to the Identity User without explicit approval.

---

# 15. USE THE COMPLETE STANDARD ASP.NET CORE IDENTITY MODEL

Do not implement only `IdentityUser`.

Use ASP.NET Core Identity's standard infrastructure directly, including the standard concepts/tables required by the configured Identity model:

- User
- Role
- UserRole
- UserClaim
- UserLogin
- UserToken
- RoleClaim

Do not create custom replacements for these concepts.

Do not remove a standard Identity table merely because it currently contains no rows.

Do not duplicate their responsibilities in Ensa-specific tables.

Application business permissions are the exception described previously and remain separate from Identity's standard infrastructure.

---

# 16. STANDARD IDENTITY USER CONTRACT MUST BE VERIFIED FROM THE ACTUAL FRAMEWORK VERSION

Do not hard-code assumptions such as "IdentityUser always has exactly N columns" based on this document.

Inspect the ASP.NET Core Identity version actually referenced by the solution and use its real model as the authority.

The final User mapping must be derived from the actual framework type being used.

Preserve all properties, keys, indexes, normalized fields, concurrency fields and relationships required by that Identity version.

`TenantId` is then added as the single approved application-specific extension.

---

# 17. DO NOT CONFUSE IDENTITY ROLES WITH BUSINESS PERMISSIONS

Identity Role infrastructure must remain available and standard.

However:

**Identity Roles are not the application's permission system.**

The migrated legacy permission model remains authoritative for business/application access decisions.

Do not translate legacy permissions into Identity roles merely to simplify authorization.

Do not create one Identity role for every legacy permission.

Do not encode endpoint permissions into role names or claims.

If legacy administrator flags represent genuine roles rather than permissions, analyze them individually and report the mapping before migration.

Do not automatically convert every legacy authorization flag into an Identity Role.

---

# 18. PARAMETERLESS `[Authorize]` IS AN INVARIANT

Application endpoints requiring authenticated/authorized access must use:

```csharp
[Authorize]
```

No business permission metadata may appear in the attribute.

The following are prohibited for application permission resolution:

```csharp
[Authorize(Policy = "...")]
[Authorize(Roles = "...")]
[Authorize("...")]
[Permission("...")]
[RequirePermission("...")]
```

or any equivalent custom attribute containing a permission identifier.

Do not circumvent this rule by creating another attribute with a different name.

The controller/action must not know the permission identifier required to access it.

---

# 19. ENDPOINT-TO-PERMISSION RESOLUTION MUST COME FROM THE LEGACY SYSTEM

Before implementing authorization, inspect the legacy project and determine exactly how it associates:

```text
User
Endpoint / operation
Permission
Tenant/context
```

Do not invent a new mapping convention.

Do not derive permissions from controller/action names unless the legacy implementation actually does so.

Do not introduce a new permission registry, naming convention, enum, attribute, claim or policy database merely because it would be easier to implement.

The migrated implementation must preserve the observable authorization semantics of the legacy project.

If the legacy mechanism cannot be reproduced cleanly without changing its semantics, STOP and report the conflict.

---

# 20. USE THE ASP.NET CORE AUTHORIZATION PIPELINE CORRECTLY

The requirement is behavioral:

```text
[Authorize]
      ↓
authenticated user
      ↓
central authorization resolution
      ↓
legacy permission model
      ↓
ALLOW / FORBID
```

Do not force authorization into arbitrary custom middleware if ASP.NET Core's standard authorization extension points provide the correct implementation.

Prefer integration with the standard ASP.NET Core authentication/authorization pipeline.

Depending on what the existing codebase and legacy permission mechanism require, valid framework extension points may include the standard authorization service/handler/policy infrastructure or middleware where genuinely necessary.

However, regardless of the internal implementation:

- `[Authorize]` remains parameterless.
- permissions remain outside controller/action attributes.
- legacy permission semantics remain authoritative.
- authorization remains centralized.
- authentication and authorization responsibilities remain separated.

Do not replace ASP.NET Core's authorization system with a home-grown request interceptor.

---

# 21. AUTHENTICATION AND AUTHORIZATION MUST REMAIN SEPARATE

Authentication answers:

`Who is this user?`

It is handled by:

```text
ASP.NET Core Identity
+
OpenIddict
```

Business authorization answers:

`May this authenticated user perform this operation?`

It is handled by:

```text
Legacy Permission Model
+
ASP.NET Core authorization integration
```

Do not place business permission data into OpenIddict tokens merely to avoid querying the permission system.

Do not use OpenIddict scopes as application permissions.

Do not use OAuth scopes as a replacement for legacy business permissions.

Do not make OpenIddict responsible for application authorization.

---

# 22. TENANTID MUST NOT CREATE A CUSTOM IDENTITY SYSTEM

`TenantId` is the only approved extension to the Identity User.

Its addition must use normal ASP.NET Core Identity + EF Core extension/mapping mechanisms.

Do not fork or copy framework Identity classes.

Do not recreate UserManager, SignInManager, RoleManager, password hashing, security stamp handling or other Identity services.

Use the framework implementations.

Existing tenancy behavior must be analyzed before changing query filters or tenant resolution.

If existing authentication or authorization requires another User-level tenant/company field, report the dependency.

Do NOT silently preserve it.

---

# 23. SECURITY-SENSITIVE LEGACY FIELDS

Fields containing credentials or security-sensitive information require explicit treatment before migration.

In particular, analyze fields such as:

- legacy password
- Medula password
- security/token-related data
- NationalId or equivalent sensitive identifiers

Do not automatically move such fields unchanged merely because a destination table exists.

For each security-sensitive field report:

1. whether it is still required,
2. where it will be stored,
3. whether encryption/hash protection is required,
4. whether the existing protection is acceptable,
5. whether migration changes its protection.

Never log decrypted credentials or plaintext passwords.

---

# 24. FOREIGN KEYS, IDS AND EXISTING REFERENCES MUST BE PRESERVED

Before renaming/dropping/restructuring the existing User table, identify every foreign key and application reference pointing to it.

The migration must preserve existing User IDs wherever possible.

Do not regenerate User IDs merely to conform to Identity.

Report any relationship that cannot preserve its existing identity/reference.

Before destructive migration verify:

```text
old User.Id == new Identity User.Id
```

for migrated users where preservation is technically possible.

No orphaned foreign keys are acceptable.

---

# 25. DESTRUCTIVE CLEANUP REQUIRES PROOF

A column may be dropped only after it has been classified as either:

```text
MOVED_AND_VERIFIED
```

or:

```text
CONFIRMED_UNUSED
```

For moved fields, verify destination row counts and/or values before dropping the source.

For unused fields, prove from the codebase and legacy data that they are not required.

Do not classify a field as dead merely because it currently contains NULL/zero values.

Code references, migration history, queries and legacy behavior must also be checked.

---

# 26. DO NOT CHANGE DATABASE DATA DURING ANALYSIS

The analysis phase is read-only.

During preparation of the revised plan:

- do not execute migrations,
- do not UPDATE/DELETE/INSERT production or development data,
- do not drop columns,
- do not alter tables,
- do not modify production code.

Database and source inspection must remain read-only until explicit approval is given.

---

# 27. FINAL TARGET ARCHITECTURE

The final responsibility boundary is fixed:

```text
ASP.NET Core Identity
│
├── Standard Identity User
│      └── + TenantId ONLY
│
├── Roles
├── UserRoles
├── UserClaims
├── RoleClaims
├── UserLogins
└── UserTokens


OpenIddict
│
├── Applications
├── Authorizations
├── Scopes
└── Tokens


Legacy Permission Model
│
├── migrated faithfully
├── remains business-permission authority
└── evaluated centrally through ASP.NET Core authorization integration


Domain Model
│
├── profile
├── employment
├── office
├── Medula
└── other non-identity user information
```

There must be no fourth authentication/authorization architecture.

---

# 28. FINAL NON-NEGOTIABLE RULE

When there is a choice between:

A) creating a custom solution,

and

B) using ASP.NET Core Identity / OpenIddict exactly as officially intended,

choose **B**.

The only approved deviations/extensions are:

1. `TenantId` on the Identity User.
2. Preservation and integration of the existing legacy business permission model.

Nothing else is implicitly approved.

If any requirement cannot be achieved within these boundaries:

**STOP. REPORT THE CONFLICT. WAIT FOR APPROVAL.**

Do not solve the conflict by expanding the architecture yourself.