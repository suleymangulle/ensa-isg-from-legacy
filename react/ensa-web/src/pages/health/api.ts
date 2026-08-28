import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type BadgeVariant } from 'rich-react-component'
import { http, type ListResult, type PagedResult } from '@/api/http'
import { resourceKey } from '@/api/mutations'
import type { LookupDto } from '@/api/endpoints'
import {
  ExamFinding,
  FitnessForWorkOpinion,
  HabitStatus,
  HabitType,
  IbysSubmissionStatus,
  ImmunizationType,
  LabTestType,
  MedicalComplaintType,
  MedicalReportType,
  PhysicalExamSystem,
  PrescriptionNoteType,
  TriStateAnswer,
  WorkConditionType,
} from '@/api/enums'

/**
 * Health surveillance data layer — medical examination forms (EK-2), e-prescriptions and the
 * read-only SKRS reference catalogue.
 *
 * PRIVACY. Occupational health records are special-category personal data. The collection
 * shapes here deliberately carry no clinical field; clinical content only ever arrives through
 * a single-record read (`{id}` / `{id}/detail`). Nothing in this file may be used to build a
 * screen that lists diagnoses, complaints or findings.
 *
 * Route names are the kebab-case controller names produced by `KebabCaseParameterTransformer`
 * (`MedicalExaminationFormController` -> `api/medical-examination-form`).
 */
export const HEALTH_ENDPOINTS = {
  medicalExaminationForm: 'medical-examination-form',
  ePrescription: 'eprescription',
  medicalReference: 'medical-reference',
} as const

// ---------------------------------------------------------------
// Badge colours (styling only; labels live in the locale bundle)
// ---------------------------------------------------------------

export const IBYS_STATUS_BADGE: Record<IbysSubmissionStatus, BadgeVariant> = {
  [IbysSubmissionStatus.NotSent]: 'primary',
  [IbysSubmissionStatus.Prepared]: 'info',
  [IbysSubmissionStatus.Sent]: 'warning',
  [IbysSubmissionStatus.Approved]: 'success',
  [IbysSubmissionStatus.Failed]: 'danger',
  [IbysSubmissionStatus.Cancelled]: 'danger',
}

// ---------------------------------------------------------------
// Enum value lists — the row set of each clinical child editor.
//
// Each child table is unique on (form, type), so an editor renders one row per enum member
// rather than letting the user add arbitrary rows.
// ---------------------------------------------------------------

export const COMPLAINT_TYPES: MedicalComplaintType[] = [
  MedicalComplaintType.ProductiveCough,
  MedicalComplaintType.BreathShortness,
  MedicalComplaintType.ChestPain,
  MedicalComplaintType.Palpitation,
  MedicalComplaintType.BackPain,
  MedicalComplaintType.DiarrheaOrConstipation,
  MedicalComplaintType.JointPain,
  MedicalComplaintType.CardiacDisease,
  MedicalComplaintType.DiabetesDisease,
  MedicalComplaintType.RenalDisease,
  MedicalComplaintType.Jaundice,
  MedicalComplaintType.GastricOrDuodenalUlcer,
  MedicalComplaintType.HearingLoss,
  MedicalComplaintType.VisionImpairment,
  MedicalComplaintType.NervousSystemDisease,
  MedicalComplaintType.SkinDisease,
  MedicalComplaintType.FoodPoisoning,
  MedicalComplaintType.HospitalAdmission,
  MedicalComplaintType.Surgery,
  MedicalComplaintType.WorkAccident,
  MedicalComplaintType.OccupationalDiseaseSuspicion,
  MedicalComplaintType.Disability,
  MedicalComplaintType.OngoingTreatment,
]

export const WORK_CONDITION_TYPES: WorkConditionType[] = [
  WorkConditionType.AtHeightWork,
  WorkConditionType.NightWork,
  WorkConditionType.ShiftWork,
  WorkConditionType.HeavyAndHazardousWork,
  WorkConditionType.ConfinedSpaceWork,
  WorkConditionType.NoisyEnvironment,
  WorkConditionType.ChemicalExposure,
  WorkConditionType.PhysicalAndMentalFitness,
]

