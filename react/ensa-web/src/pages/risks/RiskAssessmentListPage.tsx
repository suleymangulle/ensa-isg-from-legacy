import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Input, NumberInput, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
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
      return <Badge variant="danger">{t('riskAssessment.validity.expired')}</Badge>
    }
    if (report.remainingDays <= EXPIRY_WARNING_DAYS) {
      return (
        <Badge variant="warning">
          {t('riskAssessment.validity.expiring', { count: report.remainingDays })}
        </Badge>
      )
    }
    return <Badge variant="success">{t('riskAssessment.validity.valid')}</Badge>
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
        <Badge variant={HAZARD_CLASS_BADGE[report.hazardClass]}>
          {t(`enums.hazardClass.${report.hazardClass}`)}
        </Badge>
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
        <Badge variant={APPROVAL_STATUS_BADGE[report.approvalStatus]}>
          {t(`enums.approvalStatus.${report.approvalStatus}`)}
        </Badge>
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
            className="btn btn-sm"
            aria-label={t('riskAssessment.list.openDetail', { name: report.reportName })}
          >
            {t('common.detail')}
          </Link>
          <Button variant="light" size="sm" 
            onClick={() => setPendingDelete(report)}
            aria-label={t('riskAssessment.list.deleteReport', { name: report.reportName })}
          >
            {t('common.delete')}
          </Button>
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
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('riskAssessment.list.create')}
          </Button>
        }
      />

      <ExpiringPanel />

      <Card
        
        header={
          <SearchBar
            value={search}
            onChange={(next) => {
              setSearch(next)
              setPage(1)
            }}
            placeholder={t('riskAssessment.list.searchPlaceholder')}
          >
            <div style={{ minWidth: 200 }}>
              <label htmlFor="hazardClassFilter" className="visually-hidden">
                {t('riskAssessment.fields.hazardClass')}
              </label>
              <Select
                id="hazardClassFilter"
                placeholder={t('riskAssessment.list.allHazardClasses')}
                options={SELECTABLE_HAZARD_CLASSES.map((value) => ({
                  value,
                  label: t(`enums.hazardClass.${value}`),
                }))}
                value={hazardClass === '' ? null : hazardClass}
                onChange={(value) => {
                  setHazardClass(value ?? '')
                  setPage(1)
                }}
              />
            </div>
          </SearchBar>
        }
        footer={
          data && data.totalCount > 0 ? (
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          ) : undefined
        }
      >
        <DataTable
          label={t('riskAssessment.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(report) => report.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('riskAssessment.list.empty')}
        />
      </Card>

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
    <Card className="mb-4 -warning">
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
            <Badge variant={report.isExpired ? 'danger' : 'warning'}>
              {report.isExpired
                ? t('riskAssessment.validity.expired')
                : t('riskAssessment.validity.expiring', { count: report.remainingDays })}
            </Badge>
            <span style={{ color: 'var(--kt-gray-500)' }}>
              {formatDate(report.validityDate) ?? t('common.none')}
            </span>
          </li>
        ))}
      </ul>
    </Card>
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
        <Input
          id="reportName"
          label={t('riskAssessment.fields.reportName')}
          required
          error={validation.reportName}
          className="col-md-6"
          value={form.reportName}
          onChange={(value) => patch({ reportName: value })}
        />

        <Select
          id="companyId"
          label={t('riskAssessment.fields.companyName')}
          required
          error={validation.companyId}
          className="col-md-6"
          placeholder={t('riskAssessment.create.selectCompany')}
          options={
            companies.data?.items.map((company) => ({
              value: company.id,
              label: company.displayName,
            })) ?? []
          }
          value={form.companyId || null}
          onChange={(value) => patch({ companyId: value ?? 0 })}
        />

        <Select
          id="hazardClass"
          label={t('riskAssessment.fields.hazardClass')}
          required
          helpText={t('riskAssessment.create.hazardClassHint')}
          className="col-md-6"
          options={SELECTABLE_HAZARD_CLASSES.map((value) => ({
            value,
            label: t(`enums.hazardClass.${value}`),
          }))}
          value={form.hazardClass}
          onChange={(value) => patch({ hazardClass: (value ?? HazardClass.LowHazard) as HazardClass })}
        />

        <Select
          id="reportMethod"
          label={t('riskAssessment.fields.method')}
          required
          className="col-md-6"
          options={SELECTABLE_METHODS.map((value) => ({
            value,
            label: t(`enums.riskAssessmentMethod.${value}`),
          }))}
          value={form.reportMethod}
          onChange={(value) =>
            patch({ reportMethod: (value ?? RiskAssessmentMethod.FineKinney) as RiskAssessmentMethod })
          }
        />

        <Input
          id="performedDate"
          label={t('riskAssessment.fields.performedDate')}
          required
          error={validation.performedDate}
          className="col-md-4"
          inputProps={{ type: 'date' }}
          value={form.performedDate}
          onChange={(value) => patch({ performedDate: value })}
        />

        <Input
          id="revisionDate"
          label={t('riskAssessment.fields.revisionDate')}
          className="col-md-4"
          inputProps={{ type: 'date' }}
          value={form.revisionDate ?? ''}
          onChange={(value) => patch({ revisionDate: value })}
        />

        <NumberInput
          id="workerCount"
          label={t('riskAssessment.fields.workerCount')}
          className="col-md-4"
          min={0}
          value={form.workerCount}
          onChange={(value) => patch({ workerCount: value ?? 0 })}
        />

        <Input
          id="workplaceTitle"
          label={t('riskAssessment.fields.workplaceTitle')}
          className="col-md-6"
          value={form.workplaceTitle}
          onChange={(value) => patch({ workplaceTitle: value })}
        />

        <Input
          id="businessActivity"
          label={t('riskAssessment.fields.businessActivity')}
          className="col-md-6"
          value={form.businessActivity}
          onChange={(value) => patch({ businessActivity: value })}
        />

        <Input
          id="workplaceAddress"
          label={t('riskAssessment.fields.workplaceAddress')}
          className="col-md-8"
          value={form.workplaceAddress}
          onChange={(value) => patch({ workplaceAddress: value })}
        />

        <Input
          id="workplaceTelefonu"
          label={t('riskAssessment.fields.workplacePhone')}
          className="col-md-4"
          value={form.workplaceTelefonu}
          onChange={(value) => patch({ workplaceTelefonu: value })}
        />

        <Input
          id="employer"
          label={t('riskAssessment.fields.employer')}
          className="col-md-4"
          value={form.employer ?? ''}
          onChange={(value) => patch({ employer: value })}
        />

        <Input
          id="specialistFullName"
          label={t('riskAssessment.fields.specialist')}
          className="col-md-4"
          value={form.specialistFullName ?? ''}
          onChange={(value) => patch({ specialistFullName: value })}
        />

        <Input
          id="physicianFullName"
          label={t('riskAssessment.fields.physician')}
          className="col-md-4"
          value={form.physicianFullName ?? ''}
          onChange={(value) => patch({ physicianFullName: value })}
        />
      </div>
    </Modal>
  )
}
