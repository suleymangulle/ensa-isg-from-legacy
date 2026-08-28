import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, Input } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { AssignmentType, HazardClass, StaffRole } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { formatDate, formatNumber } from '@/utils/format'
import {
  useHazardClassBreakdown,
  useOfficeLookup,
  useOfficeOhsReports,
  useOhsReport,
  useOhsReportList,
  type OhsReportDto,
  type OhsReportListDto,
} from './api'
import {
  ASSIGNMENT_TYPE_BADGE,
  DistributionRow,
  EmptyHint,
  FilterDate,
  FilterSelect,
  GateHint,
  HAZARD_CLASS_BAR,
  PrintButton,
  ReportPrintStyles,
  STAFF_ROLE_BADGE,
  SummaryCard,
  Term,
  enumValues,
  percentOf,
} from './components'

const PAGE_SIZE = 20

/** Hazard classes in statutory order, so the summary always shows the same four buckets. */
const HAZARD_CLASSES: HazardClass[] = [
  HazardClass.LowHazard,
  HazardClass.Hazardous,
  HazardClass.VeryHazardous,
  HazardClass.Unspecified,
]

/** One row of the hazard-class breakdown table, including the synthetic trailing total row. */
interface BreakdownRow {
  key: string
  label: string
  count: number
  sharePercent: number
  isTotal?: boolean
}

/**
 * OHS control report — `/reports/ohs`.
 *
 * The legacy screen (`ISGKontrolRaporu.aspx`) picked an office and an İSG-Katip archive output
 * and compared assigned against used service time, professional by professional. The API keeps
 * the service-time side of that record, so this screen shows three things: the period totals of
 * one office, the filterable professional list, and — for the row the user picks — the
 * hazard-class distribution of the workplaces the report covers.
 *
 * The office is part of the `office/{officeId}` route, so the period summary waits until one is
 * chosen rather than firing a request that cannot resolve.
 */
