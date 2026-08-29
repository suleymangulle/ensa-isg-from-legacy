import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type BadgeVariant } from 'rich-react-component'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type { LookupDto } from '@/api/endpoints'
import { ActivityType, ApprovalStatus, PlanLineStatus } from '@/api/enums'

/**
 * Data layer of the work plan and activity module.
 *
 * The work plan routes are nested (`work-plan/{id}/lines/{lineId}/approve`) and the list filters
 * carry more than `filter`/`sorting`, so the queries live here rather than going through the
 * flat helpers in `@/api/endpoints`. Every cache key starts with the resource name, which is
 * what the shared mutation helpers invalidate.
 */

export const RESOURCES = {
  workPlan: 'work-plan',
  activity: 'activity',
  company: 'company',
  user: 'user',
  lookup: 'lookup',
} as const

export const PLAN_LINE_STATUSES: PlanLineStatus[] = [
  PlanLineStatus.Planned,
  PlanLineStatus.Completed,
  PlanLineStatus.NotDone,
  PlanLineStatus.Postponed,
  PlanLineStatus.Cancelled,
]

export const ACTIVITY_TYPES: ActivityType[] = [
  ActivityType.Activity,
  ActivityType.Document,
  ActivityType.Revision,
  ActivityType.MandatoryDocument,
]

/** Badge classes for the approval workflow; the state has to read at a glance. */
export const APPROVAL_STATUS_BADGE: Record<ApprovalStatus, BadgeVariant> = {
  [ApprovalStatus.Draft]: 'primary',
  [ApprovalStatus.SubmittedForApproval]: 'warning',
  [ApprovalStatus.Approved]: 'success',
  [ApprovalStatus.Rejected]: 'danger',
}

/** An approved line is statutory evidence: it can neither be edited nor removed. */
export function isApproved(status: ApprovalStatus | null | undefined): boolean {
  return status === ApprovalStatus.Approved
}

/** Only a line awaiting a decision can be approved or rejected. */
export function isAwaitingDecision(status: ApprovalStatus | null | undefined): boolean {
  return status === ApprovalStatus.SubmittedForApproval
}

/** Draft and rejected lines are the ones that can be (re)submitted. */
export function canSubmit(status: ApprovalStatus | null | undefined): boolean {
  return status == null || status === ApprovalStatus.Draft || status === ApprovalStatus.Rejected
}

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/Plans/Dtos
// ---------------------------------------------------------------

/** `WorkPlanListDto` — one row of the plan table. */
export interface WorkPlanListDto {
  id: number
  companyId: number
  companyName?: string | null
  startDate: string
  documentNo?: string | null
  revisionNo?: string | null
  publicationDate: string
  isActive: boolean
  isTransferred: boolean
  lineCount: number
}

/** `WorkPlanDto` — the plan header (cover page). */
export interface WorkPlanDto {
  id: number
  tenantId?: number | null
  companyId: number
  startDate: string
  revisionNo?: string | null
  revisionDate: string
  documentNo?: string | null
  publicationDate: string
  specialistUserId?: number | null
  physicianUserId?: number | null
  approverUserId?: number | null
  controlItemListId?: number | null
  isActive: boolean
  isTransferred: boolean
  previousPlanId?: number | null
}

/** `CreateWorkPlanDto` / `UpdateWorkPlanDto`. */
export interface SaveWorkPlanDto {
  companyId: number
  startDate: string
  revisionNo?: string | null
  revisionDate: string
  documentNo?: string | null
  publicationDate: string
  specialistUserId?: number | null
  physicianUserId?: number | null
  approverUserId?: number | null
  controlItemListId?: number | null
  previousPlanId?: number | null
  isActive?: boolean
  isTransferred?: boolean
}

/** `WorkPlanLineDto` — one planned activity with its approval state. */
export interface WorkPlanLineDto {
  id: number
  workPlanId: number
  activityId: number
  periodId?: number | null
  year: number
  month?: number | null
  status?: PlanLineStatus | null
  performedDate?: string | null
  description?: string | null
  isActive: boolean
  previousLineId?: number | null
  approvalStatus?: ApprovalStatus | null
  rejectionReason?: string | null
  forApprovalSenderUserId?: number | null
  approverUserId?: number | null
  forApprovalSendingDate?: string | null
  approvalDate?: string | null
  companyId: number
  instructorNationalId?: string | null
  instructorUserId?: number | null
  documentId?: number | null
}

