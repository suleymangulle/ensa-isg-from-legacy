# API verification scripts

Black-box checks that drive the running API over HTTPS. They complement the unit tests: the unit
tests prove the domain rules, these prove the wiring — routing, authorization, serialization,
localization and the database schema — actually holds end to end.

## Prerequisites

1. SQL Server reachable, database created and seeded:

   ```
   sqllocaldb start MSSQLLocalDB
   dotnet run --project src/Ensa.DbMigrator
   ```

2. The API running:

   ```
   dotnet run --project src/Ensa.HttpApi.Host      # https://localhost:7001
   ```

3. The development certificate exported next to these scripts. **TLS verification stays on** —
   the certificate is pinned as the certificate authority rather than verification being
   disabled, so a real certificate problem still fails the run:

   ```
   dotnet dev-certs https --export-path tools/api-tests/ensa-dev-cert.pem --format PEM --no-password
   ```

   The `.pem` is machine-local and deliberately not committed. `ENSA_DEV_CERT` overrides its
   location.

## The scripts

| Script | What it proves |
|---|---|
| `api_test.py` | The Company module end to end: the four OpenIddict grants, CRUD, the navigation DTO, soft delete, the statutory business rules, paging, search and lookup. Self-contained — it creates the records it needs, so it passes against a freshly migrated database. |
| `api_coverage.py` | Every parameterless `GET` in the live Swagger document, driven twice: once anonymously (must answer 401) and once authenticated (must not answer 5xx). Routes are read from Swagger at run time, so a newly landed endpoint joins the sweep automatically. |
| `api_authorization.py` | An authenticated user with **no permissions** gets 403 on protected endpoints, and 200 only where the endpoint needs a session rather than a permission. `api_coverage.py` proves authentication; this proves authorization. A controller carrying `[Authorize]` without a permission name passes the first check and fails this one. Creates its probe user and deletes it again. |
| `dev_stack.py` | The whole development stack the way a browser sees it: the SPA shell, then sign-in and the dashboard's endpoints **through the Vite proxy** rather than against the API directly. A misconfigured proxy leaves a perfectly working API unreachable, and only this check notices. Needs `npm run dev --prefix react/ensa-web` running as well. |
| `api_documents.py` | The document payload path: upload, download, and the promises the design makes — size and SHA-256 measured server-side, duplicate content rejected, an uploaded HTML file never served back as executable, path fragments stripped from the file name, large files byte-identical on the way out, and no anonymous download. |
| `api_mail.py` | The queue-to-delivery chain: configure an account, queue a message, and wait for the **background worker** to send it — then assert the message reached the SMTP server with both recipients resolved. Needs `fake_smtp.py` running, and the API started with `MailDelivery__PollSeconds=5` so the wait is short. |
| `api_company_scope.py` | Who a user is and what they may reach. Three things at once: a host administrator can bind a new user to an organization (and is refused a non-existent one); that user's **token actually carries permission claims**, so they can work rather than merely sign in; and a user bound to a client workplace sees that workplace and nothing else — their own company and employee, 404 on the neighbour's. Also asserts the provider's own staff are unaffected by the scope. Creates everything it needs and deletes it again. |
| `api_customer_portal.py` | The legacy customer portal (`MusteriArayuzu`) has no counterpart missing. Walks all ten of its pages with a real customer user — sign-in, dashboard, employees, departments, equipment, missing trainings, inspection documents, profile, password change, file download — asserting both that each works and that none reaches past the customer's own company. |
| `frontend_menu.py` | The seeded menu and the SPA's own navigation still describe the same product. The sidebar renders from code and the `Menu` module is the configurable administration surface; `tools/gen-enums/gen_menu.py` generates the second from the first, and this fails if it was not re-run. |
| `frontend_permissions.py` | Every permission constant the SPA declares, and every one the sidebar relies on, exists in the API's catalogue. A constant that drifts by one character hides a screen from everyone, permanently and silently — no error, no failing test. |
| `frontend_calls.py` | Every `http.get/post/put/delete` in the SPA source, resolved against the live Swagger document — module constants substituted, route parameters treated as wildcards. It checks the path **and** the HTTP method, so a screen calling `PUT` on a read-only endpoint fails here rather than in front of a user. |
| `frontend_routes.py` | Every resource named in the SPA's `ENDPOINTS` constant resolves against the running API. This is what catches a frontend pointed at a controller that does not exist. |

Run them with `PYTHONIOENCODING=utf-8` on Windows — the console defaults to cp1252 and cannot
print Turkish output.

```
PYTHONIOENCODING=utf-8 python tools/api-tests/api_test.py
PYTHONIOENCODING=utf-8 python tools/api-tests/api_coverage.py
PYTHONIOENCODING=utf-8 python tools/api-tests/api_authorization.py
PYTHONIOENCODING=utf-8 python tools/api-tests/api_company_scope.py
PYTHONIOENCODING=utf-8 python tools/api-tests/api_customer_portal.py
PYTHONIOENCODING=utf-8 python tools/api-tests/frontend_routes.py
PYTHONIOENCODING=utf-8 python tools/api-tests/frontend_calls.py
PYTHONIOENCODING=utf-8 python tools/api-tests/dev_stack.py      # SPA must be running too
```

`api_coverage.py`, `api_authorization.py`, `api_company_scope.py`, `api_customer_portal.py`, `frontend_menu.py`, `frontend_routes.py`, `frontend_calls.py` and `dev_stack.py` exit non-zero on failure, so they can gate a pipeline.

## Seeded credentials

`admin` / `Ensa!2026`. The seeder flags the account to change its password on first sign-in and
warns when the built-in default is still in use; set `Seed__AdminPassword` outside development.
