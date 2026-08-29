import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Input, Switch } from 'rich-react-component'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
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

        <Input
          id="observation-date"
          label={t('fieldObservation.fields.date')}
          required
          error={errors.date}
          className="col-md-6"
          value={form.date}
          onChange={(value) => patch({ date: value })}
          inputProps={{ type: 'date' }}
        />

        <div className="col-12">
          <Switch
            id="observation-send-mail"
            checked={form.sendMail}
            onChange={(checked) => patch({ sendMail: checked })}
            label={t('fieldObservation.fields.sendMail')}
          />
        </div>

        {form.sendMail && (
          <Input
            id="observation-mail-address"
            type="email"
            label={t('fieldObservation.fields.mailAddress')}
            required
            error={errors.mailAddress}
            className="col-12"
            value={form.mailAddress}
            onChange={(value) => patch({ mailAddress: value })}
          />
        )}
      </div>
    </Modal>
  )
}
