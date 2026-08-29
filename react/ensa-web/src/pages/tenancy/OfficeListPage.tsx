import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import OfficeFormModal from './OfficeFormModal'
import { TENANCY_RESOURCES, useOffice, useOfficeList, type OfficeListDto } from './api'

const PAGE_SIZE = 20

export default function OfficeListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [activeState, setActiveState] = useState('')
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [pendingDelete, setPendingDelete] = useState<OfficeListDto | null>(null)

  const { data, isLoading, error } = useOfficeList({
    page,
    pageSize: PAGE_SIZE,
    filter: search,
    isActive: activeState === '' ? undefined : activeState === 'true',
  })

  // `OfficeListDto` carries the company id but no company name, and the API exposes no office
  // endpoint that joins one in, so the name is resolved from the shared company lookup.
  const companies = useLookup('company')
  const companyNames = useMemo(() => {
    const map = new Map<number, string>()
    for (const company of companies.data?.items ?? []) map.set(company.id, company.displayName)
    return map
  }, [companies.data])

  const editing = useOffice(editingId ?? undefined)
  const remove = useDelete(TENANCY_RESOURCES.office, { onSuccess: () => setPendingDelete(null) })

  const columns: Column<OfficeListDto>[] = [
    {
      key: 'name',
      header: t('office.fields.name'),
      render: (office) => (
        <Link to={`/offices/${office.id}`} className="fw-semibold text-decoration-none">
          {office.name}
        </Link>
      ),
    },
    {
      key: 'company',
      header: t('office.fields.company'),
      render: (office) =>
        office.companyId == null
          ? t('office.form.attachedToOrganization')
          : (companyNames.get(office.companyId) ??
            t('office.list.unresolvedCompany', { id: office.companyId })),
    },
    {
      key: 'authorizedPerson',
      header: t('office.fields.authorizedPerson'),
      render: (office) => office.authorizedPerson ?? t('common.none'),
    },
    {
      key: 'phone',
      header: t('office.fields.phone'),
      render: (office) => office.phone ?? t('common.none'),
    },
    {
      key: 'location',
      header: t('office.fields.cityDistrict'),
      render: (office) =>
        [office.cityName, office.districtName].filter(Boolean).join(' / ') || t('common.none'),
    },
    {
      key: 'headquarterOffice',
      header: t('office.fields.headquarterOffice'),
      align: 'center',
      render: (office) =>
        office.isHeadquarterOffice ? (
          <Badge variant="primary">{t('office.badges.headquarter')}</Badge>
        ) : (
          t('common.none')
        ),
    },
    {
      key: 'status',
      header: t('office.fields.status'),
      align: 'center',
      render: (office) => (
        <Badge variant={office.isActive ? 'success' : 'danger'}>
          {office.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (office) => (
        <div className="d-inline-flex gap-1">
          <Button variant="light" size="sm" 
            onClick={() => setEditingId(office.id)}
            aria-label={t('office.actions.editNamed', { name: office.name })}
            title={t('common.edit')}
          >
            ✎
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setPendingDelete(office)}
            aria-label={t('office.actions.deleteNamed', { name: office.name })}
            title={t('common.delete')}
          >
            ✕
          </Button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('office.list.title')}
        description={t('office.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('office.list.create')}
          </Button>
        }
      />

      <Card
        
        header={
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('office.list.searchPlaceholder')}
          >
            <div style={{ maxWidth: 180 }}>
              <label htmlFor="office-filter-active" className="visually-hidden">
                {t('office.filters.status')}
              </label>
              <Select
                id="office-filter-active"
                options={[
                  { value: 'true', label: t('common.active') },
                  { value: 'false', label: t('common.passive') },
                ]}
                value={activeState === '' ? null : activeState}
                onChange={(value) => {
                  setActiveState(value ?? '')
                  setPage(1)
                }}
                placeholder={t('office.filters.allStatuses')}
              />
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
          label={t('office.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(office) => office.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('office.list.empty')}
        />
      </Card>

      {isCreateOpen && <OfficeFormModal onClose={() => setCreateOpen(false)} />}
      {editingId !== null && editing.data && (
        <OfficeFormModal office={editing.data} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('office.actions.deleteTitle')}
        message={t('office.actions.deleteMessage', { name: pendingDelete?.name ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}
