import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { StaffRole } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { useAuth } from '@/auth/AuthContext'
import UserFormModal from './UserFormModal'
import {
  MEMBERSHIP_RESOURCES,
  useSetUserActiveState,
  useUserList,
  type UserListDto,
} from './api'

const PAGE_SIZE = 20

/** Staff roles offered in the filter, in the order the administration screen lists them. */
const STAFF_ROLES: StaffRole[] = [
  StaffRole.OccupationalSafetySpecialist,
  StaffRole.WorkplacePhysician,
  StaffRole.OtherHealthPersonnel,
  StaffRole.OfficeStaff,
  StaffRole.Customer,
  StaffRole.OfficeAdministrator,
  StaffRole.OrganizationAdministrator,
  StaffRole.SystemAdministrator,
]

export default function UserListPage() {
  const { t } = useTranslation()
  const { user: currentUser } = useAuth()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [staffRole, setStaffRole] = useState('')
  const [activeState, setActiveState] = useState('')
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<UserListDto | null>(null)

  const { data, isLoading, error } = useUserList({
    page,
    pageSize: PAGE_SIZE,
    filter: search,
    staffRole: staffRole === '' ? undefined : (Number(staffRole) as StaffRole),
    isActive: activeState === '' ? undefined : activeState === 'true',
  })

  const remove = useDelete(MEMBERSHIP_RESOURCES.user, { onSuccess: () => setPendingDelete(null) })
  const setActive = useSetUserActiveState()

  function resetToFirstPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const columns: Column<UserListDto>[] = [
    {
      key: 'fullName',
      header: t('user.fields.fullName'),
      render: (user) => (
        <Link to={`/users/${user.id}`} className="fw-semibold text-decoration-none">
          {user.fullName || `${user.name} ${user.lastName}`}
        </Link>
      ),
    },
    {
      key: 'userName',
      header: t('user.fields.userName'),
      render: (user) => user.userName,
    },
    {
      key: 'email',
      header: t('user.fields.email'),
      render: (user) => user.email ?? t('common.none'),
    },
    {
      key: 'phone',
      header: t('user.fields.phoneNumber'),
      render: (user) => user.phoneNumber ?? user.gsm ?? t('common.none'),
    },
    {
      key: 'staffRole',
      header: t('user.fields.staffRole'),
      render: (user) => (
        <Badge variant="info">{t(`enums.staffRole.${user.staffRole}`)}</Badge>
      ),
    },
    {
      key: 'scope',
      header: t('user.fields.administration'),
      render: (user) => {
        const badges: string[] = []
        if (user.organizationAdmin) badges.push(t('user.badges.organizationAdmin'))
        if (user.officeAdmin) badges.push(t('user.badges.officeAdmin'))
        if (badges.length === 0) return t('common.none')
        return (
          <span className="d-inline-flex flex-wrap gap-1">
            {badges.map((badge) => (
              <Badge variant="primary" key={badge}>
                {badge}
              </Badge>
            ))}
          </span>
        )
      },
    },
    {
      key: 'status',
      header: t('user.fields.status'),
      align: 'center',
      render: (user) => (
        <Badge variant={user.isActive ? 'success' : 'danger'}>
          {user.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '160px',
      render: (user) => (
        <div className="d-inline-flex gap-1">
          <Link
            to={`/users/${user.id}`}
            className="btn btn-sm"
            aria-label={t('user.actions.openDetail', { name: user.fullName })}
            title={t('common.detail')}
          >
            ⋯
          </Link>
          <Button variant="light" size="sm"
            disabled={setActive.isPending}
            onClick={() => setActive.mutate({ id: user.id, isActive: !user.isActive })}
            aria-label={
              user.isActive
                ? t('user.actions.deactivateNamed', { name: user.fullName })
                : t('user.actions.activateNamed', { name: user.fullName })
            }
            title={user.isActive ? t('user.actions.deactivate') : t('user.actions.activate')}
          >
            {user.isActive ? '⏸' : '▶'}
          </Button>
          <Button variant="light" size="sm" 
            disabled={currentUser?.id === user.id}
            onClick={() => setPendingDelete(user)}
            aria-label={t('user.actions.deleteNamed', { name: user.fullName })}
            title={
              currentUser?.id === user.id ? t('user.actions.cannotDeleteSelf') : t('common.delete')
            }
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
        title={t('user.list.title')}
        description={t('user.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('user.list.create')}
          </Button>
        }
      />

      <Card
        
        header={
          <SearchBar
            value={search}
            onChange={resetToFirstPage(setSearch)}
            placeholder={t('user.list.searchPlaceholder')}
          >
            <div style={{ maxWidth: 260 }}>
              <label htmlFor="user-filter-staffRole" className="visually-hidden">
                {t('user.filters.staffRole')}
              </label>
              <Select
                id="user-filter-staffRole"
                placeholder={t('user.filters.allStaffRoles')}
                options={STAFF_ROLES.map((role) => ({
                  value: String(role),
                  label: t(`enums.staffRole.${role}`),
                }))}
                value={staffRole === '' ? null : staffRole}
                onChange={(value) => resetToFirstPage(setStaffRole)(value ?? '')}
              />
            </div>
            <div style={{ maxWidth: 180 }}>
              <label htmlFor="user-filter-active" className="visually-hidden">
                {t('user.filters.status')}
              </label>
              <Select
                id="user-filter-active"
                placeholder={t('user.filters.allStatuses')}
                options={[
                  { value: 'true', label: t('common.active') },
                  { value: 'false', label: t('common.passive') },
                ]}
                value={activeState === '' ? null : activeState}
                onChange={(value) => resetToFirstPage(setActiveState)(value ?? '')}
              />
            </div>
          </SearchBar>
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
          label={t('user.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(user) => user.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('user.list.empty')}
        />
      </Card>

      {isCreateOpen && (
        <UserFormModal
          isOpen
          onClose={() => setCreateOpen(false)}
          onSaved={() => setCreateOpen(false)}
        />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('user.actions.deleteTitle')}
        message={t('user.actions.deleteMessage', { name: pendingDelete?.fullName ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}
