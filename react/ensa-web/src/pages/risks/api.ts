import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type BadgeVariant } from 'rich-react-component'
import { http, type ListResult, type PagedResult } from '@/api/http'
import type {
  ApprovalStatus,
  EmergencyPlanSectionType,
  EmergencyTeamType,
  ExistingControlMeasure,
  ExposedPersonGroup,
  HazardClass,
  HazardSourceType,
  ImprovementAction,
  LookupDto,
  ReportParticipantType,
  RiskAssessmentMethod,
  RiskHistoryRecordType,
  RiskLevel,
  StaffRole,
  VulnerableWorkerGroup,
} from '@/api/endpoints'

/**
 * Data layer of the risk module.
 *
 * The types below mirror `Ensa.Application.Contracts/Risks/Dtos` one to one — the API
 * serialises property names camelCase and enums as numbers, so the JSON contract is the C#
 * property set. They live here rather than in `src/api/endpoints.ts` because that file is
 * shared by every module and only carries the DTOs several modules need.
 */

// ---------------------------------------------------------------
// Resources
// ---------------------------------------------------------------

/** `api/risk-assessment-report` — see `RiskAssessmentReportController`. */
export const RISK_ASSESSMENT_REPORT = 'risk-assessment-report'

/** `api/emergency-action-plan` — see `EmergencyActionPlanController`. */
export const EMERGENCY_ACTION_PLAN = 'emergency-action-plan'

/** `api/company` — used for the company drop-down of both create forms. */
export const COMPANY = 'company'

/** `api/company-employee` — used for the emergency team member drop-down. */
export const COMPANY_EMPLOYEE = 'company-employee'

// ---------------------------------------------------------------
// Badge colours
//
// Only the colour mapping stays in code; the labels come from the locale bundle. The scale is
// ordinal, so it is rendered as one: green for what can be lived with, amber for what has to be
// watched, red for what has to be fixed, and a solid red for the intolerable end.
// ---------------------------------------------------------------

export const RISK_LEVEL_BADGE: Record<RiskLevel, BadgeVariant> = {
  0: 'primary',
  1: 'success',
  2: 'info',
  3: 'warning',
  4: 'danger',
  5: 'danger',
}

export const APPROVAL_STATUS_BADGE: Record<ApprovalStatus, BadgeVariant> = {
  0: 'primary',
  1: 'warning',
  2: 'success',
  3: 'danger',
}

// ---------------------------------------------------------------
// Risk assessment report DTOs
// ---------------------------------------------------------------

/** `RiskAssessmentReportListDto` — the row of `GET api/risk-assessment-report`. */
export interface RiskAssessmentReportListDto {
  id: number
  reportName: string
  companyId: number
  companyName?: string | null
  hazardClass: HazardClass
  reportMethod: RiskAssessmentMethod
  approvalStatus: ApprovalStatus
  performedDate: string
  validityDate: string
  revisionDate?: string | null
  workerCount: number
  specialistFullName?: string | null
  physicianFullName?: string | null
  isExpired: boolean
  remainingDays: number
}

/** `RiskAssessmentReportDto` — the header returned by `GET api/risk-assessment-report/{id}`. */
export interface RiskAssessmentReportDto {
  id: number
  tenantId?: number | null
  reportName: string
  companyId: number
  workplaceTitle: string
  businessActivity: string
  workplaceAddress: string
  workplacePhoneNumber: string
  hazardClass: HazardClass
  workplaceDepartments?: string | null
  machineryAndEquipment?: string | null
  hazardousArticles?: string | null
  wasteOperations?: string | null
  performedDate: string
  validityDate: string
  revisionDate?: string | null
  employer?: string | null
  specialistUserId?: number | null
  specialistFullName?: string | null
  physicianUserId?: number | null
  physicianFullName?: string | null
  workerCount: number
  reportMethod: RiskAssessmentMethod
  approvalStatus: ApprovalStatus
  isValid: boolean
}

/** `CreateRiskAssessmentReportDto`. `validityDate` is computed by the domain manager. */
export interface CreateRiskAssessmentReportDto {
  reportName: string
  companyId: number
  workplaceTitle: string
  businessActivity: string
  workplaceAddress: string
  workplacePhoneNumber: string
  hazardClass: HazardClass
  workplaceDepartments?: string | null
  machineryAndEquipment?: string | null
  hazardousArticles?: string | null
  wasteOperations?: string | null
  performedDate: string
  revisionDate?: string | null
  employer?: string | null
  specialistUserId?: number | null
  specialistFullName?: string | null
  physicianUserId?: number | null
  physicianFullName?: string | null
  workerCount: number
  reportMethod: RiskAssessmentMethod
}

