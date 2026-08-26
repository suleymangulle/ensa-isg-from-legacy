import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { PageTitle, Pagination, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useEntity } from '@/api/endpoints'
import { HazardClass } from '@/api/enums'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import {
  FINANCE_ENDPOINTS,
  usePenaltyList,
  usePenaltySurvey,
  usePenaltySurveyList,
  type PenaltyDto,
  type PenaltyListDto,
  type PenaltySurveyDto,
  type PenaltySurveyListDto,
  type SavePenaltyDto,
  type SavePenaltySurveyDto,
} from './api'
import { FilterSelect, RowActions, enumValues } from './components'
import { formatNumber } from '@/utils/format'
import PenaltyForm from './PenaltyForm'
import PenaltySurveyForm from './PenaltySurveyForm'

const PAGE_SIZE = 20
const TABS = ['catalogue', 'surveys'] as const

type TabKey = (typeof TABS)[number]

/**
 * Statutory fines, in two halves that share a screen because they share a domain.
 *
 * The catalogue tab is the host-owned list of fine articles laid down by law. The survey tab is
 * the tenant-scoped questionnaire built on top of it — the modern equivalent of the legacy
 * `CezaAnketi` screen — which prices a prospective customer's exposure by answering those
 * articles for one workplace.
 *
 * The active tab is kept in the query string so a survey list can be linked to and survives a
 * browser reload.
 */
export default function PenaltiesPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()

  const requested = searchParams.get('tab')
  const activeTab: TabKey = TABS.includes(requested as TabKey) ? (requested as TabKey) : 'catalogue'

  return (
    <>
      <PageTitle
        title={t('finance.penalty.list.title')}
        description={t('finance.penalty.list.description')}
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
                  onClick={() => setSearchParams(tab === 'catalogue' ? {} : { tab })}
                >
                  {t(`finance.penalty.tabs.${tab}`)}
                </button>
              </li>
            ))}
          </ul>
        </div>

        {activeTab === 'catalogue' ? <CatalogueTab /> : <SurveyTab />}
      </div>
    </>
  )
}

// ---------------------------------------------------------------- Fine catalogue

