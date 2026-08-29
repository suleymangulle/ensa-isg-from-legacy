import { useMemo, useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  Alert,
  Badge,
  Button,
  Card,
  Input,
  MultiSelect,
  Tabs,
} from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog, Modal } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { useAuth } from '@/auth/AuthContext'
import { formatDate } from '@/utils/format'
import UserFormModal from './UserFormModal'
import {
  MEMBERSHIP_RESOURCES,
  useAssignRoles,
  useResetPassword,
  useRoleLookup,
  useSetUserActiveState,
  useUserDetail,
  type PermissionDto,
  type UserNavigationDto,
} from './api'

const TABS = ['general', 'roles', 'permissions'] as const

type TabKey = (typeof TABS)[number]

export default function UserDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const { user: currentUser } = useAuth()

  const userId = Number(id)
  const [activeTab, setActiveTab] = useState<TabKey>('general')
  const [isEditOpen, setEditOpen] = useState(false)
  const [isRolesOpen, setRolesOpen] = useState(false)
  const [isResetOpen, setResetOpen] = useState(false)
  const [isDeleteOpen, setDeleteOpen] = useState(false)

  const { data, isLoading, error } = useUserDetail(userId)
  const setActive = useSetUserActiveState()
  const remove = useDelete(MEMBERSHIP_RESOURCES.user, {
    onSuccess: () => navigate('/users', { replace: true }),
  })

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const user = data.user

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/users" className="text-decoration-none">
              {t('user.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {user.fullName}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={user.fullName || user.userName}
        description={t('user.detail.subtitle', {
          userName: user.userName,
          staffRole: t(`enums.staffRole.${user.staffRole}`),
        })}
        action={
          <div className="d-flex flex-wrap gap-2">
            <Button variant="light" 
              onClick={() => setResetOpen(true)}
            >
              {t('user.actions.resetPassword')}
            </Button>
            <Button variant="light"
              disabled={setActive.isPending}
              onClick={() => setActive.mutate({ id: user.id, isActive: !user.isActive })}
            >
              {user.isActive ? t('user.actions.deactivate') : t('user.actions.activate')}
            </Button>
            <Button variant="primary" onClick={() => setEditOpen(true)}>
              {t('common.edit')}
            </Button>
            <Button variant="light" 
              disabled={currentUser?.id === user.id}
              title={
                currentUser?.id === user.id ? t('user.actions.cannotDeleteSelf') : t('common.delete')
              }
              onClick={() => setDeleteOpen(true)}
            >
              {t('common.delete')}
            </Button>
          </div>
        }
      />

      <div className="d-flex flex-wrap gap-2 mb-4">
        <Badge variant={user.isActive ? 'success' : 'danger'}>
          {user.isActive ? t('common.active') : t('common.passive')}
        </Badge>
        {user.mustChangePassword && (
          <Badge variant="warning">{t('user.badges.mustChangePassword')}</Badge>
        )}
        {user.systemAdministrator && (
          <Badge variant="danger">{t('user.badges.systemAdministrator')}</Badge>
        )}
        {user.organizationAdmin && (
          <Badge variant="primary">{t('user.badges.organizationAdmin')}</Badge>
        )}
        {user.officeAdmin && (
          <Badge variant="primary">{t('user.badges.officeAdmin')}</Badge>
        )}
        {user.lockoutEnd && (
          <Badge variant="warning">
            {t('user.badges.lockedUntil', { value: formatDate(user.lockoutEnd) ?? '' })}
          </Badge>
        )}
      </div>

      <Card>
        <Tabs
          items={TABS.map((tab) => ({
            key: tab,
            label: t(`user.detail.tabs.${tab}`),
            content:
              tab === 'general' ? (
                <GeneralTab detail={data} />
              ) : tab === 'roles' ? (
                <RolesTab detail={data} onEdit={() => setRolesOpen(true)} />
              ) : (
                <PermissionsTab detail={data} />
              ),
          }))}
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as TabKey)}
          variant="underline"
        />
      </Card>

      {isEditOpen && (
        <UserFormModal
          isOpen
          user={user}
          onClose={() => setEditOpen(false)}
          onSaved={() => setEditOpen(false)}
        />
      )}

      {isRolesOpen && (
        <RoleAssignmentModal
          userId={user.id}
          currentRoles={data.roles.map((role) => role.displayName)}
          onClose={() => setRolesOpen(false)}
        />
      )}

      {isResetOpen && (
        <ResetPasswordModal userId={user.id} onClose={() => setResetOpen(false)} />
      )}

      <ConfirmDialog
        isOpen={isDeleteOpen}
        title={t('user.actions.deleteTitle')}
        message={t('user.actions.deleteMessage', { name: user.fullName })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setDeleteOpen(false)}
        onConfirm={() => remove.mutate(user.id)}
      />
    </>
  )
}