export default function OhsReportPage() {
  const { t } = useTranslation()

  const [officeId, setOfficeId] = useState<number | undefined>()
  const [staffRole, setStaffRole] = useState<StaffRole | undefined>()
  const [dutyType, setDutyType] = useState<AssignmentType | undefined>()
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [selectedId, setSelectedId] = useState<number | undefined>()

  const offices = useOfficeLookup()

  const list = useOhsReportList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'EmployeeName ASC',
    filter: search || undefined,
    officeId,
    staffRole,
    dutyType,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
  })

  const officeReports = useOfficeOhsReports(officeId, startDate || undefined, endDate || undefined)

  function resetToFirstPage<T>(setter: (next: T) => void) {
    return (next: T) => {
      setter(next)
      setPage(1)
    }
  }

  const columns: Column<OhsReportListDto>[] = [
    {
      key: 'employeeName',
      header: t('reports.ohs.fields.employeeName'),
      render: (row) => (
        <Button variant="link" className="p-0 fw-semibold text-decoration-none text-start"
          aria-pressed={selectedId === row.id}
          onClick={() => setSelectedId(row.id)}
        >
          {row.employeeName}
        </Button>
      ),
    },
    {
      key: 'nationalId',
      header: t('reports.ohs.fields.nationalId'),
      render: (row) => row.nationalId || t('common.none'),
    },
    {
      key: 'staffRole',
      header: t('reports.ohs.fields.staffRole'),
      render: (row) => (
        <Badge variant={STAFF_ROLE_BADGE[row.staffRole]}>
          {t(`enums.staffRole.${row.staffRole}`)}
        </Badge>
      ),
    },
    {
      key: 'dutyType',
      header: t('reports.ohs.fields.dutyType'),
      render: (row) => (
        <Badge variant={ASSIGNMENT_TYPE_BADGE[row.dutyType]}>
          {t(`enums.assignmentType.${row.dutyType}`)}
        </Badge>
      ),
    },
    {
      key: 'totalMinutes',
      header: t('reports.ohs.fields.totalMinutes'),
      align: 'end',
      render: (row) => t('reports.common.minutes', { value: formatNumber(row.totalMinutes) }),
    },
    {
      key: 'usedMonthlyMinutes',
      header: t('reports.ohs.fields.usedMonthlyMinutes'),
      align: 'end',
      render: (row) => t('reports.common.minutes', { value: formatNumber(row.usedMonthlyMinutes) }),
    },
    {
      key: 'utilisation',
      header: t('reports.ohs.fields.utilisation'),
      align: 'end',
      render: (row) =>
        t('reports.common.percent', {
          value: percentOf(row.usedMonthlyMinutes, row.totalMinutes),
        }),
    },
    {
      key: 'creationTime',
      header: t('reports.ohs.fields.creationTime'),
      render: (row) => formatDate(row.creationTime) ?? t('common.none'),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      render: (row) => (
        <Button variant="light" size="sm" className="d-print-none"
          aria-label={t('reports.ohs.breakdown.open', { name: row.employeeName })}
          title={t('reports.ohs.breakdown.title')}
          onClick={() => setSelectedId(row.id)}
        >
          <span aria-hidden="true">▤</span>
        </Button>
      ),
    },
  ]

  return (
    <div className="report-print">
      <ReportPrintStyles />

      <PageTitle
        title={t('reports.ohs.title')}
        description={t('reports.ohs.description')}
        action={<PrintButton />}
      />

      <Card
        className="mb-4 d-print-none"
      >
          <div className="d-flex flex-wrap align-items-center gap-3">
            <div className="flex-grow-1" style={{ maxWidth: 280 }}>
              <label htmlFor="ohs-search" className="visually-hidden">
                {t('reports.ohs.searchLabel')}
              </label>
              <Input
                id="ohs-search"
                type="search"
                value={search}
                placeholder={t('reports.ohs.searchPlaceholder')}
                onChange={(next) => resetToFirstPage(setSearch)(next)}
              />
            </div>

            <FilterSelect
              id="ohs-office"
              label={t('reports.ohs.fields.office')}
              value={officeId === undefined ? '' : String(officeId)}
              width={220}
              placeholder={t('reports.ohs.filters.allOffices')}
              options={
                offices.data?.items.map((office) => ({
                  value: String(office.id),
                  label: office.displayName,
                })) ?? []
              }
              onChange={(next) =>
                resetToFirstPage(setOfficeId)(next === '' ? undefined : Number(next))
              }
            />

            <FilterSelect
              id="ohs-staff-role"
              label={t('reports.ohs.fields.staffRole')}
              value={staffRole === undefined ? '' : String(staffRole)}
              placeholder={t('reports.ohs.filters.allStaffRoles')}
              options={enumValues(StaffRole).map((value) => ({
                value: String(value),
                label: t(`enums.staffRole.${value}`),
              }))}
              onChange={(next) =>
                resetToFirstPage(setStaffRole)(next === '' ? undefined : (Number(next) as StaffRole))
              }
            />

            <FilterSelect
              id="ohs-duty-type"
              label={t('reports.ohs.fields.dutyType')}
              value={dutyType === undefined ? '' : String(dutyType)}
              placeholder={t('reports.ohs.filters.allDutyTypes')}
              options={enumValues(AssignmentType).map((value) => ({
                value: String(value),
                label: t(`enums.assignmentType.${value}`),
              }))}
              onChange={(next) =>
                resetToFirstPage(setDutyType)(
                  next === '' ? undefined : (Number(next) as AssignmentType),
                )
              }
            />

            <FilterDate
              id="ohs-start-date"
              label={t('reports.common.periodStart')}
              value={startDate}
              onChange={resetToFirstPage(setStartDate)}
            />
            <FilterDate
              id="ohs-end-date"
              label={t('reports.common.periodEnd')}
              value={endDate}
              onChange={resetToFirstPage(setEndDate)}
            />

            <Button variant="light"
              onClick={() => {
                setOfficeId(undefined)
                setStaffRole(undefined)
                setDutyType(undefined)
                setStartDate('')
                setEndDate('')
                setSearch('')
                setPage(1)
              }}
            >
              {t('common.clear')}
            </Button>
          </div>
        
      </Card>

      <OfficePeriodSummary
        officeId={officeId}
        officeName={offices.data?.items.find((office) => office.id === officeId)?.displayName}
        startDate={startDate}
        endDate={endDate}
        items={officeReports.data?.items}
        isLoading={officeReports.isLoading}
        error={officeReports.error ? errorMessage(officeReports.error) : null}
      />

      <Card
        className="mb-4"
        header={<h2 className="card-title h6 mb-0 report-print-heading">{t('reports.ohs.list.title')}</h2>}
        footer={
          list.data && list.data.totalCount > 0 ? (
            <div className="d-print-none">
              <Pagination
                total={list.data.totalCount}
                page={page}
                pageSize={PAGE_SIZE}
                onPageChange={setPage}
              />
            </div>
          ) : undefined
        }
      >
        <DataTable
          label={t('reports.ohs.list.title')}
          columns={columns}
          rows={list.data?.items}
          rowKey={(row) => row.id}
          isLoading={list.isLoading}
          error={list.error ? errorMessage(list.error) : null}
          emptyMessage={t('reports.ohs.list.empty')}
        />
      </Card>

      <HazardClassBreakdown reportId={selectedId} onClear={() => setSelectedId(undefined)} />
    </div>
  )
}