/** `CreateWorkPlanLineDto` / `UpdateWorkPlanLineDto`. */
export interface SaveWorkPlanLineDto {
  activityId: number
  periodId?: number | null
  year: number
  month?: number | null
  status?: PlanLineStatus | null
  performedDate?: string | null
  description?: string | null
  instructorNationalId?: string | null
  instructorUserId?: number | null
  documentId?: number | null
  isActive?: boolean
}

/** `WorkPlanLineNavigationDto` — a line with the names the API already resolved. */
export interface WorkPlanLineNavigationDto {
  line: WorkPlanLineDto
  activityName: string
  instructorUserFullName?: string | null
  documentName?: string | null
}

/** `WorkPlanNavigationDto` — combined view behind `GET api/work-plan/{id}/detail`. */
export interface WorkPlanNavigationDto {
  workPlan: WorkPlanDto
  company?: LookupDto | null
  specialistFullName?: string | null
  physicianFullName?: string | null
  approverFullName?: string | null
  lines: WorkPlanLineNavigationDto[]
}

/** `WorkPlanCompletionDto` — the share of lines that reached `Completed`. */
export interface WorkPlanCompletionDto {
  workPlanId: number
  completionRate: number
  completionPercentage: number
}

/** `ActivityListDto` — one row of the catalogue table. */
export interface ActivityListDto {
  id: number
  activityName: string
  activityCode?: string | null
  activityGroupId?: number | null
  activityType: ActivityType
  parentActivityId?: number | null
  periodId?: number | null
  defaultActivity: boolean
  isActive: boolean
  orderNo?: number | null
  tenantId?: number | null
}

/** `ActivityDto` — the full catalogue entry. */
export interface ActivityDto {
  id: number
  tenantId?: number | null
  parentActivityId?: number | null
  activityCode?: string | null
  activityName: string
  activityGroupId?: number | null
  activityType: ActivityType
  defaultActivity: boolean
  defaultCount: number
  defaultStartMonthOffset: number
  defaultElementCondition: number
  isActive: boolean
  periodId?: number | null
  relatedTable?: string | null
  relationId?: number | null
  orderNo?: number | null
}

/** `CreateActivityDto` / `UpdateActivityDto`. */
export interface SaveActivityDto {
  activityName: string
  activityCode?: string | null
  parentActivityId?: number | null
  activityGroupId?: number | null
  activityType: ActivityType
  defaultActivity: boolean
  defaultCount: number
  defaultStartMonthOffset: number
  defaultElementCondition: number
  periodId?: number | null
  relatedTable?: string | null
  relationId?: number | null
  orderNo?: number | null
  isActive?: boolean
}

/** `ActivityNavigationDto` — combined view behind `GET api/activity/{id}/detail`. */
export interface ActivityNavigationDto {
  activity: ActivityDto
  activityGroup?: LookupDto | null
  period?: LookupDto | null
  parentActivity?: LookupDto | null
  childActivities: LookupDto[]
}

/** `PeriodLookupDto` — recurrence period definitions from `GET api/lookup/periods`. */
export interface PeriodLookupDto {
  id: number
  displayName: string
  code?: string | null
  isActive: boolean
}

// ---------------------------------------------------------------
// Queries
// ---------------------------------------------------------------

/** Drops empty values so an unset filter is not sent as an empty query parameter. */
function clean(params: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    result[key] = value
  }
  return result
}

/** Filter accepted by `GET api/work-plan`. */
export interface WorkPlanListRequest {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  companyId?: number | null
  year?: number | null
  isActive?: boolean | null
}

/** `GET api/work-plan` — the paged plan list. */
export function useWorkPlanList(request: WorkPlanListRequest) {
  return useQuery({
    queryKey: [RESOURCES.workPlan, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<WorkPlanListDto>>(`/${RESOURCES.workPlan}`, {
        params: clean({
          SkipCount: request.skipCount,
          MaxResultCount: request.maxResultCount,
          Sorting: request.sorting,
          Filter: request.filter,
          CompanyId: request.companyId,
          Year: request.year,
          IsActive: request.isActive,
        }),
      })
      return data
    },
  })
}

/** `GET api/work-plan/{id}/detail` — header, workplace, staff and every line. */
export function useWorkPlanDetail(id: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.workPlan, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<WorkPlanNavigationDto>(`/${RESOURCES.workPlan}/${id}/detail`)
      return data
    },
  })
}