/** `UpdateRiskAssessmentReportDto` — the create input plus the workflow status. */
export interface UpdateRiskAssessmentReportDto extends CreateRiskAssessmentReportDto {
  approvalStatus: ApprovalStatus
}

/** `IdentifiedHazardDto` — one hazard line of the report. */
export interface IdentifiedHazardDto {
  id: number
  tenantId?: number | null
  riskAssessmentReportId: number
  hazardCategoryId?: number | null
  hazardId?: number | null
  hazardTag: string
  activityDescription?: string | null
  ownerPerson?: string | null
  riskTag?: string | null
  measure?: string | null
  likelihood: number
  severity: number
  frequency: number
  riskScore: number
  riskLevel: RiskLevel
  comment?: string | null
  residualLikelihood?: number | null
  residualSeverity?: number | null
  residualFrequency?: number | null
  residualRiskScore?: number | null
  residualRiskLevel: RiskLevel
  residualComment?: string | null
  sourceType: HazardSourceType
  sourceId?: number | null
  documentId?: number | null
  deadlineDate?: string | null
}

/** `CreateIdentifiedHazardDto` / `UpdateIdentifiedHazardDto` — the score is server-computed. */
export interface SaveIdentifiedHazardDto {
  hazardCategoryId?: number | null
  hazardId?: number | null
  hazardTag: string
  activityDescription?: string | null
  ownerPerson?: string | null
  riskTag?: string | null
  measure?: string | null
  likelihood: number
  severity: number
  frequency: number
  comment?: string | null
  residualLikelihood?: number | null
  residualSeverity?: number | null
  residualFrequency?: number | null
  residualComment?: string | null
  sourceType: HazardSourceType
  sourceId?: number | null
  documentId?: number | null
  deadlineDate?: string | null
}

/** `ControlMeasureDto` — a measure attached to a hazard line. */
export interface ControlMeasureDto {
  id: number
  tenantId?: number | null
  identifiedHazardId: number
  measure: string
  deadlineDate?: string | null
  ownerCompanyEmployeeId?: number | null
  isCompleted: boolean
  completionDate?: string | null
}

/** `CreateControlMeasureDto`. */
export interface CreateControlMeasureDto {
  measure: string
  deadlineDate?: string | null
  ownerCompanyEmployeeId?: number | null
}

/** `RiskAssessmentExposedGroupDto`. */
export interface RiskAssessmentExposedGroupDto {
  id: number
  riskAssessmentReportId: number
  group: ExposedPersonGroup
}

/** `RiskAssessmentControlMeasureDto` — an already existing protective measure. */
export interface RiskAssessmentControlMeasureDto {
  id: number
  riskAssessmentReportId: number
  measure: ExistingControlMeasure
}

/** `RiskAssessmentImprovementActionDto`. */
export interface RiskAssessmentImprovementActionDto {
  id: number
  riskAssessmentReportId: number
  recommendation: ImprovementAction
}

/** `RiskAssessmentProtectedGroupDto` — vulnerable worker groups at the workplace. */
export interface RiskAssessmentProtectedGroupDto {
  id: number
  riskAssessmentReportId: number
  group: VulnerableWorkerGroup
  number?: number | null
}

/** `RiskAssessmentParticipantDto` — a member of the assessment team. */
export interface RiskAssessmentParticipantDto {
  id: number
  riskAssessmentReportId: number
  participantType: ReportParticipantType
  companyEmployeeId?: number | null
  fullName: string
  title?: string | null
}

/** `RiskAssessmentHistoryRecordDto` — past accident / disease / near-miss record. */
export interface RiskAssessmentHistoryRecordDto {
  id: number
  riskAssessmentReportId: number
  recordType: RiskHistoryRecordType
  date: string
  description: string
}

/** `IdentifiedHazardNavigationDto` — a hazard line with its library origin and measures. */
export interface IdentifiedHazardNavigationDto {
  identifiedHazard: IdentifiedHazardDto
  category?: LookupDto | null
  libraryHazard?: LookupDto | null
  controlMeasures: ControlMeasureDto[]
}