export const HABIT_TYPES: HabitType[] = [HabitType.Smoking, HabitType.Alcohol, HabitType.Substance]

export const PHYSICAL_EXAM_SYSTEMS: PhysicalExamSystem[] = [
  PhysicalExamSystem.SensoryEye,
  PhysicalExamSystem.SensoryEarNoseThroat,
  PhysicalExamSystem.SensorySkin,
  PhysicalExamSystem.CardiovascularSystem,
  PhysicalExamSystem.RespiratorySystem,
  PhysicalExamSystem.DigestiveSystem,
  PhysicalExamSystem.UrogenitalSystem,
  PhysicalExamSystem.MuscularSkeletalSystem,
  PhysicalExamSystem.Neurological,
  PhysicalExamSystem.Psychiatric,
  PhysicalExamSystem.Other,
]

export const LAB_TEST_TYPES: LabTestType[] = [
  LabTestType.Blood,
  LabTestType.Urine,
  LabTestType.RadiologicalImaging,
  LabTestType.Audiometry,
  LabTestType.RespiratoryFunctionTest,
  LabTestType.PsychologicalTest,
  LabTestType.Other,
]

export const IMMUNIZATION_TYPES: ImmunizationType[] = [
  ImmunizationType.Tetanus,
  ImmunizationType.HepatitisA,
  ImmunizationType.HepatitisB,
  ImmunizationType.Influenza,
  ImmunizationType.Covid,
  ImmunizationType.Other,
]

export const TRI_STATE_ANSWERS: TriStateAnswer[] = [
  TriStateAnswer.Unspecified,
  TriStateAnswer.No,
  TriStateAnswer.Yes,
  TriStateAnswer.Unknown,
]

export const EXAM_FINDINGS: ExamFinding[] = [
  ExamFinding.Unspecified,
  ExamFinding.Normal,
  ExamFinding.Pathological,
  ExamFinding.NotPerformed,
]

export const HABIT_STATUSES: HabitStatus[] = [
  HabitStatus.Unspecified,
  HabitStatus.NeverUsed,
  HabitStatus.Quit,
  HabitStatus.CurrentlyUsing,
]

export const MEDICAL_REPORT_TYPES: MedicalReportType[] = [
  MedicalReportType.PreEmploymentExamination,
  MedicalReportType.PeriodicExamination,
  MedicalReportType.JobChange,
  MedicalReportType.ReturnToWorkExamination,
  MedicalReportType.OnRequest,
]

export const FITNESS_OPINIONS: FitnessForWorkOpinion[] = [
  FitnessForWorkOpinion.Unspecified,
  FitnessForWorkOpinion.Fit,
  FitnessForWorkOpinion.ConditionallyFit,
  FitnessForWorkOpinion.Unfit,
  FitnessForWorkOpinion.FurtherTestsRequired,
]

export const IBYS_SUBMISSION_STATUSES: IbysSubmissionStatus[] = [
  IbysSubmissionStatus.NotSent,
  IbysSubmissionStatus.Prepared,
  IbysSubmissionStatus.Sent,
  IbysSubmissionStatus.Approved,
  IbysSubmissionStatus.Failed,
  IbysSubmissionStatus.Cancelled,
]

export const PRESCRIPTION_NOTE_TYPES: PrescriptionNoteType[] = [
  PrescriptionNoteType.Unspecified,
  PrescriptionNoteType.Diagnosis,
  PrescriptionNoteType.TreatmentDuration,
  PrescriptionNoteType.PatientSafetyAndMonitoringForm,
]

// ---------------------------------------------------------------
// DTOs — mirrored from Ensa.Application.Contracts/Health/Dtos
// ---------------------------------------------------------------

