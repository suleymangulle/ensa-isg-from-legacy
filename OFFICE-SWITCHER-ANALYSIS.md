# Office / OSGB Switcher — Evidence-Based Analysis and Implementation Plan

**Status:** analysis and planning only. No source file was created, modified or deleted for this
work other than this document.

**Old project root inspected:** `D:\EnsaProject` (read-only)
**New project root inspected:** `D:\EnsaFromLegacyEnsa`

**Method.** The legacy control was located by searching for the concept (`Ofis`, `OfisId`,
`Ofisler_T`, `KullaniciOfis_T`, `Kurum`, `Sube`) rather than for visible text, then traced from
the rendered markup to its JavaScript handler, to the MVC action, to the session state it writes,
to the queries that read that state back. The new project was read the same way: shell, auth,
HTTP client, cache, permissions, routing, tokens, and the component library's own type
declarations. Build-output copies under `ENSA_ISG/obj/Debug/AspnetCompileMerge/**` were used only
to confirm that the same markup is what gets compiled; every citation below points at a source
file, never at build output.

Anything the production code does not prove is in **[§9 Unverified findings](#9-unverified-findings)**.

---

## Table of contents

1. [The old application — proven behaviour](#1-the-old-application--proven-behaviour)
2. [The new application — current state](#2-the-new-application--current-state)
3. [Old → new mapping](#3-old--new-mapping)
4. [Implementation plan, file by file](#4-implementation-plan-file-by-file)
5. [Proposed user and system flow](#5-proposed-user-and-system-flow)
6. [Visual and interaction plan](#6-visual-and-interaction-plan)
7. [Risks and required decisions](#7-risks-and-required-decisions)
8. [Confirmed findings](#8-confirmed-findings)
9. [Unverified findings](#9-unverified-findings)
10. [Required backend changes](#10-required-backend-changes)
11. [Required frontend changes](#11-required-frontend-changes)
12. [Recommended implementation order](#12-recommended-implementation-order)
13. [Acceptance criteria](#13-acceptance-criteria)
14. [Decisions required from the user](#14-decisions-required-from-the-user)

---

## 1. The old application — proven behaviour

### 1.1 Where the control lives

The selector is a **native `<select>` rendered inside the side menu partial**, absolutely
positioned near the bottom-left of the sidebar.

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Shell layout | `ENSA_ISG/Views/Shared/_Layout.cshtml` | `Html.RenderAction("Index", "SideMenu")` inside `<div id="dv-main-menu">` | 163 |
| Selector markup | `ENSA_ISG/Views/SideMenu/Index.cshtml` | `<select id="ddl-main-ofis-list">` | 477–486 |
| Hidden mirror | `ENSA_ISG/Views/SideMenu/Index.cshtml` | `<input id="hfMenuOfisId" type="hidden" value="@Model.OfisId" />` | 487 |
| View model | `ENSA_ISG/Models/MenuModels/SideMenu.cs` | `SideMenu.OfisId`, `SideMenu.OfisList` | 13, 23 |

The exact markup (source, not build output):

```
@if (Model.OfisList.Count > 1 && BaseController.Kullanici.PersonelTuru == "Admin")
{
    <select class="" id="ddl-main-ofis-list" style="width: 100%; padding: 5px; position:absolute; left:0px; bottom:100px;">
        <option value="0">Tüm Ofisler</option>
        @foreach (var ofis in Model.OfisList)
        {
            <option value="@ofis.OfisId" @(ofis.OfisId == Model.OfisId ? "selected" : "")>@ofis.OfisAdi</option>
        }
    </select>
}
<input id="hfMenuOfisId" type="hidden" value="@Model.OfisId" />
```

Immediately above it, at `Views/SideMenu/Index.cshtml:469–476`, sits the Ensa logo and contact
block, also absolutely positioned (`bottom:150px`). The two together form the legacy sidebar
"footer" — there is no flex footer region; both blocks are `position:absolute` with hard-coded
`bottom` offsets inside the menu container.

> **Note on the duplicate.** `ENSA_ISG/Views/SideMenu/Index (1).cshtml:468–478` contains the same
> block. It is a stale copy kept beside the live view (the project has many `… (1).cs`/`… (1).cshtml`
> siblings). The live view is `Index.cshtml`, which is what `SideMenuController.Index()` returns via
> `PartialView(sideMenuModel)`.

### 1.2 Who loads the list

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Controller action | `ENSA_ISG/Controllers/SideMenuController.cs` | `SideMenuController.Index()` `[ChildActionOnly]` | 24–81 |
| List assignment | same | `sideMenuModel.OfisList = OfisIslemleri.GetOfisler(Kullanici, true).Where(a => !a.IsDeleted).ToList();` | 53 |
| Active id assignment | same | `sideMenuModel.OfisId = OfisId;` | 79 |
| List query | `Businness/Genel/OfisIslemleri.cs` | `OfisIslemleri.GetOfisler(CRMContext, Kullanici_T, bool?)` | 20–27 |

`GetOfisler` has two branches, both proven in source:

```
var bagliOfisler = ctx.KullaniciOfis_T.Where(o => o.KullaniciId == Kullanici.KullaniciId).Select(o => o.OfisId);
if (bagliOfisler.Count() != 0)
    return ctx.Ofisler_T.Where(a => a.KurumId == Kullanici.KurumId && (a.Aktif == Aktif || Aktif == null) && bagliOfisler.Contains(a.OfisId)).ToList();

return ctx.Ofisler_T.Where(a => a.KurumId == Kullanici.KurumId && (a.Aktif == Aktif || Aktif == null)
    && (Kullanici.OfisAdmin || Kullanici.PersonelTuru == "ofis-admin"
        ? Kullanici.OfisId.HasValue ? a.OfisId == Kullanici.OfisId.Value : false
        : true)).ToList();
```

So the list is: **the user's explicitly assigned offices** if any `KullaniciOfis_T` rows exist;
otherwise **every active office of the user's `KurumId`**, unless the user is an office
administrator, in which case only their own `OfisId`. The side-menu call additionally filters
`Aktif == true` (the `true` argument) and `!IsDeleted`.

There is **no dedicated "office list" HTTP endpoint** for the switcher. The list is rendered
server-side into the page as part of the sidebar partial on every request.

### 1.3 Models, DTOs, entities

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Office row | `DataAccess/Entities/Ofisler_T.cs` | `Ofisler_T` — `OfisId`, `OfisAdi`, `Telefon`, `Faks`, `Adres`, `YetkiliKisi`, `YetkiliKisiEmail`, `Aktif`, `SehirId`, `COFirmaId`, `IsDeleted`, `MerkezOfis`, `KurumId` | 1–29 |
| User↔office link | `DataAccess/Entities/KullaniciOfis_T.cs` | `KullaniciOfis_T` — `KullaniciOfisId`, `KullaniciId`, `OfisId`, `Sure`, `KurumId` | 1–20 |
| Ajax envelope | `ENSA_ISG/Models/AjaxResultModel.cs` | `AjaxResultModel` — `state`, `message`, `type`, `showAlert`, `data` | 9–37 |
| Extra office helpers (not used by the switcher) | `Businness/Ofisler/Ofisler.cs` | `GetOfisById`, `GetMerkezOfis`, `GetOfisList` | 14–40 |

### 1.4 The switch endpoint

**Client handler** — `ENSA_ISG/app/app.js:1065–1084`, registered inside the global
`$(function () { … })` at line 1055:

```
$("#ddl-main-ofis-list").change(function () {
    var _ofisId = $("#ddl-main-ofis-list").val();
    var data = { OfisId: _ofisId };
    startProcess();
    $.ajax({
        type: "POST",
        data: JSON.stringify(data),
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        url: "/default/SetOfisId",
        success: function (rslt) {
            $("#hfMenuOfisId").val(_ofisId);
            $("#hfOfisId").val(_ofisId);
            location.reload();
        },
        error: function (rslt) {
            alert(rslt.d);
        }
    });
});
```

**Server action** — `ENSA_ISG/Controllers/DefaultController.cs:215–242`:

```
[Log]
[HttpPost]
[CacheRemove("FirmaListesiniExceleAktar", KaldirilacakProp.KullaniciId)]
public string SetOfisId(int OfisId)
{
    var rslt = new AjaxResultModel(new { OfisId });
    setOfisId(OfisId);
    rslt.state = 1;
    return Serialize(rslt);
}

static void setOfisId(int ofisId)
{
    if (ofisId == 0) { Ofis = null; OfisId = ofisId; }
    else
    {
        OfisId = ofisId;
        using (var ctx = CRMContext.CRM())
            Ofis = ctx.Ofisler_T.FirstOrDefault(a => a.KurumId == Kullanici.KurumId && a.OfisId == ofisId);
    }
}
```

| Item | Proven value |
|---|---|
| **Method / URL** | `POST /default/SetOfisId` (route `{controller}/{action}/{id}`, `RouteConfig.cs:27–31`) |
| **Content type** | `application/json; charset=utf-8` |
| **Request body** | `{"OfisId":"<selected option value>"}` — the value is the raw `select.val()` **string**; MVC model-binds it to `int` |
| **`0`** | sentinel for "Tüm Ofisler" (all offices) |
| **Response body** | `AjaxResultModel` serialized: `{"state":1,"message":null,"type":null,"showAlert":true,"data":{"OfisId":<int>}}` |
| **Response on failure** | none defined — the action has no `try/catch`; an exception is handled by the global `HandleErrorAttribute` (`App_Start/FilterConfig.cs:10`) |

### 1.5 Where the selection is stored

**ASP.NET server session only.** There is no cookie, no `localStorage`, no `sessionStorage`, no
token and no claim carrying the office.

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Active id | `ENSA_ISG/Controllers/BaseController.cs` | `static int OfisId` — get `Session["ofisid"]`, set `Session["ofisId"]` | 202–215 |
| Active record | `ENSA_ISG/Controllers/BaseController.cs` | `static Ofisler_T Ofis` — `Session["ofis"]` | 216 |
| WebForms mirror | `ENSA_ISG/basepage.cs` | `static int OfisId` — `Session["ofisid"]` | 27 |
| WebForms user control mirror | `ENSA_ISG/menu.ascx.cs` | `int OfisId` — `Session["ofisid"]` | 19 |

Note the literal case difference between the getter (`"ofisid"`) and the setter (`"ofisId"`) at
`BaseController.cs:207` vs `:214`. See [§9 Unverified](#9-unverified-findings).

Client-side, the value is mirrored into two hidden inputs that page scripts read as their default
filter:

* `#hfMenuOfisId` — `Views/SideMenu/Index.cshtml:487`, one per page (part of the sidebar).
* `#hfOfisId` — declared per screen, e.g. `Views/CariHareketler/Index.cshtml:20`,
  `Views/GenelIstatistik/Index.cshtml:149`, `Views/MuhasebeModulu/SatisFaturasi.cshtml:28`,
  `Views/ZiyaretTakvimi/Index.cshtml:151`, `Views/FirmaEkle/Index.cshtml:62`.
  Read by e.g. `app/Controllers/mainController.js:99`,
  `app/Controllers/MuhasebeModulu/CariHareketlerController.js:6`,
  `app/Controllers/MuhasebeModulu/FaturalarController.js:6`,
  `app/Controllers/MuhasebeModulu/FirmaBakiyeListesiController.js:6`,
  `app/Controllers/MuhasebeModulu/FinansRaporuController.js:168`.

### 1.6 How the initial active office is determined

| Path | File | Symbol | Lines |
|---|---|---|---|
| Sign-in | `ENSA_ISG/Controllers/LoginController.cs` | `Login` — `if (kullanici.PersonelTuru != "ser-admin") HttpContext.Session["ofisid"] = kullanici.OfisId;` then `Ofis = OfisIslemleri.GetOfisler(Kullanici, true).FirstOrDefault();` | 157–160 |
| Profile re-seed | `ENSA_ISG/Controllers/DefaultController.cs` | `KurumProfiliAyarla` — `if (sessionKullanici.PersonelTuru != "Admin" && … != "ser-admin") HttpContext.Session["ofisid"] = sessionKullanici.OfisId;` then `Ofis = OfisIslemleri.GetOfisler(Kullanici, true).FirstOrDefault();` | 99–102 |
| Fallback | `ENSA_ISG/Controllers/BaseController.cs` | `OfisId` getter returns `0` when the session slot is null | 205–210 |

So the initial value is **`Kullanici_T.OfisId` (the user's home office column)**, and `0` — "all
offices" — when that column is null or the user is `ser-admin`. `Session["ofis"]` (the record) is
seeded independently from `GetOfisler(...).FirstOrDefault()`, which is **not** guaranteed to be the
same office as `Session["ofisid"]`.

### 1.7 What happens on change — exact sequence

1. `change` fires on `#ddl-main-ofis-list` (`app/app.js:1065`).
2. `startProcess()` (`app/app.js:685–691`) opens a blocking SweetAlert2 "Yükleniyor" modal.
3. `POST /default/SetOfisId` with `{"OfisId":"…"}`.
4. `CacheRemoveAttribute.OnActionExecuting` (`ENSA_ISG/Attributes/CacheRemoveAttribute.cs:30–39`)
   runs **before** the action and calls
   `BaseController.cacheManager.RemoveByPatterns("KullaniciId{id}", "FirmaListesiniExceleAktar")`
   → `MemoryCacheManager.RemoveByPatterns` (`Base/CrossCuttingConcerns/Caching/CacheManager/MicrosoftCacheManager/MemoryCacheManager.cs:69+`),
   a regex sweep over the in-process `IMemoryCache` keys.
5. `setOfisId(OfisId)` writes `Session["ofisId"]` and `Session["ofis"]`.
6. The action returns `{"state":1,…}`.
7. The success callback writes the value into `#hfMenuOfisId` and `#hfOfisId`, then calls
   **`location.reload()`**.
8. The full page reload re-runs `SideMenuController.Index()` (fresh office list, fresh
   `Model.OfisId`), re-renders every server-side query against the new `BaseController.OfisId`,
   and reseeds every `#hfOfisId`.

**Token / cookie / claims / session:** no token exists in this application; the ASP.NET session
cookie is untouched; no claim is written. Only two server-session slots change (`ofisId`, `ofis`).

**Reload / redirect:** yes — an unconditional `location.reload()` to the same URL. There is no
redirect, so the user stays on the same screen.

**User details, permissions, menus:** none are refetched *as a consequence of the office change*;
they are rebuilt because the whole page is rebuilt. The menu itself is **not** office-dependent:
`Businness/Menu` contains no reference to `OfisId` (verified by grep over `Businness/Menu/`), and
`MenuIslemleri.GetMenuList(...)` is called with `lochalPath, kullaniciid, "project-crm", KurumTuru,
PaketTuru, KurumYetkinModuller, Kullanici` (`SideMenuController.cs:88–91`) — no office argument.
Permission checking (`ENSA_ISG/Algoritmalar/YetkiKontrolu.cs`) likewise contains no `OfisId`
reference.

**Office-dependent state and caches:** cleared only by the full reload, plus the one explicit
cache-pattern removal in step 4. Note that `SideMenuController.Index()` writes a menu model into
`HttpContext.Application[menuKeyword]` at line 71 but the value is never read back — the local
`sideMenuModel` is initialised to `null` at line 27 and the `if (sideMenuModel == null)` at line 29
is therefore always true. The menu cache is write-only and inert.

### 1.8 How widely the selection is consumed

`OfisId` appears **168 times** across **29 controller files** under `ENSA_ISG/Controllers/`.
Representative filtering, all in `ENSA_ISG/Controllers/FirmaListController.cs`:

* line 67 — `(OfisId == 0 || f.OfisId == OfisId || GenelMethodsController.ProfMu())`
* line 100 — `(o.OfisId == OfisId || OfisId == 0 || OfisId == -1 || GenelMethodsController.ProfMu())`
* lines 306–307 — `if (OfisId != 0 && !Kullanici.SerAdmin && !ProfMu()) firmalar = firmalar.Where(f => f.OfisId == OfisId || f.OfisId == 1);`

Others: `DefaultController.getDashboardInfoes` (line ~133 `f.OfisId == OfisId || OfisId == 0`),
`KullaniciListController.cs:59,150–166,234,243`, `CariHareketlerController.cs`,
`SatisFaturalariController.cs`, `KasaIRaporlamaController.cs`, `GenelIstatistikController.cs`,
`ZiyaretTakvimiController.cs`, `BazalMaliyetController.cs`, `ModulArsiviController.cs`,
`EReceteListesiController.cs`, `FirmaBakiyelistesiController.cs`, `FirmaRaporlamaController.cs`,
`ISGKontrolRaporuController.cs`.

### 1.9 Edge cases — what the code actually does

| Case | Proven behaviour | Evidence |
|---|---|---|
| **User has exactly one office** | The `<select>` is **not rendered at all**. `#hfMenuOfisId` still is. | `Views/SideMenu/Index.cshtml:477` — `Model.OfisList.Count > 1` |
| **User is not `PersonelTuru == "Admin"`** | The `<select>` is **not rendered**, even with many offices. | same line |
| **Office not assigned to the user** | `SetOfisId` accepts it. `setOfisId` writes `Session["ofisId"] = ofisId` **before** looking the record up, and the lookup is scoped by `KurumId` only — it never consults `KullaniciOfis_T`. | `DefaultController.cs:229–241` |
| **Office id from another tenant / non-existent** | `OfisId` is still written to the session; `Ofis` becomes `null`. Every `f.OfisId == OfisId` filter then matches nothing, so screens go empty rather than erroring. | `DefaultController.cs:236–240` |
| **Deleted / inactive office** | Not offered in the list (`Aktif == true`, `!IsDeleted` at `SideMenuController.cs:53`), but `SetOfisId` would accept it if posted directly — no `Aktif`/`IsDeleted` check there. | `OfisIslemleri.cs:23`; `DefaultController.cs:239` |
| **Request fails** | `alert(rslt.d)` runs. `AjaxResultModel` has **no `d` member** (`Models/AjaxResultModel.cs:9–17`) and jQuery's error callback receives a `jqXHR`, which has none either — so the user sees a browser alert reading `undefined`. | `app/app.js:1081` |
| **Blocking modal on failure** | `startProcess()` is called but `stopProcess()` is **never** called in this handler, on success or failure. On failure the SweetAlert2 loading overlay (`allowOutsideClick:false`) stays up behind the alert. | `app/app.js:1068`, 685–697 |
| **Rollback after failure** | None. The `<select>` keeps the newly chosen option while the server still holds the old one, so the UI and the session disagree until the next page load. | `app/app.js:1079–1082` |
| **Repeated changes while pending** | Not guarded. Each `change` fires another POST; the last response to arrive wins the `location.reload()`, and the session holds whichever `setOfisId` executed last. | `app/app.js:1065–1083` |

### 1.10 The sibling mechanism: the customer branch (Şube) picker

Not the same control, but the only other context switcher in the legacy shell, and worth
recording because it shows the house pattern:

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Markup | `ENSA_ISG/Views/SideMenu/Index.cshtml` | branch list inside `#HesapDialog` (the account dialog), shown when `BaseController.Subeler.Count() > 1` | 321–355 |
| Handler | `ENSA_ISG/app/Controllers/mainController.js` | `$scope.FirmaIdDegistir = function (firmaId) { window.location.href = "/FirmaDetay?firma-id=" + firmaId; }` | 38–40 |
| Server state | `ENSA_ISG/Controllers/BaseController.cs` | `static int FirmaId` — query string `firma-id` wins, else `Session["FirmaId"]` | 216–234 |
| Unused POST | `ENSA_ISG/Controllers/FirmaDetayController.cs` | `FirmaIdDegistir(int FirmaId)` — sets `BaseController.FirmaId`; the sidebar does **not** call it | 78–83 |

The branch switch is a **full navigation**, not an AJAX call: the context travels in the URL.

---

## 2. The new application — current state

### 2.1 Shell and layout

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Shell | `react/ensa-web/src/layout/MainLayout.tsx` | `MainLayout` | 22–61 |
| Rail + drawer state | same | `const [isCollapsed, setIsCollapsed] = useState(false)`, `const [isMobileOpen, setIsMobileOpen] = useState(false)` | 24–25 |
| Sidebar wrapper | `react/ensa-web/src/layout/Sidebar.tsx` | `Sidebar({ collapsed, onCollapsedChange, mobileOpen, onMobileOpenChange })` | 42–157 |
| Top bar | `react/ensa-web/src/layout/Header.tsx` | `Header` — rail toggle, drawer button, language, appearance popover, user `Menu` | 39–153 |
| Page footer (not the sidebar's) | `react/ensa-web/src/layout/MainLayout.tsx` | `<Divider />` + `Container` with `t('app.footer', …)` | 51–57 |

The sidebar is **not** unmounted when collapsed; the library renders a real rail
(`MainLayout.tsx:18–20` comment, and `Sidebar.tsx:124` — `className={mobileOpen ? undefined : 'd-none d-lg-block'}`).

**`collapsed` and `mobileOpen` are component state only** — neither is persisted, so a reload
returns to the expanded rail with the drawer closed.

### 2.2 The component library's sidebar contract

`react/ensa-web/node_modules/rich-react-component/dist/base/Sidebar.d.ts`:

| Prop | Lines | Currently passed by `layout/Sidebar.tsx`? |
|---|---|---|
| `header?: ReactNode` | 91 | **yes** (`Sidebar.tsx:141–150`) |
| `footer?: ReactNode` | 92 | **no** |
| `collapsedFooter?: ReactNode` | 93–101 | **no** |
| `collapsed` / `onCollapsedChange` | 74–76 | yes |
| `mobileOpen` / `onMobileOpenChange` / `mobileLabel` / `closeMobileOnSelect` | 84–90 | yes |
| `renderLink` | 106 | yes |

The library's own doc comment on `collapsedFooter` (lines 93–101) states the rule this plan must
respect: *"A footer sized for the expanded aside cannot survive a ~58px rail … Supply a compact
icon-sized control here. Additive: when it is omitted the footer region is not rendered while
collapsed."*

Its stylesheet already provides exactly the layout the brief asks for
(`node_modules/rich-react-component/dist/style.css`):

```
.rrc-sidebar__body{flex:1 1 auto;min-block-size:0;overflow-y:auto;overflow-x:hidden;padding:.75rem 0}
.rrc-sidebar__footer{flex:0 0 auto;padding:var(--rrc-sidebar-pad);border-block-start:1px solid var(--rrc-sidebar-border);min-inline-size:0}
.rrc-sidebar{… inline-size:var(--rrc-sidebar-expanded-width); transition:inline-size var(--rrc-sidebar-transition-duration) var(--rrc-sidebar-transition-easing)}
.rrc-sidebar--collapsed{inline-size:var(--rrc-sidebar-collapsed-width)}
```

So the navigation region already scrolls independently, the footer is already pinned, and the
width transition already exists. There is also a right-hand flyout used by collapsed submenus:

```
.rrc-sidebar__flyout{position:fixed;inset-block-start:var(--rrc-flyout-block-start,0);inset-inline-start:var(--rrc-flyout-inline-start,100%);z-index:1030;min-inline-size:13rem;…}
```

### 2.3 Authentication and session

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Session context | `react/ensa-web/src/auth/AuthContext.tsx` | `AuthProvider`, `useAuth`, `UserInfo` | 13–145 |
| Claims → user | same | `userFromToken` — reads `sub`, `given_name`, `family_name`, `name`/`preferred_username`, `email`, `ensa:tenantId`, `ensa:companyId`, `role` | 44–68 |
| Permission fetch | same | `fetchPermissions()` → `GET /account/permissions` | 78–85 |
| Token storage | `react/ensa-web/src/auth/tokenStore.ts` | `tokenStore` — `localStorage` keys `ensa.access_token`, `ensa.refresh_token` | 3–98 |
| Grants | same | `signIn` (`password`), `refresh` (`refresh_token`), client id `ensa-spa` | 20, 66–97 |

`UserInfo` carries `tenantId` and `companyId`. **There is no office field.**

### 2.4 API client and interceptors

`react/ensa-web/src/api/http.ts`:

* `http = axios.create({ baseURL: '/api', … })` — line 21.
* Request interceptor — `Authorization: Bearer …` and `Accept-Language` — lines 26–34.
* Response interceptor — single shared in-flight refresh on 401, then retry; on failure clears the
  token and `window.location.href = '/login'` — lines 36–58.
* `errorMessage(error)` — maps the `EnsaErrorBody` envelope, 404 → `errors.moduleUnavailable`,
  403 → `errors.forbidden`, no response → `errors.network` — lines 66–83.

**No request interceptor adds any context header today.** `X-Ensa-TenantId` exists on the server
(`src/Ensa.Application.Contracts/Permissions/EnsaPermissions.cs:427`) but the SPA never sends it
(no occurrence of `X-Ensa` anywhere under `react/ensa-web/src`).

### 2.5 Query cache and global state

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Query client | `react/ensa-web/src/main.tsx` | `new QueryClient({ defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false, staleTime: 30_000 } } })` | 19–23 |
| Provider order | same | `AppearanceProvider > QueryClientProvider > BrowserRouter > AuthProvider > ToastProvider > App` | 49–72 |
| Shared list/entity hooks | `react/ensa-web/src/api/endpoints.ts` | `usePagedList`, `useEntity`, `useCompanyDetail`, `useReferenceData`, `useLookup` | 193, 206, 218, 241, 254 |
| Shared writes | `react/ensa-web/src/api/mutations.ts` | `resourceKey`, `useCreate`, `useUpdate`, `useDelete`, `useAction` | 15, 51, 72, 93, 114 |

Cache keys are `[resource, …]`; invalidation is per-resource
(`mutations.ts:64, 85, 102, 130–134`). There is **no global "context changed, drop everything"
path** today.

### 2.6 Permissions and menu

| Concern | File | Symbol | Lines |
|---|---|---|---|
| Client permission list | `react/ensa-web/src/auth/AuthContext.tsx` | `fetchPermissions` / `hasPermission` | 78–85, 128–131 |
| Permission constants | `react/ensa-web/src/api/permissions.ts` | `PERMISSIONS.Office.{Default,Create,Update,Delete}` | 23–28 |
| Menu source | `react/ensa-web/src/modules/registry.ts` | `moduleRoutes()`, `moduleNavigation(hasPermission)`, `NAV_GROUPS`, `NAV_GROUP_ICONS` | 78–102, 40, 51–58 |
| Module discovery | same | `import.meta.glob('../pages/*/module.tsx', { eager: true })` | 68 |
| Server-side menu (unused by the shell) | `react/ensa-web/src/pages/settings/api.ts` | `GET api/menu`, `GET api/menu/my-menu` | 166–200 |

The rendered sidebar is built **at build time** from module files and filtered by permission at
render time. It is not fetched. The backend `MenuController` exists and is seeded from the SPA
(ADR-035, `docs/DECISIONS.md:745`), but the shell does not consume it.

**Permissions do not depend on the office.** `IPermissionManager.GetPermissionTargetsAsync(int userId, …)`
— `src/Ensa.Domain/Membership/PermissionManager.cs:23, 165` — takes only a user id.
`HttpContextCurrentUser.HasPermission` resolves through the same manager
(`src/Ensa.HttpApi.Host/Ambient/HttpContextCurrentUser.cs:99–111`).

### 2.7 Routing and unauthorized routes

`react/ensa-web/src/App.tsx`:

* `ProtectedRoute` (lines 14–25) checks **only** `user` and `isReady`; it does not check
  permissions.
* Module routes are rendered flat under `/` (lines 51–62), with `<Route path="*" element={<NotFoundPage />} />`.

So today a user who types a URL for a screen they lack permission for **reaches the screen**; the
API answers 403 and the screen renders `errors.forbidden` through `errorMessage`
(`api/http.ts:80`). There is no client-side route guard to extend.

### 2.8 Notifications, dropdowns, primitives

| Primitive | Declaration | Placement options |
|---|---|---|
| `ToastProvider` / `useToast` | `dist/base/Toast.d.ts` | fixed toast stack; `success/error/info/warning` |
| Live region shim | `react/ensa-web/src/components/ToastRegion.tsx:19–30` | marks `.toast-container` as `role=status aria-live=polite` |
| `Menu` | `dist/base/Menu.d.ts` | `placement?: "start" \| "end"` — **no side placement** |
| `Popover` | `dist/base/Popover.d.ts` | `placement?: "top" \| "bottom"` — **no side placement** |
| `Tooltip` | `dist/base/Tooltip.d.ts` | `placement?: "top" \| "bottom"`, plus `wrapperClassName` explicitly for "a full-width row (e.g. a collapsed Sidebar item)" |
| `Popup` (anchored panel) | `dist/base/shared/Popup.d.ts` | exported from `base/index.d.ts:62`; open/close, escape, outside-click; documented limitation: *"positioning is plain CSS … no portal/collision detection yet"* |
| `ListItem`, `Avatar`, `Badge`, `Spinner`, `Button`, `IconButton`, `Text`, `Flex` | `dist/base/*.d.ts` | available |

**Consequence:** no shipped primitive opens *to the right of* an anchor. The collapsed-rail popup
must either reuse `Popup` with a small positioning wrapper, or reuse the library's own
`.rrc-sidebar__flyout` CSS class, which already implements exactly that geometry.

### 2.9 Design tokens, themes, responsive behaviour

| Concern | File | Lines |
|---|---|---|
| Metronic `--kt-*` palette, sidebar widths (`265px` / `76px`), `--rrc-*` bridge | `react/ensa-web/src/styles/ensa.scss` | 21–79 |
| Dark palette under `[data-bs-theme='dark']` | same | 85–133 |
| Raw-control dark fixes | same | 142–153 |
| Bootstrap entry / font stack | `react/ensa-web/src/styles/metronic.scss` | whole file |
| Colour schemes registered with the library | `react/ensa-web/src/styles/appearance.ts` | whole file (`ENSA_COLOR_SCHEME_ID`, `APPEARANCE_STORAGE_KEY`, `offeredColorSchemes()`) |
| Theme bootstrapping before first paint | `react/ensa-web/src/main.tsx` | 35–45 |
| Theme applied to `documentElement` | same | 49–60 |

Responsive: Bootstrap breakpoints only. `d-none d-lg-block` hides the aside below `lg`
(`Sidebar.tsx:124`); the drawer button is `d-lg-none` and the rail toggle `d-none d-lg-inline-flex`
(`Header.tsx:67, 76`).

### 2.10 Testing infrastructure

| Kind | Location |
|---|---|
| .NET unit tests | `test/Ensa.Domain.Tests`, `test/Ensa.Application.Tests`, `test/Ensa.EntityFrameworkCore.Tests`, `test/Ensa.TestBase` (`dotnet test`) |
| API verification | `tools/api-tests/*.py` — `api_coverage.py`, `api_authorization.py`, `api_privilege_escalation.py`, `api_company_scope.py`, … |
| SPA↔API contract | `tools/api-tests/frontend_calls.py` — parses every `http.get/post/put/delete/patch` literal (incl. template literals with in-file constants) and checks it against live Swagger |
| SPA routes / permissions / menu | `tools/api-tests/frontend_routes.py`, `frontend_permissions.py`, `frontend_menu.py` |
| Repo invariants | `tools/repo-check/check_ui_library.py`, `check_permission_endpoints.py`, `check_no_secrets.py` |
| i18n | `tools/i18n-check/check_locales.py` |

**There is no JavaScript test runner.** `react/ensa-web/package.json` scripts are `dev`, `build`,
`preview`, `lint` (`tsc --noEmit`) only.

Two constraints that bind this feature:

* `check_ui_library.py` applies its raw-markup rules (`<select>`, `<table>`, `btn`, `card`, …)
  **only** to files under `src/pages/**/*.tsx` (line 152: `if path.startswith(PAGES) and path.endswith(".tsx")`).
  A switcher in `src/layout/` would not be flagged — but ADR-038 (`docs/DECISIONS.md:842`) still
  applies, so the plan uses library components regardless.
* `check_permission_endpoints.py` fails the build for **any** new controller action without a row
  in `src/Ensa.DbMigrator/Seeding/PermissionEndpointSeedData.cs`.

### 2.11 Does the new project already have office/tenant switching?

**No switcher exists.** What exists:

| Capability | Status | Evidence |
|---|---|---|
| Office CRUD (list, detail, lookup, create, update, delete) | Present | `src/Ensa.HttpApi/Controllers/OfficeController.cs:18–76`; `src/Ensa.Application/Tenancy/OfficeAppService.cs`; SPA `src/pages/tenancy/{OfficeListPage,OfficeDetailPage,OfficeFormModal}.tsx`, `api.ts:250–290` |
| Office authorization | **All office reads require `Ensa.Office`** | `OfficeAppService.cs:40, 53, 88, 160`; seed rows `PermissionEndpointSeedData.cs:250–256` |
| User↔office assignment | Present as data | `src/Ensa.Domain/Membership/UserOffice.cs:11–19`; migrated by `src/Ensa.DataMigrator/Steps/UserSplitStep.cs:221–226` and `TenancyStep.cs:555–595` |
| The user's *own* office | One value, first match | `src/Ensa.Application/Membership/AccountAppService.cs:42` — `userOfficeRepository.FindAsync(o => o.UserId == user.Id)`; surfaced as `ProfileDto.OfficeId` (`ProfileDto.cs:40`) |
| Ambient office context | **Absent** | `ICurrentUser` (`src/Ensa.Domain/Common/ICurrentContext.cs:18–36`) has `TenantId` and `CompanyId`, no office. `HttpContextCurrentUser` (whole file) reads no office claim. |
| Office as a query filter | Per-request input only | `CompanyAppService.cs:252–254`, `CashRegisterAppService.cs:369–380`, `InvoiceAppService.cs:409–429`, `OhsReportAppService.cs:122–140` — each reads `input.OfficeId` |
| A comparable context switch | **Tenant** switch exists, host-admins only | `src/Ensa.HttpApi.Host/Middleware/TenantResolutionMiddleware.cs:54–98` — `X-Ensa-TenantId`, gated on the `SystemAdministrator` role; the SPA does not use it |

**Evaluation of the closest existing thing.** `X-Ensa-TenantId` + `TenantResolutionMiddleware` is
the house pattern for "a request runs in a context chosen by the caller and validated by the
server". It is the right precedent to copy *if* the decision is to make the office ambient. It is
**not** reusable as-is: it switches the tenant, is restricted to host administrators, and drives
the global tenant query filter — an office override through it would be a privilege escalation.

**Also blocking:** `GET api/office/lookup` requires `EnsaPermissions.Office.Default`
(`OfficeAppService.cs:160`). The people who most need a switcher — ordinary OSGB staff assigned to
two offices — typically do **not** hold the office-administration permission, so a switcher built on
the existing endpoint would answer 403 for exactly them. A new permission-free
`api/account/*` endpoint is required; that is the same reasoning `AccountController` already
documents for `GET /account/permissions` (`AccountController.cs:9–27, 55–67`).

---

## 3. Old → new mapping

| Concern | Proven old application behaviour | New application counterpart | Missing capability | Proposed implementation |
|---|---|---|---|---|
| **Loading the office list** | Server-rendered into the sidebar every request. `OfisIslemleri.GetOfisler(Kullanici, true)` — assigned offices via `KullaniciOfis_T`, else all active offices of the `KurumId`; `!IsDeleted` filter applied at `SideMenuController.cs:53`. No HTTP endpoint. | `GET api/office/lookup` exists but is guarded by `Ensa.Office` (`OfficeAppService.cs:160`); `UserOffice` holds the assignments; `ProfileDto.OfficeId` exposes only the first one. | A permission-free endpoint that returns **the offices the signed-in user may work in**. | **New** `GET api/account/offices` on `AccountController`, no permission policy (as `GetPermissions`), backed by `UserOffice ⋈ Office`, with the legacy fallback rule preserved. Consumed by a new `useMyOffices()` query. |
| **Determining the active office** | `Session["ofisid"]`, seeded at sign-in from `Kullanici_T.OfisId`, `0` = "all offices" (`LoginController.cs:158`, `BaseController.cs:202–215`). | Nothing. No ambient office; no client state. | Both a server answer and a client holder. | Server returns `activeOfficeId` (the user's `UserOffice` row) as part of the new endpoint; client `OfficeContext` chooses: persisted selection **if it is still in the returned list**, else the server's value, else "all offices". |
| **Sending the office-switch request** | `POST /default/SetOfisId` with `{"OfisId":"…"}`; response `AjaxResultModel`. No membership validation (`DefaultController.cs:227–241`). | No endpoint. | A validated switch. | **Decision required (D1).** Recommended Plan A: **no switch request** — the selection is client state validated against the server's list, so an unauthorized id is impossible to select. Plan B adds `POST api/account/active-office` (persisting the choice), which needs a schema column and therefore a migration. |
| **Refreshing authentication / token / session** | Nothing refreshed. No token exists; the ASP.NET session cookie is unchanged; only two session slots are rewritten. | JWT access + refresh token in `localStorage` (`tokenStore.ts:3–4`); permissions fetched separately, deliberately kept out of the token (`AuthContext.tsx:70–77`). | Nothing. | **No token refresh.** The office is not in the token and must not be — the same argument `AuthContext.tsx:70–77` makes about permissions. |
| **Refreshing current-user information** | Rebuilt as a side effect of `location.reload()`. `Session["kullanici"]` itself is not re-read. | `AuthProvider` loads the user once, on mount (`AuthContext.tsx:91–115`). | Nothing office-dependent. | **No refetch.** The office does not change identity, roles, tenant or company. |
| **Refreshing permissions and menus** | Rebuilt by the reload, but not office-dependent: `Businness/Menu` has no `OfisId`; `MenuIslemleri.GetMenuList(...)` receives no office argument (`SideMenuController.cs:88–91`). | Permissions from `GET /account/permissions` keyed by user id only (`PermissionManager.cs:23`); the menu is a build-time module glob filtered by permission (`registry.ts:89–102`). | Nothing. | **No refetch.** The provisional step 6 of the brief is **removed** for this system; keeping it would add a network round-trip and a menu flicker for a value that cannot change. Recorded as a deliberate deviation. |
| **Clearing office-dependent data and caches** | `location.reload()` discards everything client-side; `[CacheRemove("FirmaListesiniExceleAktar", KullaniciId)]` sweeps one server cache pattern (`DefaultController.cs:217`; `CacheRemoveAttribute.cs:30–39`). | TanStack Query cache with `staleTime: 30_000` (`main.tsx:21`); per-resource invalidation only (`mutations.ts:64`). | A "context changed" cache reset. | Include `officeId` in the query key of every office-scoped list, **and** call `queryClient.removeQueries()` for the office-scoped resources on switch. Key inclusion alone would leave the previous office's pages resident; removal alone would not stop a race from repopulating them. |
| **Route authorization** | Not office-aware. No route is gated on the office. | `ProtectedRoute` checks only session (`App.tsx:14–25`); no permission route guard exists. | Nothing office-specific. | **No route re-validation on switch.** Screens re-query with the new `officeId` and legitimately show empty results. Adding a route guard is out of scope for this feature. |
| **Selection persistence** | Server session only. Lost on session expiry; not shared across browsers; per-session, not per-tab. | `localStorage` is already the house mechanism for user-level UI state (`tokenStore.ts`, `APPEARANCE_STORAGE_KEY`, `LANGUAGE_STORAGE_KEY` in `i18n/index.ts:51`). | A store. | `localStorage` key `ensa.office_id`, written through a `safeGet`/`safeSet` pair modelled on `tokenStore.ts:28–50`, **always** re-validated against the server list on load. Storage is a convenience; the server list is the authority. |
| **Error and rollback behaviour** | `alert(rslt.d)` → literally `undefined`; the loading overlay is never closed; the `<select>` is never rolled back (`app/app.js:1079–1082`). | `useToast` (`Toast.d.ts`), `errorMessage()` (`http.ts:66–83`), `ErrorPanel` (`components/DataTable.tsx:269`). | A correct failure path. | Under Plan A there is no request to fail; the failure surface is the **list** query. Loading → `Spinner`; failure → an inline `ErrorPanel` inside the popup with a retry, and the previously selected office stays selected. Under Plan B, an optimistic switch is rolled back and reported with `toast.error(errorMessage(e))`. |
| **Expanded-sidebar presentation** | Native full-width `<select>`, `position:absolute; left:0; bottom:100px`, above a logo block at `bottom:150px` (`Views/SideMenu/Index.cshtml:469–486`). | `Sidebar` accepts `footer` (`Sidebar.d.ts:92`); `.rrc-sidebar__footer` is already a pinned flex footer with a top border, and `.rrc-sidebar__body` already scrolls independently. Neither is used yet. | Pass a footer. | A `ListItem`-shaped trigger button in `footer`: office glyph + office name + a caret, name truncated with ellipsis and a `Tooltip`. **No native `<select>`.** |
| **Collapsed-sidebar presentation** | Nothing — the whole sidebar is a fixed-width server-rendered menu; there is no collapsed rail in the legacy shell. | `collapsedFooter` (`Sidebar.d.ts:93–101`) exists precisely for this and is unused; `.rrc-sidebar__flyout` provides right-hand geometry (`position:fixed; inset-inline-start:100%; z-index:1030`). | An icon trigger + a right-opening popup. | `collapsedFooter` receives an icon-only `IconButton` (accessible name = active office). Its popup is a `Popup`-based panel positioned to the right of the rail, reusing the flyout geometry. |
| **Mobile presentation** | None — the legacy sidebar has no mobile mode. | Drawer via `mobileOpen` (`MainLayout.tsx:25`, `Sidebar.tsx:136–139`), `closeMobileOnSelect` already on. | Behaviour inside the drawer. | Inside the drawer the switcher renders in its **expanded** form (the drawer is full width), the popup opens **downward** rather than to the side, and choosing an office closes the drawer, matching `closeMobileOnSelect`. |
| **Light / dark themes** | Native `<select>`, no theming at all. | `[data-bs-theme='dark']` token overrides (`ensa.scss:85–133`); library reads `--rrc-*`. | Nothing new. | Use existing tokens only (`--rrc-popup-surface`, `--rrc-border`, `--rrc-text-muted`, `--rrc-hover-surface`, `--rrc-active-surface`, `--kt-primary`). **No new colour literal.** |
| **Accessibility** | A bare `<select>` with **no `<label>` and no `aria-label`** — a screen reader announces an unnamed combobox (`Views/SideMenu/Index.cshtml:479`). | `ToastRegion` marks the toast stack as a live region (`ToastRegion.tsx:19–30`); the library gives the sidebar a `nav` landmark (`navLabel`, `Sidebar.tsx:140`). | A named, keyboard-operable control. | Trigger is a `<button>` with `aria-haspopup="listbox"`, `aria-expanded`, and an accessible name naming the *current* office; the panel is `role="listbox"` with `role="option"` + `aria-selected`; arrow/Home/End/Enter/Escape handled; the change is announced through the toast live region. |

---

## 4. Implementation plan, file by file

No implementation code is written here. Every path is marked **existing** or **proposed new file**.
The plan assumes **Plan A** (see [§7 D1](#7-risks-and-required-decisions)); the deltas for Plan B
are listed at the end of this section.

### 4.1 Office API / service layer

| # | File | State | Responsibility | Planned change | Why | Depends on |
|---|---|---|---|---|---|---|
| 1 | `src/Ensa.Application.Contracts/Membership/Dtos/MyOfficeDtos.cs` | **proposed new file** | The shape of "the offices I may work in". | Add `MyOfficeDto` (`id`, `name`, `headquarterOffice`, `isActive`) and `MyOfficesDto` (`items`, `activeOfficeId`, `allowAllOffices`). | The switcher needs a list *and* the server's opinion of the active one in a single round-trip. `OfficeListDto` is a CRUD row and carries columns the switcher must not depend on. | `EntityDto`/`ListResultDto` conventions in `Ensa.Application.Contracts/Common` |
| 2 | `src/Ensa.Application.Contracts/Membership/IAccountAppService.cs` | **existing** | Self-service account contract. | Add `Task<MyOfficesDto> GetMyOfficesAsync(CancellationToken ct = default);`. | Keeps the switcher on the "acts on my own account" surface, which is the only surface that is legitimately permission-free. | file 1 |
| 3 | `src/Ensa.Application/Membership/AccountAppService.cs` | **existing** | Implements the above. | Add `GetMyOfficesAsync`. Read `UserOffice` rows for `CurrentUser.Id`; when there are none, fall back to the tenant's active offices — the legacy rule at `Businness/Genel/OfisIslemleri.cs:22–26`. Filter `IsActive` and let the soft-delete/tenant global filters do the rest. Set `activeOfficeId` from the user's own `UserOffice` row (the same source `GetProfileAsync` uses at line 42). **No `CheckPermissionAsync` call** — matching `GetPermissionsAsync`. | This is the only place that can answer "which offices may *I* work in" without the `Ensa.Office` permission. | existing `IReadOnlyRepository<UserOffice>` injection (line 27); a new `IReadOnlyRepository<Office>` or `IOfficeRepository` injection |
| 4 | `src/Ensa.HttpApi/Controllers/AccountController.cs` | **existing** | HTTP surface. | Add `[HttpGet("offices")] public Task<MyOfficesDto> GetMyOfficesAsync(CancellationToken ct)`. Add an XML doc explaining, as the class doc already does for `GetPermissions`, why it carries no policy. | Route `GET api/account/offices`. | files 1–3 |
| 5 | `src/Ensa.DbMigrator/Seeding/PermissionEndpointSeedData.cs` | **existing** | The endpoint→permission map. | Add `new("Account", "GetMyOffices", null),` beside lines 38–40. | `check_permission_endpoints.py` fails the build without it, and an unmapped endpoint is **refused at runtime**. `null` = authenticated, no permission — exactly what lines 38–40 already do. | file 4 |
| 6 | `react/ensa-web/src/api/office.ts` | **proposed new file** | SPA data layer for the switcher. | `MyOfficeDto` / `MyOfficesDto` mirrors; `useMyOffices()` → `http.get<MyOfficesDto>('/account/offices')` with `queryKey: ['account','offices']`, `staleTime` well above the default (the list changes when an administrator reassigns the user, not during a session). | Kept out of `pages/tenancy/api.ts` on purpose: that file is the tenancy **module's** data layer and is loaded with the tenancy screens; the switcher is shell infrastructure and must not depend on a feature module. | `src/api/http.ts` |

### 4.2 Office state / context

| # | File | State | Responsibility | Planned change | Why | Depends on |
|---|---|---|---|---|---|---|
| 7 | `react/ensa-web/src/auth/officeStore.ts` | **proposed new file** | The one place `localStorage` is touched for the office. | `officeStore.get()` / `.set(id \| null)` / `.clear()` over key `ensa.office_id`, each wrapped in `try/catch`. | `tokenStore.ts:28–50` establishes that storage access must not throw in a private window; the same shape is reused rather than reinvented. | none |
| 8 | `react/ensa-web/src/auth/OfficeContext.tsx` | **proposed new file** | The active office, and the only way to change it. | `OfficeProvider` + `useOffice()` exposing `{ offices, activeOfficeId, activeOffice, isLoading, error, canSwitch, selectOffice }`. Resolution order: stored id **if present in `offices`** → `activeOfficeId` from the server → `null` ("all offices"). `selectOffice(id)` writes the store, sets state, and triggers the cache reset (file 9). `canSwitch` is `offices.length > 1`, reproducing `Model.OfisList.Count > 1`. | Separating "what is the active office" from "what does the UI look like" is what lets the switcher, the query keys and the tests each depend on one small thing. Validating against `offices` is what closes the legacy hole at `DefaultController.cs:229–241`. | files 6, 7; `useAuth` (only to know a session exists) |
| 9 | `react/ensa-web/src/auth/officeCache.ts` | **proposed new file** | Cache invalidation on switch. | Export the list of office-scoped query resources and a `resetOfficeScopedQueries(queryClient)` that calls `queryClient.removeQueries({ queryKey: [resource] })` for each. | `removeQueries`, not `invalidateQueries`: invalidation leaves the old office's rows on screen until the refetch lands, which is exactly the "stale data from the previous office" risk. The resource list is explicit so that adding a scoped resource is a visible edit, not a silent omission. | `mutations.ts:15` (`resourceKey`) |
| 10 | `react/ensa-web/src/main.tsx` | **existing** | Provider composition. | Mount `OfficeProvider` **inside** `AuthProvider` and **inside** `QueryClientProvider`, outside `App`. | It needs a session to fetch the list and a query client to reset the cache; `App` and every screen must be able to read it. | files 8, 9 |

### 4.3 Office-switcher UI

| # | File | State | Responsibility | Planned change | Why | Depends on |
|---|---|---|---|---|---|---|
| 11 | `react/ensa-web/src/layout/OfficeSwitcher.tsx` | **proposed new file** | The control itself. | Two exported shapes: `OfficeSwitcher` (expanded — a `ListItem`-styled trigger `<button>` showing glyph + active office name + caret) and `OfficeSwitcherCompact` (collapsed — an `IconButton` whose accessible name is the active office). Both open the same `OfficeSwitcherPanel`. Renders `null` when `canSwitch` is false. | One file, because the two shapes share the panel, the keyboard model and every string; splitting them would duplicate all three. Returning `null` for a single office reproduces the legacy rule and avoids a dead control. | files 8, 12; library `ListItem`, `IconButton`, `Text`, `Badge`, `Spinner`, `Tooltip`, `Popup` |
| 12 | `react/ensa-web/src/layout/OfficeSwitcherPanel.tsx` | **proposed new file** | The list popup and its states. | `role="listbox"`, one `role="option"` per office plus the "Tüm Ofisler / All offices" entry when `allowAllOffices`; `aria-selected` on the active one; arrow/Home/End/Enter/Space/Escape; loading → `Spinner` with a label; empty → a muted line; error → `ErrorPanel` + retry. Takes a `placement` prop (`'top'` for the expanded footer, `'right'` for the rail, `'bottom'` for the mobile drawer). | The three states the brief requires are properties of the *panel*, not of the trigger, so they live in one place. `placement` is a prop because the shell already knows which of the three contexts it is in — deriving it from a media query would duplicate the breakpoint. | library `Popup` (`base/index.d.ts:62`), `Spinner`, `ErrorPanel` from `@/components/DataTable:269` |
| 13 | `react/ensa-web/src/layout/Sidebar.tsx` | **existing** | Navigation. | Pass `footer={<OfficeSwitcher />}` and `collapsedFooter={<OfficeSwitcherCompact />}` to `RichSidebar` (currently neither is passed; `header` is passed at lines 141–150). No other change. | The library already pins the footer and scrolls `.rrc-sidebar__body` independently; using the prop is the whole integration. | files 11, 12; `Sidebar.d.ts:92–101` |
| 14 | `react/ensa-web/src/layout/MainLayout.tsx` | **existing** | Shell. | No change expected. Listed so the reviewer knows it was considered: the switcher lives inside the sidebar, so the two navigation states at lines 24–25 are untouched. | Avoids a second source of truth for the rail state. | — |

### 4.4 Applying the office to data

| # | File | State | Responsibility | Planned change | Why | Depends on |
|---|---|---|---|---|---|---|
| 15 | `react/ensa-web/src/pages/companies/*` (list query) | **existing** | Company list. | Read `activeOfficeId` from `useOffice()` and pass it as the existing `officeId` filter; include it in the query key. | `CompanyAppService.cs:252–254` already filters on `input.OfficeId`. This is the legacy `FirmaListController.cs:67` rule, expressed through the parameter the new API already accepts. | file 8 |
| 16 | `react/ensa-web/src/pages/finance/api.ts` | **existing** | Invoice / cash-register / balance queries. | Same: default the existing `officeId` fields (lines 56, 71, 113, 148, 166, 178, 188, 196, 240) from the context where the screen does not already set one, and add it to the key. | `InvoiceAppService.cs:409–429` and `CashRegisterAppService.cs:369–380` already honour it. | file 8 |
| 17 | `react/ensa-web/src/pages/reports/*` (OHS report list) | **existing** | Report list. | Same. | `OhsReportAppService.cs:122–140` already honours it. | file 8 |

> The exact call sites in 15–17 must be enumerated during implementation by reading each module's
> `api.ts`; this analysis proves that the **backend** accepts `officeId` on those four services and
> nowhere else. Any screen not in that set is, today, office-independent — which is a real
> behavioural gap versus the legacy application's 168 `OfisId` references. See
> [§7 R11](#7-risks-and-required-decisions).

### 4.5 Styling and tokens

| # | File | State | Responsibility | Planned change | Why | Depends on |
|---|---|---|---|---|---|---|
| 18 | `react/ensa-web/src/styles/ensa.scss` | **existing** | Token layer. | Add a small `.ensa-office-switcher*` block: the trigger's truncation (`min-width:0; overflow:hidden; text-overflow:ellipsis; white-space:nowrap`), the rail popup's right-hand geometry (mirroring `.rrc-sidebar__flyout`: `position:fixed; inset-inline-start:var(--kt-sidebar-width-collapsed); z-index:1030`), and an `@media (prefers-reduced-motion: reduce)` guard. **Colour values come only from existing `--rrc-*`/`--kt-*` tokens.** | The library owns the footer chrome; only truncation and the side placement are missing, and side placement is missing because no shipped primitive offers it (`Popover.d.ts`, `Menu.d.ts`). Reusing the existing token names is what makes dark mode free (`ensa.scss:85–133`). | `ensa.scss:57–59` (`--kt-sidebar-width*`) |

### 4.6 Translations

| # | File | State | Responsibility | Planned change | Why | Depends on |
|---|---|---|---|---|---|---|
| 19 | `react/ensa-web/src/i18n/locales/tr.json` | **existing** | Turkish core bundle. | Add an `office` section: `switcher.label`, `switcher.allOffices` ("Tüm Ofisler" — the legacy string), `switcher.empty`, `switcher.error`, `switcher.retry`, `switcher.switched`, `switcher.loading`. | The switcher is shell chrome, not a module, so it belongs in the core bundle rather than a `pages/*/locales` file (`i18n/index.ts:15–18`). | — |
| 20 | `react/ensa-web/src/i18n/locales/en.json` | **existing** | English core bundle. | The same keys. | `tools/i18n-check/check_locales.py` fails on an unpaired key. | file 19 |

### 4.7 Tests and verification

| # | File | State | Responsibility | Planned change | Why | Depends on |
|---|---|---|---|---|---|---|
| 21 | `test/Ensa.Application.Tests/AccountOfficesTests.cs` | **proposed new file** | Proves the new endpoint's rules. | Cases: user with two `UserOffice` rows → both returned; user with none → the tenant fallback; an inactive office is excluded; another tenant's office never appears; `activeOfficeId` matches the user's own row. | The fallback branch is the one that silently widens access if it is wrong; it is exactly the branch a unit test can pin. | `test/Ensa.TestBase` |
| 22 | `tools/api-tests/api_office_switch.py` | **proposed new file** | End-to-end proof against the running API. | Anonymous → 401; a permission-less user → **200** on `api/account/offices` (this is the whole point) while `api/office/lookup` still answers 403; the returned ids are a subset of the caller's assignments. | `api_authorization.py` proves permission-guarded endpoints refuse; nothing yet proves a deliberately unguarded one *admits*. | `tools/api-tests/devcert.py`, the running API |
| 23 | `tools/api-tests/frontend_calls.py` | **existing** | SPA↔Swagger contract. | No change — it picks up `http.get('/account/offices')` in `src/api/office.ts` automatically (`CALL` regex, lines 30–33). | Listed so the reviewer knows the new call is already covered and will fail the check until file 4 exists. | — |
| 24 | `tools/repo-check/check_permission_endpoints.py` | **existing** | Endpoint map invariant. | No change — it will fail until file 5 is edited. | Same reason. | — |

> **There is no SPA test runner** (`package.json` scripts: `dev`, `build`, `preview`, `lint`). The
> component-level behaviour of files 11–12 is therefore verified by `npm run lint` (types),
> `check_ui_library.py`, and manual keyboard/screen-reader review against
> [§13 Acceptance criteria](#13-acceptance-criteria). Introducing Vitest is out of scope for this
> feature and is raised as [D4](#14-decisions-required-from-the-user).

### 4.8 Delta if Plan B is chosen instead

| File | State | Additional change |
|---|---|---|
| `src/Ensa.Domain/Membership/UserProfile.cs` (or a new `UserPreference`) | **existing / proposed new file** | A nullable `ActiveOfficeId` column. |
| `src/Ensa.EntityFrameworkCore/Configurations/Membership/*` | **existing** | Fluent configuration for the new column. |
| EF Core migration | **proposed new file** | `dotnet ef migrations add …` — **explicitly out of scope for this task**. |
| `src/Ensa.HttpApi/Controllers/AccountController.cs` | **existing** | `POST api/account/active-office`, validating membership before persisting. |
| `src/Ensa.DbMigrator/Seeding/PermissionEndpointSeedData.cs` | **existing** | `new("Account", "SetActiveOffice", null),` |
| `react/ensa-web/src/auth/OfficeContext.tsx` | **proposed new file** | `selectOffice` becomes a mutation with optimistic update and rollback. |

Plan B buys cross-device persistence and a server-side audit trail. It costs a migration and a
round-trip on every switch. Plan A is the recommendation.

---

## 5. Proposed user and system flow

The brief's provisional nine steps are adjusted against what was proven above. Two steps are
**removed** and one is **replaced**, each with its reason.

| # | Step | Status vs. the brief | Why this order |
|---|---|---|---|
| 0 | On sign-in, `OfficeProvider` fetches `GET api/account/offices` once. | added | The list must exist before the trigger can name the active office; fetching it lazily on first open would leave the footer showing a placeholder for the whole session. It is one small request, cached for the session. |
| 1 | The user opens the switcher (click, `Enter`, `Space`, or `ArrowDown` on the trigger). | as briefed | — |
| 2 | The panel shows the offices — with the active one marked, and "Tüm Ofisler" first when the user may see all. | as briefed | The list is already in the cache; the panel opens with content, not a spinner, in the normal case. |
| 3 | The user selects a different office. | as briefed | — |
| 4 | ~~The switch request is sent.~~ → **The selection is validated against the fetched list, written to `localStorage`, and set in context.** | **replaced** | Under Plan A there is no switch endpoint. The server is still authoritative — it decided the list — but the choice needs no round-trip. This is *stricter* than the legacy `SetOfisId`, which validated nothing (`DefaultController.cs:229–241`). |
| 5 | Office-scoped queries are removed from the cache (`resetOfficeScopedQueries`). | moved earlier | It must happen **before** the new office reaches the query keys. Reversed, React Query would briefly serve the previous office's cached pages under the new key's mount, which is precisely the "stale data from the previous office" defect. |
| 6 | ~~Current-user, permission and menu information is refreshed.~~ | **removed** | Proven office-independent in this system: permissions are keyed on user id only (`PermissionManager.cs:23`) and the menu is a build-time module glob filtered by permission (`registry.ts:89–102`). The legacy application refreshed them only as a side effect of `location.reload()`. Keeping the step would add a round-trip and a menu flicker for a value that cannot change. |
| 7 | Screens re-render; every office-scoped query refetches with the new `officeId`. | as briefed | React Query does this on its own once the key changes — no imperative refetch is needed, and none should be added. |
| 8 | ~~Authorization for the current route is checked.~~ | **removed** | No route in this application is gated on the office (`App.tsx:14–25` checks the session only), and permissions did not change. The correct outcome for an office with no data is an **empty screen**, not a redirect — which is also what the legacy application did. |
| 9 | The panel closes, focus returns to the trigger, and the change is announced through the toast live region. | as briefed, extended | Focus return is what makes the control usable from a keyboard; the announcement is what makes it perceivable without sight (`ToastRegion.tsx:19–30`). |

**No full-page reload.** The legacy `location.reload()` (`app/app.js:1079`) existed because the
application was server-rendered and the office lived in the server session — reloading *was* the
only way to re-run the queries. In a SPA with an explicit cache the equivalent is a targeted cache
reset, and a reload would additionally throw away the router position, the sidebar expansion state
and every unsaved form.

### Mermaid sequence diagram

Every participant, endpoint and state transition below appears in the evidence above. Nothing is
invented; the two elements that do not yet exist are marked **(proposed)**.

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant SW as OfficeSwitcher (proposed)<br/>src/layout/OfficeSwitcher.tsx
    participant OC as OfficeContext (proposed)<br/>src/auth/OfficeContext.tsx
    participant LS as localStorage<br/>"ensa.office_id"
    participant QC as QueryClient<br/>src/main.tsx:19
    participant HT as http (axios)<br/>src/api/http.ts:21
    participant AC as AccountController<br/>GET api/account/offices (proposed)
    participant AS as AccountAppService<br/>UserOffice + Office
    participant PG as Office-scoped screens<br/>company / invoice / cash-register / ohs-report

    Note over OC: on mount, once per session
    OC->>HT: GET /account/offices
    HT->>HT: Authorization: Bearer …<br/>Accept-Language (http.ts:26-33)
    HT->>AC: HTTP GET
    AC->>AS: GetMyOfficesAsync()
    AS-->>AC: { items[], activeOfficeId, allowAllOffices }
    AC-->>HT: 200
    HT-->>OC: MyOfficesDto
    OC->>LS: read "ensa.office_id"
    LS-->>OC: stored id or null
    Note over OC: stored id kept only if present in items;<br/>otherwise activeOfficeId; otherwise null (all offices)

    U->>SW: open switcher
    SW->>OC: read offices + activeOfficeId
    SW-->>U: listbox — active office marked

    U->>SW: select another office
    SW->>OC: selectOffice(id)
    OC->>OC: reject id not in items
    OC->>LS: write "ensa.office_id"
    OC->>QC: removeQueries for office-scoped resources
    OC->>OC: setState(activeOfficeId = id)

    Note over PG: query keys contain officeId → keys change
    PG->>HT: GET /company?OfficeId=id (and the other three)
    HT->>PG: rows for the new office
    SW-->>U: panel closes, focus returns to trigger
    SW-->>U: toast announces the new office (aria-live=polite)
```

---

## 6. Visual and interaction plan

Reference language: Metronic Tailwind **Demo 1** — a quiet left aside, a pinned footer block
separated by a hairline rule, one accent colour used sparingly for the active row. Implemented
entirely with the library's components and the existing `--rrc-*` / `--kt-*` tokens.

### 6.1 Expanded rail (≥ `lg`, `collapsed === false`)

Passed as `Sidebar footer`, so it sits in `.rrc-sidebar__footer` — already `flex: 0 0 auto` with a
`border-block-start`, and already outside the scrolling `.rrc-sidebar__body`.

```
┌──────────────────────────────┐
│  ⌂  Ensa                     │  ← header (Sidebar.tsx:141-150, unchanged)
├──────────────────────────────┤
│  ⊞ Genel Bakış               │
│  🏭 İş Yeri                  │  ← .rrc-sidebar__body, overflow-y:auto
│  ⛑ İSG                       │     (scrolls on its own)
│  ₺ Finans                    │
│  🗃 Kayıtlar                  │
│  ⚙ Yönetim                   │
│      ⋮                       │
├──────────────────────────────┤  ← .rrc-sidebar__footer, pinned
│ ▤  Kadıköy Ofisi          ⌃ │  ← trigger: glyph + name (ellipsis) + caret
└──────────────────────────────┘
```

* Trigger: a `<button>` laid out like the library's `ListItem` — `leading` = office glyph
  (`▤`, the same glyph the offices nav entry already uses at `pages/tenancy/module.tsx:21`),
  `title` = active office name, `trailing` = caret.
* Name truncation: single line, `text-overflow: ellipsis`, with a `Tooltip` carrying the full name.
  The library's `Tooltip` documents `wrapperClassName` for exactly this case
  (`dist/base/Tooltip.d.ts`).
* Above the trigger, a muted `Text size="sm" tone="muted"` caption ("Ofis" / "Office") so the
  control is self-describing without a visible `<label>` element.
* Panel opens **upward** (`placement="top"`), aligned to the footer's left edge, width = sidebar
  width.

### 6.2 Collapsed rail (≥ `lg`, `collapsed === true`)

Passed as `collapsedFooter`. The library renders this region **only** when the prop is supplied
(`Sidebar.d.ts:96–100`), so nothing is clipped if it is ever removed.

```
┌────┐
│ ⌂  │
├────┤
│ ⊞  │
│ 🏭 │        ┌───────────────────────────┐
│ ⛑  │        │ Ofis                      │
│ ₺  │        ├───────────────────────────┤
│ 🗃 │        │ ✓ Kadıköy Ofisi           │  ← opens to the RIGHT of the rail
│ ⚙  │        │   Ankara Ofisi            │
├────┤        │   Tüm Ofisler             │
│ ▤  │◀───────┴───────────────────────────┘
└────┘
```

* Trigger: `IconButton`, glyph only, `aria-label` = *"Ofis: Kadıköy Ofisi"* so the control is
  named **and** its value is announced.
* Popup geometry copies `.rrc-sidebar__flyout` — `position: fixed`, `inset-inline-start:
  var(--kt-sidebar-width-collapsed)`, `z-index: 1030`, `--rrc-popup-surface`, `--rrc-shadow-dropdown`,
  `--rrc-radius-lg`. It is vertically bottom-aligned to the trigger so a long list grows upward
  rather than off-screen.
* Hover on the trigger shows a `Tooltip` with the active office name, matching how the collapsed
  rail already names its own items.

### 6.3 Mobile (< `lg`, drawer open)

The drawer reuses the same `Sidebar` element (`Sidebar.tsx:122–124`), so the switcher is present
without extra work. Differences:

* Renders in its **expanded** shape — the drawer is wide enough.
* Panel opens **downward** (`placement="bottom"`) so it is not pushed off the bottom of a short
  viewport.
* Selecting an office closes the drawer, consistent with `closeMobileOnSelect` (`Sidebar.tsx:139`).
* Touch targets ≥ 44 px; the panel scrolls internally with `max-height: 60vh`.

### 6.4 States

| State | Presentation |
|---|---|
| **Loading** (first fetch in flight) | Trigger shows a `Skeleton`-width placeholder in place of the name; the trigger is `disabled`. Panel, if forced open, shows the library `Spinner` **with an explicit `label`** — `check_ui_library.py` fails a library `Spinner` without one (lines 108–120). |
| **Ready, one office** | The whole control renders `null`. Reproduces `Model.OfisList.Count > 1` (`Views/SideMenu/Index.cshtml:477`) and avoids a control that cannot do anything. |
| **Ready, many offices** | Trigger names the active office. Active row: `--rrc-active-surface` background, `--kt-primary` left accent bar, a check glyph, `aria-selected="true"`. |
| **"All offices" selected** | Trigger reads "Tüm Ofisler" / "All offices" with a muted tone, so it is visibly a *scope*, not a place. Offered only when `allowAllOffices`. |
| **Empty** (list returns zero rows) | Control renders `null`, exactly as the one-office case — there is nothing to switch between. |
| **Error** (request failed) | Trigger stays enabled and shows the last known office, or the caption alone. Opening it shows an `ErrorPanel` (`@/components/DataTable:269`) with the message from `errorMessage(error)` and a retry button. The active office is **not** cleared — a failed list must not silently widen or narrow the user's scope. |

### 6.5 Motion, theme, accessibility

* **Motion.** The footer inherits the aside's existing width transition
  (`--rrc-sidebar-transition-duration` / `-easing`); the label collapses under the same rule the
  library already applies to `.rrc-sidebar__label`. The panel uses the library `Popup`'s own
  show/hide. A `@media (prefers-reduced-motion: reduce)` block disables the panel transition.
* **Theme.** Every colour is a token already defined for both themes in `ensa.scss:21–79` and
  `85–133`. No new literal, so dark mode needs no second implementation.
* **Accessibility.**
  * Trigger: `<button aria-haspopup="listbox" aria-expanded={open} aria-controls={panelId}>`,
    accessible name includes the current office.
  * Panel: `role="listbox"`, `aria-activedescendant`, one `role="option"` per row with
    `aria-selected`.
  * Keys: `Enter`/`Space`/`ArrowDown` open; `ArrowUp`/`ArrowDown`/`Home`/`End` move; `Enter`/`Space`
    choose; `Escape` closes and returns focus; `Tab` closes.
  * Focus returns to the trigger on close — including after a choice.
  * The change is announced once through the existing polite live region (`ToastRegion.tsx:19–30`).
  * Contrast: the active row is distinguished by **background + accent bar + check glyph**, never
    by colour alone.
* **Explicitly not done:** no native `<select>`, no `option` elements, no reproduction of the
  legacy `position:absolute; bottom:100px` layout.

---

## 7. Risks and required decisions

| # | Risk | Evidence | Impact | Proposed resolution | Decision needed first? |
|---|---|---|---|---|---|
| **R1** | Assuming a full-page reload is required | Legacy did `location.reload()` (`app/app.js:1079`) purely because the office lived in the server session and pages were server-rendered | A reload would discard router position, sidebar expansion and unsaved forms — a regression, not parity | Targeted cache reset (`officeCache.ts`) + query-key change. No reload. | No — the evidence settles it |
| **R2** | Assuming the token must be refreshed | Legacy has no token at all; the new token deliberately excludes volatile data (`AuthContext.tsx:70–77`), and `HttpContextCurrentUser` reads no office claim | An office claim would go stale the moment an administrator reassigns the user, and would make every switch a token round-trip | Keep the office **out** of the token, permanently. | No |
| **R3** | Is the backend authoritative for the active office? | Legacy: partly — the session held it but validated nothing (`DefaultController.cs:229–241`). New: no ambient office exists at all (`ICurrentContext.cs:18–36`) | If the client alone decides, a crafted request could pass an office the user is not assigned to — and the four services that accept `input.OfficeId` would honour it | The server is authoritative for **membership** (`api/account/offices`); the client only picks from that list. For full authority the server must also *enforce* it — see R4 / D2. | **Yes — D2** |
| **R4** | The four `officeId`-aware services do not check assignment | `CompanyAppService.cs:252`, `CashRegisterAppService.cs:369`, `InvoiceAppService.cs:409`, `OhsReportAppService.cs:122` all take `input.OfficeId` at face value | A user can already query another office's rows *within their own tenant* by editing the request — this is a **pre-existing** gap, not one this feature creates, but the switcher makes it reachable from the UI | Add an assignment check in those four services, or accept it as tenant-internal. Recommended: add the check. | **Yes — D2** |
| **R5** | `localStorage` is used and may not be trustworthy | The house pattern already tolerates it (`tokenStore.ts:28–50` swallows every storage error) | A tampered or stale value could point at an office the user cannot use | The stored id is **only** honoured when it appears in the server's list; otherwise discarded silently. Storage is a convenience, never an authority. | No |
| **R6** | Stale data from the previous office | React Query `staleTime: 30_000` (`main.tsx:21`), per-resource invalidation only (`mutations.ts:64`) | A user switches and reads the previous office's invoices — the worst possible failure for this feature | `removeQueries` for office-scoped resources **before** the key changes, plus `officeId` inside every affected key. Both, not either. | No |
| **R7** | Permission / menu inconsistency after a switch | Permissions keyed on user id only (`PermissionManager.cs:23`); menu is a build-time glob (`registry.ts:89–102`) | None in this system — but the assumption must be recorded, because it would become false the day permissions are made office-scoped | Do not refetch. Add a note beside `moduleNavigation` if office-scoped permissions are ever introduced. | No |
| **R8** | User left on a route unauthorized in the new office | No route is office-gated; `ProtectedRoute` checks only the session (`App.tsx:14–25`) | None today. If the office ever gates routes, the user would sit on a forbidden screen | Screens re-query and legitimately show empty results — the legacy behaviour too. Revisit only if office-scoped permissions arrive. | No |
| **R9** | Different offices in different browser tabs | `localStorage` is shared across tabs; React state is not. Nothing listens for `storage` events today | Tab A shows Kadıköy while tab B shows Ankara; a refresh of A silently adopts B's choice | Recommended: leave tabs independent in memory (matching the legacy per-session behaviour), and treat the stored value strictly as the *initial* value. Alternative: a `storage` listener that syncs every tab. | **Yes — D3** |
| **R10** | Repeated selections while a request is pending | Legacy fired one POST per `change` with no guard, last-response-wins (`app/app.js:1065–1083`) | Under Plan A there is no in-flight switch, so this cannot occur. Under Plan B it can | Plan A: no risk. Plan B: disable the panel while the mutation is pending and keep only the last selection. | Only if D1 = Plan B |
| **R11** | Data-model mismatch: legacy filters on office in ~168 places, the new API in 4 | 168 `OfisId` references across 29 legacy controllers vs. `input.OfficeId` on exactly four new app services | A user switching offices sees company, invoice, cash-register and OHS-report lists change — and **nothing else**. That is a visible parity gap, and users will read it as a broken switcher | Ship the switcher over the four services that support it, and record the remaining screens as a follow-up backlog item with a per-screen decision on whether office scoping is meaningful. Do not silently pretend the rest are scoped. | **Yes — D5** |
| **R12** | Rollback after a failed switch | Legacy performed none: the `<select>` kept the new value while the session kept the old (`app/app.js:1079–1082`) | UI and server disagree silently | Plan A: nothing to roll back. Plan B: optimistic update with an explicit revert plus `toast.error(errorMessage(e))`. | Only if D1 = Plan B |
| **R13** | The obvious endpoint is permission-gated | `GetLookupAsync` requires `EnsaPermissions.Office.Default` (`OfficeAppService.cs:160`; seed `PermissionEndpointSeedData.cs:254`) | Building on `api/office/lookup` gives 403 to precisely the ordinary staff the switcher is for | New permission-free `GET api/account/offices`, following the reasoning `AccountController.cs:9–27` already documents | No — the evidence settles it |
| **R14** | Backend changes are required at all | No office endpoint outside the permission-guarded CRUD; no `UserOffice`-driven "my offices" query anywhere | Without backend work the feature cannot be built correctly | Files 1–5 in [§4.1](#41-office-api--service-layer). Small and additive: one DTO file, one interface method, one service method, one action, one seed row. **No migration under Plan A.** | No |
| **R15** | "All offices" (legacy `OfisId == 0`) may not be wanted | Legacy always offered it to admins (`Views/SideMenu/Index.cshtml:471`) and treated `0`/`-1` as "no filter" (`FirmaListController.cs:67, 100`) | Offering it to everyone widens what a non-admin sees compared with the legacy application | `allowAllOffices` is decided **by the server**, from the user's role, and defaults to the legacy rule (offered to organization/system administrators only). | **Yes — D6** |

---

## 8. Confirmed findings

Each of these is proven by the cited production code.

1. The legacy switcher is a native `<select id="ddl-main-ofis-list">` rendered inside the side-menu
   partial, absolutely positioned at `left:0; bottom:100px` — `ENSA_ISG/Views/SideMenu/Index.cshtml:477–486`.
2. It renders **only** when the user has more than one office **and** `PersonelTuru == "Admin"` —
   same line 477.
3. Option value `0` means "Tüm Ofisler" (no office filter) — line 471, consumed as
   `OfisId == 0` in `FirmaListController.cs:67, 100` and `DefaultController.getDashboardInfoes`.
4. The list comes from `OfisIslemleri.GetOfisler(Kullanici, true)` filtered by `!IsDeleted` —
   `SideMenuController.cs:53`; the query is `Businness/Genel/OfisIslemleri.cs:20–27`.
5. The list has **no HTTP endpoint**; it is server-rendered on every request.
6. The switch endpoint is `POST /default/SetOfisId`, body `{"OfisId":"…"}`, response an
   `AjaxResultModel` — `app/app.js:1065–1084`, `DefaultController.cs:215–225`.
7. The selection is stored **only** in the ASP.NET server session: `Session["ofisid"]` /
   `Session["ofisId"]` and `Session["ofis"]` — `BaseController.cs:202–216`. No cookie, no
   `localStorage`, no `sessionStorage`, no token, no claim.
8. The initial active office is `Kullanici_T.OfisId`, set at sign-in for every user except
   `ser-admin` — `LoginController.cs:157–158`; absent, `BaseController.OfisId` returns `0`.
9. `Session["ofis"]` is seeded independently, from `GetOfisler(...).FirstOrDefault()`
   (`LoginController.cs:160`), so it can name a different office than `Session["ofisid"]`.
10. On success the client writes `#hfMenuOfisId` and `#hfOfisId`, then calls `location.reload()` —
    `app/app.js:1076–1078`.
11. Nothing about authentication is refreshed: no token exists and the session cookie is untouched.
12. Menus and permissions are **not** office-dependent in the legacy system — no `OfisId` in
    `Businness/Menu/` or `ENSA_ISG/Algoritmalar/YetkiKontrolu.cs`.
13. Cache clearing on switch is one attribute — `[CacheRemove("FirmaListesiniExceleAktar",
    KaldirilacakProp.KullaniciId)]` (`DefaultController.cs:217`), a regex sweep in
    `MemoryCacheManager.RemoveByPatterns`. Everything else is discarded by the page reload.
14. `SideMenuController.Index()` writes a menu model into `HttpContext.Application[menuKeyword]`
    (line 71) that is never read — the local is `null` at line 27, so the `if` at line 29 always
    runs. The menu cache is inert.
15. `SetOfisId` performs **no membership validation**: it writes the session value before the
    lookup, and the lookup is scoped by `KurumId` only — `DefaultController.cs:229–241`.
16. Error handling is broken: `alert(rslt.d)` where no `d` exists (`AjaxResultModel.cs:9–17`), and
    `stopProcess()` is never called, leaving the blocking overlay up — `app/app.js:1068, 1081`.
17. There is **no rollback** and **no in-flight guard** on the legacy switcher.
18. The customer branch (Şube) picker is a different mechanism — a full navigation to
    `/FirmaDetay?firma-id=…` — `mainController.js:38–40`, markup `Views/SideMenu/Index.cshtml:321–355`.
19. The new project has **no office switcher** and **no ambient office context**: `ICurrentUser`
    (`src/Ensa.Domain/Common/ICurrentContext.cs:18–36`) exposes `TenantId` and `CompanyId` only.
20. Office filtering in the new backend exists on exactly four app services, each as a per-request
    `input.OfficeId`: `CompanyAppService.cs:252`, `CashRegisterAppService.cs:369`,
    `InvoiceAppService.cs:409`, `OhsReportAppService.cs:122`.
21. Every office read endpoint requires `EnsaPermissions.Office.Default` —
    `OfficeAppService.cs:40, 53, 88, 160`; seeded at `PermissionEndpointSeedData.cs:250–256`.
22. `ProfileDto.OfficeId` is a single value taken from the **first** `UserOffice` row —
    `AccountAppService.cs:42, 65`.
23. New-system permissions are keyed on user id only — `PermissionManager.cs:23, 165`;
    `HttpContextCurrentUser.cs:99–111`.
24. The rendered sidebar menu is a build-time module glob filtered by permission —
    `registry.ts:68, 89–102`. `GET api/menu/my-menu` exists but the shell does not use it.
25. The component library's `Sidebar` already accepts `footer` and `collapsedFooter`
    (`dist/base/Sidebar.d.ts:91–101`); `layout/Sidebar.tsx` passes neither.
26. `.rrc-sidebar__body` already scrolls independently and `.rrc-sidebar__footer` is already pinned
    with a top border — `dist/style.css`.
27. No shipped primitive opens to the side: `Popover` and `Tooltip` are `top | bottom`, `Menu` is
    `start | end`. `Popup` is exported and documents that it has no portal or collision detection.
28. The SPA sends no context header; `X-Ensa-TenantId` exists server-side
    (`EnsaPermissions.cs:427`, `TenantResolutionMiddleware.cs:54–98`) and is restricted to the
    `SystemAdministrator` role.
29. `check_permission_endpoints.py` fails the build for any new action missing a seed row;
    `check_ui_library.py` applies its raw-markup rules only under `src/pages/**/*.tsx` (line 152).
30. The SPA has no JavaScript test runner — `react/ensa-web/package.json` defines `dev`, `build`,
    `preview`, `lint` only.

---

## 9. Unverified findings

Recorded because they could not be established from the production code alone.

1. **The `Session["ofisid"]` / `Session["ofisId"]` case mismatch.** `BaseController.cs:207` reads
   `"ofisid"`; line 214 writes `"ofisId"`. Whether the two resolve to the same slot depends on the
   case sensitivity of the session item collection in the deployed ASP.NET version and
   configuration. The code fact is certain; the runtime consequence is not, and it was not
   reproduced. If the keys ever diverge, `SetOfisId` would appear to succeed while `OfisId` kept
   returning the old value — which the `location.reload()` would then display.
2. **Whether the office switcher was reachable in production for any role other than `Admin`.**
   The markup condition is unambiguous, but no configuration, feature flag or deployment note was
   examined to confirm that no other build overrode it.
3. **The real-world distribution of `KullaniciOfis_T` rows.** Whether most users have explicit
   assignments (branch 1 of `GetOfisler`) or none (branch 2, "every office of the tenant") is a
   data question; no database was read. It decides how wide the new endpoint's fallback is in
   practice.
4. **The exact set of new-project screens whose data is office-dependent in the users' minds.**
   Only four backend services accept `officeId`; whether the other screens *should* be scoped is a
   product judgement that the code cannot answer. See R11 / D5.
5. **Whether `MerkezOfis` / `HeadquarterOffice` carries switcher semantics.** The legacy selector
   never referenced it, and `Businness/Ofisler/Ofisler.cs:22–28` exposes `GetMerkezOfis` for other
   purposes. No evidence ties it to the switcher.
6. **The visual appearance of the legacy control in a browser.** It was read from source; the
   running legacy application was not started. The stated geometry
   (`position:absolute; left:0; bottom:100px; width:100%`) is what the markup declares, but
   overriding rules in `style/sosgb.css`, `dist/css/sb-admin-2.css` or `style/custom.css` were not
   traced.
7. **Whether `dotnet build`, `npm run build` or any verification script currently passes.** No
   build, test or generation command was run, as required by the task's constraints.
8. **`Views/SideMenu/Index (1).cshtml`.** Treated as a stale copy because `Index.cshtml` is what
   `PartialView(sideMenuModel)` resolves to by convention. The project file was not parsed to prove
   the `(1)` variant is excluded from compilation.

---

## 10. Required backend changes

**Under Plan A (recommended) — additive, no migration:**

1. `src/Ensa.Application.Contracts/Membership/Dtos/MyOfficeDtos.cs` — **new file**: `MyOfficeDto`,
   `MyOfficesDto { items, activeOfficeId, allowAllOffices }`.
2. `src/Ensa.Application.Contracts/Membership/IAccountAppService.cs` — add `GetMyOfficesAsync`.
3. `src/Ensa.Application/Membership/AccountAppService.cs` — implement it over `UserOffice` + `Office`,
   reproducing the legacy fallback (`Businness/Genel/OfisIslemleri.cs:22–26`), **without** a
   permission check, matching `GetPermissionsAsync`.
4. `src/Ensa.HttpApi/Controllers/AccountController.cs` — `[HttpGet("offices")]`.
5. `src/Ensa.DbMigrator/Seeding/PermissionEndpointSeedData.cs` — `new("Account", "GetMyOffices", null),`.
6. **Conditional (D2):** an assignment check inside `CompanyAppService`, `CashRegisterAppService`,
   `InvoiceAppService` and `OhsReportAppService` where `input.OfficeId` is honoured.

**Additionally under Plan B:** a persisted `ActiveOfficeId` column (entity + Fluent configuration +
**an EF Core migration**) and `POST api/account/active-office` with its own seed row.

**Not required in either plan:** token changes, new claims, new middleware, a new global query
filter, changes to `TenantResolutionMiddleware`.

---

## 11. Required frontend changes

**New files (6):**

* `react/ensa-web/src/api/office.ts` — `useMyOffices()`.
* `react/ensa-web/src/auth/officeStore.ts` — safe `localStorage` access.
* `react/ensa-web/src/auth/OfficeContext.tsx` — `OfficeProvider`, `useOffice()`.
* `react/ensa-web/src/auth/officeCache.ts` — office-scoped resource list + `resetOfficeScopedQueries`.
* `react/ensa-web/src/layout/OfficeSwitcher.tsx` — expanded + compact triggers.
* `react/ensa-web/src/layout/OfficeSwitcherPanel.tsx` — listbox popup, three states, keyboard model.

**Existing files changed (6):**

* `react/ensa-web/src/main.tsx` — mount `OfficeProvider`.
* `react/ensa-web/src/layout/Sidebar.tsx` — pass `footer` and `collapsedFooter`.
* `react/ensa-web/src/styles/ensa.scss` — truncation, rail-popup geometry, reduced-motion guard.
* `react/ensa-web/src/i18n/locales/tr.json` and `en.json` — the `office.switcher.*` keys.
* The office-scoped module `api.ts` files (companies, finance, reports) — default and key on
  `activeOfficeId`.

**Unchanged on purpose:** `App.tsx` (no route guard), `MainLayout.tsx` (no new shell state),
`api/http.ts` (no context header), `auth/tokenStore.ts` (no token change),
`modules/registry.ts` (the menu is office-independent).

---

## 12. Recommended implementation order

Each step leaves the repository buildable and verifiable.

1. **Backend endpoint** — files 1–5. Verify: `dotnet build`,
   `python tools/repo-check/check_permission_endpoints.py`.
2. **Backend tests** — `test/Ensa.Application.Tests/AccountOfficesTests.cs`. Verify: `dotnet test`.
3. **API verification script** — `tools/api-tests/api_office_switch.py`, proving a permission-less
   user gets 200 here and 403 on `api/office/lookup`.
4. **SPA data layer** — `src/api/office.ts`. Verify: `npm run lint`,
   `python tools/api-tests/frontend_calls.py` (needs the API running).
5. **SPA state layer** — `officeStore.ts`, `OfficeContext.tsx`, `officeCache.ts`, provider mounted
   in `main.tsx`. Nothing renders yet; verify with `npm run lint`.
6. **Translations** — `tr.json` / `en.json`. Verify: `python tools/i18n-check/check_locales.py`.
7. **UI** — `OfficeSwitcherPanel.tsx`, then `OfficeSwitcher.tsx`, then the two `Sidebar.tsx` props.
   Verify: `npm run lint`, `python tools/repo-check/check_ui_library.py`, manual keyboard and
   screen-reader pass.
8. **Styles** — `ensa.scss` additions; check both themes and both rail states.
9. **Apply the office to data** — companies, finance, reports query keys and `officeId` defaults.
   Verify: `python tools/api-tests/frontend_calls.py`, `python tools/api-tests/api_coverage.py`.
10. **Conditional (D2)** — assignment checks in the four app services, plus a case in
    `api_authorization.py` or the new script proving a non-assigned `officeId` is refused.

Steps 1–3 are independently useful: they close the "no way to ask which offices I may use" gap even
if the UI is deferred.

---

## 13. Acceptance criteria

**Backend**

1. `GET api/account/offices` answers **401** anonymously and **200** for any authenticated user,
   including one holding no permissions at all.
2. It returns exactly the offices the caller may work in: their `UserOffice` assignments when any
   exist; otherwise the tenant's active offices, per the legacy rule.
3. Inactive and soft-deleted offices never appear.
4. No office belonging to another tenant ever appears, with the tenant filter left enabled.
5. `activeOfficeId` matches the caller's own `UserOffice` row, or is `null`.
6. `check_permission_endpoints.py` passes.

**Frontend — behaviour**

7. A user with **one** office (or none) sees no switcher at all, in every rail state.
8. A user with two or more sees the active office named in the expanded footer, and an icon in the
   collapsed rail.
9. Selecting a different office updates the trigger, closes the panel, and returns focus to the
   trigger.
10. After a switch, company / invoice / cash-register / OHS-report lists show the new office's rows
    and **never** briefly show the previous office's rows.
11. No full-page reload occurs: the route, the sidebar expansion state and open panels survive.
12. The selection survives a reload; a stored office that is no longer in the server's list is
    discarded silently and the server's `activeOfficeId` takes over.
13. A failed list request leaves the previously active office in place and shows a retryable error
    inside the panel.
14. The token in `localStorage` is byte-identical before and after a switch.

**Frontend — presentation**

15. The navigation region scrolls independently while the switcher stays pinned at the bottom.
16. In the collapsed rail the popup opens to the **right** of the rail and is fully visible.
17. A long office name is truncated with an ellipsis and reveals the full name in a tooltip.
18. The active office is distinguished by background **and** an accent bar **and** a check glyph —
    never by colour alone.
19. Light and dark both render correctly with no new colour literal in `ensa.scss`.
20. Below `lg` the switcher appears inside the drawer, its popup opens downward, and choosing an
    office closes the drawer.
21. The control follows the sidebar's existing width transition and honours
    `prefers-reduced-motion`.

**Frontend — accessibility**

22. The trigger is a `<button>` whose accessible name includes the current office.
23. The panel is a `listbox` with `option` children carrying `aria-selected`.
24. The control is fully operable with `Enter`, `Space`, `ArrowUp`/`ArrowDown`, `Home`, `End`,
    `Escape` and `Tab`.
25. The change is announced once through the existing polite live region.

**Repository**

26. `dotnet build`, `dotnet test`, `npm run lint`, `check_ui_library.py`, `check_locales.py`,
    `check_permission_endpoints.py`, `frontend_calls.py`, `frontend_routes.py`,
    `frontend_permissions.py` all pass.

---

## 14. Decisions required from the user

Stated as decisions, not questions. Each carries the recommendation this plan assumes; say the word
only if a different answer is wanted.

**D1 — Where the selection is persisted.**
*Plan A (assumed):* client-side `localStorage`, validated against the server's list on every load.
No migration, no round-trip per switch, and strictly safer than the legacy `SetOfisId`, which
validated nothing. *Plan B:* a persisted `ActiveOfficeId` column plus `POST api/account/active-office`
— cross-device persistence and an audit trail, at the cost of a schema migration.
**Assumed: Plan A.**

**D2 — Whether the server enforces office assignment on the four `officeId`-aware services.**
`CompanyAppService`, `CashRegisterAppService`, `InvoiceAppService` and `OhsReportAppService`
currently trust `input.OfficeId`. This is a pre-existing tenant-internal gap that the switcher makes
reachable from the UI. **Assumed: add the check** (it is small, and it is what makes "the backend is
authoritative" true rather than merely stated).

**D3 — Multi-tab behaviour.** Independent per tab (legacy-equivalent, no `storage` listener), or
synchronised across tabs. **Assumed: independent**, with `localStorage` used only as the initial
value.

**D4 — Whether to introduce a SPA test runner.** There is none today. The switcher's keyboard and
state logic is the kind of thing a component test protects well. **Assumed: no** — out of scope for
this feature; verification is `tsc`, the repo checks and a manual accessibility pass.

**D5 — What to do about the parity gap.** The legacy application filtered on office in ~168 places;
the new API accepts `officeId` on four services. Switching offices will visibly change four kinds of
list and nothing else. **Assumed: ship over the four, and record the rest as an explicit follow-up
backlog** rather than implying broader scoping that does not exist.

**D6 — Who may pick "Tüm Ofisler" (all offices).** The legacy control offered it whenever the
selector rendered, i.e. to `Admin` users only. **Assumed: the server decides** via `allowAllOffices`,
defaulting to organization and system administrators.
