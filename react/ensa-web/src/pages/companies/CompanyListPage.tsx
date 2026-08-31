import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Input } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ENDPOINTS, HAZARD_CLASS_BADGE, usePagedList, type CompanyListDto } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import CompanyFormModal from './CompanyFormModal'
import { useCompany } from './api'

const PAGE_SIZE = 20

export default function CompanyListPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [isCreating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)

  // The table row is a projection; the dialog edits the record, so the id is exchanged for it.
  const editing = useCompany(editingId ?? undefined)

  const { data, isLoading, error } = usePagedList<CompanyListDto>(ENDPOINTS.company, {
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'CompanyName ASC',
    filter: search,
  })

  const columns: Column<CompanyListDto>[] = [
    {
      key: 'companyName',
      field: 'companyName',
      header: t('company.fields.companyName'),
      render: (company) => <span className="fw-semibold">{company.companyName}</span>,
    },
    {
      key: 'ssiNumber',
      field: 'ssiNumber',
      header: t('company.fields.ssiNumber'),
      render: (company) => company.ssiNumber ?? t('common.none'),
    },
    {
      key: 'hazardClass',
      field: 'hazardClass',
      header: t('company.fields.hazardClass'),
      render: (company) => (
        <Badge variant={HAZARD_CLASS_BADGE[company.hazardClass]}>
          {t(`enums.hazardClass.${company.hazardClass}`)}
        </Badge>
      ),
    },
    {
      key: 'workplaceType',
      field: 'workplaceType',
      header: t('company.fields.workplaceType'),
      render: (company) => t(`enums.workplaceType.${company.workplaceType}`),
    },
    {
      key: 'cityDistrict',
      field: 'cityName',
      header: t('company.fields.cityDistrict'),
      render: (company) =>
        [company.cityName, company.districtName].filter(Boolean).join(' / ') || t('common.none'),
    },
    {
      key: 'authorizedPerson',
      field: 'authorizedPerson',
      header: t('company.fields.authorizedPerson'),
      render: (company) => company.authorizedPerson ?? t('common.none'),
    },
    {
      key: 'workerCount',
      field: 'workerCount',
      header: t('company.fields.workerCount'),
      align: 'end',
      render: (company) => company.workerCount ?? t('common.none'),
    },
    {
      key: 'status',
      field: 'isActive',
      header: t('company.fields.status'),
      align: 'center',
      render: (company) => (
        <Badge variant={company.isActive ? 'success' : 'danger'}>
          {company.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '190px',
      render: (company) => (
        <div className="d-inline-flex gap-1">
          <Button
            variant="light"
            size="sm"
            onClick={() => navigate(`/companies/${company.id}`)}
            aria-label={t('company.actions.openDetail', { name: company.companyName })}
            title={t('common.detail')}
          >
            {t('common.detail')}
          </Button>
          <Button
            variant="light"
            size="sm"
            onClick={() => setEditingId(company.id)}
            aria-label={t('company.actions.editNamed', { name: company.companyName })}
            title={t('common.edit')}
          >
            {t('common.edit')}
          </Button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('company.list.title')}
        description={t('company.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreating(true)}>
            {t('company.list.create')}
          </Button>
        }
      />

      <Card
        
        header={
          <div style={{ maxWidth: 320 }}>
            <Input
              value={search}
              onChange={(value) => {
                setSearch(value)
                setPage(1)
              }}
              placeholder={t('company.list.searchPlaceholder')}
              inputProps={{ 'aria-label': t('company.list.searchLabel') }}
            />
          </div>
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
          label={t('company.list.title')}
          columns={columns}
          // Nine columns do not fit a laptop split in two, let alone a phone. Rather than scroll
          // the table sideways, the columns leave the row one at a time as the grid narrows and
          // reappear in reverse as it widens; what leaves is shown as a label/value pair inside
          // the row it belongs to, so nothing is lost — `Responsive`, never `Hide`.
          //
          // The order is what a person scanning this list can spare first: the worker count, then
          // the contact, then the type, the location, the SSI number and the hazard class. The
          // company name is the row's identity and never leaves. Neither does the action column:
          // the grid never reduces a command column on its own, and dropping it would take away
          // the only way into the record.
          responsive={{
            primaryColumn: 'companyName',
            columns: [
              { field: 'workerCount', behavior: 'Responsive' },
              { field: 'authorizedPerson', behavior: 'Responsive' },
              { field: 'workplaceType', behavior: 'Responsive' },
              { field: 'cityDistrict', behavior: 'Responsive' },
              { field: 'ssiNumber', behavior: 'Responsive' },
              { field: 'hazardClass', behavior: 'Responsive' },
            ],
          }}
          rows={data?.items}
          rowKey={(company) => company.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('company.list.empty')}
        />
      </Card>

      {isCreating && <CompanyFormModal onClose={() => setCreating(false)} />}
      {editingId !== null && editing.data && (
        <CompanyFormModal company={editing.data} onClose={() => setEditingId(null)} />
      )}
    </>
  )
}
