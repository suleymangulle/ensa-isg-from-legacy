import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, TextArea } from 'rich-react-component'
import { RiskCategory } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
import {
  OBSERVATION_ENDPOINTS,
  useCompanyLookup,
  useEmployeeLookup,
  type CorrectiveActionDto,
  type SaveCorrectiveActionDto,
} from './api'
import { EnumField, LookupField, enumValues, fromDateInput, toDateInput } from './components'

interface FormState {
  companyId?: number
  finding: string
  recommendation: string
  source: string
  riskCategory: RiskCategory
  owner: string
  ownerCompanyEmployeeId?: number
  findingDate: string
  deadlineDate: string
}

function initialState(action?: CorrectiveActionDto, defaultCompanyId?: number): FormState {
  return {
    companyId: action?.companyId ?? defaultCompanyId,
    finding: action?.finding ?? '',
    recommendation: action?.recommendation ?? '',
    source: action?.source ?? '',
    riskCategory: action?.riskCategory ?? RiskCategory.Unspecified,
    owner: action?.owner ?? '',
    ownerCompanyEmployeeId: action?.ownerCompanyEmployeeId ?? undefined,
    findingDate: toDateInput(action?.findingDate),
    deadlineDate: toDateInput(action?.deadlineDate),
  }
}

/**
 * Create / edit dialog of a corrective and preventive action (DOF).
 *
 * The closing fields (result, result date, status) are absent on purpose — `UpdateCorrectiveActionDto`
 * does not carry them, because closing goes through `POST api/corrective-action/{id}/close`.
 */
export default function CorrectiveActionFormModal({
  action,
  defaultCompanyId,
  fieldObservationLineId,
  onClose,
}: {
  /** Absent for a create. */
  action?: CorrectiveActionDto
  /** Pre-selected company, used when the action is raised from another screen. */
  defaultCompanyId?: number
  /** Set when the action is derived from a field observation line. */
  fieldObservationLineId?: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(action, defaultCompanyId))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const companies = useCompanyLookup()
  const employees = useEmployeeLookup(form.companyId)

  const create = useCreate<SaveCorrectiveActionDto, CorrectiveActionDto>(
    OBSERVATION_ENDPOINTS.correctiveAction,
    { onSuccess: onClose },
  )
  const update = useUpdate<SaveCorrectiveActionDto, CorrectiveActionDto>(
    OBSERVATION_ENDPOINTS.correctiveAction,
    { onSuccess: onClose },
  )

  const pending = create.isPending || update.isPending
  const failure = create.error ?? update.error

  function patch(next: Partial<FormState>) {
    setForm((current) => ({ ...current, ...next }))
  }

  function submit() {
    const found: Record<string, string> = {}
    if (!form.companyId) found.companyId = t('validation.required')
    if (!form.finding.trim()) found.finding = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveCorrectiveActionDto = {
      companyId: form.companyId!,
      finding: form.finding.trim(),
      recommendation: form.recommendation || null,
      source: form.source || null,
      riskCategory: form.riskCategory,
      owner: form.owner || null,
      ownerCompanyEmployeeId: form.ownerCompanyEmployeeId ?? null,
      findingDate: fromDateInput(form.findingDate),
      deadlineDate: fromDateInput(form.deadlineDate),
      fieldObservationLineId: action?.fieldObservationLineId ?? fieldObservationLineId ?? null,
    }

    if (action) update.mutate({ id: action.id, input: payload })
    else create.mutate(payload)
  }

  return (
    <Modal
      title={action ? t('correctiveAction.form.editTitle') : t('correctiveAction.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <LookupField
          id="action-company"
          className="col-md-6"
          label={t('correctiveAction.fields.company')}
          placeholder={t('observations.selectCompany')}
          required
          error={errors.companyId}
          disabled={defaultCompanyId !== undefined && !action}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          value={form.companyId}
          onChange={(next) => patch({ companyId: next, ownerCompanyEmployeeId: undefined })}
        />

        <EnumField
          id="action-risk-category"
          className="col-md-6"
          label={t('correctiveAction.fields.riskCategory')}
          translationPrefix="enums.riskCategory"
          values={enumValues(RiskCategory)}
          value={form.riskCategory}
          onChange={(next) => patch({ riskCategory: (next ?? RiskCategory.Unspecified) as RiskCategory })}
        />

        <TextArea
          id="action-finding"
          label={t('correctiveAction.fields.finding')}
          required
          error={errors.finding}
          className="col-12"
          rows={3}
          value={form.finding}
          onChange={(value) => patch({ finding: value })}
        />

        <TextArea
          id="action-recommendation"
          label={t('correctiveAction.fields.recommendation')}
          className="col-12"
          rows={3}
          value={form.recommendation}
          onChange={(value) => patch({ recommendation: value })}
        />

        <LookupField
          id="action-owner-employee"
          className="col-md-6"
          label={t('correctiveAction.fields.ownerEmployee')}
          placeholder={
            form.companyId ? t('observations.selectEmployee') : t('observations.selectCompanyFirst')
          }
          disabled={!form.companyId}
          items={employees.data?.items}
          isLoading={employees.isLoading}
          value={form.ownerCompanyEmployeeId}
          onChange={(next) => patch({ ownerCompanyEmployeeId: next })}
        />

        <Input
          id="action-owner"
          label={t('correctiveAction.fields.owner')}
          helpText={t('correctiveAction.form.ownerHint')}
          className="col-md-6"
          value={form.owner}
          onChange={(value) => patch({ owner: value })}
        />

        <Input
          id="action-finding-date"
          label={t('correctiveAction.fields.findingDate')}
          className="col-md-4"
          value={form.findingDate}
          onChange={(value) => patch({ findingDate: value })}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="action-deadline-date"
          label={t('correctiveAction.fields.deadlineDate')}
          className="col-md-4"
          value={form.deadlineDate}
          onChange={(value) => patch({ deadlineDate: value })}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="action-source"
          label={t('correctiveAction.fields.source')}
          helpText={t('correctiveAction.form.sourceHint')}
          className="col-md-4"
          value={form.source}
          onChange={(value) => patch({ source: value })}
        />
      </div>
    </Modal>
  )
}