/** `RiskAssessmentReportNavigationDto` — everything the detail screen needs in one round trip. */
export interface RiskAssessmentReportNavigationDto {
  report: RiskAssessmentReportDto
  company?: LookupDto | null
  specialist?: LookupDto | null
  physician?: LookupDto | null
  identifiedHazards: IdentifiedHazardNavigationDto[]
  exposedGroups: RiskAssessmentExposedGroupDto[]
  controlMeasures: RiskAssessmentControlMeasureDto[]
  improvementActions: RiskAssessmentImprovementActionDto[]
  protectedGroups: RiskAssessmentProtectedGroupDto[]
  participants: RiskAssessmentParticipantDto[]
  historyRecords: RiskAssessmentHistoryRecordDto[]
  openHighRiskHazardCount: number
}

// ---------------------------------------------------------------
// Emergency action plan DTOs
// ---------------------------------------------------------------

/** `EmergencyActionPlanListDto` — the row of `GET api/emergency-action-plan`. */
export interface EmergencyActionPlanListDto {
  id: number
  companyId: number
  /** Company name resolved from the company record. */
  resolvedCompanyName?: string | null
  /** Workplace title snapshot stored on the plan itself. */
  companyName?: string | null
  hazardClass: HazardClass
  preparedDate: string
  validityDate: string
  teamsChief?: string | null
  isExpired: boolean
  remainingDays: number
}

/** `EmergencyActionPlanDto` — the plan header. */
export interface EmergencyActionPlanDto {
  id: number
  tenantId?: number | null
  companyId: number
  preparedDate: string
  validityDate: string
  companyName?: string | null
  address?: string | null
  registrationNo?: string | null
  hazardClass: HazardClass
  phone?: string | null
  teamsChief?: string | null
  emergencyTeam?: string | null
  workerRepresentative?: string | null
  supportStaff?: string | null
  employerOrDeputy?: string | null
  occupationalSafetySpecialist?: string | null
  workplacePhysician?: string | null
  protectionEmployee?: string | null
  evacuationPlanDocumentId?: number | null
  documentId?: number | null
  isValid: boolean
}

/** `CreateEmergencyActionPlanDto` / `UpdateEmergencyActionPlanDto`. */
export interface SaveEmergencyActionPlanDto {
  companyId: number
  preparedDate: string
  hazardClass: HazardClass
  companyName?: string | null
  address?: string | null
  registrationNo?: string | null
  phone?: string | null
  teamsChief?: string | null
  emergencyTeam?: string | null
  workerRepresentative?: string | null
  supportStaff?: string | null
  employerOrDeputy?: string | null
  occupationalSafetySpecialist?: string | null
  workplacePhysician?: string | null
  protectionEmployee?: string | null
  evacuationPlanDocumentId?: number | null
  documentId?: number | null
}

/** `EmergencyPlanSectionDto` — one free-text section of the plan. */
export interface EmergencyPlanSectionDto {
  id: number
  tenantId?: number | null
  emergencyActionPlanId: number
  sectionType: EmergencyPlanSectionType
  content: string
  orderNo: number
}

/** `SaveEmergencyPlanSectionDto` — upsert of the single row per (plan, section type). */
export interface SaveEmergencyPlanSectionDto {
  sectionType: EmergencyPlanSectionType
  content: string
}

/** `EmergencyTeamMemberDto`. */
export interface EmergencyTeamMemberDto {
  id: number
  tenantId?: number | null
  emergencyActionPlanId: number
  companyEmployeeId: number
  staffRole: StaffRole
  teamType: EmergencyTeamType
}

/** `CreateEmergencyTeamMemberDto`. */
export interface CreateEmergencyTeamMemberDto {
  companyEmployeeId: number
  staffRole: StaffRole
  teamType: EmergencyTeamType
}

/** `EmergencyTeamMemberNavigationDto` — a member together with the employee summary. */
export interface EmergencyTeamMemberNavigationDto {
  teamMember: EmergencyTeamMemberDto
  employee?: LookupDto | null
}

/** `EmergencyActionPlanNavigationDto` — the combined detail view. */
export interface EmergencyActionPlanNavigationDto {
  plan: EmergencyActionPlanDto
  company?: LookupDto | null
  evacuationPlanDocument?: LookupDto | null
  document?: LookupDto | null
  sections: EmergencyPlanSectionDto[]
  teamMembers: EmergencyTeamMemberNavigationDto[]
}

// ---------------------------------------------------------------
// List inputs
// ---------------------------------------------------------------

/** `GetRiskAssessmentReportListInput`. */
export interface RiskAssessmentReportListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  companyId?: number
  approvalStatus?: ApprovalStatus
  assessmentMethod?: RiskAssessmentMethod
  hazardClass?: HazardClass
  onlyExpiringSoon?: boolean
  expiringWithinDays?: number
}

