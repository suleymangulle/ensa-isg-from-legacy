import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http, type ListResult, type PagedRequest, type PagedResult } from '@/api/http'
import type { LookupDto } from '@/api/endpoints'
import type {
  AccidentType,
  CorrectiveActionStatus,
  IncidentPersonRole,
  IncidentType,
  RiskCategory,
} from '@/api/enums'

/**
 * Data layer of the incident / corrective action / field observation module.
 *
 * The shared `usePagedList` helper only forwards `skipCount`, `maxResultCount`, `sorting` and
 * `filter`; all three list endpoints here accept a richer input (company, department, date range,
 * "only overdue", "only notification pending"), so the module declares its own query hooks. The
 * cache keys still start with the resource name, which is what `useCreate` / `useUpdate` /
 * `useDelete` from `@/api/mutations` invalidate — writes therefore refresh these lists.
 */

// ---------------------------------------------------------------
// Endpoints — verified against the controllers. `EnsaController` maps `[controller]` through a
// kebab-case token transformer, so `CorrectiveActionController` serves `api/corrective-action`.
// ---------------------------------------------------------------

export const OBSERVATION_ENDPOINTS = {
  incident: 'incident',
  correctiveAction: 'corrective-action',
  fieldObservationReport: 'field-observation-report',
  company: 'company',
  workplaceDepartment: 'workplace-department',
  companyEmployee: 'company-employee',
} as const

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/Risks/Dtos
// ---------------------------------------------------------------

/** `IncidentListDto` — row of `GET api/incident`. */
export interface IncidentListDto {
  id: number
  companyId: number
  companyName?: string | null
  departmentId: number
  departmentName?: string | null
  incidentType: IncidentType
  accidentType: AccidentType
  incidentDate: string
  lostWorkDays?: number | null
  ssiNotificationDate?: string | null
  latestSsiNotificationDate?: string | null
  ssiNotificationOverdue: boolean
}

/** `IncidentDto` — full record behind `GET api/incident/{id}`. */
export interface IncidentDto {
  id: number
  tenantId?: number | null
  companyId: number
  departmentId: number
  incidentType: IncidentType
  accidentType: AccidentType
  incidentDate: string
  description?: string | null
  expression?: string | null
  documentId?: number | null
  unitSupervisorId?: number | null
  supervisorFullName?: string | null
  lostWorkDays?: number | null
  returnToWorkDate?: string | null
  ssiNotificationDate?: string | null
  latestSsiNotificationDate?: string | null
  ssiNotificationOverdue: boolean
  remainingSsiNotificationWorkDays?: number | null
}

/** `CreateIncidentDto` / `UpdateIncidentDto`. */
export interface SaveIncidentDto {
  companyId: number
  departmentId: number
  incidentType: IncidentType
  accidentType: AccidentType
  incidentDate: string
  description?: string | null
  expression?: string | null
  documentId?: number | null
  unitSupervisorId?: number | null
  supervisorFullName?: string | null
  lostWorkDays?: number | null
  returnToWorkDate?: string | null
  ssiNotificationDate?: string | null
}

/** `IncidentPersonDto` — one person involved in the incident. */
export interface IncidentPersonDto {
  id: number
  tenantId?: number | null
  incidentId: number
  personType: IncidentPersonRole
  companyEmployeeId?: number | null
  name: string
  lastName: string
}

/** `CreateIncidentPersonDto`. */
export interface CreateIncidentPersonDto {
  personType: IncidentPersonRole
  companyEmployeeId?: number | null
  name: string
  lastName: string
}

/** `IncidentNavigationDto` — combined view behind `GET api/incident/{id}/detail`. */
export interface IncidentNavigationDto {
  incident: IncidentDto
  company?: LookupDto | null
  department?: LookupDto | null
  document?: LookupDto | null
  unitSupervisor?: LookupDto | null
  affectedPersons: IncidentPersonDto[]
  witnessPersons: IncidentPersonDto[]
  responderPersons: IncidentPersonDto[]
}

/** `GetIncidentListInput`. */
export interface GetIncidentListInput extends PagedRequest {
  companyId?: number
  departmentId?: number
  incidentType?: IncidentType
  accidentType?: AccidentType
  incidentFrom?: string
  incidentTo?: string
  onlySsiNotificationPending?: boolean
}

