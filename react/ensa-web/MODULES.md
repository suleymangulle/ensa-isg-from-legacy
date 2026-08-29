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
| `src/components/DataTable.tsx` | `DataTable`, `Pagination`, `PageTitle`, `Spinner`, `ErrorPanel` — the library's `DataGrid` / `Pagination` / `PageHeader` / `Spinner` / `Alert`, wrapped in Turkish and English. |
| `src/components/Form.tsx` | `Field`, `controlClass`, `Modal`, `ConfirmDialog`, `SearchBar` — the library's `FormField` / `Modal` / `Button` / `Input`, wrapped the same way. |
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
10. **The UI comes from `rich-react-component`.** Import `Card`, `Tabs`, `Statistic`, `Badge`,
    `Avatar`, `Skeleton`, `ProgressBar`, `Alert`, `Button`, `Tag`, `Stepper` and the rest straight
    from the package; do not hand-build a card header or a tab strip beside them. What stays in
    `src/components/` is only the wrapping: translated copy, and the props the pages already use.
11. **A successful write announces itself, a failed one does not.** `useCreate`, `useUpdate` and
    `useDelete` raise the library's toast — pass `successMessage` to reword it, `null` to silence
    it. Errors stay inline, in `Modal`'s `error` prop or an `ErrorPanel`, because a message that
    fades after four seconds is the wrong home for something that must be fixed.

## The component library