/** `GetEmergencyActionPlanListInput`. */
export interface EmergencyActionPlanListInput {
  skipCount: number
  maxResultCount: number
  sorting?: string
  filter?: string
  companyId?: number
  hazardClass?: HazardClass
  onlyExpired?: boolean
}

// ---------------------------------------------------------------
// Queries
// ---------------------------------------------------------------

/** Drops empty values so an unset filter never reaches the query string. */
function clean(input: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(input).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  )
}

/** `GET api/risk-assessment-report` */
export function useRiskAssessmentList(input: RiskAssessmentReportListInput) {
  return useQuery({
    queryKey: [RISK_ASSESSMENT_REPORT, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<RiskAssessmentReportListDto>>(
        `/${RISK_ASSESSMENT_REPORT}`,
        { params: clean({ ...input }) },
      )
      return data
    },
  })
}

/** `GET api/risk-assessment-report/{id}/detail` */
export function useRiskAssessmentDetail(id: number | undefined) {
  return useQuery({
    queryKey: [RISK_ASSESSMENT_REPORT, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<RiskAssessmentReportNavigationDto>(
        `/${RISK_ASSESSMENT_REPORT}/${id}/detail`,
      )
      return data
    },
  })
}

/**
 * `GET api/risk-assessment-report/expiring`
 *
 * Both query parameters are mandatory on the controller, so the reference date is always sent
 * explicitly rather than left to the server default.
 */
export function useExpiringRiskAssessments(withinDays: number, companyId?: number) {
  const asOf = new Date().toISOString().slice(0, 10)

  return useQuery({
    queryKey: [RISK_ASSESSMENT_REPORT, 'expiring', asOf, withinDays, companyId],
    queryFn: async () => {
      const { data } = await http.get<ListResult<RiskAssessmentReportListDto>>(
        `/${RISK_ASSESSMENT_REPORT}/expiring`,
        { params: clean({ asOf, withinDays, companyId }) },
      )
      return data
    },
  })
}

/** `GET api/emergency-action-plan` */
export function useEmergencyPlanList(input: EmergencyActionPlanListInput) {
  return useQuery({
    queryKey: [EMERGENCY_ACTION_PLAN, 'list', input],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<EmergencyActionPlanListDto>>(
        `/${EMERGENCY_ACTION_PLAN}`,
        { params: clean({ ...input }) },
      )
      return data
    },
  })
}

/** `GET api/emergency-action-plan/{id}/detail` */
export function useEmergencyPlanDetail(id: number | undefined) {
  return useQuery({
    queryKey: [EMERGENCY_ACTION_PLAN, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<EmergencyActionPlanNavigationDto>(
        `/${EMERGENCY_ACTION_PLAN}/${id}/detail`,
      )
      return data
    },
  })
}

/** `GET api/company-employee/lookup?companyId=` — the employee drop-down of one workplace. */
export function useEmployeeLookup(companyId: number | undefined, filter?: string) {
  return useQuery({
    queryKey: [COMPANY_EMPLOYEE, 'lookup', companyId, filter],
    enabled: !!companyId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(`/${COMPANY_EMPLOYEE}/lookup`, {
        params: clean({ companyId, filter }),
      })
      return data
    },
  })
}

// ---------------------------------------------------------------
// Mutations
//
// The shared helpers in `src/api/mutations.ts` cover `POST /{resource}`,
// `PUT /{resource}/{id}` and `DELETE /{resource}/{id}`; the sub-resource routes of this module
// need their own, so they are written here and invalidate the very same `[resource]` key the
// query hooks above are filed under.
// ---------------------------------------------------------------

/** Invalidates every query of a resource — lists, detail views and lookups alike. */
function useResourceInvalidation(resource: string) {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: [resource] })
}

