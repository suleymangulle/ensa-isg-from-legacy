# Adding a screen module

The SPA is assembled from self-registering modules. A module owns one folder and touches nothing
outside it, so several people can add screens at the same time without editing the router, the
sidebar or one shared translation bundle — the three files that would otherwise collide on every
change.

## The contract

```
src/pages/<module>/
  module.tsx            # required — routes + sidebar entries
  locales/tr.json       # required — Turkish labels
  locales/en.json       # required — English labels
  api.ts                # DTO types and query hooks for this module
  <Something>Page.tsx   # the screens
```

`module.tsx` exports a `ModuleDefinition`:

```tsx
import type { ModuleDefinition } from '@/modules/registry'
import IncidentListPage from './IncidentListPage'

const definition: ModuleDefinition = {
  routes: [
    { path: 'incidents', element: <IncidentListPage /> },
    { path: 'incidents/:id', element: <IncidentDetailPage /> },
  ],
  nav: [
    { path: 'incidents', labelKey: 'nav.incidents', icon: '⚡', group: 'ohs', order: 40 },
  ],
}

export const { routes, nav } = definition
export default definition
```

`src/modules/registry.ts` collects every `module.tsx` with `import.meta.glob`, and
`src/i18n/index.ts` merges every `locales/<lang>.json` onto the core bundle. Nothing else needs
to change for a new screen to appear.

- `group` is one of `overview`, `workplace`, `ohs`, `finance`, `records`, `admin`.
- `order` sorts entries inside a group; leave gaps of 10.
- Not every route needs a nav entry — detail routes usually have none.
- `permission` names the permission required to **see** the entry, from `PERMISSIONS` in
  `@/api/permissions` (generated from the backend). Use the module's `.Default`, which means "may
  view and list". Omit it only for a screen everyone with a session may open.

  Hiding a link is a courtesy, never a control: every endpoint enforces its own permission and
  answers 403 whatever the menu shows. What this prevents is the opposite failure — a user shown
  thirty entries, twenty-eight of which say "forbidden" the moment they are clicked.

## What is shared (read it, do not edit it)

| File | What it gives you |
|---|---|
| `src/api/http.ts` | The axios instance: bearer token, `Accept-Language`, 401 refresh, `errorMessage()`, `PagedResult`, `ListResult`, `PagedRequest`. |
| `src/api/endpoints.ts` | `usePagedList`, `useEntity`, `useLookup`, and the enum re-exports. |
| `src/api/enums.ts` | **Generated** from the backend enums. Never edit; run `python tools/gen-enums/gen_enums.py`. |
| `src/api/mutations.ts` | `useCreate`, `useUpdate`, `useDelete`, `useAction` — they invalidate the same cache keys the list hooks populate. |
| `src/components/DataTable.tsx` | `DataTable`, `Pagination`, `PageTitle`, `Spinner`, `ErrorPanel`. |
| `src/components/Form.tsx` | `Field`, `controlClass`, `Modal`, `ConfirmDialog`, `SearchBar`. |
| `src/utils/format.ts` | Locale-aware date and number formatting. |
| `src/i18n/locales/{tr,en}.json` | Core keys: `common.*`, `nav.group.*`, `errors.*`, `table.*`, `pagination.*`, `enums.*`. |

`nav.<key>` labels are the one exception: a module adds its own under `nav` in its own locale
file, and the merge puts them next to the core ones.

## Rules

1. **Translate everything.** No literal Turkish or English in a component — every visible string
   is `t('...')`, and both `tr.json` and `en.json` get the key. The two files must always have
   the same key set.
2. **Enum labels come from the locale bundle**, keyed by the numeric value: `t('enums.incidentType.' + row.incidentType)`. The API serialises enums as numbers.
3. **Colours come from the Metronic CSS variables** — `var(--kt-primary)`, `var(--kt-gray-500)`,
   `var(--kt-danger-light)` and so on, plus the Bootstrap utility classes. No new hex codes.
4. **Money is formatted with `formatMoney`, dates with `formatDate`** from `@/utils/format`, so
   a Turkish user sees Turkish formats and an English user sees English ones.
5. **Never invent an endpoint.** Check the running API's Swagger document
   (`https://localhost:7001/swagger/v1/swagger.json`) or the controller in
   `src/Ensa.HttpApi/Controllers/`. A page pointed at a route that does not exist is the exact
   bug `tools/api-tests/frontend_routes.py` was written to catch.
6. **DTO field names are the JSON contract.** Copy them from the `*Dto` class in
   `src/Ensa.Application.Contracts/`; the API serialises them camelCase.
7. **Deletes go through `ConfirmDialog`.** Nothing destructive happens on a single click.
8. **Every list page has**: a `PageTitle`, a `SearchBar` when the endpoint accepts `filter`, a
   `DataTable` with its `label`, and `Pagination` when the endpoint is paged. Loading and error
   states come from `DataTable`, so pass `isLoading` and `error` through.
9. **Accessibility is not optional**: label every input, give every icon-only button an
   `aria-label`, and keep `<h1>` unique per page (`PageTitle` renders it).

## Checking your work

```
npm run lint     # tsc --noEmit
npm run build    # type-check plus production build
```

Both must pass. With the API running, three more checks apply to what you wrote:

```
python tools/api-tests/frontend_calls.py     # every API call you made: path and method exist
python tools/i18n-check/check_locales.py     # no missing key, no tr/en mismatch
python tools/api-tests/frontend_routes.py    # every resource the SPA names resolves
```

`check_locales.py` also verifies the **dynamic** enum labels: for every `t('enums.x.' + value)` in
your code it checks that each numeric member of that backend enum has a label in both languages.
A missing one is invisible until a user meets that exact value and sees `enums.x.7` on screen.
