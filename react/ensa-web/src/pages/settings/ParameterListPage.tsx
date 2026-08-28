import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, CheckBox, Input, TextArea } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import {
  SETTINGS_RESOURCES,
  useParameterList,
  type CreateParameterInput,
  type ParameterListDto,
  type UpdateParameterInput,
} from './api'

const PAGE_SIZE = 20

/**
 * System parameter administration.
 *
 * The value is edited in place, because a parameter row is a single short string and opening a
 * dialog for it costs more than the edit itself. The row only offers Save once the text
 * actually differs from what the server returned, so an accidental keystroke cannot be
 * committed by clicking elsewhere. The code is not editable at all: application code reads
 * parameters by code, so renaming one would silently change behaviour somewhere else.
 */
export default function ParameterListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [drafts, setDrafts] = useState<Record<number, string>>({})
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editing, setEditing] = useState<ParameterListDto | null>(null)
  const [pendingDelete, setPendingDelete] = useState<ParameterListDto | null>(null)

  const { data, isLoading, error } = useParameterList({
    page,
    pageSize: PAGE_SIZE,
    filter: search,
  })

  const update = useUpdate<UpdateParameterInput>(SETTINGS_RESOURCES.parameter)
  const remove = useDelete(SETTINGS_RESOURCES.parameter, {
    onSuccess: () => setPendingDelete(null),
  })

  function draftOf(parameter: ParameterListDto) {
    return drafts[parameter.id] ?? parameter.value
  }

  function setDraft(id: number, value: string) {
    setDrafts((previous) => ({ ...previous, [id]: value }))
  }

  function revert(id: number) {
    setDrafts((previous) => {
      const next = { ...previous }
      delete next[id]
      return next
    })
  }

  function saveValue(parameter: ParameterListDto) {
    update.mutate(
      {
        id: parameter.id,
        input: {
          name: parameter.name,
          value: draftOf(parameter),
          isActive: parameter.isActive,
        },
      },
      { onSuccess: () => revert(parameter.id) },
    )
  }

  const columns: Column<ParameterListDto>[] = [
    {
      key: 'code',
      header: t('parameter.fields.code'),
      render: (parameter) => (
        <code style={{ color: 'var(--kt-gray-800)' }}>{parameter.code}</code>
      ),
    },
    {
      key: 'name',
      header: t('parameter.fields.name'),
      render: (parameter) => <span className="fw-semibold">{parameter.name}</span>,
    },
    {
      key: 'value',
      header: t('parameter.fields.value'),
      width: '40%',
      render: (parameter) => {
        const draft = draftOf(parameter)
        const isDirty = draft !== parameter.value

        return (
          <div className="d-flex align-items-center gap-2">
            <label htmlFor={`parameter-value-${parameter.id}`} className="visually-hidden">
              {t('parameter.actions.valueLabel', { name: parameter.name })}
            </label>
            <input
              id={`parameter-value-${parameter.id}`}
              className="form-control form-control-sm"
              value={draft}
              onChange={(event) => setDraft(parameter.id, event.target.value)}
            />
            {isDirty && (
              <>
                <Button variant="light" size="sm" 
                  disabled={update.isPending}
                  onClick={() => saveValue(parameter)}
                  aria-label={t('parameter.actions.saveNamed', { name: parameter.name })}
                  title={t('common.save')}
                >
                  ✓
                </Button>
                <Button variant="light" size="sm"
                  onClick={() => revert(parameter.id)}
                  aria-label={t('parameter.actions.revertNamed', { name: parameter.name })}
                  title={t('parameter.actions.revert')}
                >
                  ↺
                </Button>
              </>
            )}
          </div>
        )
      },
    },
    {
      key: 'status',
      header: t('parameter.fields.status'),
      align: 'center',
      render: (parameter) => (
        <Badge variant={parameter.isActive ? 'success' : 'danger'}>
          {parameter.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (parameter) => (
        <div className="d-inline-flex gap-1">
          <Button variant="light" size="sm" 
            onClick={() => setEditing(parameter)}
            aria-label={t('parameter.actions.editNamed', { name: parameter.name })}
            title={t('common.edit')}
          >
            ✎
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setPendingDelete(parameter)}
            aria-label={t('parameter.actions.deleteNamed', { name: parameter.name })}
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
        title={t('parameter.list.title')}
        description={t('parameter.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('parameter.list.create')}
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
            placeholder={t('parameter.list.searchPlaceholder')}
          />
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
        {update.error && (
          <Alert variant="danger" className="m-4">
            {errorMessage(update.error)}
          </Alert>
        )}
        <DataTable
          label={t('parameter.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(parameter) => parameter.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('parameter.list.empty')}
        />
      </Card>

      {isCreateOpen && <ParameterFormModal onClose={() => setCreateOpen(false)} />}
      {editing && (
        <ParameterFormModal parameter={editing} onClose={() => setEditing(null)} />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('parameter.actions.deleteTitle')}
        message={t('parameter.actions.deleteMessage', { code: pendingDelete?.code ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}

/** Create / edit dialog. The code is write-once: it is the key application code reads by. */
function ParameterFormModal({
  parameter,
  onClose,
}: {
  parameter?: ParameterListDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const isEdit = !!parameter

  const [code, setCode] = useState(parameter?.code ?? '')
  const [name, setName] = useState(parameter?.name ?? '')
  const [value, setValue] = useState(parameter?.value ?? '')
  const [isActive, setActive] = useState(parameter?.isActive ?? true)
  const [errors, setErrors] = useState<Record<string, string>>({})

  const create = useCreate<CreateParameterInput>(SETTINGS_RESOURCES.parameter, {
    onSuccess: onClose,
  })
  const update = useUpdate<UpdateParameterInput>(SETTINGS_RESOURCES.parameter, {
    onSuccess: onClose,
  })
  const pending = isEdit ? update : create

  function submit() {
    const next: Record<string, string> = {}
    if (!isEdit && !code.trim()) next.code = t('validation.required')
    if (!name.trim()) next.name = t('validation.required')
    if (!value.trim()) next.value = t('validation.required')

    setErrors(next)
    if (Object.keys(next).length > 0) return

    if (isEdit && parameter) {
      update.mutate({
        id: parameter.id,
        input: { name: name.trim(), value, isActive },
      })
      return
    }

    create.mutate({ code: code.trim(), name: name.trim(), value, isActive })
  }

  return (
    <Modal
      title={isEdit ? t('parameter.form.editTitle') : t('parameter.form.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending.isPending}
      error={pending.error ? errorMessage(pending.error) : null}
    >
      <div className="row g-3">
        <Input
          id="parameter-code"
          label={t('parameter.fields.code')}
          required={!isEdit}
          error={errors.code}
          helpText={t('parameter.form.codeHint')}
          className="col-12"
          value={code}
          disabled={isEdit}
          onChange={setCode}
        />

        <Input
          id="parameter-name"
          label={t('parameter.fields.name')}
          required
          error={errors.name}
          className="col-12"
          value={name}
          onChange={setName}
        />

        <TextArea
          id="parameter-value"
          label={t('parameter.fields.value')}
          required
          error={errors.value}
          className="col-12"
          rows={3}
          value={value}
          onChange={setValue}
        />

        <div className="col-12">
          <CheckBox
            id="parameter-isActive"
            checked={isActive}
            onChange={setActive}
            label={t('common.active')}
          />
        </div>
      </div>
    </Modal>
  )
}
