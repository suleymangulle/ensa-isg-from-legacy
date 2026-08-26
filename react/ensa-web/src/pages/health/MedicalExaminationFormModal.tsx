import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { FitnessForWorkOpinion, MedicalReportType } from '@/api/enums'
import { Field, Modal, controlClass } from '@/components/Form'
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

        <Field
          className="col-md-4"
          label={t('medicalExamination.fields.reportType')}
          htmlFor="exam-report-type"
          required
        >
          <select
            id="exam-report-type"
            className="form-select"
            value={state.reportType}
            onChange={(event) => patch({ reportType: Number(event.target.value) })}
          >
            {MEDICAL_REPORT_TYPES.map((type) => (
              <option key={type} value={type}>
                {t(`enums.medicalReportType.${type}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          className="col-md-4"
          label={t('medicalExamination.fields.examinationDate')}
          htmlFor="exam-date"
          required
        >
          <input
            id="exam-date"
            type="date"
            className="form-control"
            value={state.examinationDate}
            onChange={(event) => patch({ examinationDate: event.target.value })}
          />
        </Field>

        <Field
          className="col-md-4"
          label={t('medicalExamination.fields.validityDate')}
          htmlFor="exam-validity"
          hint={t('medicalExamination.form.validityHint')}
        >
          <input
            id="exam-validity"
            type="date"
            className="form-control"
            value={state.validityDate}
            onChange={(event) => patch({ validityDate: event.target.value })}
          />
        </Field>

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

        <Field
          className="col-md-6"
          label={t('medicalExamination.fields.ibysOccupationCode')}
          htmlFor="exam-occupation-code"
          hint={t('medicalExamination.form.occupationCodeHint')}
        >
          <input
            id="exam-occupation-code"
            className="form-control"
            value={state.ibysOccupationCode}
            onChange={(event) => patch({ ibysOccupationCode: event.target.value })}
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('medicalExamination.form.vitalsHeading')}
          </h3>
        </div>

        <Field
          className="col-6 col-md-2"
          label={t('medicalExamination.fields.heightCm')}
          htmlFor="exam-height"
        >
          <input
            id="exam-height"
            type="number"
            className="form-control"
            value={state.heightCm}
            onChange={(event) => patch({ heightCm: event.target.value })}
          />
        </Field>

        <Field
          className="col-6 col-md-2"
          label={t('medicalExamination.fields.weightKg')}
          htmlFor="exam-weight"
        >
          <input
            id="exam-weight"
            type="number"
            step="0.1"
            className="form-control"
            value={state.weightKg}
            onChange={(event) => patch({ weightKg: event.target.value })}
          />
        </Field>

        <Field
          className="col-6 col-md-3"
          label={t('medicalExamination.fields.bloodPressureSystolic')}
          htmlFor="exam-systolic"
        >
          <input
            id="exam-systolic"
            type="number"
            className="form-control"
            value={state.bloodPressureSystolic}
            onChange={(event) => patch({ bloodPressureSystolic: event.target.value })}
          />
        </Field>

        <Field
          className="col-6 col-md-3"
          label={t('medicalExamination.fields.bloodPressureDiastolic')}
          htmlFor="exam-diastolic"
        >
          <input
            id="exam-diastolic"
            type="number"
            className="form-control"
            value={state.bloodPressureDiastolic}
            onChange={(event) => patch({ bloodPressureDiastolic: event.target.value })}
          />
        </Field>

        <Field
          className="col-6 col-md-2"
          label={t('medicalExamination.fields.pulseRate')}
          htmlFor="exam-pulse"
        >
          <input
            id="exam-pulse"
            type="number"
            className="form-control"
            value={state.pulseRate}
            onChange={(event) => patch({ pulseRate: event.target.value })}
          />
        </Field>

        <Field
          label={t('medicalExamination.fields.chronicIllnessDeclaration')}
          htmlFor="exam-chronic"
        >
          <textarea
            id="exam-chronic"
            className="form-control"
            rows={2}
            value={state.chronicIllnessDeclaration}
            onChange={(event) => patch({ chronicIllnessDeclaration: event.target.value })}
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-bold mb-0 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('medicalExamination.form.opinionHeading')}
          </h3>
        </div>

        <Field
          className="col-md-4"
          label={t('medicalExamination.fields.opinion')}
          htmlFor="exam-opinion"
          required
          hint={t('medicalExamination.form.opinionHint')}
        >
          <select
            id="exam-opinion"
            className={controlClass('form-select')}
            value={state.opinion}
            onChange={(event) => patch({ opinion: Number(event.target.value) })}
          >
            {FITNESS_OPINIONS.map((opinion) => (
              <option key={opinion} value={opinion}>
                {t(`enums.fitnessForWorkOpinion.${opinion}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          className="col-md-8"
          label={t('medicalExamination.fields.opinionDescription')}
          htmlFor="exam-opinion-description"
        >
          <input
            id="exam-opinion-description"
            className="form-control"
            value={state.opinionDescription}
            onChange={(event) => patch({ opinionDescription: event.target.value })}
          />
        </Field>

        <Field label={t('medicalExamination.fields.recommendations')} htmlFor="exam-recommendations">
          <textarea
            id="exam-recommendations"
            className="form-control"
            rows={2}
            value={state.recommendations}
            onChange={(event) => patch({ recommendations: event.target.value })}
          />
        </Field>
      </div>
    </Modal>
  )
}
