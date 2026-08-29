import { useQuery } from '@tanstack/react-query'
import { type BadgeVariant } from 'rich-react-component'
import { http, type ListResult, type PagedRequest, type PagedResult } from './http'
import {
  FitnessForWorkOpinion,
  Gender,
  HazardClass,
  PlanLineStatus,
  StaffRole,
  WorkplaceType,
} from './enums'

// ---------------------------------------------------------------
// Endpoints
//
// Route convention is `api/{controller}` (see EnsaController). Entries may also name a
// sub-resource, as `training-plan/lines` does: the cross-plan line list is served by the
// training plan controller rather than by a controller of its own.
// ---------------------------------------------------------------

export const ENDPOINTS = {
  company: 'company',
  companyEmployee: 'company-employee',
  trainingPlanLine: 'training-plan/lines',
  riskAssessmentReport: 'risk-assessment-report',
  medicalExaminationForm: 'medical-examination-form',
  user: 'user',
} as const

// ---------------------------------------------------------------
// Enums
//
// Re-exported from the generated bundle so the SPA and the API cannot drift: the numeric
// values are produced from src/Ensa.Domain.Shared/Enums/*.cs. After changing a backend enum,
// regenerate with `python tools/gen-enums/gen_enums.py`.
//
// Labels live in the locale bundles under `enums.*`; only the badge colours stay in code,
// because they are styling.
// ---------------------------------------------------------------

export * from './enums'

export const HAZARD_CLASS_BADGE: Record<HazardClass, BadgeVariant> = {
  [HazardClass.Unspecified]: 'primary',
  [HazardClass.LowHazard]: 'success',
  [HazardClass.Hazardous]: 'warning',
  [HazardClass.VeryHazardous]: 'danger',
}

export const PLAN_LINE_STATUS_BADGE: Record<PlanLineStatus, BadgeVariant> = {
  [PlanLineStatus.Planned]: 'primary',
  [PlanLineStatus.Completed]: 'success',
  [PlanLineStatus.NotDone]: 'danger',
  [PlanLineStatus.Postponed]: 'warning',
  [PlanLineStatus.Cancelled]: 'danger',
}

export const FITNESS_OPINION_BADGE: Record<FitnessForWorkOpinion, BadgeVariant> = {
  [FitnessForWorkOpinion.Unspecified]: 'primary',
  [FitnessForWorkOpinion.Fit]: 'success',
  [FitnessForWorkOpinion.ConditionallyFit]: 'warning',
  [FitnessForWorkOpinion.Unfit]: 'danger',
  [FitnessForWorkOpinion.FurtherTestsRequired]: 'info',
}

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts
// ---------------------------------------------------------------

/** `LookupDto` — lightweight record for drop-downs and references. */
export interface LookupDto {
  id: number
  displayName: string
  code?: string | null
  isActive: boolean
}

/** `CompanyListDto` — the row shown in the company table. */
export interface CompanyListDto {
  id: number
  companyName: string
  ssiNumber?: string | null
  hazardClass: HazardClass
  workplaceType: WorkplaceType
  cityName?: string | null
  districtName?: string | null
  phone?: string | null
  authorizedPerson?: string | null
  workerCount?: number | null
  isActive: boolean
}

/** `CompanyDto` — the full company record returned by `GET api/company/{id}`. */
export interface CompanyDto {
  id: number
  tenantId?: number | null
  companyName: string
  ssiNumber?: string | null
  hazardClass: HazardClass
  workplaceType: WorkplaceType
  headquarterCompanyId?: number | null
  taxTaxOffice?: string | null
  taxNumber?: string | null
  businessActivity?: string | null
  occupationCodeId?: number | null
  address?: string | null
  cityId: number
  districtId?: number | null
  neighborhoodId?: number | null
  latitude?: number | null
  longitude?: number | null
  phone?: string | null
  fax?: string | null
  gsm?: string | null
  email?: string | null
  webUrl?: string | null
  authorizedPerson?: string | null
  authorizedPersonPhone?: string | null
  authorizedPersonEmail?: string | null
  officeId?: number | null
  organizationTypeId?: number | null
  subscriptionPlanId?: number | null
  logoDocumentId?: number | null
  notes?: string | null
  isActive: boolean
}

