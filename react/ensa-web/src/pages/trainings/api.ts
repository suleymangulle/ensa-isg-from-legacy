import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type BadgeVariant } from 'rich-react-component'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type { LookupDto } from '@/api/endpoints'
import {
  ApprovalStatus,
  HazardClass,
  IbysSubmissionStatus,
  PlanLineStatus,
  TrainingLocation,
  TrainingSubjectGroup,
  TrainingType,
} from '@/api/enums'

/**
 * Data layer of the training module.
 *
 * The shared helpers in `@/api/endpoints` and `@/api/mutations` cover flat resources; the
 * training endpoints are nested (`training/{id}/topics`, `training-plan/{id}/lines/{lineId}`)
 * and their filters carry more than `filter`/`sorting`, so the queries are declared here.
 * Every cache key starts with the resource name, which is what the shared mutation helpers
 * invalidate — a nested write therefore refreshes the list and the detail alike.
 */

/**
 * `TrainingPlanLineListDto` — one row of the cross-plan line list served by
 * `GET api/training-plan/lines`. The display names are resolved server-side, so the row is
 * ready to render without a second request.
 */
export interface TrainingPlanLineListDto {
  id: number
  trainingPlanId: number
  companyName?: string | null
  trainingName?: string | null
  year?: number | null
  month?: number | null
  durationMinutes: number
  status: PlanLineStatus
  approvalStatus?: ApprovalStatus | null
  performedDate?: string | null
  instructorFullName?: string | null
}

export const RESOURCES = {
  training: 'training',
  trainingPlan: 'training-plan',
  trainingProgress: 'employee-training-progress',
  companyEmployee: 'company-employee',
  company: 'company',
  user: 'user',
} as const

/** Hazard classes a duration can be recorded for; `Unspecified` is not one of them. */
export const HAZARD_CLASSES: HazardClass[] = [
  HazardClass.LowHazard,
  HazardClass.Hazardous,
  HazardClass.VeryHazardous,
]

export const TRAINING_TYPES: TrainingType[] = [
  TrainingType.BasicTraining,
  TrainingType.RefresherTraining,
  TrainingType.AdditionalTraining,
]

export const TRAINING_SUBJECT_GROUPS: TrainingSubjectGroup[] = [
  TrainingSubjectGroup.GeneralSubjects,
  TrainingSubjectGroup.HealthSubjects,
  TrainingSubjectGroup.TechnicalSubjects,
]

export const TRAINING_LOCATIONS: TrainingLocation[] = [
  TrainingLocation.OnSite,
  TrainingLocation.OffSite,
  TrainingLocation.RemoteTraining,
]

export const PLAN_LINE_STATUSES: PlanLineStatus[] = [
  PlanLineStatus.Planned,
  PlanLineStatus.Completed,
  PlanLineStatus.NotDone,
  PlanLineStatus.Postponed,
  PlanLineStatus.Cancelled,
]

/** Badge classes for the approval workflow; the state has to read at a glance. */
export const APPROVAL_STATUS_BADGE: Record<ApprovalStatus, BadgeVariant> = {
  [ApprovalStatus.Draft]: 'primary',
  [ApprovalStatus.SubmittedForApproval]: 'warning',
  [ApprovalStatus.Approved]: 'success',
  [ApprovalStatus.Rejected]: 'danger',
}

/** A line that has been approved is frozen: it can neither be edited nor removed. */
export function isApproved(status: ApprovalStatus | null | undefined): boolean {
  return status === ApprovalStatus.Approved
}

/** A line awaiting a decision is the only one that can be approved or rejected. */
export function isAwaitingDecision(status: ApprovalStatus | null | undefined): boolean {
  return status === ApprovalStatus.SubmittedForApproval
}

/** Draft and rejected lines are the ones that can be (re)submitted. */
export function canSubmit(status: ApprovalStatus | null | undefined): boolean {
  return status == null || status === ApprovalStatus.Draft || status === ApprovalStatus.Rejected
}

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/Trainings/Dtos
// ---------------------------------------------------------------

