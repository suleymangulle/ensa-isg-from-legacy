import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { downloadFile } from '@/api/download'
import {
  documentContentPath,
  FORM,
  useFormList,
  type FormListDto,
  type SaveFormDto,
} from './api'

const PAGE_SIZE = 20

/**
 * Form and template register — the legacy `form_ekle.aspx` / `form_sildegistir.aspx` pair,
 * merged into one screen because a separate "add" page and "list" page for six fields was a
 * WebForms postback artefact, not a workflow.
 *
 * The file behind a form lives in the central document store, referenced by `documentId`. There
 * is no upload route yet, so the field takes the id of an already registered document.
 */
export default function FormListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [editing, setEditing] = useState<FormListDto | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [deleting, setDeleting] = useState<FormListDto | null>(null)

  const { data, isLoading, error } = useFormList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    categoryId: categoryId === '' ? undefined : Number(categoryId),
  })

  const remove = useDelete(FORM, { onSuccess: () => setDeleting(null) })

  const [downloadingId, setDownloadingId] = useState<number | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  async function download(rowId: number, documentId: number, name: string) {
    setDownloadingId(rowId)
    setDownloadError(null)
    try {
      await downloadFile(documentContentPath(documentId), name)
    } catch (cause) {
      setDownloadError(errorMessage(cause))
    } finally {
      setDownloadingId(null)
    }
  }

  const columns: Column<FormListDto>[] = [
    {
      key: 'formName',
      header: t('form.fields.formName'),
      render: (row) => <span className="fw-semibold">{row.formName}</span>,
    },
    {
      key: 'categoryId',
      header: t('form.fields.categoryId'),
      align: 'end',
      render: (row) => row.categoryId,
    },
    {
      key: 'documentId',
      header: t('form.fields.documentId'),
      align: 'end',
      render: (row) => row.documentId ?? t('common.none'),
    },
    {
      key: 'defaultForm',
      header: t('form.fields.defaultForm'),
      align: 'center',
      render: (row) =>
        row.defaultForm ? (
          <span className="badge-light-info">{t('common.yes')}</span>
        ) : (
          <span style={{ color: 'var(--kt-gray-500)' }}>{t('common.no')}</span>
        ),
    },
    {
      key: 'isActive',
      header: t('form.fields.status'),
      align: 'center',
      render: (row) => (
        <span className={row.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {row.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '220px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-2">
          {/*
            A form template is a Document row behind a `documentId`, so the download is the
            document content route. A template with no file attached has nothing to download.
          */}
          <button
            type="button"
            className="btn btn-sm btn-light"
            disabled={!row.documentId || downloadingId === row.id}
            title={row.documentId ? t('form.list.download') : t('form.list.noDocument')}
            aria-label={row.documentId ? t('form.list.download') : t('form.list.noDocument')}
            onClick={() => row.documentId && download(row.id, row.documentId, row.formName)}
          >
            {downloadingId === row.id ? '…' : '⭳'}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => {
              setEditing(row)
              setIsFormOpen(true)
            }}
            aria-label={t('form.list.editAria', { name: row.formName })}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setDeleting(row)}
            aria-label={t('form.list.deleteAria', { name: row.formName })}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      {downloadError && <ErrorPanel message={downloadError} />}

      <PageTitle
        title={t('form.list.title')}
        description={t('form.list.description')}
        action={
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => {
              setEditing(null)
              setIsFormOpen(true)
            }}
          >
            {t('form.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header pt-4 pb-0 border-0">
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('form.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="form-category-filter" className="visually-hidden">
                {t('form.filters.categoryId')}
              </label>
              <input
                id="form-category-filter"
                type="number"
                min={1}
                className="form-control"
                style={{ maxWidth: 220 }}
                placeholder={t('form.filters.categoryPlaceholder')}
                value={categoryId}
                onChange={(event) => {
                  setCategoryId(event.target.value)
                  setPage(1)
                }}
              />
            </div>
          </SearchBar>
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('form.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(row) => row.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('form.list.empty')}
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

      <FormEditor
        isOpen={isFormOpen}
        form={editing}
        onClose={() => {
          setIsFormOpen(false)
          setEditing(null)
        }}
      />

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('form.list.deleteTitle')}
        message={t('form.list.deleteMessage', { name: deleting?.formName ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

interface EditorState {
  formName: string
  categoryId: string
  documentId: string
  defaultForm: boolean
  isActive: boolean
}

const EMPTY: EditorState = {
  formName: '',
  categoryId: '',
  documentId: '',
  defaultForm: false,
  isActive: true,
}

function FormEditor({
  isOpen,
  form,
  onClose,
}: {
  isOpen: boolean
  form: FormListDto | null
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [state, setState] = useState<EditorState>(EMPTY)
  const [errors, setErrors] = useState<Partial<Record<keyof EditorState, string>>>({})

  useEffect(() => {
    if (!isOpen) return
    setErrors({})
    setState(
      form
        ? {
            formName: form.formName,
            categoryId: form.categoryId.toString(),
            documentId: form.documentId?.toString() ?? '',
            defaultForm: form.defaultForm,
            isActive: form.isActive,
          }
        : EMPTY,
    )
  }, [isOpen, form])

  const create = useCreate<SaveFormDto>(FORM, { onSuccess: onClose })
  const update = useUpdate<SaveFormDto>(FORM, { onSuccess: onClose })
  const mutation = form ? update : create

  function submit() {
    const nextErrors: Partial<Record<keyof EditorState, string>> = {}
    if (!state.formName.trim()) nextErrors.formName = t('validation.required')

    const category = Number(state.categoryId)
    if (!state.categoryId.trim() || !Number.isFinite(category) || category < 1) {
      nextErrors.categoryId = t('form.editor.categoryRequired')
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const documentId = state.documentId.trim() ? Number(state.documentId) : null

    const input: SaveFormDto = {
      formName: state.formName.trim(),
      categoryId: Math.trunc(category),
      documentId: documentId && Number.isFinite(documentId) ? Math.trunc(documentId) : null,
      defaultForm: state.defaultForm,
      isActive: state.isActive,
    }

    if (form) update.mutate({ id: form.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={form ? t('form.editor.editTitle') : t('form.editor.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
    >
      <div className="row g-3">
        <Field
          label={t('form.fields.formName')}
          htmlFor="form-name"
          required
          error={errors.formName}
        >
          <input
            id="form-name"
            type="text"
            className={controlClass('form-control', errors.formName)}
            value={state.formName}
            onChange={(event) => setState((s) => ({ ...s, formName: event.target.value }))}
          />
        </Field>

        <Field
          label={t('form.fields.categoryId')}
          htmlFor="form-category"
          required
          error={errors.categoryId}
          hint={t('form.editor.categoryHint')}
          className="col-md-6"
        >
          <input
            id="form-category"
            type="number"
            min={1}
            className={controlClass('form-control', errors.categoryId)}
            value={state.categoryId}
            onChange={(event) => setState((s) => ({ ...s, categoryId: event.target.value }))}
          />
        </Field>

        <Field
          label={t('form.fields.documentId')}
          htmlFor="form-document"
          hint={t('form.editor.documentHint')}
          className="col-md-6"
        >
          <input
            id="form-document"
            type="number"
            min={1}
            className="form-control"
            value={state.documentId}
            onChange={(event) => setState((s) => ({ ...s, documentId: event.target.value }))}
          />
        </Field>

        <div className="col-md-6">
          <div className="form-check">
            <input
              id="form-default"
              type="checkbox"
              className="form-check-input"
              checked={state.defaultForm}
              onChange={(event) => setState((s) => ({ ...s, defaultForm: event.target.checked }))}
            />
            <label htmlFor="form-default" className="form-check-label">
              {t('form.fields.defaultForm')}
            </label>
          </div>
        </div>

        <div className="col-md-6">
          <div className="form-check">
            <input
              id="form-active"
              type="checkbox"
              className="form-check-input"
              checked={state.isActive}
              onChange={(event) => setState((s) => ({ ...s, isActive: event.target.checked }))}
            />
            <label htmlFor="form-active" className="form-check-label">
              {t('common.active')}
            </label>
          </div>
        </div>
      </div>
    </Modal>
  )
}