/** `MedicalExaminationFormDto` — the full record, clinical header included. */
export interface MedicalExaminationFormDto {
  id: number
  tenantId?: number | null
  companyId?: number | null
  companyEmployeeId: number
  reportType: MedicalReportType
  examinationDate: string
  validityDate?: string | null
  physicianUserId?: number | null
  heightCm?: number | null
  weightKg?: number | null
  bodyMassIndex?: number | null
  bloodPressureSystolic?: number | null
  bloodPressureDiastolic?: number | null
  pulseRate?: number | null
  chronicIllnessDeclaration?: string | null
  opinion: FitnessForWorkOpinion
  opinionDescription?: string | null
  recommendations?: string | null
  documentId?: number | null
  ibysStatus: IbysSubmissionStatus
  ibysQueryId?: number | null
  ibysStatusCode?: number | null
  ibysStatusMessage?: string | null
  ibysGroupCode?: string | null
  ibysOccupationCode?: string | null
  ibysWorkEnvironmentCodes?: string | null
  ibysWorkArrangementCodes?: string | null
  ibysWorkEquipmentCodes?: string | null
  source?: string | null
}

/**
 * `CreateMedicalExaminationFormDto` / `UpdateMedicalExaminationFormDto`.
 * `ibysGroupCode` is accepted on update only.
 */
export interface SaveMedicalExaminationFormDto {
  companyEmployeeId: number
  companyId?: number | null
  reportType: MedicalReportType
  examinationDate: string
  validityDate?: string | null
  physicianUserId?: number | null
  heightCm?: number | null
  weightKg?: number | null
  bloodPressureSystolic?: number | null
  bloodPressureDiastolic?: number | null
  pulseRate?: number | null
  chronicIllnessDeclaration?: string | null
  opinion: FitnessForWorkOpinion
  opinionDescription?: string | null
  recommendations?: string | null
  ibysOccupationCode?: string | null
}

export interface MedicalExamComplaintDto {
  id: number
  medicalExaminationFormId: number
  complaintType: MedicalComplaintType
  answer: TriStateAnswer
  description?: string | null
}

export interface SaveMedicalExamComplaintDto {
  complaintType: MedicalComplaintType
  answer: TriStateAnswer
  description?: string | null
}

export interface MedicalExamPhysicalFindingDto {
  id: number
  medicalExaminationFormId: number
  system: PhysicalExamSystem
  finding: ExamFinding
  description?: string | null
}

export interface SaveMedicalExamPhysicalFindingDto {
  system: PhysicalExamSystem
  finding: ExamFinding
  description?: string | null
}

export interface MedicalExamLabTestDto {
  id: number
  medicalExaminationFormId: number
  labTestType: LabTestType
  isCompleted: boolean
  result?: string | null
  date?: string | null
}

export interface SaveMedicalExamLabTestDto {
  labTestType: LabTestType
  isCompleted: boolean
  result?: string | null
  date?: string | null
}

export interface MedicalExamHabitDto {
  id: number
  medicalExaminationFormId: number
  habitType: HabitType
  status: HabitStatus
  dailyQuantity?: number | null
  durationYear?: number | null
  cessationYearBefore?: number | null
  description?: string | null
}

export interface SaveMedicalExamHabitDto {
  habitType: HabitType
  status: HabitStatus
  dailyQuantity?: number | null
  durationYear?: number | null
  cessationYearBefore?: number | null
  description?: string | null
}

export interface MedicalExamWorkConditionDto {
  id: number
  medicalExaminationFormId: number
  conditionType: WorkConditionType
  suitable: TriStateAnswer
}

export interface SaveMedicalExamWorkConditionDto {
  conditionType: WorkConditionType
  suitable: TriStateAnswer
}

export interface MedicalExamImmunizationDto {
  id: number
  medicalExaminationFormId: number
  immunizationType: ImmunizationType
  date?: string | null
  description?: string | null
}

export interface SaveMedicalExamImmunizationDto {
  immunizationType: ImmunizationType
  date?: string | null
  description?: string | null
}

/**
 * `MedicalExaminationFormNavigationDto` — the only shape exposing the complete clinical
 * picture. Employee and company are reduced to lookups, so no national id travels with a
 * health record; a screen must not enrich them from another endpoint.
 */
export interface MedicalExaminationFormNavigationDto {
  form: MedicalExaminationFormDto
  employee?: LookupDto | null
  company?: LookupDto | null
  physicianFullName?: string | null
  complaints: MedicalExamComplaintDto[]
  physicalFindings: MedicalExamPhysicalFindingDto[]
  labTests: MedicalExamLabTestDto[]
  habits: MedicalExamHabitDto[]
  workConditions: MedicalExamWorkConditionDto[]
  immunizations: MedicalExamImmunizationDto[]
  previousExaminationDate?: string | null
  ibysQueryNo?: string | null
}