/** `CorrectiveActionListDto` — row of `GET api/corrective-action`. */
export interface CorrectiveActionListDto {
  id: number
  companyId: number
  companyName?: string | null
  finding: string
  owner?: string | null
  riskCategory: RiskCategory
  operationResult: CorrectiveActionStatus
  findingDate?: string | null
  deadlineDate?: string | null
  resultDate?: string | null
  isOverdue: boolean
}

/** `CorrectiveActionDto` — full record behind `GET api/corrective-action/{id}`. */
export interface CorrectiveActionDto {
  id: number
  tenantId?: number | null
  companyId: number
  finding: string
  recommendation?: string | null
  result?: string | null
  source?: string | null
  findingDocumentId?: number | null
  resultDocumentId?: number | null
  riskCategory: RiskCategory
  operationResult: CorrectiveActionStatus
  owner?: string | null
  ownerCompanyEmployeeId?: number | null
  findingDate?: string | null
  deadlineDate?: string | null
  resultDate?: string | null
  fieldObservationLineId?: number | null
  isOverdue: boolean
}

/**
 * `CreateCorrectiveActionDto` / `UpdateCorrectiveActionDto`.
 * The closing fields are deliberately absent — closing goes through `POST {id}/close`.
 */
export interface SaveCorrectiveActionDto {
  companyId: number
  finding: string
  recommendation?: string | null
  source?: string | null
  findingDocumentId?: number | null
  riskCategory: RiskCategory
  owner?: string | null
  ownerCompanyEmployeeId?: number | null
  findingDate?: string | null
  deadlineDate?: string | null
  fieldObservationLineId?: number | null
  resultDocumentId?: number | null
}

/** `CloseCorrectiveActionDto`. */
export interface CloseCorrectiveActionDto {
  result: string
  resultDate: string
  resultDocumentId?: number | null
}

/** `CorrectiveActionNavigationDto` — `GET api/corrective-action/{id}/detail`. */
export interface CorrectiveActionNavigationDto {
  correctiveAction: CorrectiveActionDto
  company?: LookupDto | null
  ownerEmployee?: LookupDto | null
  findingDocument?: LookupDto | null
  resultDocument?: LookupDto | null
  sourceFieldObservationLine?: FieldObservationLineDto | null
}

/** `GetCorrectiveActionListInput`. */
export interface GetCorrectiveActionListInput extends PagedRequest {
  companyId?: number
  operationResult?: CorrectiveActionStatus
  riskCategory?: RiskCategory
  ownerCompanyEmployeeId?: number
  fieldObservationLineId?: number
  findingFrom?: string
  findingTo?: string
  onlyOverdue?: boolean
}

/** `FieldObservationReportListDto` — row of `GET api/field-observation-report`. */
export interface FieldObservationReportListDto {
  id: number
  companyId: number
  companyName?: string | null
  departmentId?: number | null
  departmentName?: string | null
  date: string
  lineCount: number
}

/** `FieldObservationReportDto` — report header. */
export interface FieldObservationReportDto {
  id: number
  tenantId?: number | null
  companyId: number
  departmentId?: number | null
  date: string
}

/** `CreateFieldObservationReportDto` / `UpdateFieldObservationReportDto`. */
export interface SaveFieldObservationReportDto {
  companyId: number
  departmentId?: number | null
  date: string
  sendMail: boolean
  mailAddress?: string | null
}

/** `FieldObservationLineDto` — one non-conformity line. */
export interface FieldObservationLineDto {
  id: number
  tenantId?: number | null
  fieldObservationReportId: number
  date?: string | null
  deadlineDate?: string | null
  nonConformity: string
  measures?: string | null
  owner?: string | null
  ownerCompanyEmployeeId?: number | null
  riskCategory: RiskCategory
  documentId?: number | null
  isOverdue: boolean
}

/** `CreateFieldObservationLineDto` / `UpdateFieldObservationLineDto`. */
export interface SaveFieldObservationLineDto {
  date?: string | null
  deadlineDate?: string | null
  nonConformity: string
  measures?: string | null
  owner?: string | null
  ownerCompanyEmployeeId?: number | null
  riskCategory: RiskCategory
  documentId?: number | null
}

/** `FieldObservationLineNavigationDto` — a line with its document, owner and derived actions. */
export interface FieldObservationLineNavigationDto {
  line: FieldObservationLineDto
  document?: LookupDto | null
  ownerEmployee?: LookupDto | null
  correctiveActions: CorrectiveActionDto[]
}

/** `FieldObservationReportNavigationDto` — `GET api/field-observation-report/{id}/detail`. */
export interface FieldObservationReportNavigationDto {
  report: FieldObservationReportDto
  company?: LookupDto | null
  department?: LookupDto | null
  lines: FieldObservationLineNavigationDto[]
}