/** `TrainingDurationDto` — mandatory minutes of a training for one hazard class. */
export interface TrainingDurationDto {
  hazardClass: HazardClass
  durationMinutes: number
}

/** `TrainingListDto` — one row of the catalogue table. */
export interface TrainingListDto {
  id: number
  trainingName: string
  trainingCode?: string | null
  trainingGroupId?: number | null
  trainingType: TrainingType
  topicGroup: TrainingSubjectGroup
  mandatoryTraining: boolean
  defaultTraining: boolean
  isActive: boolean
  tenantId?: number | null
}

/** `TrainingDto` — the full catalogue entry. */
export interface TrainingDto {
  id: number
  tenantId?: number | null
  trainingName: string
  trainingCode?: string | null
  trainingGroupId?: number | null
  trainingType: TrainingType
  topicGroup: TrainingSubjectGroup
  mandatoryTraining: boolean
  isActive: boolean
  ibysTrainingCode?: number | null
  includedInDefaultPlan: boolean
  defaultTraining: boolean
  defaultCount: number
  defaultStartMonthOffset: number
  defaultElementCondition: number
  durations: TrainingDurationDto[]
}

/** `CreateTrainingDto` / `UpdateTrainingDto` — the duration set is replaced on every save. */
export interface SaveTrainingDto {
  trainingName: string
  trainingCode?: string | null
  trainingGroupId?: number | null
  trainingType: TrainingType
  topicGroup: TrainingSubjectGroup
  mandatoryTraining: boolean
  ibysTrainingCode?: number | null
  includedInDefaultPlan: boolean
  defaultTraining: boolean
  defaultCount: number
  defaultStartMonthOffset: number
  defaultElementCondition: number
  durations: TrainingDurationDto[]
  isActive?: boolean
}

/** `TrainingTopicDto` — a slide-deck section of a training. */
export interface TrainingTopicDto {
  id: number
  trainingId: number
  topicTitle: string
  presentationAddress?: string | null
  presentationPageCount: number
  topicOrder: number
  durations: TrainingDurationDto[]
}

/** `CreateTrainingTopicDto` / `UpdateTrainingTopicDto`. */
export interface SaveTrainingTopicDto {
  topicTitle: string
  presentationAddress?: string | null
  presentationPageCount: number
  topicOrder: number
  durations: TrainingDurationDto[]
}

/** `TrainingNavigationDto` — combined view behind `GET api/training/{id}/detail`. */
export interface TrainingNavigationDto {
  training: TrainingDto
  trainingGroup?: LookupDto | null
  topics: TrainingTopicDto[]
  exams: LookupDto[]
}

/** `TrainingValidityDto` — the statutory refresh calculation the API owns. */
export interface TrainingValidityDto {
  companyEmployeeId: number
  trainingId: number
  hazardClass: HazardClass
  isValid: boolean
  mandatoryDurationMinutes: number
}