/** One label/value pair of the read-only detail grid. */
function Detail({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="col-md-6 col-xl-4 mb-4">
      <div
        className="text-uppercase fw-semibold mb-1"
        style={{ color: 'var(--kt-gray-500)', fontSize: '0.6875rem', letterSpacing: '0.06em' }}
      >
        {label}
      </div>
      <div style={{ color: 'var(--kt-gray-800)' }}>{children}</div>
    </div>
  )
}

function GeneralTab({ detail }: { detail: UserNavigationDto }) {
  const { t } = useTranslation()
  const user = detail.user
  const none = t('common.none')

  return (
    <div className="row">
      <Detail label={t('user.fields.userName')}>{user.userName}</Detail>
      <Detail label={t('user.fields.organization')}>
        {detail.organization?.displayName ?? t('common.host')}
      </Detail>
      <Detail label={t('user.fields.staffRole')}>{t(`enums.staffRole.${user.staffRole}`)}</Detail>
      <Detail label={t('user.fields.email')}>{user.email ?? none}</Detail>
      <Detail label={t('user.fields.phoneNumber')}>{user.phoneNumber ?? none}</Detail>
      <Detail label={t('user.fields.gsm')}>{user.gsm ?? none}</Detail>
      <Detail label={t('user.fields.office')}>{detail.office?.displayName ?? none}</Detail>
      <Detail label={t('user.fields.city')}>{detail.city?.displayName ?? none}</Detail>
      <Detail label={t('user.fields.district')}>{detail.district?.displayName ?? none}</Detail>
      <Detail label={t('user.fields.address')}>{user.address ?? none}</Detail>
      <Detail label={t('user.fields.hireDate')}>{formatDate(user.hireDate) ?? none}</Detail>
      <Detail label={t('user.fields.terminationDate')}>
        {formatDate(user.terminationDate) ?? none}
      </Detail>
      <Detail label={t('user.fields.partTime')}>{user.partTime ? t('common.yes') : t('common.no')}</Detail>
      <Detail label={t('user.fields.monthlyWorkDuration')}>
        {user.monthlyWorkDurationMinutes != null
          ? t('user.detail.minutes', { count: user.monthlyWorkDurationMinutes })
          : none}
      </Detail>
      <Detail label={t('user.fields.branchCode')}>{user.branchCode ?? none}</Detail>
      <Detail label={t('user.fields.mustChangePassword')}>
        {user.mustChangePassword ? t('common.yes') : t('common.no')}
      </Detail>
      <Detail label={t('user.fields.contractApproved')}>
        {user.contractApproved ? t('common.yes') : t('common.no')}
      </Detail>
      <Detail label={t('user.fields.emailConfirmed')}>
        {user.emailConfirmed ? t('common.yes') : t('common.no')}
      </Detail>

      {detail.officeAssignments.length > 0 && (
        <div className="col-12">
          <h2 className="h6 fw-bold mb-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('user.detail.officeAssignments')}
          </h2>
          <ul className="list-unstyled mb-0">
            {detail.officeAssignments.map((assignment) => (
              <li key={assignment.id} style={{ color: 'var(--kt-gray-700)' }}>
                {assignment.officeName} —{' '}
                {t('user.detail.minutes', { count: assignment.monthlyWorkDurationMinutes })}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}

function RolesTab({ detail, onEdit }: { detail: UserNavigationDto; onEdit: () => void }) {
  const { t } = useTranslation()

  return (
    <>
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <p className="mb-0" style={{ color: 'var(--kt-gray-600)' }}>
          {t('user.detail.rolesDescription')}
        </p>
        <Button variant="light" size="sm"  onClick={onEdit}>
          {t('user.actions.editRoles')}
        </Button>
      </div>

      {detail.roles.length === 0 ? (
        <div className="text-center py-4" style={{ color: 'var(--kt-gray-500)' }}>
          {t('user.detail.emptyRoles')}
        </div>
      ) : (
        <ul className="list-unstyled d-flex flex-wrap gap-2 mb-0">
          {detail.roles.map((role) => (
            <li key={role.id}>
              <Badge variant="primary">{role.displayName}</Badge>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}

/** Groups the effective permissions by the module prefix of their target (`Ensa.User.Create`). */
function moduleOf(permission: PermissionDto): string {
  const parts = permission.permissionTarget.split('.')
  return parts.length > 1 ? parts[1] : parts[0]
}

function PermissionsTab({ detail }: { detail: UserNavigationDto }) {
  const { t } = useTranslation()

  const groups = useMemo(() => {
    const map = new Map<string, PermissionDto[]>()
    for (const permission of detail.permissions) {
      const key = moduleOf(permission)
      const bucket = map.get(key)
      if (bucket) bucket.push(permission)
      else map.set(key, [permission])
    }
    return [...map.entries()].sort(([left], [right]) => left.localeCompare(right))
  }, [detail.permissions])

  return (
    <>
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <p className="mb-0" style={{ color: 'var(--kt-gray-600)' }}>
          {t('user.detail.permissionsDescription', { count: detail.permissions.length })}
        </p>
        <Link
          to={`/permissions?userId=${detail.user.id}`}
          className="btn btn-sm"
        >
          {t('user.actions.editPermissions')}
        </Link>
      </div>

      {groups.length === 0 ? (
        <div className="text-center py-4" style={{ color: 'var(--kt-gray-500)' }}>
          {t('user.detail.emptyPermissions')}
        </div>
      ) : (
        <div className="row">
          {groups.map(([group, permissions]) => (
            <div className="col-md-6 col-xl-4 mb-4" key={group}>
              <h2 className="h6 fw-bold mb-2" style={{ color: 'var(--kt-gray-700)' }}>
                {group}
              </h2>
              <ul className="list-unstyled mb-0">
                {permissions.map((permission) => (
                  <li
                    key={permission.id}
                    style={{ color: 'var(--kt-gray-600)', fontSize: '0.875rem' }}
                  >
                    {permission.permissionName}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      )}
    </>
  )
}

/** Replaces the whole role set of the user; the dialog shows what the save adds and removes. */
function RoleAssignmentModal({
  userId,
  currentRoles,
  onClose,
}: {
  userId: number
  currentRoles: string[]
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [selected, setSelected] = useState<string[]>(currentRoles)
  const roles = useRoleLookup()
  const assign = useAssignRoles(userId)

  const added = selected.filter((role) => !currentRoles.includes(role))
  const removed = currentRoles.filter((role) => !selected.includes(role))

  return (
    <Modal
      title={t('user.actions.editRoles')}
      isOpen
      onClose={onClose}
      onSubmit={() => assign.mutate(selected, { onSuccess: onClose })}
      isBusy={assign.isPending}
      error={assign.error ? errorMessage(assign.error) : null}
    >
      <div className="row g-3">
        <MultiSelect
          id="assign-roles"
          label={t('user.form.roles')}
          helpText={t('user.form.rolesHint')}
          options={(roles.data?.items ?? []).map((role) => ({
            value: role.displayName,
            label: role.displayName,
          }))}
          values={selected}
          onChange={setSelected}
        />

        <div className="col-12">
          <p className="mb-1 fw-semibold" style={{ color: 'var(--kt-gray-700)' }}>
            {t('user.detail.roleChangeSummary')}
          </p>
          <p className="mb-0" style={{ color: 'var(--kt-success)' }}>
            {added.length > 0
              ? t('user.detail.rolesAdded', { roles: added.join(', ') })
              : t('user.detail.noRolesAdded')}
          </p>
          <p className="mb-0" style={{ color: 'var(--kt-danger)' }}>
            {removed.length > 0
              ? t('user.detail.rolesRemoved', { roles: removed.join(', ') })
              : t('user.detail.noRolesRemoved')}
          </p>
        </div>
      </div>
    </Modal>
  )
}

/**
 * Administrative password reset.
 *
 * Write-only: the dialog never reads a password back from the server and offers no way to
 * reveal what is typed. Saving rotates the security stamp, which signs the user out everywhere.
 */
function ResetPasswordModal({ userId, onClose }: { userId: number; onClose: () => void }) {
  const { t } = useTranslation()
  const [password, setPassword] = useState('')
  const [repeat, setRepeat] = useState('')
  const [validation, setValidation] = useState<string | null>(null)
  const reset = useResetPassword(userId)

  function submit() {
    if (password.length < 6) {
      setValidation(t('user.form.passwordTooShort'))
      return
    }
    if (password !== repeat) {
      setValidation(t('user.form.passwordMismatch'))
      return
    }
    setValidation(null)
    reset.mutate(password, { onSuccess: onClose })
  }

  return (
    <Modal
      title={t('user.actions.resetPassword')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={reset.isPending}
      error={reset.error ? errorMessage(reset.error) : null}
      confirmLabel={t('user.actions.resetPasswordConfirm')}
    >
      <div className="row g-3">
        <div className="col-12">
          <Alert variant="warning" className="mb-0">
            {t('user.actions.resetPasswordWarning')}
          </Alert>
        </div>

        <Input
          id="reset-password"
          type="password"
          label={t('user.form.newPassword')}
          required
          error={validation ?? undefined}
          className="col-12"
          inputProps={{ autoComplete: 'new-password' }}
          value={password}
          onChange={setPassword}
        />

        <Input
          id="reset-password-repeat"
          type="password"
          label={t('user.form.passwordRepeat')}
          required
          className="col-12"
          inputProps={{ autoComplete: 'new-password' }}
          value={repeat}
          onChange={setRepeat}
        />
      </div>
    </Modal>
  )
}
