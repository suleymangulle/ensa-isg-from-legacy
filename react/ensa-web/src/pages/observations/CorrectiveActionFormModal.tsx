import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { RiskCategory } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Field, Modal, controlClass } from '@/components/Form'
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

        <Field
          label={t('correctiveAction.fields.finding')}
          htmlFor="action-finding"
          required
          error={errors.finding}
          className="col-12"
        >
          <textarea
            id="action-finding"
            className={controlClass('form-control', errors.finding)}
            rows={3}
            value={form.finding}
            aria-invalid={errors.finding ? true : undefined}
            onChange={(event) => patch({ finding: event.target.value })}
          />
        </Field>

        <Field
          label={t('correctiveAction.fields.recommendation')}
          htmlFor="action-recommendation"
          className="col-12"
        >
          <textarea
            id="action-recommendation"
            className="form-control"
            rows={3}
            value={form.recommendation}
            onChange={(event) => patch({ recommendation: event.target.value })}
          />
        </Field>

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

        <Field
          label={t('correctiveAction.fields.owner')}
          htmlFor="action-owner"
          hint={t('correctiveAction.form.ownerHint')}
          className="col-md-6"
        >
          <input
            id="action-owner"
            type="text"
            className="form-control"
            value={form.owner}
            onChange={(event) => patch({ owner: event.target.value })}
          />
        </Field>

        <Field
          label={t('correctiveAction.fields.findingDate')}
          htmlFor="action-finding-date"
          className="col-md-4"
        >
          <input
            id="action-finding-date"
            type="date"
            className="form-control"
            value={form.findingDate}
            onChange={(event) => patch({ findingDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('correctiveAction.fields.deadlineDate')}
          htmlFor="action-deadline-date"
          className="col-md-4"
        >
          <input
            id="action-deadline-date"
            type="date"
            className="form-control"
            value={form.deadlineDate}
            onChange={(event) => patch({ deadlineDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('correctiveAction.fields.source')}
          htmlFor="action-source"
          hint={t('correctiveAction.form.sourceHint')}
          className="col-md-4"
        >
          <input
            id="action-source"
            type="text"
            className="form-control"
            value={form.source}
            onChange={(event) => patch({ source: event.target.value })}
          />
        </Field>
      </div>
    </Modal>
  )
}
