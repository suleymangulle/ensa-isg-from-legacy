import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http, type ListResult, type PagedResult } from '@/api/http'
import { resourceKey } from '@/api/mutations'
import type { LookupDto } from '@/api/endpoints'
import { IbysQueryType, IbysSubmissionStatus, MedicalReportType } from '@/api/enums'

/**
 * IBYS submission tracking data layer.
 *
 * SECURITY. No shape here exposes `XmlData`, `SignedData` or the e-signature licence key —
 * the backend deliberately omits all three from every DTO. The XML is an encrypted payload
 * carrying clinical examination data, the signed blob is a reusable CAdES artefact and the
 * licence is a secret; a tracking screen needs none of them, only the envelope. `hasXmlData`
 * and `hasSignedData` say whether a payload exists, which is all an operator needs.
 */
export const IBYS_ENDPOINTS = {
  ibysQuery: 'ibys-query',
} as const

export const IBYS_STATUS_BADGE: Record<IbysSubmissionStatus, string> = {
  [IbysSubmissionStatus.NotSent]: 'badge-light-primary',
  [IbysSubmissionStatus.Prepared]: 'badge-light-info',
  [IbysSubmissionStatus.Sent]: 'badge-light-warning',
  [IbysSubmissionStatus.Approved]: 'badge-light-success',
  [IbysSubmissionStatus.Failed]: 'badge-light-danger',
  [IbysSubmissionStatus.Cancelled]: 'badge-light-danger',
}

export const IBYS_QUERY_TYPES: IbysQueryType[] = [
  IbysQueryType.Training,
  IbysQueryType.HealthReport,
  IbysQueryType.ServiceProvidedWorkplace,
  IbysQueryType.OccupationalSafetySpecialist,
  IbysQueryType.WorkplacePhysician,
]

export const IBYS_SUBMISSION_STATUSES: IbysSubmissionStatus[] = [
  IbysSubmissionStatus.NotSent,
  IbysSubmissionStatus.Prepared,
  IbysSubmissionStatus.Sent,
  IbysSubmissionStatus.Approved,
  IbysSubmissionStatus.Failed,
  IbysSubmissionStatus.Cancelled,
]

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/Ibys/Dtos
// ---------------------------------------------------------------

/** `IbysQueryListDto` — the submission envelope. */
export interface IbysQueryListDto {
  id: number
  queryNo?: string | null
  queryType: IbysQueryType
  status: IbysSubmissionStatus
  statusCode: number
  submissionDate: string
  companyId?: number | null
  companyName?: string | null
  companyEmployeeId?: number | null
  employeeFullName?: string | null
  groupId?: string | null
}

/** `IbysQueryDto` — submission detail; the payloads themselves are never exposed. */
export interface IbysQueryDto {
  id: number
  tenantId?: number | null
  queryNo?: string | null
  queryType: IbysQueryType
  status: IbysSubmissionStatus
  statusCode: number
  ibysMessage?: string | null
  submissionDate: string
  groupId?: string | null
  ibysVersion?: string | null
  timeStamp?: string | null
  companyId?: number | null
  companyEmployeeId?: number | null
  /** Whether an XML payload has been prepared. The payload itself is never returned. */
  hasXmlData: boolean
  /** Whether the payload has been e-signed. The signature is never returned. */
  hasSignedData: boolean
}

/** `IbysSubmittedFormDto` — an attached examination form, reduced to a clinical-free summary. */
export interface IbysSubmittedFormDto {
  id: number
  companyEmployeeId: number
  reportType: MedicalReportType
  examinationDate: string
  ibysStatus: IbysSubmissionStatus
  ibysStatusCode?: number | null
  ibysStatusMessage?: string | null
}

/** `IbysQueryNavigationDto`. */
export interface IbysQueryNavigationDto {
  query: IbysQueryDto
  company?: LookupDto | null
  employee?: LookupDto | null
  approverFullName?: string | null
  examinationForms: IbysSubmittedFormDto[]
}

/** `GetIbysQueryListInput`. */
export interface IbysQueryListRequest {
  skipCount?: number
  maxResultCount?: number
  sorting?: string
  filter?: string
  queryType?: IbysQueryType | null
  status?: IbysSubmissionStatus | null
  companyId?: number | null
  companyEmployeeId?: number | null
  groupId?: string | null
  submissionDateFrom?: string | null
  submissionDateTo?: string | null
}

/** `UpdateIbysQueryStatusDto`. */
export interface UpdateIbysQueryStatusDto {
  status: IbysSubmissionStatus
  message?: string | null
  submissionNumber?: string | null
}

// ---------------------------------------------------------------
// Query helpers
// ---------------------------------------------------------------

/** Drops empty entries and PascalCases the keys the `[FromQuery]` input class binds. */
function queryParams(request: Record<string, unknown>): Record<string, unknown> {
  const params: Record<string, unknown> = {}

  for (const [key, value] of Object.entries(request)) {
    if (value === null || value === undefined || value === '') continue
    params[key.charAt(0).toUpperCase() + key.slice(1)] = value
  }
  return params
}

/** `GET api/ibys-query` — paged list of submissions. */
export function useIbysQueryList(request: IbysQueryListRequest) {
  return useQuery({
    queryKey: [IBYS_ENDPOINTS.ibysQuery, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<IbysQueryListDto>>(
        `/${IBYS_ENDPOINTS.ibysQuery}`,
        { params: queryParams({ maxResultCount: 20, skipCount: 0, ...request }) },
      )
      return data
    },
  })
}

/** `GET api/ibys-query/{id}/detail` — envelope, parties and the attached form summaries. */
export function useIbysQueryDetail(id: number | undefined) {
  return useQuery({
    queryKey: [IBYS_ENDPOINTS.ibysQuery, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<IbysQueryNavigationDto>(
        `/${IBYS_ENDPOINTS.ibysQuery}/${id}/detail`,
      )
      return data
    },
  })
}

/**
 * `GET api/ibys-query/pending?type=&maxResultCount=` — submissions of one type still awaiting
 * an IBYS result. The endpoint is scoped to a single query type, so the queue is shown per
 * type rather than as one undifferentiated list.
 */
export function usePendingIbysQueries(type: IbysQueryType, maxResultCount = 25) {
  return useQuery({
    queryKey: [IBYS_ENDPOINTS.ibysQuery, 'pending', type, maxResultCount],
    queryFn: async () => {
      const { data } = await http.get<ListResult<IbysQueryListDto>>(
        `/${IBYS_ENDPOINTS.ibysQuery}/pending`,
        { params: { type, maxResultCount } },
      )
      return data
    },
  })
}

/**
 * `PUT api/ibys-query/{id}/status` — moves a submission to a new status. The transition is
 * validated server-side by `IIbysSubmissionManager`, so an invalid one comes back as a
 * business error rather than being blocked here.
 */
export function useUpdateIbysStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, input }: { id: number; input: UpdateIbysQueryStatusDto }) => {
      const { data } = await http.put<IbysQueryDto>(
        `/${IBYS_ENDPOINTS.ibysQuery}/${id}/status`,
        input,
      )
      return data
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(IBYS_ENDPOINTS.ibysQuery) })
      // An accepted notification also changes the IBYS state of the forms it carried.
      await queryClient.invalidateQueries({ queryKey: resourceKey('medical-examination-form') })
    },
  })
}