/** `TrainingPlanListDto` — one row of the plan table. */
export interface TrainingPlanListDto {
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

/** `TrainingPlanDto` — the plan header (cover page). */
export interface TrainingPlanDto {
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
  isActive: boolean
  isTransferred: boolean
}

/** `CreateTrainingPlanDto` / `UpdateTrainingPlanDto`. */
export interface SaveTrainingPlanDto {
  companyId: number
  startDate: string
  revisionNo?: string | null
  revisionDate: string
  documentNo?: string | null
  publicationDate: string
  specialistUserId?: number | null
  physicianUserId?: number | null
  approverUserId?: number | null
  isActive?: boolean
  isTransferred?: boolean
}

/** `TrainingPlanLineDto` — one planned training with its approval state. */
export interface TrainingPlanLineDto {
  id: number
  trainingPlanId: number
  trainingId: number
  companyId?: number | null
  durationMinutes: number
  year?: number | null
  month?: number | null
  status: PlanLineStatus
  approvalStatus?: ApprovalStatus | null
  rejectionReason?: string | null
  performedDate?: string | null
  source?: string | null
  description?: string | null
  isActive: boolean
  forApprovalSenderUserId?: number | null
  approverUserId?: number | null
  forApprovalSendingDate?: string | null
  approvalDate?: string | null
  instructorNationalId?: string | null
  instructorTitle?: string | null
  instructorFullName?: string | null
  instructorUserId?: number | null
  trainingLocation?: TrainingLocation | null
  trainingType?: TrainingType | null
  ibysStatus: IbysSubmissionStatus
  ibysQueryId?: number | null
  ibysStatusCode?: string | null
  ibysMessage?: string | null
  documentId?: number | null
}

/** `CreateTrainingPlanLineDto` / `UpdateTrainingPlanLineDto`. */
export interface SaveTrainingPlanLineDto {
  trainingId: number
  durationMinutes: number
  year?: number | null
  month?: number | null
  status: PlanLineStatus
  performedDate?: string | null
  source?: string | null
  description?: string | null
  instructorNationalId?: string | null
  instructorTitle?: string | null
  instructorFullName?: string | null
  instructorUserId?: number | null
  trainingLocation?: TrainingLocation | null
  trainingType?: TrainingType | null
  documentId?: number | null
  isActive?: boolean
}

/** `TrainingPlanLineNavigationDto` — a line with the names the API already resolved. */
export interface TrainingPlanLineNavigationDto {
  line: TrainingPlanLineDto
  trainingName: string
  instructorUserFullName?: string | null
  documentName?: string | null
}

/** `TrainingPlanNavigationDto` — combined view behind `GET api/training-plan/{id}/detail`. */
export interface TrainingPlanNavigationDto {
  trainingPlan: TrainingPlanDto
  company?: LookupDto | null
  specialistFullName?: string | null
  physicianFullName?: string | null
  approverFullName?: string | null
  lines: TrainingPlanLineNavigationDto[]
}

/** `EmployeeTrainingProgressDto` — one employee's progress through one remote training. */
export interface EmployeeTrainingProgressDto {
  id: number
  tenantId?: number | null
  companyEmployeeId: number
  trainingId: number
  trainingTopicId?: number | null
  firstTestCompleted: boolean
  firstTestNote?: number | null
  latestTestCompleted: boolean
  latestTestNote?: number | null
  elapsedDurationSeconds: number
  activePage: number
  trainingSpecialistUserId?: number | null
  trainingPhysicianUserId?: number | null
  isActive: boolean
}

/** `EmployeeTrainingProgressNavigationDto` — progress plus the remaining statutory time. */
export interface EmployeeTrainingProgressNavigationDto {
  progress: EmployeeTrainingProgressDto
  employee?: LookupDto | null
  training?: LookupDto | null
  remainingDurationSeconds: number
}

/** `StartTrainingProgressDto`. */
export interface StartTrainingProgressDto {
  companyEmployeeId: number
  trainingId: number
  trainingTopicId?: number | null
  trainingSpecialistUserId?: number | null
  trainingPhysicianUserId?: number | null
}

/** `SaveTopicProgressDto` — the elapsed value never decreases server side. */
export interface SaveTopicProgressDto {
  trainingTopicId?: number | null
  elapsedDurationSeconds: number
  activePage: number
}

/** `SubmitExamDto`. */
export interface SubmitExamDto {
  isFirstTest: boolean
  score: number
  isCompleted: boolean
}

// ---------------------------------------------------------------
// Queries
// ---------------------------------------------------------------

/** Filter accepted by `GET api/training`. */
export interface TrainingListRequest {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  trainingType?: TrainingType | null
  topicGroup?: TrainingSubjectGroup | null
  isActive?: boolean | null
}

/** Drops empty values so an unset filter is not sent as an empty query parameter. */
function clean(params: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    result[key] = value
  }
  return result
}

/** `GET api/training` — the paged catalogue. */
export function useTrainingList(request: TrainingListRequest) {
  return useQuery({
    queryKey: [RESOURCES.training, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<TrainingListDto>>(`/${RESOURCES.training}`, {
        params: clean({
          SkipCount: request.skipCount,
          MaxResultCount: request.maxResultCount,
          Sorting: request.sorting,
          Filter: request.filter,
          TrainingType: request.trainingType,
          TopicGroup: request.topicGroup,
          IsActive: request.isActive,
        }),
      })
      return data
    },
  })
}

