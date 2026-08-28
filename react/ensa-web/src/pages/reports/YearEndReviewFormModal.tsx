import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Switch } from 'rich-react-component'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
import {
  REPORT_ENDPOINTS,
  useCompanyLookup,
  useUserLookup,
  type SaveYearEndReviewReportDto,
  type YearEndReviewReportDto,
} from './api'
import { LookupField, TextField, toDateInput } from './components'

interface FormState {
  reportTitle: string
  companyId?: number
  reportDate: string
  maleWorker: string
  femaleWorker: string
  childWorker: string
  youngWorker: string
  specialistUserId?: number
  specialistFullName: string
  physicianUserId?: number
  physicianFullName: string
  deputyFullName: string
  isActive: boolean
}

function initialState(report?: YearEndReviewReportDto): FormState {
  return {
    reportTitle: report?.reportTitle ?? '',
    companyId: report?.companyId,
    reportDate: toDateInput(report?.reportDate),
    maleWorker: numberInput(report?.maleWorker),
    femaleWorker: numberInput(report?.femaleWorker),
    childWorker: numberInput(report?.childWorker),
    youngWorker: numberInput(report?.youngWorker),
    specialistUserId: report?.specialistUserId ?? undefined,
    specialistFullName: report?.specialistFullName ?? '',
    physicianUserId: report?.physicianUserId ?? undefined,
    physicianFullName: report?.physicianFullName ?? '',
    deputyFullName: report?.deputyFullName ?? '',
    isActive: report?.isActive ?? true,
  }
}

function numberInput(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value)
}

function numberPayload(value: string): number | null {
  return value.trim() === '' ? null : Number(value)
}

/**
 * Create / edit dialog of a year-end review report header.
 *
 * The specialist and physician names are stored on the report itself so that the document keeps
 * reading correctly after the user record is gone; picking someone from the drop-down therefore
 * fills the name field too, and the name stays editable.
 */
