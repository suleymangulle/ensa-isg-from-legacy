import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ActivityReportType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useCreate, useUpdate } from '@/api/mutations'
import { Modal } from '@/components/Form'
import {
  REPORT_ENDPOINTS,
  useCompanyLookup,
  type ActivityReportDto,
  type SaveActivityReportDto,
} from './api'
import { EnumField, LookupField, TextField, enumValues, toDateInput } from './components'

interface FormState {
  companyId?: number
  reportType: ActivityReportType
  reportName: string
  reportStart: string
  reportEnd: string
}

function initialState(report?: ActivityReportDto): FormState {
  return {
    companyId: report?.companyId,
    reportType: report?.reportType ?? ActivityReportType.MonthlyActivityReport,
    reportName: report?.reportName ?? '',
    reportStart: toDateInput(report?.reportStart),
    reportEnd: toDateInput(report?.reportEnd),
  }
}

/**
 * Create / edit dialog of an activity report header.
 *
 * Mount it conditionally (`{isOpen && <ActivityReportFormModal … />}`) so the draft is discarded
 * when the dialog closes rather than surviving into the next opening.
 */
export default function ActivityReportFormModal({
  report,
  onClose,
}: {
  /** Absent for a create. */
  report?: ActivityReportDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<FormState>(() => initialState(report))
  const [errors, setErrors] = useState<Record<string, string>>({})

  const companies = useCompanyLookup()

  const create = useCreate<SaveActivityReportDto, ActivityReportDto>(
    REPORT_ENDPOINTS.activityReport,
    { onSuccess: onClose },
  )
  const update = useUpdate<SaveActivityReportDto, ActivityReportDto>(
    REPORT_ENDPOINTS.activityReport,
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
    if (!form.reportName.trim()) found.reportName = t('validation.required')
    if (!form.reportStart) found.reportStart = t('validation.required')
    if (!form.reportEnd) found.reportEnd = t('validation.required')
    if (form.reportStart && form.reportEnd && form.reportEnd < form.reportStart) {
      found.reportEnd = t('reports.validation.endBeforeStart')
    }
    setErrors(found)
    if (Object.keys(found).length) return

    const payload: SaveActivityReportDto = {
      companyId: form.companyId!,
      reportType: form.reportType,
      reportName: form.reportName.trim(),
      reportStart: form.reportStart,
      reportEnd: form.reportEnd,
    }

    if (report) update.mutate({ id: report.id, input: payload })
    else create.mutate(payload)
  }

  return (
    <Modal
      title={report ? t('reports.activity.form.editTitle') : t('reports.activity.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <div className="row g-3">
        <LookupField
          id="activity-report-company"
          className="col-md-6"
          label={t('reports.activity.fields.company')}
          placeholder={t('reports.common.selectCompany')}
          required
          error={errors.companyId}
          items={companies.data?.items}
          isLoading={companies.isLoading}
          value={form.companyId}
          onChange={(next) => patch({ companyId: next })}
        />

        <EnumField
          id="activity-report-type"
          className="col-md-6"
          label={t('reports.activity.fields.reportType')}
          values={enumValues(ActivityReportType)}
          translationPrefix="enums.activityReportType"
          required
          value={form.reportType}
          onChange={(next) =>
            patch({ reportType: (next ?? ActivityReportType.Unspecified) as ActivityReportType })
          }
        />

        <TextField
          id="activity-report-name"
          className="col-12"
          label={t('reports.activity.fields.reportName')}
          required
          error={errors.reportName}
          value={form.reportName}
          onChange={(next) => patch({ reportName: next })}
        />

        <TextField
          id="activity-report-start"
          className="col-md-6"
          type="date"
          label={t('reports.activity.fields.reportStart')}
          required
          error={errors.reportStart}
          value={form.reportStart}
          onChange={(next) => patch({ reportStart: next })}
        />

        <TextField
          id="activity-report-end"
          className="col-md-6"
          type="date"
          label={t('reports.activity.fields.reportEnd')}
          required
          error={errors.reportEnd}
          value={form.reportEnd}
          onChange={(next) => patch({ reportEnd: next })}
        />
      </div>
    </Modal>
  )
}
