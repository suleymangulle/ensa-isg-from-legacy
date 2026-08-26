import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { PageTitle, Pagination, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import FieldObservationFormModal from './FieldObservationFormModal'
import {
  OBSERVATION_ENDPOINTS,
  useCompanyLookup,
  useFieldObservationReportDetail,
  useFieldObservationReportList,
  type FieldObservationReportListDto,
} from './api'
import { FilterSelect, RowActions } from './components'

const PAGE_SIZE = 20

export default function FieldObservationListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [isCreating, setIsCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<FieldObservationReportListDto | null>(null)

  const companies = useCompanyLookup()

  const { data, isLoading, error } = useFieldObservationReportList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'Date DESC',
    filter: search,
    companyId: companyId ? Number(companyId) : undefined,
    dateFrom: dateFrom || undefined,
    dateTo: dateTo || undefined,
  })

  const editing = useFieldObservationReportDetail(editingId ?? undefined)

  const remove = useDelete(OBSERVATION_ENDPOINTS.fieldObservationReport, {
    onSuccess: () => setDeleting(null),
  })

  function resetPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const columns: Column<FieldObservationReportListDto>[] = [
    {
      key: 'date',
      header: t('fieldObservation.fields.date'),
      render: (row) => (
        <Link to={`/field-observations/${row.id}`} className="fw-semibold text-decoration-none">
          {formatDate(row.date) ?? t('common.none')}
        </Link>
      ),
    },
    {
      key: 'company',
      header: t('fieldObservation.fields.company'),
      render: (row) => row.companyName ?? t('common.none'),
    },
    {
      key: 'department',
      header: t('fieldObservation.fields.department'),
      render: (row) => row.departmentName ?? t('common.none'),
    },
    {
      key: 'lineCount',
      header: t('fieldObservation.fields.lineCount'),
      align: 'center',
      render: (row) => (
        <span className={row.lineCount > 0 ? 'badge-light-warning' : 'badge-light-success'}>
          {row.lineCount}
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
          editLabel={t('fieldObservation.list.editAction')}
          deleteLabel={t('fieldObservation.list.deleteAction')}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setDeleting(row)}
        />
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('fieldObservation.list.title')}
        description={t('fieldObservation.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setIsCreating(true)}>
            {t('fieldObservation.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header pt-4 pb-0 border-0">
          <SearchBar
            value={search}
            onChange={resetPage(setSearch)}
            placeholder={t('fieldObservation.list.searchPlaceholder')}
          >
            <FilterSelect
              id="observation-filter-company"
              label={t('fieldObservation.fields.company')}
              value={companyId}
              onChange={resetPage(setCompanyId)}
              width={220}
            >
              <option value="">{t('fieldObservation.list.allCompanies')}</option>
              {companies.data?.items.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.displayName}
                </option>
              ))}
            </FilterSelect>

            <div>
              <label htmlFor="observation-filter-from" className="visually-hidden">
                {t('fieldObservation.list.dateFrom')}
              </label>
              <input
                id="observation-filter-from"
                type="date"
                className="form-control"
                value={dateFrom}
                aria-label={t('fieldObservation.list.dateFrom')}
                onChange={(event) => resetPage(setDateFrom)(event.target.value)}
              />
            </div>

            <div>
              <label htmlFor="observation-filter-to" className="visually-hidden">
                {t('fieldObservation.list.dateTo')}
              </label>
              <input
                id="observation-filter-to"
                type="date"
                className="form-control"
                value={dateTo}
                aria-label={t('fieldObservation.list.dateTo')}
                onChange={(event) => resetPage(setDateTo)(event.target.value)}
              />
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('fieldObservation.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('fieldObservation.list.empty')}
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

      {isCreating && <FieldObservationFormModal onClose={() => setIsCreating(false)} />}

      {editingId !== null && editing.data && (
        <FieldObservationFormModal
          report={editing.data.report}
          onClose={() => setEditingId(null)}
        />
      )}

      <ConfirmDialog
        isOpen={deleting !== null}
        title={t('fieldObservation.list.deleteTitle')}
        message={t('fieldObservation.list.deleteMessage', {
          date: formatDate(deleting?.date) ?? '',
          company: deleting?.companyName ?? '',
        })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
      />
    </>
  )
}
