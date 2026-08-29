import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, type BadgeVariant } from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { errorMessage } from '@/api/http'
import { ConfirmDialog } from '@/components/Form'
import { formatDate, formatNumber } from '@/utils/format'
import YearEndReviewFormModal from './YearEndReviewFormModal'
import YearEndReviewLineFormModal, { type ParentOption } from './YearEndReviewLineFormModal'
import {
  useRemoveYearEndReviewLine,
  useYearEndReviewDetail,
  type YearEndReviewLineDto,
  type YearEndReviewLineNavigationDto,
} from './api'
import {
  DistributionRow,
  EmptyHint,
  PrintButton,
  ReportPeriodBanner,
  ReportPrintStyles,
  RowActions,
  Term,
  percentOf,
} from './components'

/** A work item paired with its depth, so the tree can be rendered as an indented table. */
interface FlatLine {
  line: YearEndReviewLineDto
  depth: number
}

/** Depth-first walk of the work item tree; the API already returns each level in order. */
function flatten(nodes: YearEndReviewLineNavigationDto[], depth = 0): FlatLine[] {
  return nodes.flatMap((node) => [
    { line: node.line, depth },
    ...flatten(node.childActivities, depth + 1),
  ])
}

/**
 * Year-end review report detail — `/reports/year-end/:id`.
 *
 * This is a statutory document, so the workplace and the reporting date lead the page and the
 * printout. One request (`GET api/year-end-review-report/{id}/detail`) returns the header, the
 * workplace and the whole work item tree, which is flattened here for display — no request is
 * made per node.
 */
