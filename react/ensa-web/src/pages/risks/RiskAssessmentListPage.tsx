import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
import { HAZARD_CLASS_BADGE, HazardClass, RiskAssessmentMethod, useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useCreate, useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import {
  APPROVAL_STATUS_BADGE,
  COMPANY,
  RISK_ASSESSMENT_REPORT,
  useExpiringRiskAssessments,
  useRiskAssessmentList,
  type CreateRiskAssessmentReportDto,
  type RiskAssessmentReportListDto,
} from './api'
import {
  SELECTABLE_HAZARD_CLASSES,
  SELECTABLE_METHODS,
  fromDateInput,
  todayInput,
} from './helpers'

const PAGE_SIZE = 20

/** Reports whose validity ends inside this window are surfaced in the warning panel. */
const EXPIRY_WARNING_DAYS = 90

export default function RiskAssessmentListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [hazardClass, setHazardClass] = useState<HazardClass | ''>('')
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<RiskAssessmentReportListDto | null>(null)

  const { data, isLoading, error } = useRiskAssessmentList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'PerformedDate DESC',
    filter: search || undefined,
    hazardClass: hazardClass === '' ? undefined : hazardClass,
  })

  const remove = useDelete(RISK_ASSESSMENT_REPORT, {
    onSuccess: () => setPendingDelete(null),
  })

  /** Badge reflecting how much of the report validity period is left. */
  function validityBadge(report: RiskAssessmentReportListDto): ReactNode {
    if (report.isExpired) {
      return <span className="badge-light-danger">{t('riskAssessment.validity.expired')}</span>
    }
    if (report.remainingDays <= EXPIRY_WARNING_DAYS) {
      return (
        <span className="badge-light-warning">
          {t('riskAssessment.validity.expiring', { count: report.remainingDays })}
        </span>
      )
    }
    return <span className="badge-light-success">{t('riskAssessment.validity.valid')}</span>
  }

  const columns: Column<RiskAssessmentReportListDto>[] = [
    {
      key: 'reportName',
      header: t('riskAssessment.fields.reportName'),
      render: (report) => (
        <Link to={`/risk-assessments/${report.id}`} className="fw-semibold text-decoration-none">
          {report.reportName}
        </Link>
      ),
    },
    {
      key: 'companyName',
      header: t('riskAssessment.fields.companyName'),
      render: (report) => report.companyName ?? t('common.none'),
    },
    {
      key: 'hazardClass',
      header: t('riskAssessment.fields.hazardClass'),
      render: (report) => (
        <span className={HAZARD_CLASS_BADGE[report.hazardClass]}>
          {t(`enums.hazardClass.${report.hazardClass}`)}
        </span>
      ),
    },
    {
      key: 'method',
      header: t('riskAssessment.fields.method'),
      render: (report) => t(`enums.riskAssessmentMethod.${report.reportMethod}`),
    },
    {
      key: 'approvalStatus',
      header: t('riskAssessment.fields.approvalStatus'),
      render: (report) => (
        <span className={APPROVAL_STATUS_BADGE[report.approvalStatus]}>
          {t(`enums.approvalStatus.${report.approvalStatus}`)}
        </span>
      ),
    },
    {
      key: 'workerCount',
      header: t('riskAssessment.fields.workerCount'),
      align: 'end',
      render: (report) => report.workerCount,
    },
    {
      key: 'performedDate',
      header: t('riskAssessment.fields.performedDate'),
      render: (report) => formatDate(report.performedDate) ?? t('common.none'),
    },
    {
      key: 'validity',
      header: t('riskAssessment.fields.validity'),
      align: 'center',
      render: (report) => validityBadge(report),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (report) => (
        <div className="d-flex justify-content-end gap-2">
          <Link
            to={`/risk-assessments/${report.id}`}
            className="btn btn-sm btn-light-primary"
            aria-label={t('riskAssessment.list.openDetail', { name: report.reportName })}
          >
            {t('common.detail')}
          </Link>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setPendingDelete(report)}
            aria-label={t('riskAssessment.list.deleteReport', { name: report.reportName })}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('riskAssessment.list.title')}
        description={t('riskAssessment.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setCreateOpen(true)}>
            {t('riskAssessment.list.create')}
          </button>
        }
      />

      <ExpiringPanel />

      <div className="card">
        <div className="card-body">
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('riskAssessment.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="hazardClassFilter" className="visually-hidden">
                {t('riskAssessment.fields.hazardClass')}
              </label>
              <select
                id="hazardClassFilter"
                className="form-select"
                style={{ minWidth: 200 }}
                value={hazardClass}
                onChange={(event) => {
                  const value = event.target.value
                  setHazardClass(value === '' ? '' : (Number(value) as HazardClass))
                  setPage(1)
                }}
              >
                <option value="">{t('riskAssessment.list.allHazardClasses')}</option>
                {SELECTABLE_HAZARD_CLASSES.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.hazardClass.${value}`)}
                  </option>
                ))}
              </select>
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('riskAssessment.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(report) => report.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('riskAssessment.list.empty')}
          />
        </div>

        {data && data.totalCount > 0 && (
          <div className="card-footer bg-transparent border-0 pt-0">
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          </div>
        )}
      </div>

      <CreateReportModal isOpen={isCreateOpen} onClose={() => setCreateOpen(false)} />

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('riskAssessment.list.deleteTitle')}
        message={t('riskAssessment.list.deleteMessage', { name: pendingDelete?.reportName ?? '' })}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/**
 * Reports that already expired or expire soon.
 *
 * Fetched once from `GET api/risk-assessment-report/expiring`; the panel disappears entirely
 * when there is nothing to warn about, so a healthy list stays quiet.
 */
function ExpiringPanel() {
  const { t } = useTranslation()
  const { data } = useExpiringRiskAssessments(EXPIRY_WARNING_DAYS)

  const items = data?.items ?? []
  if (!items.length) return null

  return (
    <div className="card mb-4" style={{ borderLeft: '4px solid var(--kt-warning)' }}>
      <div className="card-body">
        <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
          {t('riskAssessment.expiring.title', { count: items.length })}
        </h2>
        <p className="mb-3" style={{ color: 'var(--kt-gray-600)' }}>
          {t('riskAssessment.expiring.description', { days: EXPIRY_WARNING_DAYS })}
        </p>
        <ul className="list-unstyled mb-0 d-flex flex-column gap-2">
          {items.map((report) => (
            <li key={report.id} className="d-flex flex-wrap align-items-center gap-2">
              <Link to={`/risk-assessments/${report.id}`} className="fw-semibold text-decoration-none">
                {report.reportName}
              </Link>
              <span style={{ color: 'var(--kt-gray-500)' }}>{report.companyName}</span>
              <span className={report.isExpired ? 'badge-light-danger' : 'badge-light-warning'}>
                {report.isExpired
                  ? t('riskAssessment.validity.expired')
                  : t('riskAssessment.validity.expiring', { count: report.remainingDays })}
              </span>
              <span style={{ color: 'var(--kt-gray-500)' }}>
                {formatDate(report.validityDate) ?? t('common.none')}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

/** Blank create form; only the fields the domain requires to compute a validity date. */
function emptyReport(): CreateRiskAssessmentReportDto {
  return {
    reportName: '',
    companyId: 0,
    workplaceTitle: '',
    businessActivity: '',
    workplaceAddress: '',
    workplaceTelefonu: '',
    hazardClass: HazardClass.LowHazard,
    performedDate: todayInput(),
    revisionDate: null,
    employer: null,
    specialistFullName: null,
    physicianFullName: null,
    workerCount: 0,
    reportMethod: RiskAssessmentMethod.FineKinney,
  }
}

function CreateReportModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const [form, setForm] = useState<CreateRiskAssessmentReportDto>(emptyReport)
  const [validation, setValidation] = useState<Record<string, string>>({})

  const companies = useLookup(COMPANY)
  const create = useCreate<CreateRiskAssessmentReportDto>(RISK_ASSESSMENT_REPORT, {
    onSuccess: () => {
      setForm(emptyReport())
      setValidation({})
      onClose()
    },
  })

  function patch(changes: Partial<CreateRiskAssessmentReportDto>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function submit() {
    const errors: Record<string, string> = {}
    if (!form.reportName.trim()) errors.reportName = t('validation.required')
    if (!form.companyId) errors.companyId = t('validation.required')
    if (!form.performedDate) errors.performedDate = t('validation.required')
    setValidation(errors)
    if (Object.keys(errors).length) return

    create.mutate({
      ...form,
      revisionDate: fromDateInput(form.revisionDate ?? ''),
      employer: form.employer || null,
      specialistFullName: form.specialistFullName || null,
      physicianFullName: form.physicianFullName || null,
    })
  }

  return (
    <Modal
      title={t('riskAssessment.create.title')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={create.isPending}
      error={create.error ? errorMessage(create.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field
          label={t('riskAssessment.fields.reportName')}
          htmlFor="reportName"
          required
          error={validation.reportName}
          className="col-md-6"
        >
          <input
            id="reportName"
            className={controlClass('form-control', validation.reportName)}
            value={form.reportName}
            onChange={(event) => patch({ reportName: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.companyName')}
          htmlFor="companyId"
          required
          error={validation.companyId}
          className="col-md-6"
        >
          <select
            id="companyId"
            className={controlClass('form-select', validation.companyId)}
            value={form.companyId || ''}
            onChange={(event) => patch({ companyId: Number(event.target.value) })}
          >
            <option value="">{t('riskAssessment.create.selectCompany')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('riskAssessment.fields.hazardClass')}
          htmlFor="hazardClass"
          required
          hint={t('riskAssessment.create.hazardClassHint')}
          className="col-md-6"
        >
          <select
            id="hazardClass"
            className="form-select"
            value={form.hazardClass}
            onChange={(event) => patch({ hazardClass: Number(event.target.value) as HazardClass })}
          >
            {SELECTABLE_HAZARD_CLASSES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.hazardClass.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('riskAssessment.fields.method')}
          htmlFor="reportMethod"
          required
          className="col-md-6"
        >
          <select
            id="reportMethod"
            className="form-select"
            value={form.reportMethod}
            onChange={(event) =>
              patch({ reportMethod: Number(event.target.value) as RiskAssessmentMethod })
            }
          >
            {SELECTABLE_METHODS.map((value) => (
              <option key={value} value={value}>
                {t(`enums.riskAssessmentMethod.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('riskAssessment.fields.performedDate')}
          htmlFor="performedDate"
          required
          error={validation.performedDate}
          className="col-md-4"
        >
          <input
            id="performedDate"
            type="date"
            className={controlClass('form-control', validation.performedDate)}
            value={form.performedDate}
            onChange={(event) => patch({ performedDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.revisionDate')}
          htmlFor="revisionDate"
          className="col-md-4"
        >
          <input
            id="revisionDate"
            type="date"
            className="form-control"
            value={form.revisionDate ?? ''}
            onChange={(event) => patch({ revisionDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workerCount')}
          htmlFor="workerCount"
          className="col-md-4"
        >
          <input
            id="workerCount"
            type="number"
            min={0}
            className="form-control"
            value={form.workerCount}
            onChange={(event) => patch({ workerCount: Number(event.target.value) })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workplaceTitle')}
          htmlFor="workplaceTitle"
          className="col-md-6"
        >
          <input
            id="workplaceTitle"
            className="form-control"
            value={form.workplaceTitle}
            onChange={(event) => patch({ workplaceTitle: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.businessActivity')}
          htmlFor="businessActivity"
          className="col-md-6"
        >
          <input
            id="businessActivity"
            className="form-control"
            value={form.businessActivity}
            onChange={(event) => patch({ businessActivity: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workplaceAddress')}
          htmlFor="workplaceAddress"
          className="col-md-8"
        >
          <input
            id="workplaceAddress"
            className="form-control"
            value={form.workplaceAddress}
            onChange={(event) => patch({ workplaceAddress: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.workplacePhone')}
          htmlFor="workplaceTelefonu"
          className="col-md-4"
        >
          <input
            id="workplaceTelefonu"
            className="form-control"
            value={form.workplaceTelefonu}
            onChange={(event) => patch({ workplaceTelefonu: event.target.value })}
          />
        </Field>

        <Field label={t('riskAssessment.fields.employer')} htmlFor="employer" className="col-md-4">
          <input
            id="employer"
            className="form-control"
            value={form.employer ?? ''}
            onChange={(event) => patch({ employer: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.specialist')}
          htmlFor="specialistFullName"
          className="col-md-4"
        >
          <input
            id="specialistFullName"
            className="form-control"
            value={form.specialistFullName ?? ''}
            onChange={(event) => patch({ specialistFullName: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.fields.physician')}
          htmlFor="physicianFullName"
          className="col-md-4"
        >
          <input
            id="physicianFullName"
            className="form-control"
            value={form.physicianFullName ?? ''}
            onChange={(event) => patch({ physicianFullName: event.target.value })}
          />
        </Field>
      </div>
    </Modal>
  )
}