/** `GetFieldObservationReportListInput`. */
export interface GetFieldObservationReportListInput extends PagedRequest {
  companyId?: number
  departmentId?: number
  dateFrom?: string
  dateTo?: string
}

// ---------------------------------------------------------------
// Query helpers
// ---------------------------------------------------------------

/**
 * Drops empty values and upper-cases the first letter of every key, matching the PascalCase
 * property names the API model binder expects (see `@/api/endpoints`, which does the same).
 */
function toQuery(input: Record<string, unknown>): Record<string, unknown> {
  const params: Record<string, unknown> = {}

  for (const [key, value] of Object.entries(input)) {
    if (value === undefined || value === null || value === '' || value === false) continue
    params[key.charAt(0).toUpperCase() + key.slice(1)] = value
  }

  // `SkipCount` and `MaxResultCount` must survive even when they are zero / defaulted.
  params.SkipCount = input.skipCount ?? 0
  params.MaxResultCount = input.maxResultCount ?? 20
  return params
}

function usePagedResource<TRow, TInput extends PagedRequest>(
  resource: string,
  input: TInput,
  enabled = true,
) {
  return useQuery({
    queryKey: [resource, 'list', input],
    enabled,
    queryFn: async () => {
      const { data } = await http.get<PagedResult<TRow>>(`/${resource}`, {
        params: toQuery(input as Record<string, unknown>),
      })
      return data
    },
  })
}

function useDetail<T>(resource: string, id: number | undefined) {
  return useQuery({
    queryKey: [resource, 'detail', id],
    enabled: Number.isFinite(id) && (id ?? 0) > 0,
    queryFn: async () => {
      const { data } = await http.get<T>(`/${resource}/${id}/detail`)
      return data
    },
  })
}

/** Paged incident list. */
export function useIncidentList(input: GetIncidentListInput) {
  return usePagedResource<IncidentListDto, GetIncidentListInput>(
    OBSERVATION_ENDPOINTS.incident,
    input,
  )
}

/** Combined incident detail (`GET api/incident/{id}/detail`). */
export function useIncidentDetail(id: number | undefined) {
  return useDetail<IncidentNavigationDto>(OBSERVATION_ENDPOINTS.incident, id)
}

/** Paged corrective action list. */
export function useCorrectiveActionList(input: GetCorrectiveActionListInput) {
  return usePagedResource<CorrectiveActionListDto, GetCorrectiveActionListInput>(
    OBSERVATION_ENDPOINTS.correctiveAction,
    input,
  )
}

/** Combined corrective action detail (`GET api/corrective-action/{id}/detail`). */
export function useCorrectiveActionDetail(id: number | undefined) {
  return useDetail<CorrectiveActionNavigationDto>(OBSERVATION_ENDPOINTS.correctiveAction, id)
}

/** Open actions whose deadline has already passed (`GET api/corrective-action/overdue`). */
export function useOverdueCorrectiveActions(companyId?: number) {
  return useQuery({
    queryKey: [OBSERVATION_ENDPOINTS.correctiveAction, 'overdue', companyId ?? null],
    queryFn: async () => {
      const { data } = await http.get<ListResult<CorrectiveActionListDto>>(
        `/${OBSERVATION_ENDPOINTS.correctiveAction}/overdue`,
        { params: { companyId: companyId || undefined } },
      )
      return data
    },
  })
}

/** Paged field observation report list. */
export function useFieldObservationReportList(input: GetFieldObservationReportListInput) {
  return usePagedResource<FieldObservationReportListDto, GetFieldObservationReportListInput>(
    OBSERVATION_ENDPOINTS.fieldObservationReport,
    input,
  )
}

/** Combined field observation report detail (`GET api/field-observation-report/{id}/detail`). */
export function useFieldObservationReportDetail(id: number | undefined) {
  return useDetail<FieldObservationReportNavigationDto>(
    OBSERVATION_ENDPOINTS.fieldObservationReport,
    id,
  )
}

/** Company drop-down (`GET api/company/lookup`). */
export function useCompanyLookup(filter?: string) {
  return useQuery({
    queryKey: [OBSERVATION_ENDPOINTS.company, 'lookup', filter ?? ''],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(
        `/${OBSERVATION_ENDPOINTS.company}/lookup`,
        { params: { filter: filter || undefined } },
      )
      return data
    },
  })
}

