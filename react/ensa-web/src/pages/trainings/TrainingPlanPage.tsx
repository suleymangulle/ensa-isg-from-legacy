import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import {
  ENDPOINTS,
  PLAN_LINE_STATUS_BADGE,
  usePagedList,
} from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import type { TrainingPlanLineListDto } from './api'
import { formatDate } from '@/utils/format'

const PAGE_SIZE = 20

export default function TrainingPlanPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const { data, isLoading, error } = usePagedList<TrainingPlanLineListDto>(
    ENDPOINTS.trainingPlanLine,
    {
      skipCount: (page - 1) * PAGE_SIZE,
      maxResultCount: PAGE_SIZE,
      sorting: 'Year DESC, Month DESC',
      filter: search,
    },
  )

  /** "March 2026" built from the numeric year/month pair. */
  function formatPeriod(line: TrainingPlanLineListDto): string {
    if (!line.year) return t('common.none')
    const month = line.month ? t(`enums.month.${line.month}`) : ''
    return `${month} ${line.year}`.trim()
  }

  const columns: Column<TrainingPlanLineListDto>[] = [
    {
      key: 'companyName',
      header: t('trainingPlan.fields.companyName'),
      render: (line) => <span className="fw-semibold">{line.companyName ?? t('common.none')}</span>,
    },
    {
      key: 'trainingName',
      header: t('trainingPlan.fields.trainingName'),
      render: (line) => line.trainingName ?? t('common.none'),
    },
    {
      key: 'period',
      header: t('trainingPlan.fields.period'),
      render: formatPeriod,
    },
    {
      key: 'duration',
      header: t('trainingPlan.fields.duration'),
      align: 'end',
      render: (line) => t('trainingPlan.minutes', { count: line.durationMinutes }),
    },
    {
      key: 'instructor',
      header: t('trainingPlan.fields.instructor'),
      render: (line) => line.instructorFullName ?? t('common.none'),
    },
    {
      key: 'performedDate',
      header: t('trainingPlan.fields.performedDate'),
      render: (line) => formatDate(line.performedDate) ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('trainingPlan.fields.status'),
      align: 'center',
      render: (line) => (
        <span className={PLAN_LINE_STATUS_BADGE[line.status]}>
          {t(`enums.planLineStatus.${line.status}`)}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('trainingPlan.list.title')}
        description={t('trainingPlan.list.description')}
        action={
          <Link to="/training-plans/plans" className="btn btn-primary">
            {t('trainingPlans.list.title')}
          </Link>
        }
      />

      <div className="card">
        <div className="card-header">
          <input
            className="form-control"
            style={{ maxWidth: 320 }}
            placeholder={t('trainingPlan.list.searchPlaceholder')}
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            aria-label={t('trainingPlan.list.searchLabel')}
          />
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('trainingPlan.list.title')}
            columns={columns}
            rows={data?.items}
            rowKey={(line) => line.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('trainingPlan.list.empty')}
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
    </>
  )
}
