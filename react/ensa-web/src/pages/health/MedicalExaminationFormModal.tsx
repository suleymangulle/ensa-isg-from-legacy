import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, Select, TextArea } from 'rich-react-component'
import { useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { FitnessForWorkOpinion, MedicalReportType } from '@/api/enums'
import { Modal } from '@/components/Form'
import {
  FITNESS_OPINIONS,
  HEALTH_ENDPOINTS,
  MEDICAL_REPORT_TYPES,
  useEmployeeLookup,
  useUserLookup,
  type MedicalExaminationFormDto,
  type SaveMedicalExaminationFormDto,
} from './api'
import LookupPicker from './components/LookupPicker'

/**
 * Create / edit dialog for the administrative and conclusion part of an EK-2 form.
 *
 * The six clinical child sets are not edited here — they live on the detail screen behind
 * their own save endpoints, so a form can be opened and its conclusion recorded without the
 * whole examination having to be re-entered.
 */

interface FormState {
  companyId: number | null
  companyName: string | null
  companyEmployeeId: number | null
  employeeName: string | null
  physicianUserId: number | null
  physicianName: string | null
  reportType: MedicalReportType
  examinationDate: string
  validityDate: string
  heightCm: string
  weightKg: string
  bloodPressureSystolic: string
  bloodPressureDiastolic: string
  pulseRate: string
  chronicIllnessDeclaration: string
  opinion: FitnessForWorkOpinion
  opinionDescription: string
  recommendations: string
  ibysOccupationCode: string
}

function toDateInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : ''
}

function initialState(
  form: MedicalExaminationFormDto | undefined,
  employeeName: string | null,
  companyName: string | null,
  physicianName: string | null,
): FormState {
  return {
    companyId: form?.companyId ?? null,
    companyName,
    companyEmployeeId: form?.companyEmployeeId ?? null,
    employeeName,
    physicianUserId: form?.physicianUserId ?? null,
    physicianName,
    reportType: form?.reportType ?? MedicalReportType.PeriodicExamination,
    examinationDate: toDateInput(form?.examinationDate) || new Date().toISOString().slice(0, 10),
    validityDate: toDateInput(form?.validityDate),
    heightCm: form?.heightCm != null ? String(form.heightCm) : '',
    weightKg: form?.weightKg != null ? String(form.weightKg) : '',
    bloodPressureSystolic:
      form?.bloodPressureSystolic != null ? String(form.bloodPressureSystolic) : '',
    bloodPressureDiastolic:
      form?.bloodPressureDiastolic != null ? String(form.bloodPressureDiastolic) : '',
    pulseRate: form?.pulseRate != null ? String(form.pulseRate) : '',
    chronicIllnessDeclaration: form?.chronicIllnessDeclaration ?? '',
    opinion: form?.opinion ?? FitnessForWorkOpinion.Unspecified,
    opinionDescription: form?.opinionDescription ?? '',
    recommendations: form?.recommendations ?? '',
    ibysOccupationCode: form?.ibysOccupationCode ?? '',
  }
}

function optionalNumber(value: string): number | null {
  const parsed = Number(value)
  return value.trim() === '' || Number.isNaN(parsed) ? null : parsed
}

interface Props {
  isOpen: boolean
  onClose: () => void
  /** Omitted for a create; supplied for an edit. */
  form?: MedicalExaminationFormDto
  employeeName?: string | null
  companyName?: string | null
  physicianName?: string | null
  onSaved?: (saved: MedicalExaminationFormDto) => void
}

