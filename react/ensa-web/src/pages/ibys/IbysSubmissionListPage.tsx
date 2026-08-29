import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Card, Input, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { SearchBar } from '@/components/Form'
import { useLookup } from '@/api/endpoints'
import { IbysQueryType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import {
  IBYS_QUERY_TYPES,
  IBYS_STATUS_BADGE,
  IBYS_SUBMISSION_STATUSES,
  useIbysQueryList,
  usePendingIbysQueries,
  type IbysQueryListDto,
} from './api'

/**
 * IBYS submission tracking.
 *
 * SECURITY. The screen shows the submission envelope only — workplace, employee, type, dates,
 * status and the service message. The notification XML and the e-signed payload are not part
 * of any DTO in this module and must never be surfaced here; `hasXmlData` / `hasSignedData` on
 * the detail view are the only statement made about them.
 */

const PAGE_SIZE = 20

/** Debounce for the free-text filter, which matches query no, group id and service message. */
function useDebouncedValue<T>(value: T, delay = 350): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delay)
    return () => window.clearTimeout(timer)
  }, [value, delay])

  return debounced
}

export default function IbysSubmissionListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)

  const [queryType, setQueryType] = useState<number | ''>('')
  const [status, setStatus] = useState<number | ''>('')
  const [companyId, setCompanyId] = useState<number | ''>('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')

  const companies = useLookup('company')

  const { data, isLoading, error } = useIbysQueryList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'SubmissionDate DESC',
    filter: debouncedSearch,
    queryType: queryType === '' ? null : queryType,
    status: status === '' ? null : status,
    companyId: companyId === '' ? null : companyId,
    submissionDateFrom: dateFrom || null,
    submissionDateTo: dateTo || null,
  })

  function resetPaging<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const columns: Column<IbysQueryListDto>[] = [
    {
      key: 'queryNo',
      header: t('ibys.fields.queryNo'),
      render: (row) => (
        <Link to={`/ibys/${row.id}`} className="fw-semibold text-decoration-none">
          {row.queryNo ?? t('ibys.list.noQueryNo')}
        </Link>
      ),
    },
    {
      key: 'queryType',
      header: t('ibys.fields.queryType'),
      render: (row) => t(`enums.ibysQueryType.${row.queryType}`),
    },
    {
      key: 'companyName',
      header: t('ibys.fields.company'),
      render: (row) => row.companyName ?? t('common.none'),
    },
    {
      key: 'employee',
      header: t('ibys.fields.employee'),
      render: (row) => row.employeeFullName ?? t('common.none'),
    },
    {
      key: 'submissionDate',
      header: t('ibys.fields.submissionDate'),
      render: (row) => formatDate(row.submissionDate) ?? t('common.none'),
    },
    {
      key: 'groupId',
      header: t('ibys.fields.groupId'),
      render: (row) => row.groupId ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('ibys.fields.status'),
      align: 'center',
      render: (row) => (
        <Badge variant={IBYS_STATUS_BADGE[row.status]}>
          {t(`enums.ibysSubmissionStatus.${row.status}`)}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '80px',
      render: (row) => (
        <Link
          to={`/ibys/${row.id}`}
          className="btn btn-sm btn-light"
          aria-label={t('ibys.list.openDetail', { queryNo: row.queryNo ?? String(row.id) })}
        >
          <span aria-hidden="true">→</span>
        </Link>
      ),
    },
  ]

  return (
    <>
      <PageTitle title={t('ibys.list.title')} description={t('ibys.list.description')} />

      <PendingQueue />

      <Card
        
        header={
          <SearchBar
            value={search}
            onChange={resetPaging(setSearch)}
            placeholder={t('ibys.list.searchPlaceholder')}
          >
            <div className="d-flex flex-wrap gap-2">
              <FilterSelect
                id="ibys-filter-type"
                label={t('ibys.filters.queryType')}
                value={queryType}
                onChange={resetPaging(setQueryType)}
                options={IBYS_QUERY_TYPES.map((type) => ({
                  value: type,
                  label: t(`enums.ibysQueryType.${type}`),
                }))}
              />
              <FilterSelect
                id="ibys-filter-status"
                label={t('ibys.filters.status')}
                value={status}
                onChange={resetPaging(setStatus)}
                options={IBYS_SUBMISSION_STATUSES.map((item) => ({
                  value: item,
                  label: t(`enums.ibysSubmissionStatus.${item}`),
                }))}
              />
              <FilterSelect
                id="ibys-filter-company"
                label={t('ibys.filters.company')}
                value={companyId}
                onChange={resetPaging(setCompanyId)}
                options={(companies.data?.items ?? []).map((item) => ({
                  value: item.id,
                  label: item.displayName,
                }))}
              />
              <div style={{ minWidth: 150 }}>
                <label htmlFor="ibys-filter-from" className="visually-hidden">
                  {t('ibys.filters.dateFrom')}
                </label>
                <Input
                  id="ibys-filter-from"
                  value={dateFrom}
                  onChange={resetPaging(setDateFrom)}
                  inputProps={{ type: 'date', title: t('ibys.filters.dateFrom') }}
                />
              </div>
              <div style={{ minWidth: 150 }}>
                <label htmlFor="ibys-filter-to" className="visually-hidden">
                  {t('ibys.filters.dateTo')}
                </label>
                <Input
                  id="ibys-filter-to"
                  value={dateTo}
                  onChange={resetPaging(setDateTo)}
                  inputProps={{ type: 'date', title: t('ibys.filters.dateTo') }}
                />
              </div>
            </div>
          </SearchBar>
        }
        footer={
          data &&
          data.totalCount > 0 && (
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          )
        }
      >
        <DataTable
          label={t('ibys.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('ibys.list.empty')}
        />
      </Card>
    </>
  )
}

/**
 * Submissions still awaiting an IBYS result. The endpoint answers for one query type at a
 * time, so the panel carries its own type selector rather than firing one request per type.
 */
function PendingQueue() {
  const { t } = useTranslation()
  const [type, setType] = useState<IbysQueryType>(IbysQueryType.HealthReport)
  const { data, isLoading, error } = usePendingIbysQueries(type)

  return (
    <Card
      className="mb-4"
      header={
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
        <h2 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
          {t('ibys.pending.title')}
        </h2>
        <div style={{ minWidth: 200 }}>
          <label htmlFor="ibys-pending-type" className="visually-hidden">
            {t('ibys.pending.typeLabel')}
          </label>
          <Select
            id="ibys-pending-type"
            options={IBYS_QUERY_TYPES.map((item) => ({
              value: item,
              label: t(`enums.ibysQueryType.${item}`),
            }))}
            value={type}
            onChange={(value) => setType(value ?? IbysQueryType.HealthReport)}
          />
        </div>
      
      </div>
      }
    >
        {isLoading ? (
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
            {t('common.loading')}
          </p>
        ) : error ? (
          <p className="mb-0" style={{ color: 'var(--kt-danger)' }} role="alert">
            {errorMessage(error)}
          </p>
        ) : !data?.items.length ? (
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
            {t('ibys.pending.empty')}
          </p>
        ) : (
          <ul className="list-unstyled mb-0 d-flex flex-wrap gap-2">
            {data.items.map((row) => (
              <li key={row.id}>
                <Link to={`/ibys/${row.id}`} className="text-decoration-none">
                  <Badge variant="warning">
                    {row.queryNo ?? t('ibys.list.noQueryNo')}
                    {' · '}
                    {row.companyName ?? t('common.none')}
                    {' · '}
                    {formatDate(row.submissionDate) ?? t('common.none')}
                  </Badge>
                </Link>
              </li>
            ))}
          </ul>
        )}
      
    </Card>
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
    <div style={{ minWidth: 170 }}>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <Select
        id={id}
        options={options}
        value={value === '' ? null : value}
        onChange={(next) => onChange(next ?? '')}
        placeholder={`${t('common.all')} — ${label}`}
      />
    </div>
  )
}
