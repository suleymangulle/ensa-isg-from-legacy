import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, NumberInput, Select, Tabs, TextArea, type TabItem } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
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
  const [moduleType, setModuleType] = useState<DocumentOwnerType | null>(null)
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [year, setYear] = useState<number | null>(null)
  const [month, setMonth] = useState<number | null>(null)

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
    moduleType: moduleType ?? undefined,
    companyId: companyId ?? undefined,
    year: year ?? undefined,
    month: month ?? undefined,
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
        <Badge variant="primary">{t(`enums.documentOwnerType.${row.moduleType}`)}</Badge>
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
          <Button variant="light" size="sm" 
            onClick={() => {
              setEditingId(row.id)
              setIsEditorOpen(true)
            }}
            aria-label={t('archive.list.editAria', { id: row.id })}
          >
            {t('common.edit')}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setDeleting(row)}
            aria-label={t('archive.list.deleteAria', { id: row.id })}
          >
            {t('common.delete')}
          </Button>
        </div>
      ),
    },
  ]

  const tabItems: TabItem[] = TABS.map((tab) => ({ key: tab, label: t(`archive.tabs.${tab}`) }))

  return (
    <>
      <PageTitle
        title={t('archive.list.title')}
        description={t('archive.list.description')}
        action={
          <Button variant="primary"
            onClick={() => {
              setEditingId(undefined)
              setIsEditorOpen(true)
            }}
          >
            {t('archive.list.create')}
          </Button>
        }
      />

      <Card
        
        header={
          <Tabs
            items={tabItems}
            activeKey={activeTab}
            onChange={(key) => setActiveTab(key as TabKey)}
          />
        }
      >
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
                <div style={{ minWidth: 190 }}>
                  <label htmlFor="archive-module-filter" className="visually-hidden">
                    {t('archive.filters.moduleType')}
                  </label>
                  <Select
                    id="archive-module-filter"
                    placeholder={t('archive.filters.allModules')}
                    options={OWNER_TYPES.map((value) => ({
                      value,
                      label: t(`enums.documentOwnerType.${value}`),
                    }))}
                    value={moduleType}
                    onChange={(value) => {
                      setModuleType(value)
                      setPage(1)
                    }}
                  />
                </div>
                <div style={{ minWidth: 190 }}>
                  <label htmlFor="archive-company-filter" className="visually-hidden">
                    {t('archive.filters.company')}
                  </label>
                  <Select
                    id="archive-company-filter"
                    placeholder={t('archive.filters.allCompanies')}
                    options={(companies.data?.items ?? []).map((company) => ({
                      value: company.id,
                      label: company.displayName,
                    }))}
                    value={companyId}
                    onChange={(value) => {
                      setCompanyId(value)
                      setPage(1)
                    }}
                  />
                </div>
                <div style={{ minWidth: 130 }}>
                  <label htmlFor="archive-year-filter" className="visually-hidden">
                    {t('archive.filters.year')}
                  </label>
                  <Select
                    id="archive-year-filter"
                    placeholder={t('archive.filters.allYears')}
                    options={recentYears().map((value) => ({ value, label: String(value) }))}
                    value={year}
                    onChange={(value) => {
                      setYear(value)
                      setPage(1)
                    }}
                  />
                </div>
                <div style={{ minWidth: 150 }}>
                  <label htmlFor="archive-month-filter" className="visually-hidden">
                    {t('archive.filters.month')}
                  </label>
                  <Select
                    id="archive-month-filter"
                    placeholder={t('archive.filters.allMonths')}
                    options={MONTHS.map((value) => ({
                      value,
                      label: t(`enums.month.${value}`),
                    }))}
                    value={month}
                    onChange={(value) => {
                      setMonth(value)
                      setPage(1)
                    }}
                  />
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
      </Card>

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
  const [moduleId, setModuleId] = useState<number | null>(null)
  const [month, setMonth] = useState<number | null>(null)
  const [year, setYear] = useState<number | null>(null)
  const [query, setQuery] = useState<{
    moduleType?: DocumentOwnerType
    moduleId?: number
    month?: number
    year?: number
  }>({})

  const result = useArchiveByModule(query.moduleType, query.moduleId, query.month, query.year)
  const isReady = moduleId !== null && moduleId > 0

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
              moduleId: Math.trunc(moduleId!),
              month: month ?? undefined,
              year: year ?? undefined,
            })
          }}
        >
          <Select
            id="by-module-type"
            label={t('archive.fields.moduleType')}
            required
            className="col-md-3"
            options={OWNER_TYPES.map((value) => ({
              value,
              label: t(`enums.documentOwnerType.${value}`),
            }))}
            value={moduleType}
            onChange={(value) => value !== null && setModuleType(value)}
          />

          <NumberInput
            id="by-module-id"
            label={t('archive.fields.moduleId')}
            required
            helpText={t('archive.byModule.recordHint')}
            className="col-md-3"
            min={1}
            value={moduleId}
            onChange={setModuleId}
          />

          <Select
            id="by-module-year"
            label={t('archive.filters.year')}
            placeholder={t('archive.filters.allYears')}
            className="col-md-2"
            options={recentYears().map((value) => ({ value, label: String(value) }))}
            value={year}
            onChange={setYear}
          />

          <Select
            id="by-module-month"
            label={t('archive.filters.month')}
            placeholder={t('archive.filters.allMonths')}
            className="col-md-2"
            options={MONTHS.map((value) => ({ value, label: t(`enums.month.${value}`) }))}
            value={month}
            onChange={setMonth}
          />

          <div className="col-md-2">
            <Button variant="primary" className="w-100" type="submit" disabled={!isReady}>
              {t('archive.byModule.show')}
            </Button>
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
  moduleId: number | null
  documentId: number | null
  companyId: number | null
  lineId: number | null
  month: number | null
  year: number | null
  description: string
  moduleDescription: string
}