/**
 * Period totals of one office.
 *
 * Everything here is derived from the single `office/{officeId}` response — no per-row request
 * is made — and each figure is printed as text next to its bar.
 */
function OfficePeriodSummary({
  officeId,
  officeName,
  startDate,
  endDate,
  items,
  isLoading,
  error,
}: {
  officeId: number | undefined
  officeName: string | undefined
  startDate: string
  endDate: string
  items: OhsReportDto[] | undefined
  isLoading: boolean
  error: string | null
}) {
  const { t } = useTranslation()

  const totals = useMemo(() => {
    const rows = items ?? []
    const assigned = rows.reduce((sum, row) => sum + row.totalMinutes, 0)
    const used = rows.reduce((sum, row) => sum + row.usedMonthlyMinutes, 0)
    const overtime = rows.reduce((sum, row) => sum + row.totalMonthlyFazlaOvertimeDuration, 0)

    const byRole = new Map<StaffRole, { assigned: number; used: number }>()
    for (const row of rows) {
      const bucket = byRole.get(row.staffRole) ?? { assigned: 0, used: 0 }
      bucket.assigned += row.totalMinutes
      bucket.used += row.usedMonthlyMinutes
      byRole.set(row.staffRole, bucket)
    }

    return { count: rows.length, assigned, used, overtime, byRole: [...byRole.entries()] }
  }, [items])

  if (!officeId) {
    return (
      <Card
        className="mb-4"
        header={
          <h2 className="card-title h6 mb-0 report-print-heading">
            {t('reports.ohs.summary.title')}
          </h2>
        
        }
      >
          <GateHint message={t('reports.ohs.summary.selectOffice')} />
        
      </Card>
    )
  }

  const periodLabel =
    startDate || endDate
      ? t('reports.common.periodRange', {
          from: formatDate(startDate) ?? t('reports.common.openStart'),
          to: formatDate(endDate) ?? t('reports.common.openEnd'),
        })
      : t('reports.common.wholePeriod')

  return (
    <Card
      className="mb-4"
      header={
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
        <h2 className="card-title h6 mb-0 report-print-heading">
          {t('reports.ohs.summary.title')}
        </h2>
        <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
          {officeName ?? t('common.none')} · {periodLabel}
        </span>
      
      </div>
      }
    >
        {isLoading && <Spinner />}
        {!isLoading && error && (
          <Alert variant="danger" className="mb-0">
            {error}
          </Alert>
        )}
        {!isLoading && !error && totals.count === 0 && (
          <EmptyHint message={t('reports.ohs.summary.empty')} />
        )}
        {!isLoading && !error && totals.count > 0 && (
          <>
            <div className="row g-3 mb-4">
              <div className="col-12 col-sm-6 col-xl-3">
                <SummaryCard
                  icon="☰"
                  tone="primary"
                  label={t('reports.ohs.summary.staffCount')}
                  value={formatNumber(totals.count) ?? '0'}
                />
              </div>
              <div className="col-12 col-sm-6 col-xl-3">
                <SummaryCard
                  icon="◷"
                  tone="info"
                  label={t('reports.ohs.summary.assignedMinutes')}
                  value={t('reports.common.minutes', { value: formatNumber(totals.assigned) })}
                />
              </div>
              <div className="col-12 col-sm-6 col-xl-3">
                <SummaryCard
                  icon="✓"
                  tone="success"
                  label={t('reports.ohs.summary.usedMinutes')}
                  value={t('reports.common.minutes', { value: formatNumber(totals.used) })}
                  hint={t('reports.ohs.summary.utilisationHint', {
                    value: percentOf(totals.used, totals.assigned),
                  })}
                />
              </div>
              <div className="col-12 col-sm-6 col-xl-3">
                <SummaryCard
                  icon="⚠"
                  tone="warning"
                  label={t('reports.ohs.summary.overtimeMinutes')}
                  value={t('reports.common.minutes', { value: formatNumber(totals.overtime) })}
                />
              </div>
            </div>

            <h3 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
              {t('reports.ohs.summary.byStaffRole')}
            </h3>
            {totals.byRole.map(([role, bucket]) => (
              <DistributionRow
                key={role}
                label={t(`enums.staffRole.${role}`)}
                value={bucket.used}
                total={totals.assigned}
                variant="primary"
                shareLabel={t('reports.ohs.summary.ofAssigned', {
                  assigned: formatNumber(bucket.assigned),
                })}
              />
            ))}
          </>
        )}
      
    </Card>
  )
}

