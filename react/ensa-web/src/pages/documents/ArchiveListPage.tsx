import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, SearchBar, controlClass } from '@/components/Form'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { useEntity } from '@/api/endpoints'
import { DocumentOwnerType } from '@/api/enums'
import { formatDate } from '@/utils/format'
import {
  ARCHIVE,
  useArchiveByModule,
  useArchiveList,
  useCompanyLookup,
  type ArchiveDto,
  type ArchiveListDto,
  type SaveArchiveDto,
} from './api'
import { MONTHS, OWNER_TYPES, recentYears } from './helpers'

const PAGE_SIZE = 20

const TABS = ['all', 'byModule'] as const
type TabKey = (typeof TABS)[number]

/**
 * Module archive — the legacy `modul_arsivi.aspx`.
 *
 * Two views over the same data: the paged register, and the by-module lookup that answers
 * "what has been filed against this record?". The second one is a route with two mandatory path
 * segments (`by-module/{moduleType}/{moduleId}`), so the screen collects both before it fires —
 * a request with a missing record id can only ever come back 400.
 */
export default function ArchiveListPage() {
  const { t } = useTranslation()
  const [activeTab, setActiveTab] = useState<TabKey>('all')

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [moduleType, setModuleType] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [year, setYear] = useState('')
  const [month, setMonth] = useState('')

  const [editingId, setEditingId] = useState<number | undefined>()
  const [isEditorOpen, setIsEditorOpen] = useState(false)
  const [deleting, setDeleting] = useState<ArchiveListDto | null>(null)

  const companies = useCompanyLookup()

  /** One batched request feeds every company cell; the table never asks per row. */
  const companyNames = useMemo(() => {
    const map = new Map<number, string>()
    for (const company of companies.data?.items ?? []) map.set(company.id, company.displayName)
    return map
  }, [companies.data])

  const list = useArchiveList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    filter: search || undefined,
    moduleType: moduleType === '' ? undefined : (Number(moduleType) as DocumentOwnerType),
    companyId: companyId === '' ? undefined : Number(companyId),
    year: year === '' ? undefined : Number(year),
    month: month === '' ? undefined : Number(month),
  })

  const editing = useEntity<ArchiveDto>(ARCHIVE, editingId)
  const remove = useDelete(ARCHIVE, { onSuccess: () => setDeleting(null) })

  function companyLabel(id: number) {
    return companyNames.get(id) ?? t('archive.list.companyFallback', { id })
  }

  const columns: Column<ArchiveListDto>[] = [
    {
      key: 'moduleType',
      header: t('archive.fields.moduleType'),
      render: (row) => (
        <span className="badge-light-primary">{t(`enums.documentOwnerType.${row.moduleType}`)}</span>
      ),
    },
    { key: 'moduleId', header: t('archive.fields.moduleId'), align: 'end', render: (row) => row.moduleId },
    {
      key: 'companyId',
      header: t('archive.fields.company'),
      render: (row) => companyLabel(row.companyId),
    },
    {
      key: 'documentId',
      header: t('archive.fields.documentId'),
      align: 'end',
      render: (row) => row.documentId,
    },
    {
      key: 'period',
      header: t('archive.fields.period'),
      render: (row) =>
        row.month || row.year
          ? [row.month ? t(`enums.month.${row.month}`) : null, row.year].filter(Boolean).join(' ')
          : t('common.none'),
    },
    {
      key: 'description',
      header: t('archive.fields.description'),
      render: (row) => row.description ?? t('common.none'),
    },
    {
      key: 'creationTime',
      header: t('archive.fields.creationTime'),
      render: (row) => formatDate(row.creationTime) ?? t('common.none'),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '180px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-2">
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => {
              setEditingId(row.id)
              setIsEditorOpen(true)
            }}
            aria-label={t('archive.list.editAria', { id: row.id })}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setDeleting(row)}
            aria-label={t('archive.list.deleteAria', { id: row.id })}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('archive.list.title')}
        description={t('archive.list.description')}
        action={
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => {
              setEditingId(undefined)
              setIsEditorOpen(true)
            }}
          >
            {t('archive.list.create')}
          </button>
        }
      />

      <div className="card">
        <div className="card-header p-0 px-4">
          <ul className="nav nav-tabs border-0" role="tablist">
            {TABS.map((tab) => (
              <li className="nav-item" key={tab} role="presentation">
                <button
                  type="button"
                  role="tab"
                  aria-selected={activeTab === tab}
                  className={`nav-link border-0 px-3 py-3 ${activeTab === tab ? 'active fw-semibold' : ''}`}
                  style={{
                    color: activeTab === tab ? 'var(--kt-primary)' : 'var(--kt-gray-600)',
                    borderBottom: `2px solid ${activeTab === tab ? 'var(--kt-primary)' : 'transparent'}`,
                    backgroundColor: 'transparent',
                  }}
                  onClick={() => setActiveTab(tab)}
                >
                  {t(`archive.tabs.${tab}`)}
                </button>
              </li>
            ))}
          </ul>
        </div>

        {activeTab === 'all' ? (
          <>
            <div className="card-body pb-0">
              <SearchBar
                value={search}
                onChange={(value) => {
                  setSearch(value)
                  setPage(1)
                }}
                placeholder={t('archive.list.searchPlaceholder')}
              >
                <div>
                  <label htmlFor="archive-module-filter" className="visually-hidden">
                    {t('archive.filters.moduleType')}
                  </label>
                  <select
                    id="archive-module-filter"
                    className="form-select"
                    style={{ minWidth: 190 }}
                    value={moduleType}
                    onChange={(event) => {
                      setModuleType(event.target.value)
                      setPage(1)
                    }}
                  >
                    <option value="">{t('archive.filters.allModules')}</option>
                    {OWNER_TYPES.map((value) => (
                      <option key={value} value={value}>
                        {t(`enums.documentOwnerType.${value}`)}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="archive-company-filter" className="visually-hidden">
                    {t('archive.filters.company')}
                  </label>
                  <select
                    id="archive-company-filter"
                    className="form-select"
                    style={{ minWidth: 190 }}
                    value={companyId}
                    onChange={(event) => {
                      setCompanyId(event.target.value)
                      setPage(1)
                    }}
                  >
                    <option value="">{t('archive.filters.allCompanies')}</option>
                    {companies.data?.items.map((company) => (
                      <option key={company.id} value={company.id}>
                        {company.displayName}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="archive-year-filter" className="visually-hidden">
                    {t('archive.filters.year')}
                  </label>
                  <select
                    id="archive-year-filter"
                    className="form-select"
                    style={{ minWidth: 130 }}
                    value={year}
                    onChange={(event) => {
                      setYear(event.target.value)
                      setPage(1)
                    }}
                  >
                    <option value="">{t('archive.filters.allYears')}</option>
                    {recentYears().map((value) => (
                      <option key={value} value={value}>
                        {value}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label htmlFor="archive-month-filter" className="visually-hidden">
                    {t('archive.filters.month')}
                  </label>
                  <select
                    id="archive-month-filter"
                    className="form-select"
                    style={{ minWidth: 150 }}
                    value={month}
                    onChange={(event) => {
                      setMonth(event.target.value)
                      setPage(1)
                    }}
                  >
                    <option value="">{t('archive.filters.allMonths')}</option>
                    {MONTHS.map((value) => (
                      <option key={value} value={value}>
                        {t(`enums.month.${value}`)}
                      </option>
                    ))}
                  </select>
                </div>
              </SearchBar>
            </div>

            <div className="card-body p-0">
              <DataTable
                label={t('archive.list.title')}
                columns={columns}
                rows={list.data?.items}
                rowKey={(row) => row.id}
                isLoading={list.isLoading}
                error={list.error ? errorMessage(list.error) : null}
                emptyMessage={t('archive.list.empty')}
              />
            </div>

            {list.data && list.data.totalCount > 0 && (
              <div className="card-footer bg-transparent border-0 pt-0">
                <Pagination
                  total={list.data.totalCount}
                  page={page}
                  pageSize={PAGE_SIZE}
                  onPageChange={setPage}
                />
              </div>
            )}
          </>
        ) : (
          <ByModulePanel columns={columns} />
        )}
      </div>

      {isEditorOpen && (!editingId || editing.data) && (
        <ArchiveEditor
          isOpen
          archive={editingId ? editing.data : undefined}
          onClose={() => {
            setIsEditorOpen(false)
            setEditingId(undefined)
          }}
        />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('archive.list.deleteTitle')}
        message={t('archive.list.deleteMessage', { id: deleting?.id ?? 0 })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/**
 * `GET api/archive/by-module/{moduleType}/{moduleId}` — both segments are part of the path, so
 * the panel gathers a module type *and* a record id before it asks anything.
 */
function ByModulePanel({ columns }: { columns: Column<ArchiveListDto>[] }) {
  const { t } = useTranslation()

  const [moduleType, setModuleType] = useState<DocumentOwnerType>(DocumentOwnerType.Company)
  const [moduleId, setModuleId] = useState('')
  const [month, setMonth] = useState('')
  const [year, setYear] = useState('')
  const [query, setQuery] = useState<{
    moduleType?: DocumentOwnerType
    moduleId?: number
    month?: number
    year?: number
  }>({})

  const result = useArchiveByModule(query.moduleType, query.moduleId, query.month, query.year)
  const isReady = !!moduleId.trim() && Number(moduleId) > 0

  return (
    <>
      <div className="card-body pb-0">
        <form
          className="row g-3 align-items-end"
          onSubmit={(event) => {
            event.preventDefault()
            if (!isReady) return
            setQuery({
              moduleType,
              moduleId: Math.trunc(Number(moduleId)),
              month: month === '' ? undefined : Number(month),
              year: year === '' ? undefined : Number(year),
            })
          }}
        >
          <Field
            label={t('archive.fields.moduleType')}
            htmlFor="by-module-type"
            required
            className="col-md-3"
          >
            <select
              id="by-module-type"
              className="form-select"
              value={moduleType}
              onChange={(event) => setModuleType(Number(event.target.value))}
            >
              {OWNER_TYPES.map((value) => (
                <option key={value} value={value}>
                  {t(`enums.documentOwnerType.${value}`)}
                </option>
              ))}
            </select>
          </Field>

          <Field
            label={t('archive.fields.moduleId')}
            htmlFor="by-module-id"
            required
            hint={t('archive.byModule.recordHint')}
            className="col-md-3"
          >
            <input
              id="by-module-id"
              type="number"
              min={1}
              className="form-control"
              value={moduleId}
              onChange={(event) => setModuleId(event.target.value)}
            />
          </Field>

          <Field label={t('archive.filters.year')} htmlFor="by-module-year" className="col-md-2">
            <select
              id="by-module-year"
              className="form-select"
              value={year}
              onChange={(event) => setYear(event.target.value)}
            >
              <option value="">{t('archive.filters.allYears')}</option>
              {recentYears().map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </select>
          </Field>

          <Field label={t('archive.filters.month')} htmlFor="by-module-month" className="col-md-2">
            <select
              id="by-module-month"
              className="form-select"
              value={month}
              onChange={(event) => setMonth(event.target.value)}
            >
              <option value="">{t('archive.filters.allMonths')}</option>
              {MONTHS.map((value) => (
                <option key={value} value={value}>
                  {t(`enums.month.${value}`)}
                </option>
              ))}
            </select>
          </Field>

          <div className="col-md-2">
            <button type="submit" className="btn btn-primary w-100" disabled={!isReady}>
              {t('archive.byModule.show')}
            </button>
          </div>
        </form>
      </div>

      <div className="card-body p-0 pt-4">
        {query.moduleId ? (
          <DataTable
            label={t('archive.byModule.tableLabel')}
            columns={columns}
            rows={result.data?.items}
            rowKey={(row) => row.id}
            isLoading={result.isLoading}
            error={result.error ? errorMessage(result.error) : null}
            emptyMessage={t('archive.byModule.empty')}
          />
        ) : (
          <p className="text-center py-5 mb-0" style={{ color: 'var(--kt-gray-500)' }}>
            {t('archive.byModule.prompt')}
          </p>
        )}
      </div>
    </>
  )
}

interface EditorState {
  moduleType: DocumentOwnerType
  moduleId: string
  documentId: string
  companyId: string
  lineId: string
  month: string
  year: string
  description: string
  moduleDescription: string
}

const EMPTY_EDITOR: EditorState = {
  moduleType: DocumentOwnerType.Company,
  moduleId: '',
  documentId: '',
  companyId: '',
  lineId: '',
  month: '',
  year: '',
  description: '',
  moduleDescription: '',
}

function ArchiveEditor({
  isOpen,
  archive,
  onClose,
}: {
  isOpen: boolean
  archive?: ArchiveDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [state, setState] = useState<EditorState>(EMPTY_EDITOR)
  const [errors, setErrors] = useState<Partial<Record<keyof EditorState, string>>>({})
  const companies = useCompanyLookup()

  useEffect(() => {
    if (!isOpen) return
    setErrors({})
    setState(
      archive
        ? {
            moduleType: archive.moduleType,
            moduleId: archive.moduleId.toString(),
            documentId: archive.documentId.toString(),
            companyId: archive.companyId.toString(),
            lineId: archive.lineId?.toString() ?? '',
            month: archive.month?.toString() ?? '',
            year: archive.year?.toString() ?? '',
            description: archive.description ?? '',
            moduleDescription: archive.moduleDescription ?? '',
          }
        : EMPTY_EDITOR,
    )
  }, [isOpen, archive])

  const create = useCreate<SaveArchiveDto>(ARCHIVE, { onSuccess: onClose })
  const update = useUpdate<SaveArchiveDto>(ARCHIVE, { onSuccess: onClose })
  const mutation = archive ? update : create

  function positive(value: string) {
    const parsed = Number(value)
    return value.trim() && Number.isFinite(parsed) && parsed >= 1 ? Math.trunc(parsed) : null
  }

  function submit() {
    const nextErrors: Partial<Record<keyof EditorState, string>> = {}
    const moduleId = positive(state.moduleId)
    const documentId = positive(state.documentId)
    const companyId = positive(state.companyId)

    if (!moduleId) nextErrors.moduleId = t('archive.editor.moduleIdRequired')
    if (!documentId) nextErrors.documentId = t('archive.editor.documentRequired')
    if (!companyId) nextErrors.companyId = t('archive.editor.companyRequired')

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SaveArchiveDto = {
      moduleType: state.moduleType,
      moduleId: moduleId!,
      documentId: documentId!,
      companyId: companyId!,
      lineId: positive(state.lineId),
      month: state.month === '' ? null : Number(state.month),
      year: state.year === '' ? null : Number(state.year),
      description: state.description.trim() || null,
      moduleDescription: state.moduleDescription.trim() || null,
    }

    if (archive) update.mutate({ id: archive.id, input })
    else create.mutate(input)
  }

  return (
    <Modal
      title={archive ? t('archive.editor.editTitle') : t('archive.editor.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field label={t('archive.fields.moduleType')} htmlFor="archive-module-type" required className="col-md-6">
          <select
            id="archive-module-type"
            className="form-select"
            value={state.moduleType}
            onChange={(event) => setState((s) => ({ ...s, moduleType: Number(event.target.value) }))}
          >
            {OWNER_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.documentOwnerType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('archive.fields.moduleId')}
          htmlFor="archive-module-id"
          required
          error={errors.moduleId}
          className="col-md-6"
        >
          <input
            id="archive-module-id"
            type="number"
            min={1}
            className={controlClass('form-control', errors.moduleId)}
            value={state.moduleId}
            onChange={(event) => setState((s) => ({ ...s, moduleId: event.target.value }))}
          />
        </Field>

        <Field
          label={t('archive.fields.documentId')}
          htmlFor="archive-document-id"
          required
          error={errors.documentId}
          hint={t('archive.editor.documentHint')}
          className="col-md-6"
        >
          <input
            id="archive-document-id"
            type="number"
            min={1}
            className={controlClass('form-control', errors.documentId)}
            value={state.documentId}
            onChange={(event) => setState((s) => ({ ...s, documentId: event.target.value }))}
          />
        </Field>

        <Field
          label={t('archive.fields.company')}
          htmlFor="archive-company"
          required
          error={errors.companyId}
          className="col-md-6"
        >
          <select
            id="archive-company"
            className={controlClass('form-select', errors.companyId)}
            value={state.companyId}
            onChange={(event) => setState((s) => ({ ...s, companyId: event.target.value }))}
          >
            <option value="">{t('archive.editor.selectCompany')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('archive.fields.lineId')} htmlFor="archive-line" className="col-md-4">
          <input
            id="archive-line"
            type="number"
            min={1}
            className="form-control"
            value={state.lineId}
            onChange={(event) => setState((s) => ({ ...s, lineId: event.target.value }))}
          />
        </Field>

        <Field label={t('archive.filters.year')} htmlFor="archive-year" className="col-md-4">
          <select
            id="archive-year"
            className="form-select"
            value={state.year}
            onChange={(event) => setState((s) => ({ ...s, year: event.target.value }))}
          >
            <option value="">{t('common.none')}</option>
            {recentYears().map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('archive.filters.month')} htmlFor="archive-month" className="col-md-4">
          <select
            id="archive-month"
            className="form-select"
            value={state.month}
            onChange={(event) => setState((s) => ({ ...s, month: event.target.value }))}
          >
            <option value="">{t('common.none')}</option>
            {MONTHS.map((value) => (
              <option key={value} value={value}>
                {t(`enums.month.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t('archive.fields.description')} htmlFor="archive-description">
          <textarea
            id="archive-description"
            className="form-control"
            rows={2}
            value={state.description}
            onChange={(event) => setState((s) => ({ ...s, description: event.target.value }))}
          />
        </Field>

        <Field label={t('archive.fields.moduleDescription')} htmlFor="archive-module-description">
          <textarea
            id="archive-module-description"
            className="form-control"
            rows={2}
            value={state.moduleDescription}
            onChange={(event) => setState((s) => ({ ...s, moduleDescription: event.target.value }))}
          />
        </Field>
      </div>
    </Modal>
  )
}