/** `AssignedSpecialistDto` — specialist or physician assigned to a company. */
export interface AssignedSpecialistDto {
  id: number
  userId: number
  fullName: string
  staffRole: StaffRole
  monthlyWorkDurationMinutes?: number | null
  isActive: boolean
}

/** `CompanyWarningSummaryDto` — denormalised counters of missing obligations. */
export interface CompanyWarningSummaryDto {
  isSafetyTrainingNoneCount: number
  isSafetyTrainingMissingCount: number
  isHealthTrainingNoneCount: number
  isHealthTrainingMissingCount: number
  preEmploymentHealthExaminationMissingCount: number
  equipmentExaminationMissingCount: number
  totalMissing: number
}

/** `CompanyNavigationDto` — combined view behind `GET api/company/{id}/detail`. */
export interface CompanyNavigationDto {
  company: CompanyDto
  city?: LookupDto | null
  district?: LookupDto | null
  neighborhood?: LookupDto | null
  office?: LookupDto | null
  headquarterCompany?: LookupDto | null
  branches: LookupDto[]
  assignedSpecialists: AssignedSpecialistDto[]
  departments: LookupDto[]
  activeEmployeeCount: number
  warningSummary?: CompanyWarningSummaryDto | null
}

/** Row of the company employee table (`api/company-employee`). */
export interface CompanyEmployeeListDto {
  id: number
  companyId: number
  companyName?: string | null
  name: string
  lastName: string
  nationalId?: string | null
  gender: Gender
  duty?: string | null
  hireDate?: string | null
  isActive: boolean
}

// ---------------------------------------------------------------
// Query helpers
// ---------------------------------------------------------------

/** Maps the request object onto the PascalCase query string the API binds. */
function toQueryParams(request: PagedRequest): Record<string, unknown> {
  return {
    SkipCount: request.skipCount ?? 0,
    MaxResultCount: request.maxResultCount ?? 20,
    Sorting: request.sorting,
    Filter: request.filter || undefined,
  }
}

/** Fetches a paged list from any module. */
export function usePagedList<T>(resource: string, request: PagedRequest) {
  return useQuery({
    queryKey: [resource, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<T>>(`/${resource}`, {
        params: toQueryParams(request),
      })
      return data
    },
  })
}

/** Fetches a single record by id. */
export function useEntity<T>(resource: string, id: number | undefined) {
  return useQuery({
    queryKey: [resource, 'entity', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<T>(`/${resource}/${id}`)
      return data
    },
  })
}

/** Fetches the combined detail view of a company (`GET api/company/{id}/detail`). */
export function useCompanyDetail(id: number | undefined) {
  return useQuery({
    queryKey: [ENDPOINTS.company, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<CompanyNavigationDto>(`/${ENDPOINTS.company}/${id}/detail`)
      return data
    },
  })
}

/**
 * Host reference data behind the `api/lookup/*` endpoints — provinces, occupation codes,
 * organization types, subscription plans, staff types, payment methods, service items, menu
 * types.
 *
 * These tables are identical for every organization and change about once a year, so the result
 * is cached for an hour rather than refetched per screen. The name is the route segment:
 * `useReferenceData('payment-methods')`.
 *
 * It lives in the shared layer because every module needs some of it; a copy per module folder
 * would mean eight caches of the same eighty rows and eight places to fix a route.
 */
export function useReferenceData(name: string, enabled = true) {
  return useQuery({
    queryKey: ['lookup', name],
    enabled,
    staleTime: 60 * 60 * 1000,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/lookup/${name}`)
      return data
    },
  })
}

/** Fetches drop-down records (`GET api/{resource}/lookup`, at most 50 rows). */
export function useLookup(resource: string, filter?: string) {
  return useQuery({
    queryKey: [resource, 'lookup', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/${resource}/lookup`, {
        params: { filter: filter || undefined },
      })
      return data
    },
  })
}
