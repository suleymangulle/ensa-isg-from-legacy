import { useQuery } from '@tanstack/react-query'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type { ContractStatus } from '@/api/enums'
import type { LookupDto } from '@/api/endpoints'

/**
 * Data layer of the tenancy module — organizations (the OSGB tenants) and their offices.
 *
 * Both resources follow the standard controller shape, so creates, updates and deletes go
 * through the shared `useCreate` / `useUpdate` / `useDelete`. What lives here are the filtered
 * list queries and the two combined detail views (`/{id}/detail`), which the shared helpers do
 * not cover. Query keys start with the same resource string the shared mutations invalidate.
 */

export const TENANCY_RESOURCES = {
  organization: 'organization',
  office: 'office',
} as const

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/Tenancy/Dtos
// ---------------------------------------------------------------

/** `OrganizationListDto` — row of `GET api/organization`. */
export interface OrganizationListDto {
  id: number
  name: string
  code: string
  organizationTypeId: number
  subscriptionPlanId: number
  organizationTypeName?: string | null
  subscriptionPlanName?: string | null
  phone?: string | null
  email?: string | null
  subscriptionStart: string
  subscriptionEnd?: string | null
  isActive: boolean
}

/**
 * `OrganizationDto` — `GET api/organization/{id}`.
 *
 * The organization is a host record: it carries no `tenantId` of its own, because its `id` is
 * what every other entity's `tenantId` points at.
 */
export interface OrganizationDto {
  id: number
  name: string
  code: string
  organizationTypeId: number
  subscriptionPlanId: number
  taxOffice?: string | null
  taxNumber?: string | null
  address?: string | null
  cityId?: number | null
  districtId?: number | null
  phone?: string | null
  email?: string | null
  webUrl?: string | null
  authorizedFullName?: string | null
  authorizedPhone?: string | null
  authorizedEmail?: string | null
  logoDocumentId?: number | null
  isActive: boolean
  subscriptionStart: string
  subscriptionEnd?: string | null
  maximumUserCount?: number | null
  maximumCompanyCount?: number | null
}

/** `OrganizationContractSummaryDto` — the subscription contract in force. */
export interface OrganizationContractSummaryDto {
  id: number
  organizationId: number
  organizationName: string
  contractDate: string
  unitPrice: number
  userCount: number
  totalPrice: number
  subscriptionPlanId?: number | null
  organizationTypeId?: number | null
  isApproved: boolean
  isPaid: boolean
  isActive: boolean
  contractStatus: ContractStatus
  contractStatusDate?: string | null
  accountClosingDate?: string | null
}

/** `OrganizationNavigationDto` — `GET api/organization/{id}/detail`. */
export interface OrganizationNavigationDto {
  organization: OrganizationDto
  organizationType?: LookupDto | null
  subscriptionPlan?: LookupDto | null
  city?: LookupDto | null
  district?: LookupDto | null
  offices: LookupDto[]
  headquarterOffice?: LookupDto | null
  currentContract?: OrganizationContractSummaryDto | null
  officeCount: number
  activeUserCount: number
  activeCompanyCount: number
}

/** `CreateOrganizationDto`; `UpdateOrganizationDto` adds `isActive`. */
export interface OrganizationInput {
  name: string
  code: string
  organizationTypeId: number
  subscriptionPlanId: number
  taxOffice?: string | null
  taxNumber?: string | null
  address?: string | null
  cityId?: number | null
  districtId?: number | null
  phone?: string | null
  email?: string | null
  webUrl?: string | null
  authorizedFullName?: string | null
  authorizedPhone?: string | null
  authorizedEmail?: string | null
  logoDocumentId?: number | null
  subscriptionStart: string
  subscriptionEnd?: string | null
  maximumUserCount?: number | null
  maximumCompanyCount?: number | null
  isActive?: boolean
}

/** `OfficeListDto` — row of `GET api/office`. */
export interface OfficeListDto {
  id: number
  name: string
  phone?: string | null
  cityId?: number | null
  cityName?: string | null
  districtName?: string | null
  authorizedPerson?: string | null
  companyId?: number | null
  isHeadquarterOffice: boolean
  isActive: boolean
}

/** `OfficeDto` — `GET api/office/{id}`. */
export interface OfficeDto {
  id: number
  tenantId?: number | null
  name: string
  phone?: string | null
  fax?: string | null
  address?: string | null
  cityId?: number | null
  districtId?: number | null
  authorizedPerson?: string | null
  authorizedEmail?: string | null
  companyId?: number | null
  isHeadquarterOffice: boolean
  isActive: boolean
}

