import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Field, Modal, controlClass } from '@/components/Form'
import {
  OBSERVATION_ENDPOINTS,
  useCompanyLookup,
  useDepartmentLookup,
  type FieldObservationReportDto,
  type SaveFieldObservationReportDto,
} from './api'
import { LookupField, toDateInput } from './components'

interface FormState {
  companyId?: number
  departmentId?: number
  date: string
  sendMail: boolean
  mailAddress: string
}

function initialState(report?: FieldObservationReportDto): FormState {
  return {
    companyId: report?.companyId,
    departmentId: report?.departmentId ?? undefined,
    date: toDateInput(report?.date ?? new Date().toISOString()),
    sendMail: false,
    mailAddress: '',
  }
}

/**
 * Create / edit dialog of a field observation report header.
 *
 * `sendMail` / `mailAddress` are the legacy `MailGonder` / `MailAddress` pass-through fields: they
 * are not persisted, the application service only uses them to notify the workplace after a save.
 */
export default function FieldObservationFormModal({
  report,
  onClose,
}: {
  /** Absent for a create. */
  report?: FieldObservationReportDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(report))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const companies = useCompanyLookup()
  const departments = useDepartmentLookup(form.companyId)

  const create = useCreate<SaveFieldObservationReportDto, FieldObservationReportDto>(
    OBSERVATION_ENDPOINTS.fieldObservationReport,
    { onSuccess: onClose },
  )
  const update = useUpdate<SaveFieldObservationReportDto, FieldObservationReportDto>(
    OBSERVATION_ENDPOINTS.fieldObservationReport,
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
    if (!form.date) found.date = t('validation.required')
    if (form.sendMail && !form.mailAddress.trim()) found.mailAddress = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveFieldObservationReportDto = {
      companyId: form.companyId!,
      departmentId: form.departmentId ?? null,
      date: form.date,
      sendMail: form.sendMail,
      mailAddress: form.sendMail ? form.mailAddress.trim() : null,
    }

    if (report) update.mutate({ id: report.id, input: payload })
    else create.mutate(payload)
  }

  return (
    <Modal
      title={report ? t('fieldObservation.form.editTitle') : t('fieldObservation.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
    >
      <div className="row g-3">
        <LookupField
          id="observation-company"
          className="col-md-6"
          label={t('fieldObservation.fields.company')}
          placeholder={t('observations.selectCompany')}
          required
          error={errors.companyId}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          value={form.companyId}
          onChange={(next) => patch({ companyId: next, departmentId: undefined })}
        />

        <LookupField
          id="observation-department"
          className="col-md-6"
          label={t('fieldObservation.fields.department')}
          placeholder={
            form.companyId
              ? t('observations.selectDepartment')
              : t('observations.selectCompanyFirst')
          }
          disabled={!form.companyId}
          items={departments.data?.items}
          isLoading={departments.isLoading}
          value={form.departmentId}
          onChange={(next) => patch({ departmentId: next })}
        />

        <Field
          label={t('fieldObservation.fields.date')}
          htmlFor="observation-date"
          required
          error={errors.date}
          className="col-md-6"
        >
          <input
            id="observation-date"
            type="date"
            className={controlClass('form-control', errors.date)}
            value={form.date}
            aria-invalid={errors.date ? true : undefined}
            onChange={(event) => patch({ date: event.target.value })}
          />
        </Field>

        <div className="col-12">
          <div className="form-check form-switch">
            <input
              id="observation-send-mail"
              type="checkbox"
              className="form-check-input"
              checked={form.sendMail}
              onChange={(event) => patch({ sendMail: event.target.checked })}
            />
            <label className="form-check-label" htmlFor="observation-send-mail">
              {t('fieldObservation.fields.sendMail')}
            </label>
          </div>
        </div>

        {form.sendMail && (
          <Field
            label={t('fieldObservation.fields.mailAddress')}
            htmlFor="observation-mail-address"
            required
            error={errors.mailAddress}
            className="col-12"
          >
            <input
              id="observation-mail-address"
              type="email"
              className={controlClass('form-control', errors.mailAddress)}
              value={form.mailAddress}
              aria-invalid={errors.mailAddress ? true : undefined}
              onChange={(event) => patch({ mailAddress: event.target.value })}
            />
          </Field>
        )}
      </div>
    </Modal>
  )
}