export default function YearEndReviewDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const reportId = Number(id)

  const [isEditing, setIsEditing] = useState(false)
  const [addingUnder, setAddingUnder] = useState<{ parentId?: number } | undefined>()
  const [editingLine, setEditingLine] = useState<YearEndReviewLineDto | undefined>()
  const [pendingDelete, setPendingDelete] = useState<YearEndReviewLineDto | undefined>()

  const detail = useYearEndReviewDetail(reportId)
  const removeLine = useRemoveYearEndReviewLine(reportId)

  const flatLines = useMemo(() => flatten(detail.data?.activities ?? []), [detail.data])

  const parentOptions: ParentOption[] = useMemo(
    () =>
      flatLines.map((item) => ({
        id: item.line.id,
        label: item.line.work || `#${item.line.id}`,
        depth: item.depth,
      })),
    [flatLines],
  )

  if (detail.isLoading) return <Spinner />
  if (detail.error) return <ErrorPanel message={errorMessage(detail.error)} />
  if (!detail.data) return <ErrorPanel message={t('errors.notFound')} />

  const report = detail.data.yearEndReviewReport
  const companyName =
    detail.data.company?.displayName ??
    t('reports.common.companyFallback', { id: report.companyId })

  const workforce: { key: string; value: number; variant: BadgeVariant }[] = [
    { key: 'maleWorker', value: report.maleWorker ?? 0, variant: 'primary' },
    { key: 'femaleWorker', value: report.femaleWorker ?? 0, variant: 'info' },
    { key: 'youngWorker', value: report.youngWorker ?? 0, variant: 'warning' },
    { key: 'childWorker', value: report.childWorker ?? 0, variant: 'danger' },
  ]
  const headcount = (report.maleWorker ?? 0) + (report.femaleWorker ?? 0)

  const columns: Column<FlatLine>[] = [
    {
      key: 'work',
      header: t('reports.yearEnd.fields.work'),
      render: (item) => (
        <span style={{ paddingInlineStart: item.depth * 20 }}>
          {item.depth > 0 && (
            <span aria-hidden="true" style={{ color: 'var(--kt-gray-400)' }}>
              ↳{' '}
            </span>
          )}
          <span className={item.depth === 0 ? 'fw-semibold' : undefined}>
            {item.line.work || t('common.none')}
          </span>
        </span>
      ),
    },
    {
      key: 'date',
      header: t('reports.yearEnd.fields.date'),
      render: (item) => formatDate(item.line.date) ?? t('common.none'),
    },
    {
      key: 'personVeTitle',
      header: t('reports.yearEnd.fields.personVeTitle'),
      render: (item) => item.line.personAndTitle || t('common.none'),
    },
    {
      key: 'repeatCount',
      header: t('reports.yearEnd.fields.repeatCount'),
      align: 'end',
      render: (item) => item.line.repeatCount || t('common.none'),
    },
    {
      key: 'usedMethod',
      header: t('reports.yearEnd.fields.usedMethod'),
      render: (item) => item.line.usedMethod || t('common.none'),
    },
    {
      key: 'resultVeComment',
      header: t('reports.yearEnd.fields.resultVeComment'),
      render: (item) => item.line.resultAndComment || t('common.none'),
    },
    {
      key: 'status',
      header: t('reports.yearEnd.fields.status'),
      align: 'center',
      render: (item) => (
        <Badge variant={item.line.isActive ? 'success' : 'danger'}>
          {item.line.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      render: (item) => (
        <RowActions
          editLabel={t('reports.yearEnd.lineActions.edit', {
            name: item.line.work || `#${item.line.id}`,
          })}
          deleteLabel={t('reports.yearEnd.lineActions.delete', {
            name: item.line.work || `#${item.line.id}`,
          })}
          onEdit={() => setEditingLine(item.line)}
          onDelete={() => setPendingDelete(item.line)}
          extra={
            <Button variant="light" size="sm"
              aria-label={t('reports.yearEnd.lineActions.addChild', {
                name: item.line.work || `#${item.line.id}`,
              })}
              title={t('reports.yearEnd.detail.addChild')}
              onClick={() => setAddingUnder({ parentId: item.line.id })}
            >
              <span aria-hidden="true">＋</span>
            </Button>
          }
        />
      ),
    },
  ]

  return (
    <div className="report-print">
      <ReportPrintStyles />

      <nav aria-label={t('nav.breadcrumb')} className="mb-3 d-print-none">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/reports/year-end" className="text-decoration-none">
              {t('reports.yearEnd.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {report.reportTitle}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={report.reportTitle || t('reports.yearEnd.fallbackTitle')}
        description={t('reports.yearEnd.detail.description')}
        action={
          <div className="d-flex gap-2">
            <PrintButton />
            <Button variant="light" className="d-print-none"
              onClick={() => setIsEditing(true)}
            >
              {t('common.edit')}
            </Button>
          </div>
        }
      />

      <ReportPeriodBanner
        companyLabel={t('reports.yearEnd.fields.company')}
        companyName={companyName}
        periodLabel={t('reports.yearEnd.fields.reportDate')}
        periodValue={formatDate(report.reportDate) ?? t('common.none')}
        extraLabel={t('reports.yearEnd.fields.status')}
        extraValue={report.isActive ? t('common.active') : t('common.passive')}
      />

      <div className="row g-4 mb-4">
        <div className="col-12 col-xl-7">
          <Card
            className="h-100"
            header={
              <h2 className="card-title h6 mb-0 report-print-heading">
                {t('reports.yearEnd.detail.headerTitle')}
              </h2>
            
            }
          >
              <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
                <Term label={t('reports.yearEnd.fields.reportTitle')}>{report.reportTitle}</Term>
                <Term label={t('reports.yearEnd.fields.company')}>{companyName}</Term>
                <Term label={t('reports.yearEnd.fields.reportDate')}>
                  {formatDate(report.reportDate) ?? t('common.none')}
                </Term>
                <Term label={t('reports.yearEnd.fields.specialistFullName')}>
                  {report.specialistFullName || t('common.none')}
                </Term>
                <Term label={t('reports.yearEnd.fields.physicianFullName')}>
                  {report.physicianFullName || t('common.none')}
                </Term>
                <Term label={t('reports.yearEnd.fields.deputyFullName')}>
                  {report.deputyFullName || t('common.none')}
                </Term>
                <Term label={t('reports.common.lineCount')}>{formatNumber(flatLines.length)}</Term>
              </dl>
            
          </Card>
        </div>

        <div className="col-12 col-xl-5">
          <Card
            className="h-100"
            header={
              <h2 className="card-title h6 mb-0 report-print-heading">
                {t('reports.yearEnd.detail.workforceTitle')}
              </h2>
            
            }
          >
              {headcount === 0 ? (
                <EmptyHint message={t('reports.yearEnd.detail.emptyWorkforce')} />
              ) : (
                <>
                  <p className="mb-3" style={{ color: 'var(--kt-gray-600)' }}>
                    {t('reports.yearEnd.detail.headcount', { value: formatNumber(headcount) })}
                  </p>
                  {workforce.map((item) => (
                    <DistributionRow
                      key={item.key}
                      label={t(`reports.yearEnd.fields.${item.key}`)}
                      value={item.value}
                      total={headcount}
                      variant={item.variant}
                      shareLabel={t('reports.common.percent', {
                        value: percentOf(item.value, headcount),
                      })}
                    />
                  ))}
                </>
              )}
            
          </Card>
        </div>
      </div>

      <Card
        
        header={
        <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
          <h2 className="card-title h6 mb-0 report-print-heading">
            {t('reports.yearEnd.detail.activitiesTitle')}
          </h2>
          <Button variant="primary" size="sm" className="d-print-none"
            onClick={() => setAddingUnder({ parentId: undefined })}
          >
            {t('reports.yearEnd.detail.addLine')}
          </Button>
        
        </div>
        }
      >
          <DataTable
            label={t('reports.yearEnd.detail.activitiesTitle')}
            columns={columns}
            rows={flatLines}
            rowKey={(item) => item.line.id}
            emptyMessage={t('reports.yearEnd.detail.emptyActivities')}
          />
        
      </Card>

      {isEditing && (
        <YearEndReviewFormModal report={report} onClose={() => setIsEditing(false)} />
      )}
      {addingUnder && (
        <YearEndReviewLineFormModal
          reportId={reportId}
          defaultParentId={addingUnder.parentId}
          parents={parentOptions}
          onClose={() => setAddingUnder(undefined)}
        />
      )}
      {editingLine && (
        <YearEndReviewLineFormModal
          reportId={reportId}
          line={editingLine}
          parents={parentOptions}
          onClose={() => setEditingLine(undefined)}
        />
      )}

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('reports.yearEnd.detail.deleteLineTitle')}
        message={t('reports.yearEnd.detail.deleteLineMessage')}
        isBusy={removeLine.isPending}
        error={removeLine.error ? errorMessage(removeLine.error) : null}
        onCancel={() => setPendingDelete(undefined)}
        onConfirm={() =>
          pendingDelete &&
          removeLine.mutate(pendingDelete.id, { onSuccess: () => setPendingDelete(undefined) })
        }
      />
    </div>
  )
}