/** `OfficeNavigationDto` — `GET api/office/{id}/detail`. */
export interface OfficeNavigationDto {
  office: OfficeDto
  organization?: LookupDto | null
  city?: LookupDto | null
  district?: LookupDto | null
  userCount: number
  cashRegisterCount: number
}

/** `CreateOfficeDto`; `UpdateOfficeDto` adds `isActive`. */
export interface OfficeInput {
  name: string
  phone?: string | null
  fax?: string | null
  address?: string | null
  cityId?: number | null
  districtId?: number | null
  authorizedPerson?: string | null
  authorizedEmail?: string | null
  companyId?: number | null
  isHeadquarterOffice: boolean
  isActive?: boolean
}

// ---------------------------------------------------------------
// Queries
// ---------------------------------------------------------------

export interface OrganizationListRequest {
  page: number
  pageSize: number
  filter: string
  isActive?: boolean
}

/** `GET api/organization` — free text runs over the name, the code and the tax number. */
export function useOrganizationList(request: OrganizationListRequest) {
  return useQuery({
    queryKey: [TENANCY_RESOURCES.organization, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<OrganizationListDto>>('/organization', {
        params: {
          SkipCount: (request.page - 1) * request.pageSize,
          MaxResultCount: request.pageSize,
          Sorting: 'Name ASC',
          Filter: request.filter || undefined,
          IsActive: request.isActive,
        },
      })
      return data
    },
  })
}

/** `GET api/organization/{id}` — the plain record, used to seed the edit form. */
export function useOrganization(id: number | undefined) {
  return useQuery({
    queryKey: [TENANCY_RESOURCES.organization, 'entity', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<OrganizationDto>(`/organization/${id}`)
      return data
    },
  })
}

/** `GET api/organization/{id}/detail` — type, plan, location, offices and quota counters. */
export function useOrganizationDetail(id: number | undefined) {
  return useQuery({
    queryKey: [TENANCY_RESOURCES.organization, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<OrganizationNavigationDto>(`/organization/${id}/detail`)
      return data
    },
  })
}

export interface OfficeListRequest {
  page: number
  pageSize: number
  filter: string
  companyId?: number
  isActive?: boolean
  isHeadquarterOffice?: boolean
}

/** `GET api/office` — free text runs over the office name and the authorized person. */
export function useOfficeList(request: OfficeListRequest) {
  return useQuery({
    queryKey: [TENANCY_RESOURCES.office, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<OfficeListDto>>('/office', {
        params: {
          SkipCount: (request.page - 1) * request.pageSize,
          MaxResultCount: request.pageSize,
          Sorting: 'Name ASC',
          Filter: request.filter || undefined,
          CompanyId: request.companyId,
          IsActive: request.isActive,
          IsHeadquarterOffice: request.isHeadquarterOffice,
        },
      })
      return data
    },
  })
}

/** `GET api/office/{id}` — the plain record, used to seed the edit form. */
export function useOffice(id: number | undefined) {
  return useQuery({
    queryKey: [TENANCY_RESOURCES.office, 'entity', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<OfficeDto>(`/office/${id}`)
      return data
    },
  })
}

/** `GET api/office/{id}/detail` — organization, location and the attached counters. */
export function useOfficeDetail(id: number | undefined) {
  return useQuery({
    queryKey: [TENANCY_RESOURCES.office, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<OfficeNavigationDto>(`/office/${id}/detail`)
      return data
    },
  })
}

// ---------------------------------------------------------------
// Reference data
//
// The province and district lists belong to the settings module; the two hooks are repeated
// here rather than imported across module folders so tenancy stays self-contained
// (see MODULES.md).
// ---------------------------------------------------------------

/** `GET api/lookup/cities` — every province. */
export function useCityLookup() {
  return useQuery({
    queryKey: ['lookup', 'cities'],
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/lookup/cities')
      return data
    },
  })
}

/** `GET api/lookup/cities/{cityId}/districts` — districts of one province. */
export function useDistrictLookup(cityId: number | null | undefined) {
  return useQuery({
    queryKey: ['lookup', 'districts', cityId],
    enabled: !!cityId,
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/lookup/cities/${cityId}/districts`)
      return data
    },
  })
}

// ---------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------

/** ISO timestamp -> `yyyy-MM-dd`, the value an `<input type="date">` expects. */
export function toDateInput(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10)
}

export function optionalNumber(value: string): number | null {
  const parsed = Number(value)
  return value.trim() === '' || Number.isNaN(parsed) ? null : parsed
}

export function optionalText(value: string): string | null {
  return value.trim() === '' ? null : value.trim()
}