The SPA is built on [`rich-react-component`](https://www.npmjs.com/package/rich-react-component) —
Base / Remote / Smart layers over Bootstrap and the Metronic conventions this theme already
follows. `ToastProvider` is mounted once in `src/main.tsx`; `useToast` works anywhere below it.

Two things to know before you reach for it:

- **The library ships English literals in a few places** — `DataGrid`'s "Loading…", `Pagination`'s
  "Previous"/"Next" and its `aria-label`, `Alert`'s and `Modal`'s "Close" button label. Where the
  state carries words, render it from the wrapper in `src/components/` instead of handing the job
  to the library prop: `DataTable` draws its own loading and error states and passes the grid only
  the translated `emptyText`, and `Pagination` is still local markup for the same reason. Both
  move to the library the day it accepts those labels as props.
- **`react` is declared as a peer at `^18`** while this SPA is on React 19. `.npmrc` sets
  `legacy-peer-deps=true` so a fresh clone installs; without it `npm install` fails outright.

### What to reach for

| Instead of writing | Use | Its props |
|---|---|---|
| `<input class="form-control">` | `Input` | `value`, `onChange(value: string)`, `type`, `placeholder`, `label`, `error`, `helpText`, `maxLength`, `endAdornment`, `inputProps` (native passthrough, spread last) |
| `<input type="date">` | `Input` + `inputProps={{ type: 'date' }}` | keeps the ISO **string** the API sends. `DatePicker` exists but speaks `Date`, and swapping the value type is a serialisation change, not a component swap — see the exception below |
| `<input type="number">` | `NumberInput` | `value: number \| null`, `onChange(value: number \| null)`, `min`, `max`, `step` |
| `<input type="password">` | `PasswordInput` | as `Input`; it owns the visibility toggle |
| `<select class="form-select">` | `Select` | `options: { value, label, disabled? }[]`, `value`, `onChange(value: TValue \| null)`, `placeholder` (the old empty first `<option>`) |
| `<textarea>` | `TextArea` | `value`, `onChange(value: string)`, `rows`, `maxLength` |
| `<input type="checkbox">` | `CheckBox` / `Switch` | `checked`, `onChange(checked: boolean)`, `label`, `helpText`, `error` |
| a radio group | `RadioGroup` | `options`, `value`, `onChange(value)`, `inline` |
| `<input type="file">` | `FileInput` | `accept`, `multiple`, `onChange(files)` — selection only, no upload |
| `<button class="btn btn-*">` | `Button` | `variant`, `size`, `disabled`, `loading`, `type` |
| an icon-only button | `IconButton` | `icon`, **`aria-label` required**, `iconSize`, `tooltip` |
| `<span class="badge …">` | `Badge` | `variant`, `pill` |
| a chip the user can remove | `Tag` | `variant`, `onRemove` |
| `<div class="card">` | `Card` | `title`, `subtitle`, `icon` \| `avatar`, `actions`, `header` (escape hatch), `footer`, `loading` |
| `<ul class="nav nav-tabs">` | `Tabs` | `items: { key, label, icon?, badge?, disabled?, content? }[]`, `activeKey`, `onChange`, `variant`, `stretch` |
| `<div class="alert">` | `Alert` | `variant`, `dismissible`, `onDismiss` |
| `<div class="spinner-border">` | `Spinner` | `size`, `variant`, `label` — **always pass a translated `label`**, the default is English |
| a grey loading placeholder | `Skeleton` | `width`, `height`, `circle` |
| a KPI tile | `Statistic` | `label`, `value`, `prefix`, `suffix`, `precision`, `trend`, `delta`, `loading` |
| `<div class="progress">` | `ProgressBar` | `value` (0–100), `variant`, `label` |
| `data-bs-toggle="tooltip"` / `"popover"` | `Tooltip` / `Popover` | `content`, `placement`, `children` (the trigger) |
| an initials circle | `Avatar` | `src`, `name` (alt **and** initials), `size` |
| `<div class="accordion">` | `Accordion` | `items: { key, header, content, disabled? }[]`, `multiple`, `openKeys`, `onChange` |
| numbered wizard steps | `Stepper` | `steps`, `currentStep` — presentation only |
| a `…` action dropdown | `Menu` | `items: { key, label, disabled?, danger?, onSelect }[]`, `children` (trigger), `placement` |
| a star rating | `Rating` | `value`, `onChange`, `max`, `disabled` |
| an inline trend chart | `Sparkline` | `type`, `data`, `tone`, `width`, `height`, `label` |
| `<li class="list-group-item">` with an avatar and an action | `ListItem` | `leading`, `title`, `description`, `trailing`, `onClick`, `dense` |
| a table | `DataTable` from `@/components/DataTable` | never the library's `DataGrid` directly — see below |

### When raw markup is right

The cases below, and nothing else. Every file that keeps a raw control or table is named in
`tools/repo-check/check_ui_library.py` with its reason, so the list is enforced rather than
remembered — and anything not on it fails the build.

1. **A `<Link>` that looks like a button.** `Button` always renders a `<button>`; a route change
   needs an anchor, or the user loses middle-click, open-in-new-tab and the browser's own history.
   `<Link to="…" className="btn btn-light-primary">` stays. Use it for navigation only — never for
   an action that just changes state on the page you are on.
2. **The routed sidebar and the routed breadcrumb.** The library's `Menu` is a pop-up action list
   that closes on select, and its `Breadcrumb` renders `href` as a plain `<a>`, which reloads the
   application. `NavLink` and `Link` stay.
3. **`DataGrid`, `Pagination` and `Modal` are reached through the wrappers**, never imported into a
   screen: they render English words with no prop to change them. `@/components/DataTable` and
   `@/components/Form` supply the translation. `tools/repo-check/check_ui_library.py` fails a build
   that imports them directly.
4. **Metronic's soft variants.** `variant="light"` plus the theme's own `btn-light-primary` class is
   how the soft look is reached — the library's `ButtonVariant` has no such member. Writing
   `<button className="btn btn-light-primary">` by hand instead is the version that is wrong.
5. **A date field.** Until the DTO contract is revisited, a date stays a string: `Input` with
   `inputProps={{ type: 'date' }}`. `DatePicker` hands back a `Date`, and what the API accepts is
   `yyyy-MM-dd`.
6. **A control the library has no answer for.** An always-visible listbox (`<select size={n}>`), a
   native colour swatch, a file input beside a monospace hash field — and a compact control inside
   a table cell, because `FieldShell` adds `mb-3` and full-size padding to every field with no prop
   to suppress either. In a filter row that is answered by the theme instead: `SearchBar` carries
   `ensa-toolbar`, which drops the margin and restores the compact sizing for everything inside it.
7. **A page that is a sheet of paper.** The printable invoice keeps its own `<table>` and its own
   card element: there the markup is the deliverable.

`finance/components.tsx`, `observations/components.tsx` and `reports/components.tsx` are on that
list too, but as **pending work rather than a limit** — their `FilterSelect` takes `children` (raw
`<option>` elements) and has 32 call sites, so moving it to the library's `options` API is a change
to every caller. Do it when you are next in those files.

### Before you call a screen done

1. Every visible string is `t('…')`, and the key exists in both `tr.json` and `en.json`.
2. Accessible names survived: labels still pair with their control, icon-only buttons still carry
   an `aria-label`, and any `role` / `aria-*` the raw markup had is either reproduced or supplied
   by the component itself.
3. `onChange` takes the value, not the event — `(value: string) => …`, not `(e) => e.target.value`.
4. Nothing behaves differently: same validation moment, same `disabled` / `readOnly` / `required`,
   same keyboard flow (Tab order, Enter to submit, Escape to close).
5. Validation goes through the component's `error` prop, not a hand-drawn `invalid-feedback` div.
6. No new dependency, and no raw markup left beside the component that replaced it.
7. No new hex code: Metronic CSS variables and Bootstrap utilities only.
8. `npm run lint` and `npm run build` pass, with no `any` added to force a prop through.
9. `python tools/i18n-check/check_locales.py` and `python tools/repo-check/check_ui_library.py` pass.
10. Money still goes through `formatMoney` and dates through `formatDate` — a converted field must
    not fall back to the browser's own formatting.

### The three conversions that go wrong

- **A `<select>`, a date or a number field on a write form.** The callback shape and the value type
  both change. Trace the call site to the `*Dto` in the module's `api.ts` and confirm an
  enum-backed select still stores the numeric member, not the option's string.
- **`nav-tabs` → `Tabs`.** Bootstrap kept every panel mounted; `Tabs` renders only the active one.
  A tab holding a half-filled sub-form loses it on switch. Check that first.
- **Importing `Pagination` / `DataGrid` / `Breadcrumb` from the package** instead of the wrapper.
  English text and full page reloads, both invisible to `check_locales.py`.

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