/** `GetMedicalExaminationFormListInput`. */
export interface MedicalExaminationListRequest {
  skipCount?: number
  maxResultCount?: number
  sorting?: string
  filter?: string
  companyId?: number | null
  companyEmployeeId?: number | null
  physicianUserId?: number | null
  reportType?: MedicalReportType | null
  opinion?: FitnessForWorkOpinion | null
  ibysStatus?: IbysSubmissionStatus | null
  examinationDateFrom?: string | null
  examinationDateTo?: string | null
}

/** `MedicalExaminationFormListDto` — administrative envelope only, no clinical field. */
export interface MedicalExaminationFormListDto {
  id: number
  companyEmployeeId: number
  employeeFullName?: string | null
  companyId?: number | null
  companyName?: string | null
  reportType: MedicalReportType
  examinationDate: string
  validityDate?: string | null
  physicianUserId?: number | null
  physicianFullName?: string | null
  opinion: FitnessForWorkOpinion
  ibysStatus: IbysSubmissionStatus
}

/** `EPrescriptionListDto` — the prescription envelope; carries no medication or diagnosis. */
export interface EPrescriptionListDto {
  id: number
  ePrescriptionCode?: string | null
  protocolNo?: string | null
  patientNationalId: string
  patientCompanyEmployeeId?: number | null
  patientFullName?: string | null
  cancelled: boolean
  submissionDate?: string | null
  creationTime: string
  resultCode?: string | null
}

/** `EPrescriptionDto` — the prescription header. */
export interface EPrescriptionDto {
  id: number
  tenantId?: number | null
  ePrescriptionCode?: string | null
  protocolNo?: string | null
  patientNationalId: string
  patientCompanyEmployeeId?: number | null
  description?: string | null
  descriptionType: PrescriptionNoteType
  cancelled: boolean
  submissionDate?: string | null
  resultCode?: string | null
  resultMessage?: string | null
  warningMessage?: string | null
}

export interface EPrescriptionMedicationLineDto {
  id: number
  medicationId: number
  medicationName?: string | null
  medicationBarcode?: string | null
  usageMethodId: number
  usageMethodName?: string | null
  usageDoseUnitId: number
  doseUnitName?: string | null
  usagePeriodUnitId: number
  periodUnitName?: string | null
  box: number
  dose: number
  doseFraction?: number | null
  period: number
  medicationDescription?: string | null
}

export interface EPrescriptionDiagnosisLineDto {
  id: number
  icd10Code: string
  icd10Id?: number | null
  icd10Name?: string | null
}

/** `EPrescriptionNavigationDto`. */
export interface EPrescriptionNavigationDto {
  ePrescription: EPrescriptionDto
  patient?: LookupDto | null
  medications: EPrescriptionMedicationLineDto[]
  diagnoses: EPrescriptionDiagnosisLineDto[]
}

/** `SaveEPrescriptionMedicationDto`. */
export interface SaveEPrescriptionMedicationDto {
  medicationId: number
  medicationBarcode?: string | null
  usageMethodId: number
  usageDoseUnitId: number
  usagePeriodUnitId: number
  box: number
  dose: number
  doseFraction?: number | null
  period: number
  medicationDescription?: string | null
  medicationDescriptionType: PrescriptionNoteType
}

/** `SaveEPrescriptionDiagnosisDto`. */
export interface SaveEPrescriptionDiagnosisDto {
  icd10Code: string
  icd10Id?: number | null
}

/** `CreateEPrescriptionDto` / `UpdateEPrescriptionDto`. */
export interface SaveEPrescriptionDto {
  patientNationalId: string
  patientCompanyEmployeeId?: number | null
  protocolNo?: string | null
  description?: string | null
  descriptionType: PrescriptionNoteType
  medications: SaveEPrescriptionMedicationDto[]
  diagnoses: SaveEPrescriptionDiagnosisDto[]
}

