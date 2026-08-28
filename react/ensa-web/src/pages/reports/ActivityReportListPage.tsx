import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Input } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ActivityReportType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { ConfirmDialog } from '@/components/Form'
import { formatDate } from '@/utils/format'
import ActivityReportFormModal from './ActivityReportFormModal'
import {
  REPORT_ENDPOINTS,
  useActivityReportList,
  useCompanyLookup,
  type ActivityReportDto,
  type ActivityReportListDto,
} from './api'
import { FilterDate, FilterSelect, RowActions, enumValues } from './components'

const PAGE_SIZE = 20

/**
 * Activity report list — `/reports/activities`.
 *
 * `ActivityReportListDto` carries `companyId` but no company name, so the workplace column is
 * resolved from the company lookup that already backs the filter drop-down: one request for the
 * whole table rather than one per row. The lookup is capped server-side, so a workplace outside
 * the cap falls back to its id.
 */
export default function ActivityReportListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState<number | undefined>()
  const [reportType, setReportType] = useState<ActivityReportType | undefined>()
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  const [editing, setEditing] = useState<ActivityReportDto | undefined>()
  const [isCreating, setIsCreating] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<ActivityReportListDto | undefined>()

  const companies = useCompanyLookup()

  const list = useActivityReportList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'ReportStart DESC',
    filter: search || undefined,
    companyId,
    reportType,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
  })

  const remove = useDelete(REPORT_ENDPOINTS.activityReport, {
    onSuccess: () => setPendingDelete(undefined),
  })

  // The list DTO now carries the resolved name, so no client-side id -> name map and
  // no "Company #12" fallback for rows the lookup did not cover.
  function companyName(row: { companyId: number; companyName?: string | null }) {
    return row.companyName ?? t('reports.common.companyFallback', { id: row.companyId })
  }

  const columns: Column<ActivityReportListDto>[] = [
    {
      key: 'reportName',
      header: t('reports.activity.fields.reportName'),
      render: (row) => (
        <Link
          to={`/reports/activities/${row.id}`}
          className="fw-semibold text-decoration-none"
        >
          {row.reportName}
        </Link>
      ),
    },
    {
      key: 'company',
      header: t('reports.activity.fields.company'),
      render: (row) => companyName(row),
    },
    {
      key: 'reportType',
      header: t('reports.activity.fields.reportType'),
      render: (row) => (
        <Badge variant="primary">{t(`enums.activityReportType.${row.reportType}`)}</Badge>
      ),
    },
    {
      key: 'period',
      header: t('reports.common.period'),
      render: (row) =>
        t('reports.common.periodRange', {
          from: formatDate(row.reportStart) ?? t('common.none'),
          to: formatDate(row.reportEnd) ?? t('common.none'),
        }),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      render: (row) => (
        <RowActions
          editLabel={t('reports.activity.actions.edit', { name: row.reportName })}
          deleteLabel={t('reports.activity.actions.delete', { name: row.reportName })}
          onEdit={() => setEditing(toHeader(row))}
          onDelete={() => setPendingDelete(row)}
          extra={
            <Link
              to={`/reports/activities/${row.id}`}
              className="btn btn-sm btn-light"
              aria-label={t('reports.activity.actions.detail', { name: row.reportName })}
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
        title={t('reports.activity.title')}
        description={t('reports.activity.description')}
        action={
          <Button variant="primary" onClick={() => setIsCreating(true)}>
            {t('reports.activity.create')}
          </Button>
        }
      />

      <Card
        
        header={
          <div className="d-flex flex-wrap align-items-center gap-3 w-100">
            <div className="flex-grow-1" style={{ maxWidth: 280 }}>
              <label htmlFor="activity-search" className="visually-hidden">
                {t('reports.activity.searchLabel')}
              </label>
              <Input
                id="activity-search"
                type="search"
                value={search}
                placeholder={t('reports.activity.searchPlaceholder')}
                onChange={(next) => {
                  setSearch(next)
                  setPage(1)
                }}
              />
            </div>

            <FilterSelect
              id="activity-company"
              label={t('reports.activity.fields.company')}
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
              id="activity-type"
              label={t('reports.activity.fields.reportType')}
              value={reportType === undefined ? '' : String(reportType)}
              placeholder={t('reports.activity.filters.allTypes')}
              options={enumValues(ActivityReportType).map((value) => ({
                value: String(value),
                label: t(`enums.activityReportType.${value}`),
              }))}
              onChange={(next) => {
                setReportType(next === '' ? undefined : (Number(next) as ActivityReportType))
                setPage(1)
              }}
            />

            <FilterDate
              id="activity-start-date"
              label={t('reports.common.periodStart')}
              value={startDate}
              onChange={(next) => {
                setStartDate(next)
                setPage(1)
              }}
            />
            <FilterDate
              id="activity-end-date"
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
          label={t('reports.activity.title')}
          columns={columns}
          rows={list.data?.items}
          rowKey={(row) => row.id}
          isLoading={list.isLoading}
          error={list.error ? errorMessage(list.error) : null}
          emptyMessage={t('reports.activity.empty')}
        />
      </Card>

      {isCreating && <ActivityReportFormModal onClose={() => setIsCreating(false)} />}
      {editing && (
        <ActivityReportFormModal report={editing} onClose={() => setEditing(undefined)} />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('reports.activity.deleteTitle')}
        message={t('reports.activity.deleteMessage', { name: pendingDelete?.reportName ?? '' })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setPendingDelete(undefined)}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
      />
    </>
  )
}

/**
 * The edit dialog wants the header shape; the list row already carries every field the update
 * input needs, so it is widened here instead of costing a second round trip.
 */
function toHeader(row: ActivityReportListDto): ActivityReportDto {
  return {
    id: row.id,
    companyId: row.companyId,
    reportType: row.reportType,
    reportName: row.reportName,
    reportStart: row.reportStart,
    reportEnd: row.reportEnd,
  }
}
