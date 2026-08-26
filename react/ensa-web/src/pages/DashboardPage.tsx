import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/auth/AuthContext'
import { PageTitle, Spinner } from '@/components/DataTable'
import { http, errorMessage, type ListResult, type PagedResult } from '@/api/http'
import { formatNumber } from '@/utils/format'

/**
 * Operational home page.
 *
 * The cards are not decoration: each one is a statutory obligation that has a deadline, and the
 * three "attention" counters are the ones that turn into findings during an inspection. Every
 * figure is read from the API — nothing is computed in the browser — and each links to the screen
 * where the work is actually done.
 */

/** Counts a paged endpoint without transferring its rows. */
function useTotalCount(resource: string) {
  return useQuery({
    queryKey: [resource, 'count'],
    queryFn: async () => {
      const { data } = await http.get<PagedResult<unknown>>(`/${resource}`, {
        params: { skipCount: 0, maxResultCount: 1 },
      })
      return data.totalCount
    },
  })
}

/** Counts an unpaged `{ items: [] }` endpoint. */
function useItemCount(path: string, key: string) {
  return useQuery({
    queryKey: [key, 'attention'],
    queryFn: async () => {
      const { data } = await http.get<ListResult<unknown>>(`/${path}`)
      return data.items.length
    },
  })
}

interface Metric {
  key: string
  labelKey: string
  value: number | undefined
  isLoading: boolean
  error: unknown
  tone: 'primary' | 'success' | 'warning' | 'danger'
  icon: string
  to: string
}

function MetricCard({ metric }: { metric: Metric }) {
  const { t } = useTranslation()

  return (
    <div className="col-12 col-sm-6 col-xl-3">
      <Link to={metric.to} className="text-decoration-none">
        <div className="card h-100">
          <div className="card-body d-flex align-items-center gap-3">
            <span
              className="d-inline-flex align-items-center justify-content-center flex-shrink-0"
              style={{
                width: 52,
                height: 52,
                borderRadius: 12,
                fontSize: 22,
                backgroundColor: `var(--kt-${metric.tone}-light)`,
                color: `var(--kt-${metric.tone})`,
              }}
              aria-hidden="true"
            >
              {metric.icon}
            </span>
            <div className="min-w-0">
              <div
                className="fw-bold"
                style={{ fontSize: '1.75rem', color: 'var(--kt-gray-900)', lineHeight: 1.2 }}
              >
                {metric.isLoading
                  ? '…'
                  : metric.error
                    ? '—'
                    : (formatNumber(metric.value) ?? '—')}
              </div>
              <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
                {t(metric.labelKey)}
              </div>
            </div>
          </div>
        </div>
      </Link>
    </div>
  )
}

