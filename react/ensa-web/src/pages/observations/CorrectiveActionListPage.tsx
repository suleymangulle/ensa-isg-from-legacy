import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { PageTitle, Pagination, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { CorrectiveActionStatus, RiskCategory } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import CorrectiveActionFormModal from './CorrectiveActionFormModal'
import {
  OBSERVATION_ENDPOINTS,
  useCompanyLookup,
  useCorrectiveActionDetail,
  useCorrectiveActionList,
  useOverdueCorrectiveActions,
  type CorrectiveActionListDto,
} from './api'
import {
  AlertPanel,
  CORRECTIVE_ACTION_STATUS_BADGE,
  FilterSelect,
  RISK_CATEGORY_BADGE,
  RowActions,
  enumValues,
} from './components'

const PAGE_SIZE = 20

export default function CorrectiveActionListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [status, setStatus] = useState('')
  const [riskCategory, setRiskCategory] = useState('')
  const [onlyOverdue, setOnlyOverdue] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<CorrectiveActionListDto | null>(null)

  const companies = useCompanyLookup()

  const { data, isLoading, error } = useCorrectiveActionList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'DeadlineDate ASC',
    filter: search,
    companyId: companyId ? Number(companyId) : undefined,
    operationResult: status ? (Number(status) as CorrectiveActionStatus) : undefined,
    riskCategory: riskCategory ? (Number(riskCategory) as RiskCategory) : undefined,
    onlyOverdue: onlyOverdue || undefined,
  })

  // One request for the banner; `GET api/corrective-action/overdue` already returns the whole set.
  const overdue = useOverdueCorrectiveActions(companyId ? Number(companyId) : undefined)

  const editing = useCorrectiveActionDetail(editingId ?? undefined)

  const remove = useDelete(OBSERVATION_ENDPOINTS.correctiveAction, {
    onSuccess: () => setDeleting(null),
  })

  function resetPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const overdueCount = overdue.data?.items.length ?? 0

  const columns: Column<CorrectiveActionListDto>[] = [
    {
      key: 'accent',
      header: '',
      width: '6px',
      render: (row) => (
        <span
          aria-hidden="true"
          className="d-block rounded"
          style={{
            width: 4,
            height: 34,
            backgroundColor: row.isOverdue ? 'var(--kt-danger)' : 'transparent',
          }}
        />
      ),
    },
    {
      key: 'finding',
      header: t('correctiveAction.fields.finding'),
      render: (row) => (
        <Link to={`/corrective-actions/${row.id}`} className="fw-semibold text-decoration-none">
          {row.finding}
        </Link>
      ),
    },
    {
      key: 'company',
      header: t('correctiveAction.fields.company'),
      render: (row) => row.companyName ?? t('common.none'),
    },
    {
      key: 'owner',
      header: t('correctiveAction.fields.owner'),
      render: (row) => row.owner ?? t('common.none'),
    },
    {
      key: 'riskCategory',
      header: t('correctiveAction.fields.riskCategory'),
      render: (row) => (
        <span className={RISK_CATEGORY_BADGE[row.riskCategory]}>
          {t(`enums.riskCategory.${row.riskCategory}`)}
        </span>
      ),
    },
    {
      key: 'findingDate',
      header: t('correctiveAction.fields.findingDate'),
      render: (row) => formatDate(row.findingDate) ?? t('common.none'),
    },
    {
      key: 'deadlineDate',
      header: t('correctiveAction.fields.deadlineDate'),
      render: (row) =>
        row.isOverdue ? (
          <span className="badge-light-danger fw-bold">
            {t('correctiveAction.overdue.since', {
              date: formatDate(row.deadlineDate) ?? '',
            })}
          </span>
        ) : (
          (formatDate(row.deadlineDate) ?? t('common.none'))
        ),
    },
    {
      key: 'status',
      header: t('correctiveAction.fields.status'),
      align: 'center',
      render: (row) => (
        <span className={CORRECTIVE_ACTION_STATUS_BADGE[row.operationResult]}>
          {t(`enums.correctiveActionStatus.${row.operationResult}`)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '120px',
      render: (row) => (
        <RowActions
          editLabel={t('correctiveAction.list.editAction')}
          deleteLabel={t('correctiveAction.list.deleteAction')}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setDeleting(row)}
        />
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('correctiveAction.list.title')}
        description={t('correctiveAction.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setIsCreating(true)}>
            {t('correctiveAction.list.create')}
          </button>
        }
      />

      {overdueCount > 0 && (
        <AlertPanel tone="danger">
          <div>
            <strong className="d-block">
              {t('correctiveAction.overdue.bannerTitle', { total: overdueCount })}
            </strong>
            <span>{t('correctiveAction.overdue.bannerDescription')}</span>
          </div>
          <button
            type="button"
            className="btn btn-sm btn-danger"
            disabled={onlyOverdue}
            onClick={() => {
              setOnlyOverdue(true)
              setPage(1)
            }}
          >
            {t('correctiveAction.overdue.showOverdue')}
          </button>
        </AlertPanel>
      )}

      <div className="card">
        <div className="card-header pt-4 pb-0 border-0">
          <SearchBar
            value={search}
            onChange={resetPage(setSearch)}
            placeholder={t('correctiveAction.list.searchPlaceholder')}
          >
            <FilterSelect
              id="action-filter-company"
              label={t('correctiveAction.fields.company')}
              value={companyId}
              onChange={resetPage(setCompanyId)}
              width={220}
            >
              <option value="">{t('correctiveAction.list.allCompanies')}</option>
              {companies.data?.items.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.displayName}
                </option>
              ))}
            </FilterSelect>

            <FilterSelect
              id="action-filter-status"
              label={t('correctiveAction.fields.status')}
              value={status}
              onChange={resetPage(setStatus)}
            >
              <option value="">{t('correctiveAction.list.allStatuses')}</option>
              {enumValues(CorrectiveActionStatus).map((value) => (
                <option key={value} value={value}>
                  {t(`enums.correctiveActionStatus.${value}`)}
                </option>
              ))}
            </FilterSelect>

            <FilterSelect
              id="action-filter-risk"
              label={t('correctiveAction.fields.riskCategory')}
              value={riskCategory}
              onChange={resetPage(setRiskCategory)}
            >
              <option value="">{t('correctiveAction.list.allRiskCategories')}</option>
              {enumValues(RiskCategory).map((value) => (
                <option key={value} value={value}>
                  {t(`enums.riskCategory.${value}`)}
                </option>
              ))}
            </FilterSelect>

            <div className="form-check form-switch mb-0">
              <input
                id="action-filter-overdue"
                type="checkbox"
                className="form-check-input"
                checked={onlyOverdue}
                onChange={(event) => {
                  setOnlyOverdue(event.target.checked)
                  setPage(1)
                }}
              />
              <label className="form-check-label" htmlFor="action-filter-overdue">
                {t('correctiveAction.list.onlyOverdue')}
              </label>
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('correctiveAction.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('correctiveAction.list.empty')}
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

      {isCreating && <CorrectiveActionFormModal onClose={() => setIsCreating(false)} />}

      {editingId !== null && editing.data && (
        <CorrectiveActionFormModal
          action={editing.data.correctiveAction}
          onClose={() => setEditingId(null)}
        />
      )}

      <ConfirmDialog
        isOpen={deleting !== null}
        title={t('correctiveAction.list.deleteTitle')}
        message={t('correctiveAction.list.deleteMessage', { finding: deleting?.finding ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
      />
    </>
  )
}