/** Departments of one workplace (`GET api/workplace-department/lookup?companyId=`). */
export function useDepartmentLookup(companyId: number | undefined) {
  return useQuery({
    queryKey: [OBSERVATION_ENDPOINTS.workplaceDepartment, 'lookup', companyId ?? null],
    enabled: !!companyId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(
        `/${OBSERVATION_ENDPOINTS.workplaceDepartment}/lookup`,
        { params: { companyId } },
      )
      return data
    },
  })
}

/** Employees of one workplace (`GET api/company-employee/lookup?companyId=`). */
export function useEmployeeLookup(companyId: number | undefined) {
  return useQuery({
    queryKey: [OBSERVATION_ENDPOINTS.companyEmployee, 'lookup', companyId ?? null],
    enabled: !!companyId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(
        `/${OBSERVATION_ENDPOINTS.companyEmployee}/lookup`,
        { params: { companyId } },
      )
      return data
    },
  })
}

// ---------------------------------------------------------------
// Child-collection mutations
//
// `useCreate` / `useUpdate` / `useDelete` only cover `/{resource}` and `/{resource}/{id}`; the
// person and line collections hang off a parent id, so they get their own mutations here. Each
// invalidates the parent resource key, which is what the list and detail queries above use.
// ---------------------------------------------------------------

function useChildMutation<TVariables, TResult>(
  resource: string,
  request: (variables: TVariables) => Promise<TResult>,
  onDone?: () => void,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: request,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [resource] })
      onDone?.()
    },
  })
}

/** `POST api/incident/{id}/persons` */
export function useAddIncidentPerson(incidentId: number, onDone?: () => void) {
  return useChildMutation<CreateIncidentPersonDto, IncidentPersonDto>(
    OBSERVATION_ENDPOINTS.incident,
    async (input) => {
      const { data } = await http.post<IncidentPersonDto>(
        `/${OBSERVATION_ENDPOINTS.incident}/${incidentId}/persons`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `DELETE api/incident/{id}/persons/{personId}` */
export function useRemoveIncidentPerson(incidentId: number, onDone?: () => void) {
  return useChildMutation<number, void>(
    OBSERVATION_ENDPOINTS.incident,
    async (personId) => {
      await http.delete(`/${OBSERVATION_ENDPOINTS.incident}/${incidentId}/persons/${personId}`)
    },
    onDone,
  )
}

/** `POST api/field-observation-report/{id}/lines` */
export function useAddObservationLine(reportId: number, onDone?: () => void) {
  return useChildMutation<SaveFieldObservationLineDto, FieldObservationLineDto>(
    OBSERVATION_ENDPOINTS.fieldObservationReport,
    async (input) => {
      const { data } = await http.post<FieldObservationLineDto>(
        `/${OBSERVATION_ENDPOINTS.fieldObservationReport}/${reportId}/lines`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `PUT api/field-observation-report/{id}/lines/{lineId}` */
export function useUpdateObservationLine(reportId: number, onDone?: () => void) {
  return useChildMutation<
    { lineId: number; input: SaveFieldObservationLineDto },
    FieldObservationLineDto
  >(
    OBSERVATION_ENDPOINTS.fieldObservationReport,
    async ({ lineId, input }) => {
      const { data } = await http.put<FieldObservationLineDto>(
        `/${OBSERVATION_ENDPOINTS.fieldObservationReport}/${reportId}/lines/${lineId}`,
        input,
      )
      return data
    },
    onDone,
  )
}

/** `DELETE api/field-observation-report/{id}/lines/{lineId}` */
export function useRemoveObservationLine(reportId: number, onDone?: () => void) {
  return useChildMutation<number, void>(
    OBSERVATION_ENDPOINTS.fieldObservationReport,
    async (lineId) => {
      await http.delete(
        `/${OBSERVATION_ENDPOINTS.fieldObservationReport}/${reportId}/lines/${lineId}`,
      )
    },
    onDone,
  )
}

/** `POST api/corrective-action/{id}/close` — the closing workflow, not a plain update. */
export function useCloseCorrectiveAction(actionId: number, onDone?: () => void) {
  return useChildMutation<CloseCorrectiveActionDto, CorrectiveActionDto>(
    OBSERVATION_ENDPOINTS.correctiveAction,
    async (input) => {
      const { data } = await http.post<CorrectiveActionDto>(
        `/${OBSERVATION_ENDPOINTS.correctiveAction}/${actionId}/close`,
        input,
      )
      return data
    },
    onDone,
  )
}