/** `GetEPrescriptionListInput`. */
export interface EPrescriptionListRequest {
  skipCount?: number
  maxResultCount?: number
  sorting?: string
  filter?: string
  /** Exact match only — a partial match on a national id would enumerate identities. */
  patientNationalId?: string | null
  patientCompanyEmployeeId?: number | null
  cancelled?: boolean | null
  dateFrom?: string | null
  dateTo?: string | null
}

/** `Icd10LookupDto` — host reference data, no personal content. */
export interface Icd10LookupDto {
  id: number
  code: string
  name: string
  parentCode?: string | null
  level?: number | null
  isActive: boolean
}

/** `MedicationLookupDto` — SKRS catalogue entry. */
export interface MedicationLookupDto {
  id: number
  medicationName: string
  barcode?: string | null
  generatorCompanyName?: string | null
  atcCode?: string | null
  atcName?: string | null
  prescriptionType?: string | null
  isActive: boolean
}

// ---------------------------------------------------------------
// Query helpers
// ---------------------------------------------------------------

/**
 * Drops null/undefined/empty entries and upper-cases the first letter, which is what the
 * `[FromQuery]` input classes bind against.
 */
function queryParams(request: Record<string, unknown>): Record<string, unknown> {
  const params: Record<string, unknown> = {}

  for (const [key, value] of Object.entries(request)) {
    if (value === null || value === undefined || value === '') continue
    params[key.charAt(0).toUpperCase() + key.slice(1)] = value
  }
  return params
}

/** `GET api/medical-examination-form` — clinical-free paged list. */
export function useMedicalExaminationList(request: MedicalExaminationListRequest) {
  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.medicalExaminationForm, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<MedicalExaminationFormListDto>>(
        `/${HEALTH_ENDPOINTS.medicalExaminationForm}`,
        { params: queryParams({ maxResultCount: 20, skipCount: 0, ...request }) },
      )
      return data
    },
  })
}

/** `GET api/medical-examination-form/{id}/detail` — one record, clinical content included. */
export function useMedicalExaminationDetail(id: number | undefined) {
  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.medicalExaminationForm, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<MedicalExaminationFormNavigationDto>(
        `/${HEALTH_ENDPOINTS.medicalExaminationForm}/${id}/detail`,
      )
      return data
    },
  })
}

/**
 * `GET api/medical-examination-form/company/{companyId}/expiring?asOf=` — examinations of one
 * workplace whose validity has lapsed as of the given date. The endpoint is per workplace, so
 * the caller passes the selected company; without one the query stays idle.
 */
export function useExpiringExaminations(companyId: number | undefined, asOf: string) {
  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.medicalExaminationForm, 'expiring', companyId, asOf],
    enabled: !!companyId,
    queryFn: async () => {
      const { data } = await http.get<ListResult<MedicalExaminationFormListDto>>(
        `/${HEALTH_ENDPOINTS.medicalExaminationForm}/company/${companyId}/expiring`,
        { params: { asOf } },
      )
      return data
    },
  })
}

/** The six clinical child sets, each replaced wholesale by its own PUT. */
export const CHILD_SET_PATHS = {
  complaints: 'complaints',
  workConditions: 'work-conditions',
  habits: 'habits',
  physicalFindings: 'physical-findings',
  labTests: 'lab-tests',
  immunizations: 'immunizations',
} as const

export type ChildSetKey = keyof typeof CHILD_SET_PATHS

/**
 * `PUT api/medical-examination-form/{id}/{childSet}` — replaces one clinical child set.
 * Every set is unique on (form, type), so the payload holds at most one row per enum member.
 */
export function useSaveChildSet<TInput>(formId: number, childSet: ChildSetKey) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (rows: TInput[]) => {
      const { data } = await http.put<ListResult<unknown>>(
        `/${HEALTH_ENDPOINTS.medicalExaminationForm}/${formId}/${CHILD_SET_PATHS[childSet]}`,
        rows,
      )
      return data
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: resourceKey(HEALTH_ENDPOINTS.medicalExaminationForm),
      })
    },
  })
}

