import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ENDPOINTS, usePagedList, type CompanyEmployeeListDto } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'

const PAGE_SIZE = 20

export default function EmployeeListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const { data, isLoading, error } = usePagedList<CompanyEmployeeListDto>(
    ENDPOINTS.companyEmployee,
    {
      skipCount: (page - 1) * PAGE_SIZE,
      maxResultCount: PAGE_SIZE,
      sorting: 'Name ASC',
      filter: search,
    },
  )

  const columns: Column<CompanyEmployeeListDto>[] = [
    {
      key: 'fullName',
      header: t('employee.fields.fullName'),
      render: (employee) => (
        <span className="fw-semibold">
          {employee.name} {employee.lastName}
        </span>
      ),
    },
    {
      key: 'nationalId',
      header: t('employee.fields.nationalId'),
      render: (employee) => employee.nationalId ?? t('common.none'),
    },
    {
      key: 'companyName',
      header: t('employee.fields.companyName'),
      render: (employee) => employee.companyName ?? t('common.none'),
    },
    {
      key: 'duty',
      header: t('employee.fields.duty'),
      render: (employee) => employee.duty ?? t('common.none'),
    },
    {
      key: 'gender',
      header: t('employee.fields.gender'),
      render: (employee) => t(`enums.gender.${employee.gender}`),
    },
    {
      key: 'hireDate',
      header: t('employee.fields.hireDate'),
      render: (employee) => formatDate(employee.hireDate) ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('employee.fields.status'),
      align: 'center',
      render: (employee) => (
        <span className={employee.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {employee.isActive ? t('employee.status.active') : t('employee.status.left')}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('employee.list.title')}
        description={t('employee.list.description')}
        action={
          <button className="btn btn-primary" type="button">
            {t('employee.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header">
          <input
            className="form-control"
            style={{ maxWidth: 320 }}
            placeholder={t('employee.list.searchPlaceholder')}
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            aria-label={t('employee.list.searchLabel')}
          />
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('employee.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(employee) => employee.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('employee.list.empty')}
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
    </>
  )
}
