import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, Input } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { useEntity } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { ConfirmDialog, Modal } from '@/components/Form'
import { formatDate } from '@/utils/format'
import YearEndReviewFormModal from './YearEndReviewFormModal'
import {
  REPORT_ENDPOINTS,
  useCompanyLookup,
  useCurrentYearEndReview,
  useUserLookup,
  useYearEndReviewList,
  type YearEndReviewReportDto,
  type YearEndReviewReportListDto,
} from './api'
import { FilterDate, FilterSelect, RowActions } from './components'

const PAGE_SIZE = 20

/**
 * Year-end review report list — `/reports/year-end`.
 *
 * `YearEndReviewReportListDto` carries `companyId` but no company name, so the workplace column
 * is resolved from the company lookup that already backs the filter drop-down — one request for
 * the whole table. Picking a workplace also fetches that workplace's most recent report, which
 * is the question the statutory screen is usually opened to answer.
 */
export default function YearEndReviewListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState<number | undefined>()
  const [specialistUserId, setSpecialistUserId] = useState<number | undefined>()
  const [physicianUserId, setPhysicianUserId] = useState<number | undefined>()
  const [activeFilter, setActiveFilter] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  const [isCreating, setIsCreating] = useState(false)
  const [editingId, setEditingId] = useState<number | undefined>()
  const [pendingDelete, setPendingDelete] = useState<YearEndReviewReportListDto | undefined>()

  const companies = useCompanyLookup()
  const users = useUserLookup()

  const list = useYearEndReviewList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'ReportDate DESC',
    filter: search || undefined,
    companyId,
    specialistUserId,
    physicianUserId,
    isActive: activeFilter === '' ? undefined : activeFilter === 'true',
    startDate: startDate || undefined,
    endDate: endDate || undefined,
  })

  const current = useCurrentYearEndReview(companyId)

  const remove = useDelete(REPORT_ENDPOINTS.yearEndReviewReport, {
    onSuccess: () => setPendingDelete(undefined),
  })

  // The list DTO now carries the resolved name, so no client-side id -> name map and
  // no "Company #12" fallback for rows the lookup did not cover.
  function companyName(row: { companyId: number; companyName?: string | null }) {
    return row.companyName ?? t('reports.common.companyFallback', { id: row.companyId })
  }

  /**
   * Name of the workplace chosen in the filter. That one is not a list row, so it still comes
   * from the lookup the filter itself is populated from — no extra request.
   */
  function selectedCompanyName(id: number) {
    const match = companies.data?.items.find((item) => item.id === id)
    return match?.displayName ?? t('reports.common.companyFallback', { id })
  }

  const columns: Column<YearEndReviewReportListDto>[] = [
    {
      key: 'reportTitle',
      header: t('reports.yearEnd.fields.reportTitle'),
      render: (row) => (
        <Link to={`/reports/year-end/${row.id}`} className="fw-semibold text-decoration-none">
          {row.reportTitle}
        </Link>
      ),
    },
    {
      key: 'company',
      header: t('reports.yearEnd.fields.company'),
      render: (row) => companyName(row),
    },
    {
      key: 'reportDate',
      header: t('reports.yearEnd.fields.reportDate'),
      render: (row) => formatDate(row.reportDate) ?? t('common.none'),
    },
    {
      key: 'specialist',
      header: t('reports.yearEnd.fields.specialistFullName'),
      render: (row) => row.specialistFullName || t('common.none'),
    },
    {
      key: 'physician',
      header: t('reports.yearEnd.fields.physicianFullName'),
      render: (row) => row.physicianFullName || t('common.none'),
    },
    {
      key: 'status',
      header: t('reports.yearEnd.fields.status'),
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
      render: (row) => (
        <RowActions
          editLabel={t('reports.yearEnd.actions.edit', { name: row.reportTitle })}
          deleteLabel={t('reports.yearEnd.actions.delete', { name: row.reportTitle })}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setPendingDelete(row)}
          extra={
            <Link
              to={`/reports/year-end/${row.id}`}
              className="btn btn-sm btn-light"
              aria-label={t('reports.yearEnd.actions.detail', { name: row.reportTitle })}
              title={t('common.detail')}
            >
              <span aria-hidden="true">▤</span>
            </Link>
          }
        />
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('reports.yearEnd.title')}
        description={t('reports.yearEnd.description')}
        action={
          <Button variant="primary" onClick={() => setIsCreating(true)}>
            {t('reports.yearEnd.create')}
          </Button>
        }
      />

      {companyId && !current.isLoading && (
        <Alert
          variant={current.data ? 'success' : 'warning'}
          className="d-flex flex-wrap align-items-center justify-content-between gap-3"
        >
          <span>
            {current.data
              ? t('reports.yearEnd.current.found', {
                  company: selectedCompanyName(companyId),
                  title: current.data.reportTitle,
                  date: formatDate(current.data.reportDate) ?? t('common.none'),
                })
              : t('reports.yearEnd.current.none', { company: selectedCompanyName(companyId) })}
          </span>
          {current.data && (
            <Link to={`/reports/year-end/${current.data.id}`} className="btn btn-sm btn-light">
              {t('common.detail')}
            </Link>
          )}
        </Alert>
      )}

      <Card
        
        header={
          <div className="d-flex flex-wrap align-items-center gap-3 w-100">
            <div className="flex-grow-1" style={{ maxWidth: 260 }}>
              <label htmlFor="year-end-search" className="visually-hidden">
                {t('reports.yearEnd.searchLabel')}
              </label>
              <Input
                id="year-end-search"
                type="search"
                value={search}
                placeholder={t('reports.yearEnd.searchPlaceholder')}
                onChange={(next) => {
                  setSearch(next)
                  setPage(1)
                }}
              />
            </div>

            <FilterSelect
              id="year-end-company"
              label={t('reports.yearEnd.fields.company')}
              value={companyId === undefined ? '' : String(companyId)}
              width={220}
              placeholder={t('reports.common.allCompanies')}
              options={
                companies.data?.items.map((company) => ({
                  value: String(company.id),
                  label: company.displayName,
                })) ?? []
              }
              onChange={(next) => {
                setCompanyId(next === '' ? undefined : Number(next))
                setPage(1)
              }}
            />

            <FilterSelect
              id="year-end-specialist-filter"
              label={t('reports.yearEnd.fields.specialist')}
              value={specialistUserId === undefined ? '' : String(specialistUserId)}
              placeholder={t('reports.yearEnd.filters.allSpecialists')}
              options={
                users.data?.items.map((user) => ({
                  value: String(user.id),
                  label: user.displayName,
                })) ?? []
              }
              onChange={(next) => {
                setSpecialistUserId(next === '' ? undefined : Number(next))
                setPage(1)
              }}
            />

            <FilterSelect
              id="year-end-physician-filter"
              label={t('reports.yearEnd.fields.physician')}
              value={physicianUserId === undefined ? '' : String(physicianUserId)}
              placeholder={t('reports.yearEnd.filters.allPhysicians')}
              options={
                users.data?.items.map((user) => ({
                  value: String(user.id),
                  label: user.displayName,
                })) ?? []
              }
              onChange={(next) => {
                setPhysicianUserId(next === '' ? undefined : Number(next))
                setPage(1)
              }}
            />

            <FilterSelect
              id="year-end-status"
              label={t('reports.yearEnd.fields.status')}
              value={activeFilter}
              width={150}
              placeholder={t('common.all')}
              options={[
                { value: 'true', label: t('common.active') },
                { value: 'false', label: t('common.passive') },
              ]}
              onChange={(next) => {
                setActiveFilter(next)
                setPage(1)
              }}
            />

            <FilterDate
              id="year-end-start-date"
              label={t('reports.common.periodStart')}
              value={startDate}
              onChange={(next) => {
                setStartDate(next)
                setPage(1)
              }}
            />
            <FilterDate
              id="year-end-end-date"
              label={t('reports.common.periodEnd')}
              value={endDate}
              onChange={(next) => {
                setEndDate(next)
                setPage(1)
              }}
            />
          </div>
        }
        footer={
          list.data && list.data.totalCount > 0 ? (
            <Pagination
              total={list.data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          ) : undefined
        }
      >
        <DataTable
          label={t('reports.yearEnd.title')}
          columns={columns}
          rows={list.data?.items}
          rowKey={(row) => row.id}
          isLoading={list.isLoading}
          error={list.error ? errorMessage(list.error) : null}
          emptyMessage={t('reports.yearEnd.empty')}
        />
      </Card>

      {isCreating && <YearEndReviewFormModal onClose={() => setIsCreating(false)} />}
      {editingId && (
        <EditGate reportId={editingId} onClose={() => setEditingId(undefined)} />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('reports.yearEnd.deleteTitle')}
        message={t('reports.yearEnd.deleteMessage', { name: pendingDelete?.reportTitle ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setPendingDelete(undefined)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}

/**
 * Loads the full header before the edit dialog opens.
 *
 * The list row carries six of the header's eighteen fields, so editing from the row alone would
 * blank the workforce counts and the user references on save. One request is made when the user
 * asks to edit — never one per row.
 */
function EditGate({ reportId, onClose }: { reportId: number; onClose: () => void }) {
  const { t } = useTranslation()
  const report = useEntity<YearEndReviewReportDto>(REPORT_ENDPOINTS.yearEndReviewReport, reportId)

  if (report.data) return <YearEndReviewFormModal report={report.data} onClose={onClose} />

  return (
    <Modal title={t('reports.yearEnd.form.editTitle')} isOpen onClose={onClose} size="sm">
      {report.error ? (
        <Alert variant="danger" className="mb-0">
          {errorMessage(report.error)}
        </Alert>
      ) : (
        <Spinner />
      )}
    </Modal>
  )
}