/** `GET api/eprescription` — prescription envelopes only. */
export function useEPrescriptionList(request: EPrescriptionListRequest) {
  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.ePrescription, 'list', request],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<EPrescriptionListDto>>(
        `/${HEALTH_ENDPOINTS.ePrescription}`,
        { params: queryParams({ maxResultCount: 20, skipCount: 0, ...request }) },
      )
      return data
    },
  })
}

/** `GET api/eprescription/{id}/detail` — header plus medication and diagnosis lines. */
export function useEPrescriptionDetail(id: number | undefined) {
  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.ePrescription, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await http.get<EPrescriptionNavigationDto>(
        `/${HEALTH_ENDPOINTS.ePrescription}/${id}/detail`,
      )
      return data
    },
  })
}

/** `POST api/eprescription/{id}/cancel` — cancellation always records a reason. */
export function useCancelPrescription() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, reason }: { id: number; reason: string }) => {
      const { data } = await http.post<EPrescriptionDto>(
        `/${HEALTH_ENDPOINTS.ePrescription}/${id}/cancel`,
        { reason },
      )
      return data
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: resourceKey(HEALTH_ENDPOINTS.ePrescription) })
    },
  })
}

// ---------------------------------------------------------------
// SKRS reference catalogue (read-only host data, no personal content)
// ---------------------------------------------------------------

/** Shortest search term the catalogue endpoints are queried with. */
export const REFERENCE_MIN_TERM_LENGTH = 2

/** `GET api/medical-reference/icd10` — requires a search term. */
export function useIcd10Search(term: string, maxResultCount = 20) {
  const enabled = term.trim().length >= REFERENCE_MIN_TERM_LENGTH

  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.medicalReference, 'icd10', term, maxResultCount],
    enabled,
    queryFn: async () => {
      const { data } = await http.get<ListResult<Icd10LookupDto>>(
        `/${HEALTH_ENDPOINTS.medicalReference}/icd10`,
        { params: { filter: term.trim(), maxResultCount } },
      )
      return data
    },
  })
}

/** `GET api/medical-reference/medications` — exact barcode or name fragment. */
export function useMedicationSearch(term: string, maxResultCount = 20) {
  const enabled = term.trim().length >= REFERENCE_MIN_TERM_LENGTH

  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.medicalReference, 'medications', term, maxResultCount],
    enabled,
    queryFn: async () => {
      const { data } = await http.get<ListResult<MedicationLookupDto>>(
        `/${HEALTH_ENDPOINTS.medicalReference}/medications`,
        { params: { filter: term.trim(), maxResultCount } },
      )
      return data
    },
  })
}

/** The three fixed SKRS code lists, cached for the session — they never change at runtime. */
function useReferenceList(path: string) {
  return useQuery({
    queryKey: [HEALTH_ENDPOINTS.medicalReference, path],
    staleTime: Infinity,
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>(
        `/${HEALTH_ENDPOINTS.medicalReference}/${path}`,
      )
      return data
    },
  })
}

/** `GET api/medical-reference/medication-routes` — routes of administration. */
export function useMedicationRoutes() {
  return useReferenceList('medication-routes')
}

/** `GET api/medical-reference/medication-dose-units`. */
export function useMedicationDoseUnits() {
  return useReferenceList('medication-dose-units')
}

/** `GET api/medical-reference/medication-frequency-units`. */
export function useMedicationFrequencyUnits() {
  return useReferenceList('medication-frequency-units')
}

/** `GET api/company-employee/lookup?companyId=` — employee picker, scoped to a workplace. */
export function useEmployeeLookup(companyId: number | undefined, filter: string) {
  return useQuery({
    queryKey: ['company-employee', 'lookup', companyId, filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/company-employee/lookup', {
        params: { companyId: companyId || undefined, filter: filter || undefined },
      })
      return data
    },
  })
}

/** `GET api/user/lookup` — physician picker. */
export function useUserLookup(filter: string) {
  return useQuery({
    queryKey: ['user', 'lookup', filter],
    queryFn: async () => {
      const { data } = await http.get<ListResult<LookupDto>>('/user/lookup', {
        params: { filter: filter || undefined },
      })
      return data
    },
  })
}
