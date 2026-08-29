import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, CheckBox, Input, TextArea } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { MEMBERSHIP_RESOURCES, useRoleList, type RoleInput, type RoleListDto } from './api'

const PAGE_SIZE = 20

export default function RoleListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<RoleListDto | null>(null)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<RoleListDto | null>(null)

  const { data, isLoading, error } = useRoleList({ page, pageSize: PAGE_SIZE, filter: search })
  const remove = useDelete(MEMBERSHIP_RESOURCES.role, { onSuccess: () => setPendingDelete(null) })

  const columns: Column<RoleListDto>[] = [
    {
      key: 'name',
      header: t('role.fields.name'),
      render: (role) => <span className="fw-semibold">{role.name}</span>,
    },
    {
      key: 'description',
      header: t('role.fields.description'),
      render: (role) => role.description ?? t('common.none'),
    },
    {
      key: 'scope',
      header: t('role.fields.scope'),
      render: (role) =>
        role.tenantId == null ? t('role.scope.host') : t('role.scope.organization'),
    },
    {
      key: 'isDefault',
      header: t('role.fields.isDefault'),
      align: 'center',
      render: (role) =>
        role.isDefault ? (
          <Badge variant="primary">{t('common.yes')}</Badge>
        ) : (
          t('common.no')
        ),
    },
    {
      key: 'isStatic',
      header: t('role.fields.isStatic'),
      align: 'center',
      render: (role) =>
        role.isStatic ? (
          <Badge variant="warning">{t('role.badges.system')}</Badge>
        ) : (
          t('common.no')
        ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (role) => (
        <div className="d-inline-flex gap-1">
          <Button variant="light" size="sm" 
            onClick={() => setEditing(role)}
            aria-label={t('role.actions.editNamed', { name: role.name })}
            title={t('common.edit')}
          >
            ✎
          </Button>
          <Button variant="light" size="sm" 
            disabled={role.isStatic}
            onClick={() => setPendingDelete(role)}
            aria-label={t('role.actions.deleteNamed', { name: role.name })}
            title={role.isStatic ? t('role.actions.systemRoleLocked') : t('common.delete')}
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
        title={t('role.list.title')}
        description={t('role.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('role.list.create')}
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
            placeholder={t('role.list.searchPlaceholder')}
          />
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
          label={t('role.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(role) => role.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('role.list.empty')}
        />
      </Card>

      {isCreateOpen && <RoleFormModal onClose={() => setCreateOpen(false)} />}
      {editing && <RoleFormModal role={editing} onClose={() => setEditing(null)} />}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('role.actions.deleteTitle')}
        message={t('role.actions.deleteMessage', { name: pendingDelete?.name ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}

/**
 * Create / edit dialog for a role.
 *
 * A static (system) role keeps its name: the service refuses a rename with
 * `Ensa:Role:SystemRoleImmutable`, so the field is disabled rather than left to fail on save.
 */
function RoleFormModal({ role, onClose }: { role?: RoleListDto; onClose: () => void }) {
  const { t } = useTranslation()
  const isEdit = !!role

  const [name, setName] = useState(role?.name ?? '')
  const [description, setDescription] = useState(role?.description ?? '')
  const [isDefault, setDefault] = useState(role?.isDefault ?? false)
  const [nameError, setNameError] = useState<string | undefined>()

  const create = useCreate<RoleInput>(MEMBERSHIP_RESOURCES.role, { onSuccess: onClose })
  const update = useUpdate<RoleInput>(MEMBERSHIP_RESOURCES.role, { onSuccess: onClose })
  const pending = isEdit ? update : create

  function submit() {
    if (!name.trim()) {
      setNameError(t('validation.required'))
      return
    }
    setNameError(undefined)

    const input: RoleInput = {
      name: name.trim(),
      description: description.trim() === '' ? null : description.trim(),
      isDefault,
    }

    if (isEdit && role) update.mutate({ id: role.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={isEdit ? t('role.form.editTitle') : t('role.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending.isPending}
      error={pending.error ? errorMessage(pending.error) : null}
    >
      <div className="row g-3">
        <Input
          id="role-name"
          label={t('role.fields.name')}
          required
          error={nameError}
          helpText={role?.isStatic ? t('role.form.staticNameHint') : undefined}
          value={name}
          disabled={role?.isStatic}
          onChange={setName}
          className="col-12"
        />

        <TextArea
          id="role-description"
          label={t('role.fields.description')}
          rows={3}
          value={description}
          onChange={setDescription}
          className="col-12"
        />

        <div className="col-12">
          <CheckBox
            id="role-isDefault"
            checked={isDefault}
            onChange={setDefault}
            label={t('role.fields.isDefault')}
            helpText={t('role.form.isDefaultHint')}
          />
        </div>

        <div className="col-12">
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
            {t('role.form.permissionNote')}
          </p>
        </div>
      </div>
    </Modal>
  )
}