export default function YearEndReviewFormModal({
  report,
  onClose,
}: {
  /** Absent for a create. */
  report?: YearEndReviewReportDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(report))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const companies = useCompanyLookup()
  const users = useUserLookup()

  const create = useCreate<SaveYearEndReviewReportDto, YearEndReviewReportDto>(
    REPORT_ENDPOINTS.yearEndReviewReport,
    { onSuccess: onClose },
  )
  const update = useUpdate<SaveYearEndReviewReportDto, YearEndReviewReportDto>(
    REPORT_ENDPOINTS.yearEndReviewReport,
    { onSuccess: onClose },
  )

  const pending = create.isPending || update.isPending
  const failure = create.error ?? update.error

  function patch(next: Partial<FormState>) {
    setForm((current) => ({ ...current, ...next }))
  }

  function userName(id: number | undefined) {
    return users.data?.items.find((user) => user.id === id)?.displayName ?? ''
  }

  function submit() {
    const found: Record<string, string> = {}
    if (!form.reportTitle.trim()) found.reportTitle = t('validation.required')
    if (!form.companyId) found.companyId = t('validation.required')
    if (!form.reportDate) found.reportDate = t('validation.required')
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveYearEndReviewReportDto = {
      reportTitle: form.reportTitle.trim(),
      companyId: form.companyId!,
      reportDate: form.reportDate,
      maleWorker: numberPayload(form.maleWorker),
      femaleWorker: numberPayload(form.femaleWorker),
      childWorker: numberPayload(form.childWorker),
      youngWorker: numberPayload(form.youngWorker),
      specialistUserId: form.specialistUserId ?? null,
      specialistFullName: form.specialistFullName || null,
      physicianUserId: form.physicianUserId ?? null,
      physicianFullName: form.physicianFullName || null,
      deputyFullName: form.deputyFullName || null,
    }

    if (report) {
      update.mutate({ id: report.id, input: { ...payload, isActive: form.isActive } })
    } else {
      create.mutate(payload)
    }
  }

  return (
    <Modal
      title={report ? t('reports.yearEnd.form.editTitle') : t('reports.yearEnd.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="xl"
    >
      <div className="row g-3">
        <TextField
          id="year-end-title"
          className="col-md-8"
          label={t('reports.yearEnd.fields.reportTitle')}
          required
          error={errors.reportTitle}
          value={form.reportTitle}
          onChange={(next) => patch({ reportTitle: next })}
        />

        <TextField
          id="year-end-date"
          className="col-md-4"
          type="date"
          label={t('reports.yearEnd.fields.reportDate')}
          required
          error={errors.reportDate}
          value={form.reportDate}
          onChange={(next) => patch({ reportDate: next })}
        />

        <LookupField
          id="year-end-company"
          className="col-md-6"
          label={t('reports.yearEnd.fields.company')}
          placeholder={t('reports.common.selectCompany')}
          required
          error={errors.companyId}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          value={form.companyId}
          onChange={(next) => patch({ companyId: next })}
        />

        <TextField
          id="year-end-deputy"
          className="col-md-6"
          label={t('reports.yearEnd.fields.deputyFullName')}
          value={form.deputyFullName}
          onChange={(next) => patch({ deputyFullName: next })}
        />

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('reports.yearEnd.form.workforceSection')}
          </h3>
        </div>

        <TextField
          id="year-end-male"
          className="col-6 col-md-3"
          type="number"
          min={0}
          label={t('reports.yearEnd.fields.maleWorker')}
          value={form.maleWorker}
          onChange={(next) => patch({ maleWorker: next })}
        />
        <TextField
          id="year-end-female"
          className="col-6 col-md-3"
          type="number"
          min={0}
          label={t('reports.yearEnd.fields.femaleWorker')}
          value={form.femaleWorker}
          onChange={(next) => patch({ femaleWorker: next })}
        />
        <TextField
          id="year-end-child"
          className="col-6 col-md-3"
          type="number"
          min={0}
          label={t('reports.yearEnd.fields.childWorker')}
          value={form.childWorker}
          onChange={(next) => patch({ childWorker: next })}
        />
        <TextField
          id="year-end-young"
          className="col-6 col-md-3"
          type="number"
          min={0}
          label={t('reports.yearEnd.fields.youngWorker')}
          value={form.youngWorker}
          onChange={(next) => patch({ youngWorker: next })}
        />

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-0 mt-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('reports.yearEnd.form.signatorySection')}
          </h3>
        </div>

        <LookupField
          id="year-end-specialist"
          className="col-md-3"
          label={t('reports.yearEnd.fields.specialist')}
          placeholder={t('reports.common.selectUser')}
          items={users.data?.items}
          isLoading={users.isLoading}
          value={form.specialistUserId}
          onChange={(next) =>
            patch({
              specialistUserId: next,
              specialistFullName: next ? userName(next) : form.specialistFullName,
            })
          }
        />
        <TextField
          id="year-end-specialist-name"
          className="col-md-3"
          label={t('reports.yearEnd.fields.specialistFullName')}
          hint={t('reports.yearEnd.form.nameSnapshotHint')}
          value={form.specialistFullName}
          onChange={(next) => patch({ specialistFullName: next })}
        />

        <LookupField
          id="year-end-physician"
          className="col-md-3"
          label={t('reports.yearEnd.fields.physician')}
          placeholder={t('reports.common.selectUser')}
          items={users.data?.items}
          isLoading={users.isLoading}
          value={form.physicianUserId}
          onChange={(next) =>
            patch({
              physicianUserId: next,
              physicianFullName: next ? userName(next) : form.physicianFullName,
            })
          }
        />
        <TextField
          id="year-end-physician-name"
          className="col-md-3"
          label={t('reports.yearEnd.fields.physicianFullName')}
          hint={t('reports.yearEnd.form.nameSnapshotHint')}
          value={form.physicianFullName}
          onChange={(next) => patch({ physicianFullName: next })}
        />

        {report && (
          <div className="col-md-4 mb-3">
            <label htmlFor="year-end-active" className="form-label">
              {t('reports.yearEnd.fields.status')}
            </label>
            <Switch
              id="year-end-active"
              className="mt-2"
              checked={form.isActive}
              onChange={(checked) => patch({ isActive: checked })}
              label={form.isActive ? t('common.active') : t('common.passive')}
            />
          </div>
        )}
      </div>
    </Modal>
  )
}
