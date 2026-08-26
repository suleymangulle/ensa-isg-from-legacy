import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
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
        <span className={organization.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {organization.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (organization) => (
        <div className="d-inline-flex gap-1">
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => setEditingId(organization.id)}
            aria-label={t('organization.actions.editNamed', { name: organization.name })}
            title={t('common.edit')}
          >
            ✎
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setPendingDelete(organization)}
            aria-label={t('organization.actions.deleteNamed', { name: organization.name })}
            title={t('common.delete')}
          >
            ✕
          </button>
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
          <button className="btn btn-primary" type="button" onClick={() => setCreateOpen(true)}>
            {t('organization.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header border-0 pt-4 pb-0 d-block">
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('organization.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="organization-filter-active" className="visually-hidden">
                {t('organization.filters.status')}
              </label>
              <select
                id="organization-filter-active"
                className="form-select"
                style={{ maxWidth: 180 }}
                value={activeState}
                onChange={(event) => {
                  setActiveState(event.target.value)
                  setPage(1)
                }}
              >
                <option value="">{t('organization.filters.allStatuses')}</option>
                <option value="true">{t('common.active')}</option>
                <option value="false">{t('common.passive')}</option>
              </select>
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('organization.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(organization) => organization.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('organization.list.empty')}
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