/**
 * Hazard-class distribution of the workplaces one report covers.
 *
 * Rendered as a table plus Bootstrap progress bars: no charting dependency is available and a
 * clear table beats a chart that would need one. Every bar repeats its count as text.
 */
function HazardClassBreakdown({
  reportId,
  onClear,
}: {
  reportId: number | undefined
  onClear: () => void
}) {
  const { t } = useTranslation()

  const report = useOhsReport(reportId)
  const breakdown = useHazardClassBreakdown(reportId)

  if (!reportId) {
    return (
      <Card
        header={
          <h2 className="card-title h6 mb-0 report-print-heading">
            {t('reports.ohs.breakdown.title')}
          </h2>
        
        }
      >
          <GateHint message={t('reports.ohs.breakdown.selectRow')} />
        
      </Card>
    )
  }

  const buckets = breakdown.data?.items ?? []
  const byClass = new Map(buckets.map((bucket) => [bucket.hazardClass, bucket.companyCount]))
  const total = buckets.reduce((sum, bucket) => sum + bucket.companyCount, 0)
  const failure = report.error ?? breakdown.error

  const breakdownRows: BreakdownRow[] = [
    ...HAZARD_CLASSES.map((hazardClass) => ({
      key: String(hazardClass),
      label: t(`enums.hazardClass.${hazardClass}`),
      count: byClass.get(hazardClass) ?? 0,
      sharePercent: percentOf(byClass.get(hazardClass) ?? 0, total),
    })),
    {
      key: 'total',
      label: t('reports.common.total'),
      count: total,
      sharePercent: 100,
      isTotal: true,
    },
  ]

  const breakdownColumns: Column<BreakdownRow>[] = [
    {
      key: 'label',
      header: t('reports.ohs.fields.hazardClass'),
      render: (row) => <span className={row.isTotal ? 'fw-semibold' : undefined}>{row.label}</span>,
    },
    {
      key: 'count',
      header: t('reports.ohs.fields.companyCount'),
      align: 'end',
      render: (row) => (
        <span className={row.isTotal ? 'fw-semibold' : undefined}>{formatNumber(row.count)}</span>
      ),
    },
    {
      key: 'share',
      header: t('reports.ohs.fields.share'),
      align: 'end',
      render: (row) => (
        <span className={row.isTotal ? 'fw-semibold' : undefined}>
          {t('reports.common.percent', { value: row.sharePercent })}
        </span>
      ),
    },
  ]

  return (
    <Card
      header={
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
        <h2 className="card-title h6 mb-0 report-print-heading">
          {t('reports.ohs.breakdown.title')}
        </h2>
        <Button variant="light" size="sm" className="d-print-none" onClick={onClear}>
          {t('common.clear')}
        </Button>
      
      </div>
      }
    >
        {(report.isLoading || breakdown.isLoading) && <Spinner />}

        {!report.isLoading && !breakdown.isLoading && failure && (
          <Alert variant="danger" className="mb-0">
            {errorMessage(failure)}
          </Alert>
        )}

        {!report.isLoading && !breakdown.isLoading && !failure && (
          <div className="row g-4">
            <div className="col-12 col-lg-5">
              <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
                <Term label={t('reports.ohs.fields.employeeName')}>
                  {report.data?.employeeName ?? t('common.none')}
                </Term>
                <Term label={t('reports.ohs.fields.nationalId')}>
                  {report.data?.nationalId || t('common.none')}
                </Term>
                <Term label={t('reports.ohs.fields.staffRole')}>
                  {report.data ? t(`enums.staffRole.${report.data.staffRole}`) : t('common.none')}
                </Term>
                <Term label={t('reports.ohs.fields.dutyType')}>
                  {report.data ? t(`enums.assignmentType.${report.data.dutyType}`) : t('common.none')}
                </Term>
                <Term label={t('reports.ohs.fields.totalMinutes')}>
                  {t('reports.common.minutes', { value: formatNumber(report.data?.totalMinutes) })}
                </Term>
                <Term label={t('reports.ohs.fields.usedMonthlyMinutes')}>
                  {t('reports.common.minutes', {
                    value: formatNumber(report.data?.usedMonthlyMinutes),
                  })}
                </Term>
                <Term label={t('reports.ohs.fields.overtimeMinutes')}>
                  {t('reports.common.minutes', {
                    value: formatNumber(report.data?.totalMonthlyFazlaOvertimeDuration),
                  })}
                </Term>
                <Term label={t('reports.ohs.fields.archiveDetail')}>
                  {report.data?.moduleArchiveDetailId ?? t('common.none')}
                </Term>
                <Term label={t('reports.ohs.fields.creationTime')}>
                  {formatDate(report.data?.creationTime) ?? t('common.none')}
                </Term>
              </dl>
            </div>

            <div className="col-12 col-lg-7">
              <p className="mb-3" style={{ color: 'var(--kt-gray-600)' }}>
                {t('reports.ohs.breakdown.total', { value: formatNumber(total) })}
              </p>

              {HAZARD_CLASSES.map((hazardClass) => (
                <DistributionRow
                  key={hazardClass}
                  label={t(`enums.hazardClass.${hazardClass}`)}
                  value={byClass.get(hazardClass) ?? 0}
                  total={total}
                  variant={HAZARD_CLASS_BAR[hazardClass]}
                  shareLabel={t('reports.common.percent', {
                    value: percentOf(byClass.get(hazardClass) ?? 0, total),
                  })}
                />
              ))}

              <div className="mt-3">
                <DataTable
                  label={t('reports.ohs.breakdown.tableLabel')}
                  columns={breakdownColumns}
                  rows={breakdownRows}
                  rowKey={(row) => row.key}
                />
              </div>
            </div>
          </div>
        )}
      
    </Card>
  )
}