/** `GET api/training/{id}/detail` — entry, group, topics and exams in one request. */
export function useTrainingDetail(id: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.training, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<TrainingNavigationDto>(`/${RESOURCES.training}/${id}/detail`)
      return data
    },
  })
}

/**
 * `GET api/training/{id}/validity` — the statutory refresh calculation.
 * Disabled until an employee has been picked, because the API requires one.
 */
export function useTrainingValidity(
  trainingId: number | undefined,
  companyEmployeeId: number | undefined,
  hazardClass: HazardClass,
) {
  return useQuery({
    queryKey: [RESOURCES.training, 'validity', trainingId, companyEmployeeId, hazardClass],
    enabled: !!trainingId && !!companyEmployeeId,
    queryFn: async () => {
      const { data } = await http.get<TrainingValidityDto>(
        `/${RESOURCES.training}/${trainingId}/validity`,
        { params: { companyEmployeeId, hazardClass } },
      )
      return data
    },
  })
}

/** Filter accepted by `GET api/training-plan`. */
export interface TrainingPlanListRequest {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  companyId?: number | null
  year?: number | null
  isActive?: boolean | null
}

/** `GET api/training-plan` — the paged plan list. */
export function useTrainingPlanList(request: TrainingPlanListRequest) {
  return useQuery({
    queryKey: [RESOURCES.trainingPlan, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<TrainingPlanListDto>>(
        `/${RESOURCES.trainingPlan}`,
        {
          params: clean({
            SkipCount: request.skipCount,
            MaxResultCount: request.maxResultCount,
            Sorting: request.sorting,
            Filter: request.filter,
            CompanyId: request.companyId,
            Year: request.year,
            IsActive: request.isActive,
          }),
        },
      )
      return data
    },
  })
}

/** `GET api/training-plan/{id}/detail` — header, workplace, staff and every line. */
export function useTrainingPlanDetail(id: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.trainingPlan, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<TrainingPlanNavigationDto>(
        `/${RESOURCES.trainingPlan}/${id}/detail`,
      )
      return data
    },
  })
}

/** `GET api/training-plan/{id}/incomplete-lines` — everything not yet `Completed`. */
export function useIncompleteTrainingPlanLines(id: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.trainingPlan, 'incomplete-lines', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<ListResult<TrainingPlanLineDto>>(
        `/${RESOURCES.trainingPlan}/${id}/incomplete-lines`,
      )
      return data
    },
  })
}

/**
 * `GET api/employee-training-progress/employee/{employeeId}` — every record of one employee.
 *
 * The controller offers no cross-employee paged list, so the screen picks an employee first;
 * training names are resolved from a single lookup request rather than one call per row.
 */
export function useEmployeeProgressList(employeeId: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.trainingProgress, 'employee', employeeId],
    enabled: !!employeeId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<EmployeeTrainingProgressDto>>(
        `/${RESOURCES.trainingProgress}/employee/${employeeId}`,
      )
      return data
    },
  })
}

/** `GET api/employee-training-progress/{id}/detail` — adds the remaining statutory seconds. */
export function useProgressDetail(id: number | undefined) {
  return useQuery({
    queryKey: [RESOURCES.trainingProgress, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<EmployeeTrainingProgressNavigationDto>(
        `/${RESOURCES.trainingProgress}/${id}/detail`,
      )
      return data
    },
  })
}

/** `GET api/company-employee/lookup` — employees of one workplace, for the pickers. */
export function useEmployeeLookup(companyId: number | undefined, filter?: string) {
  return useQuery({
    queryKey: [RESOURCES.companyEmployee, 'lookup', companyId, filter],
    enabled: !!companyId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(
        `/${RESOURCES.companyEmployee}/lookup`,
        { params: clean({ companyId, filter }) },
      )
      return data
    },
  })
}