export default function DashboardPage() {
  const { t } = useTranslation()
  const { user } = useAuth()

  const companies = useTotalCount('company')
  const employees = useTotalCount('company-employee')
  const visits = useTotalCount('visit')
  const tickets = useTotalCount('support-ticket')

  const overdueInspections = useItemCount('equipment/overdue-inspections', 'equipment')
  const overdueActions = useItemCount('corrective-action/overdue', 'corrective-action')
  const expiringAssessments = useItemCount(
    'risk-assessment-report/expiring',
    'risk-assessment-report',
  )

  const metrics: Metric[] = [
    {
      key: 'companies',
      labelKey: 'dashboard.cards.activeCompanies',
      value: companies.data,
      isLoading: companies.isLoading,
      error: companies.error,
      tone: 'primary',
      icon: '▦',
      to: '/companies',
    },
    {
      key: 'employees',
      labelKey: 'dashboard.cards.totalEmployees',
      value: employees.data,
      isLoading: employees.isLoading,
      error: employees.error,
      tone: 'success',
      icon: '☰',
      to: '/employees',
    },
    {
      key: 'visits',
      labelKey: 'dashboard.cards.visits',
      value: visits.data,
      isLoading: visits.isLoading,
      error: visits.error,
      tone: 'primary',
      icon: '◷',
      to: '/visits',
    },
    {
      key: 'tickets',
      labelKey: 'dashboard.cards.supportTickets',
      value: tickets.data,
      isLoading: tickets.isLoading,
      error: tickets.error,
      tone: 'warning',
      icon: '✉',
      to: '/support-tickets',
    },
  ]

  const attention = [
    {
      key: 'overdueInspections',
      labelKey: 'dashboard.attention.overdueInspections',
      descriptionKey: 'dashboard.attention.overdueInspectionsHint',
      query: overdueInspections,
      to: '/equipment',
    },
    {
      key: 'overdueActions',
      labelKey: 'dashboard.attention.overdueActions',
      descriptionKey: 'dashboard.attention.overdueActionsHint',
      query: overdueActions,
      to: '/corrective-actions',
    },
    {
      key: 'expiringAssessments',
      labelKey: 'dashboard.attention.expiringAssessments',
      descriptionKey: 'dashboard.attention.expiringAssessmentsHint',
      query: expiringAssessments,
      to: '/risk-assessments',
    },
  ]

  const attentionLoading = attention.some((item) => item.query.isLoading)

  return (
    <>
      <PageTitle
        title={t('dashboard.welcome', { name: user?.fullName ?? '' })}
        description={t('dashboard.description')}
      />

      <div className="row g-4 mb-4">
        {metrics.map((metric) => (
          <MetricCard key={metric.key} metric={metric} />
        ))}
      </div>

      <div className="row g-4">
        <div className="col-12 col-xl-7">
          <div className="card h-100">
            <div className="card-header">
              <h2 className="card-title h6">{t('dashboard.attention.title')}</h2>
            </div>
            <div className="card-body">
              {attentionLoading ? (
                <Spinner />
              ) : (
                <ul className="list-unstyled mb-0">
                  {attention.map((item) => {
                    const count = item.query.data ?? 0
                    const failed = Boolean(item.query.error)

                    return (
                      <li
                        key={item.key}
                        className="d-flex align-items-start gap-3 py-3"
                        style={{ borderTop: '1px solid var(--kt-gray-200)' }}
                      >
                        <span
                          className="d-inline-flex align-items-center justify-content-center fw-bold flex-shrink-0"
                          style={{
                            minWidth: 44,
                            height: 32,
                            borderRadius: 8,
                            padding: '0 8px',
                            backgroundColor:
                              count > 0 ? 'var(--kt-danger-light)' : 'var(--kt-success-light)',
                            color: count > 0 ? 'var(--kt-danger)' : 'var(--kt-success)',
                          }}
                        >
                          {failed ? '—' : (formatNumber(count) ?? '0')}
                        </span>
                        <div className="min-w-0">
                          <Link to={item.to} className="fw-semibold text-decoration-none">
                            {t(item.labelKey)}
                          </Link>
                          <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
                            {failed
                              ? errorMessage(item.query.error)
                              : t(item.descriptionKey)}
                          </div>
                        </div>
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
          </div>
        </div>

        <div className="col-12 col-xl-5">
          <div className="card h-100">
            <div className="card-header">
              <h2 className="card-title h6">{t('dashboard.session.title')}</h2>
            </div>
            <div className="card-body">
              <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
                <dt className="col-5" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                  {t('dashboard.session.userName')}
                </dt>
                <dd className="col-7 fw-semibold">{user?.userName}</dd>

                <dt className="col-5" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                  {t('dashboard.session.tenant')}
                </dt>
                <dd className="col-7 fw-semibold">{user?.tenantId ?? t('common.host')}</dd>

                <dt className="col-5" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                  {t('dashboard.session.roles')}
                </dt>
                <dd className="col-7">
                  {user?.roles.length
                    ? user.roles.map((role) => (
                        <span key={role} className="badge badge-light-primary me-1 mb-1">
                          {role}
                        </span>
                      ))
                    : t('common.none')}
                </dd>

                <dt className="col-5" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
                  {t('dashboard.session.permissionCount')}
                </dt>
                <dd className="col-7 fw-semibold">{user?.permissions.length ?? 0}</dd>
              </dl>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
