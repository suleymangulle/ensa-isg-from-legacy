import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ENDPOINTS, HAZARD_CLASS_BADGE, usePagedList, type CompanyListDto } from '@/api/endpoints'
import { errorMessage } from '@/api/http'

const PAGE_SIZE = 20

export default function CompanyListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const { data, isLoading, error } = usePagedList<CompanyListDto>(ENDPOINTS.company, {
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'CompanyName ASC',
    filter: search,
  })

  const columns: Column<CompanyListDto>[] = [
    {
      key: 'companyName',
      header: t('company.fields.companyName'),
      render: (company) => (
        <Link to={`/companies/${company.id}`} className="fw-semibold text-decoration-none">
          {company.companyName}
        </Link>
      ),
    },
    {
      key: 'ssiNumber',
      header: t('company.fields.ssiNumber'),
      render: (company) => company.ssiNumber ?? t('common.none'),
    },
    {
      key: 'hazardClass',
      header: t('company.fields.hazardClass'),
      render: (company) => (
        <span className={HAZARD_CLASS_BADGE[company.hazardClass]}>
          {t(`enums.hazardClass.${company.hazardClass}`)}
        </span>
      ),
    },
    {
      key: 'workplaceType',
      header: t('company.fields.workplaceType'),
      render: (company) => t(`enums.workplaceType.${company.workplaceType}`),
    },
    {
      key: 'cityDistrict',
      header: t('company.fields.cityDistrict'),
      render: (company) =>
        [company.cityName, company.districtName].filter(Boolean).join(' / ') || t('common.none'),
    },
    {
      key: 'authorizedPerson',
      header: t('company.fields.authorizedPerson'),
      render: (company) => company.authorizedPerson ?? t('common.none'),
    },
    {
      key: 'workerCount',
      header: t('company.fields.workerCount'),
      align: 'end',
      render: (company) => company.workerCount ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('company.fields.status'),
      align: 'center',
      render: (company) => (
        <span className={company.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {company.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('company.list.title')}
        description={t('company.list.description')}
        action={
          <button className="btn btn-primary" type="button">
            {t('company.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header">
          <input
            className="form-control"
            style={{ maxWidth: 320 }}
            placeholder={t('company.list.searchPlaceholder')}
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            aria-label={t('company.list.searchLabel')}
          />
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('company.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(company) => company.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('company.list.empty')}
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
