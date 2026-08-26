import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
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
                <button
                  type="button"
                  className="btn btn-sm btn-light-success"
                  disabled={update.isPending}
                  onClick={() => saveValue(parameter)}
                  aria-label={t('parameter.actions.saveNamed', { name: parameter.name })}
                  title={t('common.save')}
                >
                  ✓
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-light"
                  onClick={() => revert(parameter.id)}
                  aria-label={t('parameter.actions.revertNamed', { name: parameter.name })}
                  title={t('parameter.actions.revert')}
                >
                  ↺
                </button>
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
        <span className={parameter.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {parameter.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '140px',
      render: (parameter) => (
        <div className="d-inline-flex gap-1">
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => setEditing(parameter)}
            aria-label={t('parameter.actions.editNamed', { name: parameter.name })}
            title={t('common.edit')}
          >
            ✎
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setPendingDelete(parameter)}
            aria-label={t('parameter.actions.deleteNamed', { name: parameter.name })}
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
        title={t('parameter.list.title')}
        description={t('parameter.list.description')}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setCreateOpen(true)}>
            {t('parameter.list.create')}
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
            placeholder={t('parameter.list.searchPlaceholder')}
          />
        </div>

        <div className="card-body p-0">
          {update.error && (
            <div
              className="alert border-0 m-4"
              style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
              role="alert"
            >
              {errorMessage(update.error)}
            </div>
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
        <Field
          label={t('parameter.fields.code')}
          htmlFor="parameter-code"
          required={!isEdit}
          error={errors.code}
          hint={t('parameter.form.codeHint')}
        >
          <input
            id="parameter-code"
            className={controlClass('form-control', errors.code)}
            value={code}
            disabled={isEdit}
            onChange={(event) => setCode(event.target.value)}
          />
        </Field>

        <Field
          label={t('parameter.fields.name')}
          htmlFor="parameter-name"
          required
          error={errors.name}
        >
          <input
            id="parameter-name"
            className={controlClass('form-control', errors.name)}
            value={name}
            onChange={(event) => setName(event.target.value)}
          />
        </Field>

        <Field
          label={t('parameter.fields.value')}
          htmlFor="parameter-value"
          required
          error={errors.value}
        >
          <textarea
            id="parameter-value"
            className={controlClass('form-control', errors.value)}
            rows={3}
            value={value}
            onChange={(event) => setValue(event.target.value)}
          />
        </Field>

        <div className="col-12">
          <div className="form-check">
            <input
              id="parameter-isActive"
              type="checkbox"
              className="form-check-input"
              checked={isActive}
              onChange={(event) => setActive(event.target.checked)}
            />
            <label className="form-check-label" htmlFor="parameter-isActive">
              {t('common.active')}
            </label>
          </div>
        </div>
      </div>
    </Modal>
  )
}