export default function MedicalExaminationFormModal({
  isOpen,
  onClose,
  form,
  employeeName,
  companyName,
  physicianName,
  onSaved,
}: Props) {
  const { t } = useTranslation()
  const [state, setState] = useState<FormState>(() =>
    initialState(form, employeeName ?? null, companyName ?? null, physicianName ?? null),
  )
  const [employeeError, setEmployeeError] = useState<string>()

  const [companySearch, setCompanySearch] = useState('')
  const [employeeSearch, setEmployeeSearch] = useState('')
  const [physicianSearch, setPhysicianSearch] = useState('')

  const companies = useLookup('company', companySearch)
  const employees = useEmployeeLookup(state.companyId ?? undefined, employeeSearch)
  const physicians = useUserLookup(physicianSearch)

  const create = useCreate<SaveMedicalExaminationFormDto, MedicalExaminationFormDto>(
    HEALTH_ENDPOINTS.medicalExaminationForm,
    { onSuccess: (saved) => finish(saved) },
  )
  const update = useUpdate<SaveMedicalExaminationFormDto, MedicalExaminationFormDto>(
    HEALTH_ENDPOINTS.medicalExaminationForm,
    { onSuccess: (saved) => finish(saved) },
  )

  function finish(saved: MedicalExaminationFormDto) {
    onSaved?.(saved)
    onClose()
  }

  function patch(changes: Partial<FormState>) {
    setState((current) => ({ ...current, ...changes }))
  }

  function submit() {
    if (!state.companyEmployeeId) {
      setEmployeeError(t('validation.required'))
      return
    }
    setEmployeeError(undefined)

    const input: SaveMedicalExaminationFormDto = {
      companyEmployeeId: state.companyEmployeeId,
      companyId: state.companyId,
      reportType: state.reportType,
      examinationDate: state.examinationDate,
      validityDate: state.validityDate || null,
      physicianUserId: state.physicianUserId,
      heightCm: optionalNumber(state.heightCm),
      weightKg: optionalNumber(state.weightKg),
      bloodPressureSystolic: optionalNumber(state.bloodPressureSystolic),
      bloodPressureDiastolic: optionalNumber(state.bloodPressureDiastolic),
      pulseRate: optionalNumber(state.pulseRate),
      chronicIllnessDeclaration: state.chronicIllnessDeclaration.trim() || null,
      opinion: state.opinion,
      opinionDescription: state.opinionDescription.trim() || null,
      recommendations: state.recommendations.trim() || null,
      ibysOccupationCode: state.ibysOccupationCode.trim() || null,
    }

    if (form) update.mutate({ id: form.id, input })
    else create.mutate(input)
  }

  const mutation = form ? update : create

  return (
    <Modal
      title={form ? t('medicalExamination.form.editTitle') : t('medicalExamination.form.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
      size="xl"
    >
      <div className="row g-3">
        <LookupPicker
          id="exam-company"
          className="col-md-6"
          label={t('medicalExamination.fields.companyName')}
          searchPlaceholder={t('medicalExamination.form.searchCompany')}
          value={state.companyId}
          selectedName={state.companyName}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          onSearch={setCompanySearch}
          onChange={(id, name) =>
            patch({
              companyId: id,
              companyName: name,
              // The employee list is scoped to the workplace, so a company change clears it.
              companyEmployeeId: null,
              employeeName: null,
            })
          }
        />

        <LookupPicker
          id="exam-employee"
          className="col-md-6"
          required
          label={t('medicalExamination.fields.employee')}
          searchPlaceholder={t('medicalExamination.form.searchEmployee')}
          value={state.companyEmployeeId}
          selectedName={state.employeeName}
          items={employees.data?.items}
          isLoading={employees.isLoading}
          error={employeeError}
          onSearch={setEmployeeSearch}
          onChange={(id, name) => patch({ companyEmployeeId: id, employeeName: name })}
        />

        <Select<MedicalReportType>
          id="exam-report-type"
          className="col-md-4"
          label={t('medicalExamination.fields.reportType')}
          required
          options={MEDICAL_REPORT_TYPES.map((type) => ({
            value: type,
            label: t(`enums.medicalReportType.${type}`),
          }))}
          value={state.reportType}
          onChange={(value) =>
            patch({ reportType: value ?? MedicalReportType.PeriodicExamination })
          }
        />

        <Input
          id="exam-date"
          className="col-md-4"
          label={t('medicalExamination.fields.examinationDate')}
          required
          inputProps={{ type: 'date' }}
          value={state.examinationDate}
          onChange={(value) => patch({ examinationDate: value })}
        />

        <Input
          id="exam-validity"
          className="col-md-4"
          label={t('medicalExamination.fields.validityDate')}
          helpText={t('medicalExamination.form.validityHint')}
          inputProps={{ type: 'date' }}
          value={state.validityDate}
          onChange={(value) => patch({ validityDate: value })}
        />

        <LookupPicker
          id="exam-physician"
          className="col-md-6"
          label={t('medicalExamination.fields.physician')}
          searchPlaceholder={t('medicalExamination.form.searchPhysician')}
          value={state.physicianUserId}
          selectedName={state.physicianName}
          items={physicians.data?.items}
          isLoading={physicians.isLoading}
          onSearch={setPhysicianSearch}
          onChange={(id, name) => patch({ physicianUserId: id, physicianName: name })}
        />

        <Input
          id="exam-occupation-code"
          className="col-md-6"
          label={t('medicalExamination.fields.ibysOccupationCode')}
          helpText={t('medicalExamination.form.occupationCodeHint')}
          value={state.ibysOccupationCode}
          onChange={(value) => patch({ ibysOccupationCode: value })}
        />

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('medicalExamination.form.vitalsHeading')}
          </h3>
        </div>

        <Input
          id="exam-height"
          className="col-6 col-md-2"
          label={t('medicalExamination.fields.heightCm')}
          inputProps={{ type: 'number' }}
          value={state.heightCm}
          onChange={(value) => patch({ heightCm: value })}
        />

        <Input
          id="exam-weight"
          className="col-6 col-md-2"
          label={t('medicalExamination.fields.weightKg')}
          inputProps={{ type: 'number', step: '0.1' }}
          value={state.weightKg}
          onChange={(value) => patch({ weightKg: value })}
        />

        <Input
          id="exam-systolic"
          className="col-6 col-md-3"
          label={t('medicalExamination.fields.bloodPressureSystolic')}
          inputProps={{ type: 'number' }}
          value={state.bloodPressureSystolic}
          onChange={(value) => patch({ bloodPressureSystolic: value })}
        />

        <Input
          id="exam-diastolic"
          className="col-6 col-md-3"
          label={t('medicalExamination.fields.bloodPressureDiastolic')}
          inputProps={{ type: 'number' }}
          value={state.bloodPressureDiastolic}
          onChange={(value) => patch({ bloodPressureDiastolic: value })}
        />

        <Input
          id="exam-pulse"
          className="col-6 col-md-2"
          label={t('medicalExamination.fields.pulseRate')}
          inputProps={{ type: 'number' }}
          value={state.pulseRate}
          onChange={(value) => patch({ pulseRate: value })}
        />

        <TextArea
          id="exam-chronic"
          className="col-12"
          label={t('medicalExamination.fields.chronicIllnessDeclaration')}
          rows={2}
          value={state.chronicIllnessDeclaration}
          onChange={(value) => patch({ chronicIllnessDeclaration: value })}
        />

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('medicalExamination.form.opinionHeading')}
          </h3>
        </div>

        <Select<FitnessForWorkOpinion>
          id="exam-opinion"
          className="col-md-4"
          label={t('medicalExamination.fields.opinion')}
          required
          helpText={t('medicalExamination.form.opinionHint')}
          options={FITNESS_OPINIONS.map((opinion) => ({
            value: opinion,
            label: t(`enums.fitnessForWorkOpinion.${opinion}`),
          }))}
          value={state.opinion}
          onChange={(value) => patch({ opinion: value ?? FitnessForWorkOpinion.Unspecified })}
        />

        <Input
          id="exam-opinion-description"
          className="col-md-8"
          label={t('medicalExamination.fields.opinionDescription')}
          value={state.opinionDescription}
          onChange={(value) => patch({ opinionDescription: value })}
        />

        <TextArea
          id="exam-recommendations"
          className="col-12"
          label={t('medicalExamination.fields.recommendations')}
          rows={2}
          value={state.recommendations}
          onChange={(value) => patch({ recommendations: value })}
        />
      </div>
    </Modal>
  )
}
