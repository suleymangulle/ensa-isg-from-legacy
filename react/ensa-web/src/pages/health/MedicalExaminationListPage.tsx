import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { FITNESS_OPINION_BADGE, useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import {
  FITNESS_OPINIONS,
  HEALTH_ENDPOINTS,
  IBYS_STATUS_BADGE,
  IBYS_SUBMISSION_STATUSES,
  MEDICAL_REPORT_TYPES,
  useExpiringExaminations,
  useMedicalExaminationList,
  type MedicalExaminationFormListDto,
} from './api'
import { useDebouncedValue } from './components/ReferencePickers'
import MedicalExaminationFormModal from './MedicalExaminationFormModal'

/**
 * EK-2 medical examination forms.
 *
 * PRIVACY. The list DTO deliberately carries no clinical field, and the free-text filter is
 * matched against the submission envelope only. Nothing on this screen may be enriched with
 * diagnoses, complaints or findings — those exist solely on the single-record detail view.
 */

const PAGE_SIZE = 20

/** Today as `yyyy-MM-dd`, the default cut-off of the expiring query. */
function today(): string {
  return new Date().toISOString().slice(0, 10)
}

export default function MedicalExaminationListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)

  const [companyId, setCompanyId] = useState<number | ''>('')
  const [reportType, setReportType] = useState<number | ''>('')
  const [opinion, setOpinion] = useState<number | ''>('')
  const [ibysStatus, setIbysStatus] = useState<number | ''>('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<MedicalExaminationFormListDto | null>(null)

  const companies = useLookup('company')

  const { data, isLoading, error } = useMedicalExaminationList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'ExaminationDate DESC',
    filter: debouncedSearch,
    companyId: companyId === '' ? null : companyId,
    reportType: reportType === '' ? null : reportType,
    opinion: opinion === '' ? null : opinion,
    ibysStatus: ibysStatus === '' ? null : ibysStatus,
    examinationDateFrom: dateFrom || null,
    examinationDateTo: dateTo || null,
  })

  const remove = useDelete(HEALTH_ENDPOINTS.medicalExaminationForm, {
    onSuccess: () => setPendingDelete(null),
  })

  /** Any filter change restarts paging, otherwise page 3 of a new result set comes up empty. */
  function resetPaging<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const columns: Column<MedicalExaminationFormListDto>[] = [
    {
      key: 'employee',
      header: t('medicalExamination.fields.employee'),
      render: (form) => (
        <Link
          to={`/medical-examinations/${form.id}`}
          className="fw-semibold text-decoration-none"
        >
          {form.employeeFullName ?? t('common.none')}
        </Link>
      ),
    },
    {
      key: 'companyName',
      header: t('medicalExamination.fields.companyName'),
      render: (form) => form.companyName ?? t('common.none'),
    },
    {
      key: 'reportType',
      header: t('medicalExamination.fields.reportType'),
      render: (form) => t(`enums.medicalReportType.${form.reportType}`),
    },
    {
      key: 'examinationDate',
      header: t('medicalExamination.fields.examinationDate'),
      render: (form) => formatDate(form.examinationDate) ?? t('common.none'),
    },
    {
      key: 'validityDate',
      header: t('medicalExamination.fields.validityDate'),
      render: (form) => formatDate(form.validityDate) ?? t('common.none'),
    },
    {
      key: 'physician',
      header: t('medicalExamination.fields.physician'),
      render: (form) => form.physicianFullName ?? t('common.none'),
    },
    {
      key: 'opinion',
      header: t('medicalExamination.fields.opinion'),
      align: 'center',
      render: (form) => (
        <span className={FITNESS_OPINION_BADGE[form.opinion]}>
          {t(`enums.fitnessForWorkOpinion.${form.opinion}`)}
        </span>
      ),
    },
    {
      key: 'ibysStatus',
      header: t('medicalExamination.fields.ibysStatus'),
      align: 'center',
      render: (form) => (
        <span className={IBYS_STATUS_BADGE[form.ibysStatus]}>
          {t(`enums.ibysSubmissionStatus.${form.ibysStatus}`)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (form) => (
        <div className="d-flex justify-content-end gap-1">
          <Link
            to={`/medical-examinations/${form.id}`}
            className="btn btn-sm btn-icon btn-light"
            aria-label={t('medicalExamination.list.openDetail', {
              name: form.employeeFullName ?? '',
            })}
          >
            <span aria-hidden="true">→</span>
          </Link>
          <button
            type="button"
            className="btn btn-sm btn-icon btn-light-danger"
            aria-label={t('medicalExamination.list.deleteAction', {
              name: form.employeeFullName ?? '',
            })}
            onClick={() => setPendingDelete(form)}
          >
            <span aria-hidden="true">✕</span>
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('medicalExamination.list.title')}
        description={t('medicalExamination.list.description')}
        action={
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => setIsCreateOpen(true)}
          >
            {t('medicalExamination.list.create')}
          </button>
        }
      />

      <ExpiringPanel companyId={companyId === '' ? undefined : companyId} />

      <div className="card">
        <div className="card-header d-block py-4">
          <SearchBar
            value={search}
            onChange={resetPaging(setSearch)}
            placeholder={t('medicalExamination.list.searchPlaceholder')}
          >
            <div className="d-flex flex-wrap gap-2">
              <FilterSelect
                id="filter-company"
                label={t('medicalExamination.filters.company')}
                value={companyId}
                onChange={resetPaging(setCompanyId)}
                options={(companies.data?.items ?? []).map((item) => ({
                  value: item.id,
                  label: item.displayName,
                }))}
              />
              <FilterSelect
                id="filter-report-type"
                label={t('medicalExamination.filters.reportType')}
                value={reportType}
                onChange={resetPaging(setReportType)}
                options={MEDICAL_REPORT_TYPES.map((type) => ({
                  value: type,
                  label: t(`enums.medicalReportType.${type}`),
                }))}
              />
              <FilterSelect
                id="filter-opinion"
                label={t('medicalExamination.filters.opinion')}
                value={opinion}
                onChange={resetPaging(setOpinion)}
                options={FITNESS_OPINIONS.map((item) => ({
                  value: item,
                  label: t(`enums.fitnessForWorkOpinion.${item}`),
                }))}
              />
              <FilterSelect
                id="filter-ibys-status"
                label={t('medicalExamination.filters.ibysStatus')}
                value={ibysStatus}
                onChange={resetPaging(setIbysStatus)}
                options={IBYS_SUBMISSION_STATUSES.map((item) => ({
                  value: item,
                  label: t(`enums.ibysSubmissionStatus.${item}`),
                }))}
              />
              <FilterDate
                id="filter-date-from"
                label={t('medicalExamination.filters.dateFrom')}
                value={dateFrom}
                onChange={resetPaging(setDateFrom)}
              />
              <FilterDate
                id="filter-date-to"
                label={t('medicalExamination.filters.dateTo')}
                value={dateTo}
                onChange={resetPaging(setDateTo)}
              />
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('medicalExamination.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(form) => form.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('medicalExamination.list.empty')}
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

      {isCreateOpen && (
        <MedicalExaminationFormModal
          isOpen={isCreateOpen}
          onClose={() => setIsCreateOpen(false)}
        />
      )}

      <ConfirmDialog
        isOpen={pendingDelete != null}
        title={t('medicalExamination.list.deleteTitle')}
        message={t('medicalExamination.list.deleteMessage', {
          name: pendingDelete?.employeeFullName ?? '',
        })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => {
          remove.reset()
          setPendingDelete(null)
        }}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}

/**
 * Examinations of the selected workplace whose validity has already lapsed. The endpoint is
 * scoped to one company, so the panel appears once a workplace has been picked — which is also
 * the question the screen is usually opened to answer.
 */
function ExpiringPanel({ companyId }: { companyId: number | undefined }) {
  const { t } = useTranslation()
  const { data, isLoading, error } = useExpiringExaminations(companyId, today())

  if (!companyId) {
    return (
      <div
        className="alert border-0 mb-4"
        style={{ backgroundColor: 'var(--kt-light)', color: 'var(--kt-gray-600)' }}
        role="status"
      >
        {t('medicalExamination.expiring.selectCompany')}
      </div>
    )
  }

  if (isLoading || error) {
    return (
      <div
        className="alert border-0 mb-4"
        style={
          error
            ? { backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }
            : { backgroundColor: 'var(--kt-light)', color: 'var(--kt-gray-600)' }
        }
        role={error ? 'alert' : 'status'}
      >
        {error ? errorMessage(error) : t('common.loading')}
      </div>
    )
  }

  if (!data?.items.length) {
    return (
      <div
        className="alert border-0 mb-4"
        style={{ backgroundColor: 'var(--kt-success-light)', color: 'var(--kt-success)' }}
        role="status"
      >
        {t('medicalExamination.expiring.none')}
      </div>
    )
  }

  return (
    <div className="card mb-4">
      <div className="card-header">
        <h2 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-danger)' }}>
          {t('medicalExamination.expiring.title', { count: data.items.length })}
        </h2>
      </div>
      <div className="card-body">
        <ul className="list-unstyled mb-0 d-flex flex-wrap gap-2">
          {data.items.map((form) => (
            <li key={form.id}>
              <Link
                to={`/medical-examinations/${form.id}`}
                className="badge-light-danger text-decoration-none"
              >
                {form.employeeFullName ?? t('common.none')}
                {' · '}
                {formatDate(form.validityDate) ?? t('common.none')}
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

/** Compact labelled drop-down used in the filter bar. */
function FilterSelect({
  id,
  label,
  value,
  onChange,
  options,
}: {
  id: string
  label: string
  value: number | ''
  onChange: (next: number | '') => void
  options: { value: number; label: string }[]
}) {
  const { t } = useTranslation()

  return (
    <div>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <select
        id={id}
        className="form-select form-select-sm"
        style={{ minWidth: 170 }}
        value={value}
        onChange={(event) => onChange(event.target.value === '' ? '' : Number(event.target.value))}
      >
        <option value="">{t('common.all')} — {label}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </div>
  )
}

/** Compact labelled date input used in the filter bar. */
function FilterDate({
  id,
  label,
  value,
  onChange,
}: {
  id: string
  label: string
  value: string
  onChange: (next: string) => void
}) {
  return (
    <div>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <input
        id={id}
        type="date"
        className="form-control form-control-sm"
        style={{ minWidth: 150 }}
        title={label}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  )
}
