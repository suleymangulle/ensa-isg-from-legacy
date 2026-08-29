import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, NumberInput, TextArea } from 'rich-react-component'
import { AccidentType, IncidentType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
import {
  OBSERVATION_ENDPOINTS,
  useCompanyLookup,
  useDepartmentLookup,
  useEmployeeLookup,
  type IncidentDto,
  type SaveIncidentDto,
} from './api'
import { EnumField, LookupField, enumValues, fromDateInput, toDateInput } from './components'

interface FormState {
  companyId?: number
  departmentId?: number
  incidentType?: IncidentType
  accidentType: AccidentType
  incidentDate: string
  description: string
  expression: string
  unitSupervisorId?: number
  supervisorFullName: string
  lostWorkDays: string
  isPerDate: string
  ssiNotificationDate: string
}

function initialState(incident?: IncidentDto): FormState {
  return {
    companyId: incident?.companyId,
    departmentId: incident?.departmentId,
    incidentType: incident?.incidentType,
    accidentType: incident?.accidentType ?? AccidentType.Unspecified,
    incidentDate: toDateInput(incident?.incidentDate),
    description: incident?.description ?? '',
    expression: incident?.expression ?? '',
    unitSupervisorId: incident?.unitSupervisorId ?? undefined,
    supervisorFullName: incident?.supervisorFullName ?? '',
    lostWorkDays: incident?.lostWorkDays != null ? String(incident.lostWorkDays) : '',
    isPerDate: toDateInput(incident?.isPerDate),
    ssiNotificationDate: toDateInput(incident?.ssiNotificationDate),
  }
}

/**
 * Create / edit dialog of an incident.
 *
 * Mount it conditionally (`{isOpen && <IncidentFormModal … />}`) so the draft is discarded when
 * the dialog closes rather than surviving into the next opening.
 */
export default function IncidentFormModal({
  incident,
  onClose,
}: {
  /** Absent for a create. */
  incident?: IncidentDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(incident))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const companies = useCompanyLookup()
  const departments = useDepartmentLookup(form.companyId)
  const employees = useEmployeeLookup(form.companyId)

  const create = useCreate<SaveIncidentDto, IncidentDto>(OBSERVATION_ENDPOINTS.incident, {
    onSuccess: onClose,
  })
  const update = useUpdate<SaveIncidentDto, IncidentDto>(OBSERVATION_ENDPOINTS.incident, {
    onSuccess: onClose,
  })

  const pending = create.isPending || update.isPending
  const failure = create.error ?? update.error

  function patch(next: Partial<FormState>) {
    setForm((current) => ({ ...current, ...next }))
  }

  function submit() {
    const found: Record<string, string> = {}
    if (!form.companyId) found.companyId = t('validation.required')
    if (!form.departmentId) found.departmentId = t('validation.required')
    if (!form.incidentType) found.incidentType = t('validation.required')
    if (!form.incidentDate) found.incidentDate = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveIncidentDto = {
      companyId: form.companyId!,
      departmentId: form.departmentId!,
      incidentType: form.incidentType!,
      accidentType: form.accidentType,
      incidentDate: form.incidentDate,
      description: form.description || null,
      expression: form.expression || null,
      unitSupervisorId: form.unitSupervisorId ?? null,
      supervisorFullName: form.supervisorFullName || null,
      lostWorkDays: form.lostWorkDays === '' ? null : Number(form.lostWorkDays),
      isPerDate: fromDateInput(form.isPerDate),
      ssiNotificationDate: fromDateInput(form.ssiNotificationDate),
    }

    if (incident) update.mutate({ id: incident.id, input: payload })
    else create.mutate(payload)
  }

  return (
    <Modal
      title={incident ? t('incident.form.editTitle') : t('incident.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <LookupField
          id="incident-company"
          className="col-md-6"
          label={t('incident.fields.company')}
          placeholder={t('observations.selectCompany')}
          required
          error={errors.companyId}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          value={form.companyId}
          onChange={(next) => patch({ companyId: next, departmentId: undefined, unitSupervisorId: undefined })}
        />

        <LookupField
          id="incident-department"
          className="col-md-6"
          label={t('incident.fields.department')}
          placeholder={
            form.companyId ? t('observations.selectDepartment') : t('observations.selectCompanyFirst')
          }
          required
          error={errors.departmentId}
          disabled={!form.companyId}
          items={departments.data?.items}
          isLoading={departments.isLoading}
          value={form.departmentId}
          onChange={(next) => patch({ departmentId: next })}
        />

        <EnumField
          id="incident-type"
          className="col-md-6"
          label={t('incident.fields.incidentType')}
          required
          error={errors.incidentType}
          placeholder={t('observations.select')}
          translationPrefix="enums.incidentType"
          values={enumValues(IncidentType)}
          value={form.incidentType}
          onChange={(next) => patch({ incidentType: next as IncidentType | undefined })}
        />

        <EnumField
          id="incident-accident-type"
          className="col-md-6"
          label={t('incident.fields.accidentType')}
          translationPrefix="enums.accidentType"
          values={enumValues(AccidentType)}
          value={form.accidentType}
          onChange={(next) => patch({ accidentType: (next ?? AccidentType.Unspecified) as AccidentType })}
        />

        <Input
          id="incident-date"
          label={t('incident.fields.incidentDate')}
          required
          error={errors.incidentDate}
          className="col-md-4"
          value={form.incidentDate}
          onChange={(value) => patch({ incidentDate: value })}
          inputProps={{ type: 'date' }}
        />

        <NumberInput
          id="incident-lost-days"
          label={t('incident.fields.lostWorkDays')}
          className="col-md-4"
          min={0}
          value={form.lostWorkDays === '' ? null : Number(form.lostWorkDays)}
          onChange={(value) => patch({ lostWorkDays: value === null ? '' : String(value) })}
        />

        <Input
          id="incident-isper-date"
          label={t('incident.fields.isPerDate')}
          helpText={t('incident.form.isPerDateHint')}
          className="col-md-4"
          value={form.isPerDate}
          onChange={(value) => patch({ isPerDate: value })}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="incident-ssi-date"
          label={t('incident.fields.ssiNotificationDate')}
          helpText={t('incident.form.ssiNotificationHint')}
          className="col-md-6"
          value={form.ssiNotificationDate}
          onChange={(value) => patch({ ssiNotificationDate: value })}
          inputProps={{ type: 'date' }}
        />

        <LookupField
          id="incident-supervisor"
          className="col-md-6"
          label={t('incident.fields.unitSupervisor')}
          placeholder={
            form.companyId ? t('observations.selectEmployee') : t('observations.selectCompanyFirst')
          }
          disabled={!form.companyId}
          items={employees.data?.items}
          isLoading={employees.isLoading}
          value={form.unitSupervisorId}
          onChange={(next) => patch({ unitSupervisorId: next })}
        />

        <Input
          id="incident-supervisor-name"
          label={t('incident.fields.supervisorFullName')}
          helpText={t('incident.form.supervisorNameHint')}
          className="col-12"
          value={form.supervisorFullName}
          onChange={(value) => patch({ supervisorFullName: value })}
        />

        <TextArea
          id="incident-description"
          label={t('incident.fields.description')}
          className="col-12"
          rows={3}
          value={form.description}
          onChange={(value) => patch({ description: value })}
        />

        <TextArea
          id="incident-expression"
          label={t('incident.fields.expression')}
          helpText={t('incident.form.expressionHint')}
          className="col-12"
          rows={3}
          value={form.expression}
          onChange={(value) => patch({ expression: value })}
        />
      </div>
    </Modal>
  )
}
