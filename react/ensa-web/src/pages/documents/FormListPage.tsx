import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, CheckBox, Input, NumberInput } from 'rich-react-component'
import DataTable, { ErrorPanel, Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
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
  const [categoryId, setCategoryId] = useState<number | null>(null)
  const [editing, setEditing] = useState<FormListDto | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [deleting, setDeleting] = useState<FormListDto | null>(null)

  const { data, isLoading, error } = useFormList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    categoryId: categoryId ?? undefined,
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
          <Badge variant="info">{t('common.yes')}</Badge>
        ) : (
          <span style={{ color: 'var(--kt-gray-500)' }}>{t('common.no')}</span>
        ),
    },
    {
      key: 'isActive',
      header: t('form.fields.status'),
      align: 'center',
      render: (row) => (
        <Badge variant={row.isActive ? 'success' : 'danger'}>
          {row.isActive ? t('common.active') : t('common.passive')}
        </Badge>
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
          <Button variant="light" size="sm"
            disabled={!row.documentId || downloadingId === row.id}
            title={row.documentId ? t('form.list.download') : t('form.list.noDocument')}
            aria-label={row.documentId ? t('form.list.download') : t('form.list.noDocument')}
            onClick={() => row.documentId && download(row.id, row.documentId, row.formName)}
          >
            {downloadingId === row.id ? '…' : '⭳'}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => {
              setEditing(row)
              setIsFormOpen(true)
            }}
            aria-label={t('form.list.editAria', { name: row.formName })}
          >
            {t('common.edit')}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setDeleting(row)}
            aria-label={t('form.list.deleteAria', { name: row.formName })}
          >
            {t('common.delete')}
          </Button>
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
          <Button variant="primary"
            onClick={() => {
              setEditing(null)
              setIsFormOpen(true)
            }}
          >
            {t('form.list.create')}
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
            placeholder={t('form.list.searchPlaceholder')}
          >
            <div style={{ maxWidth: 220 }}>
              <label htmlFor="form-category-filter" className="visually-hidden">
                {t('form.filters.categoryId')}
              </label>
              <NumberInput
                id="form-category-filter"
                min={1}
                placeholder={t('form.filters.categoryPlaceholder')}
                value={categoryId}
                onChange={(value) => {
                  setCategoryId(value)
                  setPage(1)
                }}
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
          label={t('form.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('form.list.empty')}
        />
      </Card>

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
  categoryId: number | null
  documentId: number | null
  defaultForm: boolean
  isActive: boolean
}

const EMPTY: EditorState = {
  formName: '',
  categoryId: null,
  documentId: null,
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
            categoryId: form.categoryId,
            documentId: form.documentId ?? null,
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

    if (state.categoryId === null || state.categoryId < 1) {
      nextErrors.categoryId = t('form.editor.categoryRequired')
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SaveFormDto = {
      formName: state.formName.trim(),
      categoryId: Math.trunc(state.categoryId!),
      documentId: state.documentId !== null ? Math.trunc(state.documentId) : null,
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
        <Input
          id="form-name"
          label={t('form.fields.formName')}
          required
          error={errors.formName}
          className="col-12"
          value={state.formName}
          onChange={(value) => setState((s) => ({ ...s, formName: value }))}
        />

        <NumberInput
          id="form-category"
          label={t('form.fields.categoryId')}
          required
          error={errors.categoryId}
          helpText={t('form.editor.categoryHint')}
          className="col-md-6"
          min={1}
          value={state.categoryId}
          onChange={(value) => setState((s) => ({ ...s, categoryId: value }))}
        />

        <NumberInput
          id="form-document"
          label={t('form.fields.documentId')}
          helpText={t('form.editor.documentHint')}
          className="col-md-6"
          min={1}
          value={state.documentId}
          onChange={(value) => setState((s) => ({ ...s, documentId: value }))}
        />

        <div className="col-md-6">
          <CheckBox
            id="form-default"
            label={t('form.fields.defaultForm')}
            checked={state.defaultForm}
            onChange={(checked) => setState((s) => ({ ...s, defaultForm: checked }))}
          />
        </div>

        <div className="col-md-6">
          <CheckBox
            id="form-active"
            label={t('common.active')}
            checked={state.isActive}
            onChange={(checked) => setState((s) => ({ ...s, isActive: checked }))}
          />
        </div>
      </div>
    </Modal>
  )
}