const EMPTY_EDITOR: EditorState = {
  moduleType: DocumentOwnerType.Company,
  moduleId: null,
  documentId: null,
  companyId: null,
  lineId: null,
  month: null,
  year: null,
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
            moduleId: archive.moduleId,
            documentId: archive.documentId,
            companyId: archive.companyId,
            lineId: archive.lineId ?? null,
            month: archive.month ?? null,
            year: archive.year ?? null,
            description: archive.description ?? '',
            moduleDescription: archive.moduleDescription ?? '',
          }
        : EMPTY_EDITOR,
    )
  }, [isOpen, archive])

  const create = useCreate<SaveArchiveDto>(ARCHIVE, { onSuccess: onClose })
  const update = useUpdate<SaveArchiveDto>(ARCHIVE, { onSuccess: onClose })
  const mutation = archive ? update : create

  function submit() {
    const nextErrors: Partial<Record<keyof EditorState, string>> = {}

    if (state.moduleId === null || state.moduleId < 1) nextErrors.moduleId = t('archive.editor.moduleIdRequired')
    if (state.documentId === null || state.documentId < 1) nextErrors.documentId = t('archive.editor.documentRequired')
    if (state.companyId === null || state.companyId < 1) nextErrors.companyId = t('archive.editor.companyRequired')

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const input: SaveArchiveDto = {
      moduleType: state.moduleType,
      moduleId: Math.trunc(state.moduleId!),
      documentId: Math.trunc(state.documentId!),
      companyId: Math.trunc(state.companyId!),
      lineId: state.lineId !== null ? Math.trunc(state.lineId) : null,
      month: state.month,
      year: state.year,
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
        <Select
          id="archive-module-type"
          label={t('archive.fields.moduleType')}
          required
          className="col-md-6"
          options={OWNER_TYPES.map((value) => ({
            value,
            label: t(`enums.documentOwnerType.${value}`),
          }))}
          value={state.moduleType}
          onChange={(value) => value !== null && setState((s) => ({ ...s, moduleType: value }))}
        />

        <NumberInput
          id="archive-module-id"
          label={t('archive.fields.moduleId')}
          required
          error={errors.moduleId}
          className="col-md-6"
          min={1}
          value={state.moduleId}
          onChange={(value) => setState((s) => ({ ...s, moduleId: value }))}
        />

        <NumberInput
          id="archive-document-id"
          label={t('archive.fields.documentId')}
          required
          error={errors.documentId}
          helpText={t('archive.editor.documentHint')}
          className="col-md-6"
          min={1}
          value={state.documentId}
          onChange={(value) => setState((s) => ({ ...s, documentId: value }))}
        />

        <Select
          id="archive-company"
          label={t('archive.fields.company')}
          required
          error={errors.companyId}
          className="col-md-6"
          placeholder={t('archive.editor.selectCompany')}
          options={(companies.data?.items ?? []).map((company) => ({
            value: company.id,
            label: company.displayName,
          }))}
          value={state.companyId}
          onChange={(value) => setState((s) => ({ ...s, companyId: value }))}
        />

        <NumberInput
          id="archive-line"
          label={t('archive.fields.lineId')}
          className="col-md-4"
          min={1}
          value={state.lineId}
          onChange={(value) => setState((s) => ({ ...s, lineId: value }))}
        />

        <Select
          id="archive-year"
          label={t('archive.filters.year')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={recentYears().map((value) => ({ value, label: String(value) }))}
          value={state.year}
          onChange={(value) => setState((s) => ({ ...s, year: value }))}
        />

        <Select
          id="archive-month"
          label={t('archive.filters.month')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={MONTHS.map((value) => ({ value, label: t(`enums.month.${value}`) }))}
          value={state.month}
          onChange={(value) => setState((s) => ({ ...s, month: value }))}
        />

        <TextArea
          id="archive-description"
          label={t('archive.fields.description')}
          className="col-12"
          rows={2}
          value={state.description}
          onChange={(value) => setState((s) => ({ ...s, description: value }))}
        />

        <TextArea
          id="archive-module-description"
          label={t('archive.fields.moduleDescription')}
          className="col-12"
          rows={2}
          value={state.moduleDescription}
          onChange={(value) => setState((s) => ({ ...s, moduleDescription: value }))}
        />
      </div>
    </Modal>
  )
}
