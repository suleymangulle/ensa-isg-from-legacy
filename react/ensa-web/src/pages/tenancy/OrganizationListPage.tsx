import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import OrganizationFormModal from './OrganizationFormModal'
import {
  TENANCY_RESOURCES,
  useOrganization,
  useOrganizationList,
  type OrganizationListDto,
} from './api'

const PAGE_SIZE = 20

export default function OrganizationListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [activeState, setActiveState] = useState('')
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [pendingDelete, setPendingDelete] = useState<OrganizationListDto | null>(null)

  const { data, isLoading, error } = useOrganizationList({
    page,
    pageSize: PAGE_SIZE,
    filter: search,
    isActive: activeState === '' ? undefined : activeState === 'true',
  })

  const editing = useOrganization(editingId ?? undefined)
  const remove = useDelete(TENANCY_RESOURCES.organization, {
    onSuccess: () => setPendingDelete(null),
  })

  const columns: Column<OrganizationListDto>[] = [
    {
      key: 'name',
      header: t('organization.fields.name'),
      render: (organization) => (
        <Link
          to={`/organizations/${organization.id}`}
          className="fw-semibold text-decoration-none"
        >
          {organization.name}
        </Link>
      ),
    },
    {
      key: 'code',
      header: t('organization.fields.code'),
      render: (organization) => organization.code,
    },
    {
      key: 'organizationType',
      header: t('organization.fields.organizationType'),
      render: (organization) => organization.organizationTypeName ?? t('common.none'),
    },
    {
      key: 'subscriptionPlan',
      header: t('organization.fields.subscriptionPlan'),
      render: (organization) => organization.subscriptionPlanName ?? t('common.none'),
    },
    {
      key: 'contact',
      header: t('organization.fields.contact'),
      render: (organization) =>
        [organization.phone, organization.email].filter(Boolean).join(' · ') || t('common.none'),
    },
    {
      key: 'subscription',
      header: t('organization.fields.subscription'),
      render: (organization) =>
        organization.subscriptionEnd
          ? `${formatDate(organization.subscriptionStart) ?? ''} – ${formatDate(organization.subscriptionEnd) ?? ''}`
          : t('organization.subscription.openEnded', {
              value: formatDate(organization.subscriptionStart) ?? '',
            }),
    },
    {
      key: 'status',
      header: t('organization.fields.status'),
      align: 'center',
      render: (organization) => (
        <Badge variant={organization.isActive ? 'success' : 'danger'}>
          {organization.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (organization) => (
        <div className="d-inline-flex gap-1">
          <Button variant="light" size="sm" 
            onClick={() => setEditingId(organization.id)}
            aria-label={t('organization.actions.editNamed', { name: organization.name })}
            title={t('common.edit')}
          >
            ✎
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setPendingDelete(organization)}
            aria-label={t('organization.actions.deleteNamed', { name: organization.name })}
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
        title={t('organization.list.title')}
        description={t('organization.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('organization.list.create')}
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
            placeholder={t('organization.list.searchPlaceholder')}
          >
            <div style={{ maxWidth: 180 }}>
              <label htmlFor="organization-filter-active" className="visually-hidden">
                {t('organization.filters.status')}
              </label>
              <Select
                id="organization-filter-active"
                options={[
                  { value: 'true', label: t('common.active') },
                  { value: 'false', label: t('common.passive') },
                ]}
                value={activeState === '' ? null : activeState}
                onChange={(value) => {
                  setActiveState(value ?? '')
                  setPage(1)
                }}
                placeholder={t('organization.filters.allStatuses')}
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
          label={t('organization.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(organization) => organization.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('organization.list.empty')}
        />
      </Card>

      {isCreateOpen && <OrganizationFormModal onClose={() => setCreateOpen(false)} />}
      {editingId !== null && editing.data && (
        <OrganizationFormModal
          organization={editing.data}
          onClose={() => setEditingId(null)}
        />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('organization.actions.deleteTitle')}
        message={t('organization.actions.deleteMessage', { name: pendingDelete?.name ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}