/** `GET api/work-plan/{id}/completion-rate` — the share of lines that reached `Completed`. */
export function useWorkPlanCompletion(id: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.workPlan, 'completion-rate', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<WorkPlanCompletionDto>(
        `/${RESOURCES.workPlan}/${id}/completion-rate`,
      )
      return data
    },
  })
}

/** Filter accepted by `GET api/activity`. */
export interface ActivityListRequest {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  activityType?: ActivityType | null
  parentActivityId?: number | null
  defaultActivity?: boolean | null
  isActive?: boolean | null
}

/** `GET api/activity` — the paged catalogue. */
export function useActivityList(request: ActivityListRequest) {
  return useQuery({
    queryKey: [RESOURCES.activity, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<ActivityListDto>>(`/${RESOURCES.activity}`, {
        params: clean({
          SkipCount: request.skipCount,
          MaxResultCount: request.maxResultCount,
          Sorting: request.sorting,
          Filter: request.filter,
          ActivityType: request.activityType,
          ParentActivityId: request.parentActivityId,
          DefaultActivity: request.defaultActivity,
          IsActive: request.isActive,
        }),
      })
      return data
    },
  })
}

/** `GET api/activity/{id}/detail` — entry, group, period, parent and children. */
export function useActivityDetail(id: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.activity, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<ActivityNavigationDto>(`/${RESOURCES.activity}/${id}/detail`)
      return data
    },
  })
}

/** `GET api/lookup/periods` — recurrence period definitions. */
export function usePeriodLookup() {
  return useQuery({
    queryKey: [RESOURCES.lookup, 'periods'],
    queryFn: async () => {
      const { data } = await http.get<ListResult<PeriodLookupDto>>(`/${RESOURCES.lookup}/periods`)
      return data
    },
  })
}

// ---------------------------------------------------------------
// Writes
// ---------------------------------------------------------------

/**
 * A write against a nested route. The shared `useCreate` / `useUpdate` / `useDelete` helpers
 * only address `api/{resource}/{id}`, which the line routes are not; this keeps the same
 * contract — run the request, then invalidate the resources it changed.
 */
export function useApiMutation<TInput, TResult = void>(
  send: (input: TInput) => Promise<TResult>,
  invalidates: string[],
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: send,
    onSuccess: async () => {
      await Promise.all(
        invalidates.map((resource) => queryClient.invalidateQueries({ queryKey: [resource] })),
      )
    },
  })
}

/** `POST api/work-plan/{id}/lines` and `PUT .../{lineId}` behind one call. */
export function useSaveWorkPlanLine(planId: number) {
  return useApiMutation<{ lineId?: number; input: SaveWorkPlanLineDto }, WorkPlanLineDto>(
    async ({ lineId, input }) => {
      const base = `/${RESOURCES.workPlan}/${planId}/lines`
      const { data } = lineId
        ? await http.put<WorkPlanLineDto>(`${base}/${lineId}`, input)
        : await http.post<WorkPlanLineDto>(base, input)
      return data
    },
    [RESOURCES.workPlan],
  )
}

/** `DELETE api/work-plan/{id}/lines/{lineId}`. */
export function useDeleteWorkPlanLine(planId: number) {
  return useApiMutation<number>(async (lineId) => {
    await http.delete(`/${RESOURCES.workPlan}/${planId}/lines/${lineId}`)
  }, [RESOURCES.workPlan])
}

/** The three workflow transitions of a work plan line; `reject` carries the reason. */
export function useWorkPlanLineWorkflow(planId: number) {
  return useApiMutation<{ lineId: number; action: 'submit' | 'approve' | 'reject'; reason?: string }>(
    async ({ lineId, action, reason }) => {
      await http.post(
        `/${RESOURCES.workPlan}/${planId}/lines/${lineId}/${action}`,
        action === 'reject' ? { reason } : {},
      )
    },
    [RESOURCES.workPlan],
  )
}

/**
 * `POST api/work-plan/{id}/generate-default-lines?year=…`
 *
 * A one-off scaffolding step: the API refuses a second run on a plan that already has lines,
 * so the button is disabled once any line exists rather than letting the call fail.
 */
export function useGenerateDefaultLines(planId: number) {
  return useApiMutation<number, ListResult<WorkPlanLineDto>>(async (year) => {
    const { data } = await http.post<ListResult<WorkPlanLineDto>>(
      `/${RESOURCES.workPlan}/${planId}/generate-default-lines?year=${year}`,
      {},
    )
    return data
  }, [RESOURCES.workPlan])
}