function CatalogueTab() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [activeFilter, setActiveFilter] = useState('')
  const [multiplierFilter, setMultiplierFilter] = useState('')

  const [isCreating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<PenaltyListDto | null>(null)

  const { data, isLoading, error } = usePenaltyList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'LawArticle ASC',
    filter: search || undefined,
    isActive: activeFilter === '' ? undefined : activeFilter === 'true',
    multiplierCalculate: multiplierFilter === '' ? undefined : multiplierFilter === 'true',
  })

  const create = useCreate<SavePenaltyDto, PenaltyDto>(FINANCE_ENDPOINTS.penalty, {
    onSuccess: () => setCreating(false),
  })
  const remove = useDelete(FINANCE_ENDPOINTS.penalty, { onSuccess: () => setDeleting(null) })

  const columns: Column<PenaltyListDto>[] = [
    {
      key: 'treeNodeCode',
      header: t('finance.penalty.fields.treeNodeCode'),
      width: '110px',
      render: (row) => row.treeNodeCode || t('common.none'),
    },
    {
      key: 'lawArticle',
      header: t('finance.penalty.fields.lawArticle'),
      render: (row) => (
        <Link to={`/penalties/${row.id}`} className="fw-semibold text-decoration-none">
          {row.lawArticle}
        </Link>
      ),
    },
    {
      key: 'penaltyArticle',
      header: t('finance.penalty.fields.penaltyArticle'),
      render: (row) => row.penaltyArticle,
    },
    {
      key: 'multiplier',
      header: t('finance.penalty.fields.multiplierCalculate'),
      align: 'center',
      render: (row) => (
        <span className={row.multiplierCalculate ? 'badge-light-warning' : 'badge-light-primary'}>
          {row.multiplierCalculate ? t('common.yes') : t('common.no')}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('finance.penalty.fields.status'),
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
      width: '110px',
      render: (row) => (
        <RowActions
          editLabel={t('finance.penalty.actions.edit', { article: row.lawArticle })}
          deleteLabel={t('finance.penalty.actions.delete', { article: row.lawArticle })}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setDeleting(row)}
        />
      ),
    },
  ]

  return (
    <>
      <div className="card-header border-0 pt-4 pb-0">
        <SearchBar
          value={search}
          onChange={(next) => {
            setSearch(next)
            setPage(1)
          }}
          placeholder={t('finance.penalty.list.searchPlaceholder')}
        >
          <FilterSelect
            id="penalty-filter-status"
            label={t('finance.penalty.fields.status')}
            value={activeFilter}
            onChange={(next) => {
              setActiveFilter(next)
              setPage(1)
            }}
          >
            <option value="">{t('common.all')}</option>
            <option value="true">{t('common.active')}</option>
            <option value="false">{t('common.passive')}</option>
          </FilterSelect>

          <FilterSelect
            id="penalty-filter-multiplier"
            label={t('finance.penalty.fields.multiplierCalculate')}
            value={multiplierFilter}
            onChange={(next) => {
              setMultiplierFilter(next)
              setPage(1)
            }}
            width={220}
          >
            <option value="">{t('finance.penalty.filters.allMultipliers')}</option>
            <option value="true">{t('finance.penalty.filters.perEmployee')}</option>
            <option value="false">{t('finance.penalty.filters.flat')}</option>
          </FilterSelect>

          <button className="btn btn-primary ms-auto" type="button" onClick={() => setCreating(true)}>
            {t('finance.penalty.list.create')}
          </button>
        </SearchBar>
      </div>

      <div className="card-body p-0">
        <DataTable
          label={t('finance.penalty.tabs.catalogue')}
          columns={columns}
          rows={data?.items}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('finance.penalty.list.empty')}
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

      {isCreating && (
        <PenaltyForm
          isOpen
          onClose={() => setCreating(false)}
          onSubmit={(input) => create.mutate(input)}
          isBusy={create.isPending}
          error={create.error ? errorMessage(create.error) : null}
        />
      )}

      {editingId !== null && (
        <PenaltyEditor penaltyId={editingId} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('finance.penalty.delete.title')}
        message={t('finance.penalty.delete.message', { article: deleting?.lawArticle ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

// ------------------------------------------------------------------ Fine surveys

function SurveyTab() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [hazardClass, setHazardClass] = useState('')

  const [isCreating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<PenaltySurveyListDto | null>(null)

  const { data, isLoading, error } = usePenaltySurveyList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'CreationTime DESC',
    filter: search || undefined,
    hazardClass: hazardClass ? (Number(hazardClass) as HazardClass) : undefined,
  })

  const create = useCreate<SavePenaltySurveyDto, PenaltySurveyDto>(
    FINANCE_ENDPOINTS.penaltySurvey,
    { onSuccess: () => setCreating(false) },
  )
  const remove = useDelete(FINANCE_ENDPOINTS.penaltySurvey, { onSuccess: () => setDeleting(null) })

  const columns: Column<PenaltySurveyListDto>[] = [
    {
      key: 'companyTitle',
      header: t('finance.penaltySurvey.fields.companyTitle'),
      render: (row) => (
        <Link to={`/penalties/surveys/${row.id}`} className="fw-semibold text-decoration-none">
          {row.companyTitle}
        </Link>
      ),
    },
    {
      key: 'facilityName',
      header: t('finance.penaltySurvey.fields.facilityName'),
      render: (row) => row.facilityName || t('common.none'),
    },
    {
      key: 'hazardClass',
      header: t('finance.penaltySurvey.fields.hazardClass'),
      render: (row) => t(`enums.hazardClass.${row.hazardClass}`),
    },
    {
      key: 'workerCount',
      header: t('finance.penaltySurvey.fields.workerCount'),
      align: 'end',
      render: (row) => formatNumber(row.workerCount) ?? t('common.none'),
    },
    {
      key: 'creationTime',
      header: t('finance.penaltySurvey.fields.creationTime'),
      render: (row) => formatDate(row.creationTime) ?? t('common.none'),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '110px',
      render: (row) => (
        <RowActions
          editLabel={t('finance.penaltySurvey.actions.edit', { title: row.companyTitle })}
          deleteLabel={t('finance.penaltySurvey.actions.delete', { title: row.companyTitle })}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setDeleting(row)}
        />
      ),
    },
  ]

  return (
    <>
      <div className="card-header border-0 pt-4 pb-0">
        <SearchBar
          value={search}
          onChange={(next) => {
            setSearch(next)
            setPage(1)
          }}
          placeholder={t('finance.penaltySurvey.list.searchPlaceholder')}
        >
          <FilterSelect
            id="survey-filter-hazard"
            label={t('finance.penaltySurvey.fields.hazardClass')}
            value={hazardClass}
            onChange={(next) => {
              setHazardClass(next)
              setPage(1)
            }}
            width={200}
          >
            <option value="">{t('finance.penaltySurvey.filters.allHazardClasses')}</option>
            {enumValues(HazardClass).map((value) => (
              <option key={value} value={value}>
                {t(`enums.hazardClass.${value}`)}
              </option>
            ))}
          </FilterSelect>

          <button className="btn btn-primary ms-auto" type="button" onClick={() => setCreating(true)}>
            {t('finance.penaltySurvey.list.create')}
          </button>
        </SearchBar>
      </div>

      <div className="card-body p-0">
        <DataTable
          label={t('finance.penalty.tabs.surveys')}
          columns={columns}
          rows={data?.items}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('finance.penaltySurvey.list.empty')}
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

      {isCreating && (
        <PenaltySurveyForm
          isOpen
          onClose={() => setCreating(false)}
          onSubmit={(input) => create.mutate(input)}
          isBusy={create.isPending}
          error={create.error ? errorMessage(create.error) : null}
        />
      )}

      {editingId !== null && (
        <PenaltySurveyEditor surveyId={editingId} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('finance.penaltySurvey.delete.title')}
        message={t('finance.penaltySurvey.delete.message', { title: deleting?.companyTitle ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

// ---------------------------------------------------------------------- Editors

/** Loads a fine article before opening the edit dialog, so every field round-trips. */
export function PenaltyEditor({
  penaltyId,
  onClose,
}: {
  penaltyId: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const { data, isLoading, error } = useEntity<PenaltyDto>(FINANCE_ENDPOINTS.penalty, penaltyId)
  const update = useUpdate<SavePenaltyDto, PenaltyDto>(FINANCE_ENDPOINTS.penalty, {
    onSuccess: onClose,
  })

  if (isLoading || error || !data) {
    return (
      <Modal
        title={t('finance.penalty.form.editTitle')}
        isOpen
        onClose={onClose}
        error={error ? errorMessage(error) : null}
      >
        {isLoading ? <Spinner /> : null}
      </Modal>
    )
  }

  return (
    <PenaltyForm
      isOpen
      penalty={data}
      onClose={onClose}
      onSubmit={(input) => update.mutate({ id: penaltyId, input })}
      isBusy={update.isPending}
      error={update.error ? errorMessage(update.error) : null}
    />
  )
}

/** Loads a survey header before opening the edit dialog. */
export function PenaltySurveyEditor({
  surveyId,
  onClose,
}: {
  surveyId: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const { data, isLoading, error } = usePenaltySurvey(surveyId)
  const update = useUpdate<SavePenaltySurveyDto, PenaltySurveyDto>(
    FINANCE_ENDPOINTS.penaltySurvey,
    { onSuccess: onClose },
  )

  if (isLoading || error || !data) {
    return (
      <Modal
        title={t('finance.penaltySurvey.form.editTitle')}
        isOpen
        onClose={onClose}
        error={error ? errorMessage(error) : null}
      >
        {isLoading ? <Spinner /> : null}
      </Modal>
    )
  }

  return (
    <PenaltySurveyForm
      isOpen
      survey={data}
      onClose={onClose}
      onSubmit={(input) => update.mutate({ id: surveyId, input })}
      isBusy={update.isPending}
      error={update.error ? errorMessage(update.error) : null}
    />
  )
}