// ---------------------------------------------------------------
// Writes
// ---------------------------------------------------------------

/**
 * A write against a nested route. The shared `useCreate` / `useUpdate` / `useDelete` helpers
 * only address `api/{resource}/{id}`, which the topic and line routes are not; this keeps the
 * same contract — run the request, then invalidate the resources it changed.
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

/** `POST api/training/{id}/topics` and `PUT .../{topicId}` behind one call. */
export function useSaveTopic(trainingId: number) {
  return useApiMutation<{ topicId?: number; input: SaveTrainingTopicDto }, TrainingTopicDto>(
    async ({ topicId, input }) => {
      const base = `/${RESOURCES.training}/${trainingId}/topics`
      const { data } = topicId
        ? await http.put<TrainingTopicDto>(`${base}/${topicId}`, input)
        : await http.post<TrainingTopicDto>(base, input)
      return data
    },
    [RESOURCES.training],
  )
}

/** `DELETE api/training/{id}/topics/{topicId}`. */
export function useDeleteTopic(trainingId: number) {
  return useApiMutation<number>(async (topicId) => {
    await http.delete(`/${RESOURCES.training}/${trainingId}/topics/${topicId}`)
  }, [RESOURCES.training])
}

/** `POST api/training-plan/{id}/lines` and `PUT .../{lineId}` behind one call. */
export function useSavePlanLine(planId: number) {
  return useApiMutation<{ lineId?: number; input: SaveTrainingPlanLineDto }, TrainingPlanLineDto>(
    async ({ lineId, input }) => {
      const base = `/${RESOURCES.trainingPlan}/${planId}/lines`
      const { data } = lineId
        ? await http.put<TrainingPlanLineDto>(`${base}/${lineId}`, input)
        : await http.post<TrainingPlanLineDto>(base, input)
      return data
    },
    [RESOURCES.trainingPlan],
  )
}

/** `DELETE api/training-plan/{id}/lines/{lineId}`. */
export function useDeletePlanLine(planId: number) {
  return useApiMutation<number>(async (lineId) => {
    await http.delete(`/${RESOURCES.trainingPlan}/${planId}/lines/${lineId}`)
  }, [RESOURCES.trainingPlan])
}

/** The three workflow transitions of a plan line; `reject` carries the reason. */
export function usePlanLineWorkflow(planId: number) {
  return useApiMutation<{ lineId: number; action: 'submit' | 'approve' | 'reject'; reason?: string }>(
    async ({ lineId, action, reason }) => {
      await http.post(
        `/${RESOURCES.trainingPlan}/${planId}/lines/${lineId}/${action}`,
        action === 'reject' ? { reason } : {},
      )
    },
    [RESOURCES.trainingPlan],
  )
}

/** `POST api/employee-training-progress/start`. */
export function useStartProgress() {
  return useApiMutation<StartTrainingProgressDto, EmployeeTrainingProgressDto>(async (input) => {
    const { data } = await http.post<EmployeeTrainingProgressDto>(
      `/${RESOURCES.trainingProgress}/start`,
      input,
    )
    return data
  }, [RESOURCES.trainingProgress])
}

/** `PUT api/employee-training-progress/{id}/topic-progress`. */
export function useSaveTopicProgress() {
  return useApiMutation<{ id: number; input: SaveTopicProgressDto }, EmployeeTrainingProgressDto>(
    async ({ id, input }) => {
      const { data } = await http.put<EmployeeTrainingProgressDto>(
        `/${RESOURCES.trainingProgress}/${id}/topic-progress`,
        input,
      )
      return data
    },
    [RESOURCES.trainingProgress],
  )
}

/** `POST api/employee-training-progress/{id}/exam`. */
export function useSubmitExam() {
  return useApiMutation<{ id: number; input: SubmitExamDto }, EmployeeTrainingProgressDto>(
    async ({ id, input }) => {
      const { data } = await http.post<EmployeeTrainingProgressDto>(
        `/${RESOURCES.trainingProgress}/${id}/exam`,
        input,
      )
      return data
    },
    [RESOURCES.trainingProgress],
  )
}