/** `POST api/risk-assessment-report/{id}/hazards` */
export function useAddHazard(reportId: number) {
  const invalidate = useResourceInvalidation(RISK_ASSESSMENT_REPORT)

  return useMutation({
    mutationFn: async (input: SaveIdentifiedHazardDto) => {
      const { data } = await http.post<IdentifiedHazardDto>(
        `/${RISK_ASSESSMENT_REPORT}/${reportId}/hazards`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `PUT api/risk-assessment-report/{id}/hazards/{hazardId}` */
export function useUpdateHazard(reportId: number) {
  const invalidate = useResourceInvalidation(RISK_ASSESSMENT_REPORT)

  return useMutation({
    mutationFn: async ({ hazardId, input }: { hazardId: number; input: SaveIdentifiedHazardDto }) => {
      const { data } = await http.put<IdentifiedHazardDto>(
        `/${RISK_ASSESSMENT_REPORT}/${reportId}/hazards/${hazardId}`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE api/risk-assessment-report/{id}/hazards/{hazardId}` */
export function useRemoveHazard(reportId: number) {
  const invalidate = useResourceInvalidation(RISK_ASSESSMENT_REPORT)

  return useMutation({
    mutationFn: async (hazardId: number) => {
      await http.delete(`/${RISK_ASSESSMENT_REPORT}/${reportId}/hazards/${hazardId}`)
    },
    onSuccess: invalidate,
  })
}

/** `POST api/risk-assessment-report/hazards/{hazardId}/control-measures` */
export function useAddControlMeasure() {
  const invalidate = useResourceInvalidation(RISK_ASSESSMENT_REPORT)

  return useMutation({
    mutationFn: async ({
      hazardId,
      input,
    }: {
      hazardId: number
      input: CreateControlMeasureDto
    }) => {
      const { data } = await http.post<ControlMeasureDto>(
        `/${RISK_ASSESSMENT_REPORT}/hazards/${hazardId}/control-measures`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/**
 * `POST api/risk-assessment-report/control-measures/{id}/complete?completionDate=`
 *
 * The completion date is a query parameter on the controller, not a body field.
 */
export function useCompleteControlMeasure() {
  const invalidate = useResourceInvalidation(RISK_ASSESSMENT_REPORT)

  return useMutation({
    mutationFn: async ({
      controlMeasureId,
      completionDate,
    }: {
      controlMeasureId: number
      completionDate: string
    }) => {
      const { data } = await http.post<ControlMeasureDto>(
        `/${RISK_ASSESSMENT_REPORT}/control-measures/${controlMeasureId}/complete`,
        null,
        { params: { completionDate } },
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** The three header enum sets are replaced wholesale, one `PUT` per set. */
export type HeaderSetName = 'exposed-groups' | 'existing-control-measures' | 'improvement-actions'

/** `PUT api/risk-assessment-report/{id}/{set}` with the complete list of selected values. */
export function useSaveHeaderSet(reportId: number, set: HeaderSetName) {
  const invalidate = useResourceInvalidation(RISK_ASSESSMENT_REPORT)

  return useMutation({
    mutationFn: async (values: number[]) => {
      await http.put(`/${RISK_ASSESSMENT_REPORT}/${reportId}/${set}`, values)
    },
    onSuccess: invalidate,
  })
}

/** `PUT api/emergency-action-plan/{id}/sections` — upsert of one section type. */
export function useSaveEmergencyPlanSection(planId: number) {
  const invalidate = useResourceInvalidation(EMERGENCY_ACTION_PLAN)

  return useMutation({
    mutationFn: async (input: SaveEmergencyPlanSectionDto) => {
      const { data } = await http.put<EmergencyPlanSectionDto>(
        `/${EMERGENCY_ACTION_PLAN}/${planId}/sections`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE api/emergency-action-plan/{id}/sections/{sectionType}` */
export function useRemoveEmergencyPlanSection(planId: number) {
  const invalidate = useResourceInvalidation(EMERGENCY_ACTION_PLAN)

  return useMutation({
    mutationFn: async (sectionType: EmergencyPlanSectionType) => {
      await http.delete(`/${EMERGENCY_ACTION_PLAN}/${planId}/sections/${sectionType}`)
    },
    onSuccess: invalidate,
  })
}

/** `POST api/emergency-action-plan/{id}/team-members` */
export function useAddEmergencyTeamMember(planId: number) {
  const invalidate = useResourceInvalidation(EMERGENCY_ACTION_PLAN)

  return useMutation({
    mutationFn: async (input: CreateEmergencyTeamMemberDto) => {
      const { data } = await http.post<EmergencyTeamMemberDto>(
        `/${EMERGENCY_ACTION_PLAN}/${planId}/team-members`,
        input,
      )
      return data
    },
    onSuccess: invalidate,
  })
}

/** `DELETE api/emergency-action-plan/{id}/team-members/{teamMemberId}` */
export function useRemoveEmergencyTeamMember(planId: number) {
  const invalidate = useResourceInvalidation(EMERGENCY_ACTION_PLAN)

  return useMutation({
    mutationFn: async (teamMemberId: number) => {
      await http.delete(`/${EMERGENCY_ACTION_PLAN}/${planId}/team-members/${teamMemberId}`)
    },
    onSuccess: invalidate,
  })
}
