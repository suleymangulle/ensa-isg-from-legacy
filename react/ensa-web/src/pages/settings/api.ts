import { useQuery } from '@tanstack/react-query'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type { LookupDto } from '@/api/endpoints'
import type { HazardClass, PeriodUnit } from '@/api/enums'

/**
 * Data layer of the settings module — system parameters, menu definitions and the shared
 * reference data.
 *
 * Parameters are a normal CRUD resource, so writes go through the shared `useCreate` /
 * `useUpdate` / `useDelete`. Menus and lookups are read-only over HTTP: the menu controller
 * exposes no write endpoint at all, and the reference tables are seeded catalogues.
 */

export const SETTINGS_RESOURCES = {
  parameter: 'parameter',
  menu: 'menu',
  lookup: 'lookup',
} as const

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/{Lookups,Menus}/Dtos
// ---------------------------------------------------------------

/** `ParameterListDto` — row of `GET api/parameter`. */
export interface ParameterListDto {
  id: number
  code: string
  name: string
  value: string
  isActive: boolean
}

/** `CreateParameterDto` — the code is set once and is immutable afterwards. */
export interface CreateParameterInput {
  code: string
  name: string
  value: string
  isActive: boolean
}

/** `UpdateParameterDto` — same fields without the code. */
export interface UpdateParameterInput {
  name: string
  value: string
  isActive: boolean
}

/** `MenuListDto` — row of `GET api/menu`. */
export interface MenuListDto {
  id: number
  name: string
  menuTypeCode?: string | null
  userTypeCode?: string | null
  isActive: boolean
  sortOrder: number
}

/** `MenuDto` — the root of a menu tree. */
export interface MenuDto {
  id: number
  name: string
  menuTypeCode?: string | null
  userTypeCode?: string | null
  organizationTypeId?: number | null
  subscriptionPlanId?: number | null
  isActive: boolean
  sortOrder: number
}

/** `MenuNodeNavigationDto` — a placement merged with the catalogue entry it renders. */
export interface MenuNodeNavigationDto {
  id: number
  menuItemId: number
  parentMenuNodeId?: number | null
  menuItemCode: string
  title: string
  url?: string | null
  iconCssClass?: string | null
  cssClass?: string | null
  cssClass2?: string | null
  moduleId?: number | null
  sortOrder: number
  children: MenuNodeNavigationDto[]
  childMenuExists: boolean
  userHidden: boolean
}

/** `MenuElementNavigationDto` — a free-form element of the legacy menu structure. */
export interface MenuElementNavigationDto {
  id: number
  parentMenuElementId?: number | null
  text: string
  iconCssClass?: string | null
  cssClass?: string | null
  cssStyle?: string | null
  url?: string | null
  urlParameters?: string | null
  sortOrder: number
  children: MenuElementNavigationDto[]
  childMenuExists: boolean
}

/** `MenuNavigationDto` — `GET api/menu/my-menu`, already filtered for the caller. */
export interface MenuNavigationDto {
  menu: MenuDto
  menuType?: LookupDto | null
  roots: MenuNodeNavigationDto[]
  elementRoots: MenuElementNavigationDto[]
}

/** `OccupationCodeLookupDto` — a NACE entry with the hazard class it implies. */
export interface OccupationCodeLookupDto extends LookupDto {
  hazardClass: HazardClass
  tag: string
}

/** `PeriodLookupDto` — a recurrence definition such as "every six months". */
export interface PeriodLookupDto extends LookupDto {
  periodValue: number
  periodUnit: PeriodUnit
}

// ---------------------------------------------------------------
// Parameters
// ---------------------------------------------------------------

export interface ParameterListRequest {
  page: number
  pageSize: number
  filter: string
  isActive?: boolean
}

/** `GET api/parameter` — the per-organization key/value settings. */
export function useParameterList(request: ParameterListRequest) {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.parameter, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<ParameterListDto>>('/parameter', {
        params: {
          SkipCount: (request.page - 1) * request.pageSize,
          MaxResultCount: request.pageSize,
          Sorting: 'Code ASC',
          Filter: request.filter || undefined,
          IsActive: request.isActive,
        },
      })
      return data
    },
  })
}

// ---------------------------------------------------------------
// Menus
// ---------------------------------------------------------------

export interface MenuListRequest {
  page: number
  pageSize: number
  filter: string
  menuTypeCode?: string
  isActive?: boolean
}

/** `GET api/menu` — the menu definitions (menu administration). */
export function useMenuList(request: MenuListRequest) {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.menu, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<MenuListDto>>('/menu', {
        params: {
          SkipCount: (request.page - 1) * request.pageSize,
          MaxResultCount: request.pageSize,
          Sorting: 'SortOrder ASC',
          Filter: request.filter || undefined,
          MenuTypeCode: request.menuTypeCode || undefined,
          IsActive: request.isActive,
        },
      })
      return data
    },
  })
}

/**
 * `GET api/menu/my-menu` — the signed-in user's rendered menu for one layout type.
 *
 * `menuTypeCode` is `[Required]` on the server, so an empty code answers 400. The query stays
 * disabled until the screen actually has a code to send.
 */
export function useMyMenu(menuTypeCode: string) {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.menu, 'my-menu', menuTypeCode],
    enabled: menuTypeCode.trim().length > 0,
    queryFn: async () => {
      const { data } = await http.get<MenuNavigationDto>('/menu/my-menu', {
        params: { MenuTypeCode: menuTypeCode.trim() },
      })
      return data
    },
  })
}

// ---------------------------------------------------------------
// Reference data
//
// `LookupController` exposes seven read endpoints and no write endpoint: the catalogues are
// seeded, so the lookup screen is a browser rather than an editor.
// ---------------------------------------------------------------

/** `GET api/lookup/cities` — every province. */
export function useCities() {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.lookup, 'cities'],
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/lookup/cities')
      return data
    },
  })
}

/** `GET api/lookup/cities/{cityId}/districts` — districts of one province. */
export function useDistricts(cityId: number | null) {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.lookup, 'districts', cityId],
    enabled: !!cityId,
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/lookup/cities/${cityId}/districts`)
      return data
    },
  })
}

/** `GET api/lookup/districts/{districtId}/neighborhoods` — neighbourhoods of one district. */
export function useNeighborhoods(districtId: number | null) {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.lookup, 'neighborhoods', districtId],
    enabled: !!districtId,
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(
        `/lookup/districts/${districtId}/neighborhoods`,
      )
      return data
    },
  })
}

/** `GET api/lookup/occupation-codes` — NACE search; the result carries the hazard class. */
export function useOccupationCodes(filter: string) {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.lookup, 'occupation-codes', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<OccupationCodeLookupDto>>(
        '/lookup/occupation-codes',
        { params: { filter: filter || undefined } },
      )
      return data
    },
  })
}

/** `GET api/lookup/duties` — active duty / title definitions. */
export function useDuties() {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.lookup, 'duties'],
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/lookup/duties')
      return data
    },
  })
}

/** `GET api/lookup/certificates` — certificate type definitions. */
export function useCertificates() {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.lookup, 'certificates'],
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/lookup/certificates')
      return data
    },
  })
}

/** `GET api/lookup/periods` — recurrence period definitions. */
export function usePeriods() {
  return useQuery({
    queryKey: [SETTINGS_RESOURCES.lookup, 'periods'],
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<PeriodLookupDto>>('/lookup/periods')
      return data
    },
  })
}
