import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { errorMessage } from '@/api/http'
import { useActivityDetail, type ActivityNavigationDto } from './api'
import ActivityFormModal from './ActivityFormModal'

/**
 * One activity of the catalogue, shown in its place in the tree: the parent it hangs under and
 * the children that hang under it, each a link so the hierarchy can be walked.
 */
export default function ActivityDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const activityId = Number(id)

  const { data, isLoading, error } = useActivityDetail(activityId)
  const [isEditOpen, setEditOpen] = useState(false)
  const [isChildOpen, setChildOpen] = useState(false)

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const activity = data.activity

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/activities" className="text-decoration-none">
              {t('activity.list.title')}
            </Link>
          </li>
          {data.parentActivity && (
            <li className="breadcrumb-item">
              <Link to={`/activities/${data.parentActivity.id}`} className="text-decoration-none">
                {data.parentActivity.displayName}
              </Link>
            </li>
          )}
          <li className="breadcrumb-item active" aria-current="page">
            {activity.activityName}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={activity.activityName}
        description={
          activity.activityCode
            ? t('activity.detail.code', { value: activity.activityCode })
            : undefined
        }
        action={
          <div className="d-flex gap-2">
            <button className="btn btn-light" type="button" onClick={() => setChildOpen(true)}>
              {t('activity.detail.addChild')}
            </button>
            <button
              className="btn btn-light-primary"
              type="button"
              onClick={() => setEditOpen(true)}
            >
              {t('common.edit')}
            </button>
          </div>
        }
      />

      <div className="row g-4">
        <div className="col-lg-7">
          <GeneralCard detail={data} />
        </div>

        <div className="col-lg-5">
          <div className="card h-100">
            <div className="card-header">
              <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                {t('activity.detail.hierarchy')}
              </h2>
            </div>
            <div className="card-body">
              <h3
                className="text-uppercase fw-semibold mb-2"
                style={{ color: 'var(--kt-gray-500)', fontSize: '0.6875rem', letterSpacing: '0.08em' }}
              >
                {t('activity.fields.parentActivity')}
              </h3>
              {data.parentActivity ? (
                <Link
                  to={`/activities/${data.parentActivity.id}`}
                  className="d-inline-block mb-4 text-decoration-none fw-semibold"
                >
                  {data.parentActivity.displayName}
                </Link>
              ) : (
                <p className="mb-4">
                  <span className="badge-light-primary">{t('activity.root')}</span>
                </p>
              )}

              <h3
                className="text-uppercase fw-semibold mb-2"
                style={{ color: 'var(--kt-gray-500)', fontSize: '0.6875rem', letterSpacing: '0.08em' }}
              >
                {t('activity.detail.children')}
              </h3>
              {data.childActivities.length ? (
                <ul className="list-unstyled mb-0 d-flex flex-column gap-2">
                  {data.childActivities.map((child) => (
                    <li key={child.id}>
                      <Link to={`/activities/${child.id}`} className="text-decoration-none">
                        {child.displayName}
                      </Link>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
                  {t('activity.detail.noChildren')}
                </p>
              )}
            </div>
          </div>
        </div>
      </div>

      {isEditOpen && (
        <ActivityFormModal activity={activity} onClose={() => setEditOpen(false)} />
      )}

      {isChildOpen && (
        <ActivityFormModal parentActivityId={activity.id} onClose={() => setChildOpen(false)} />
      )}
    </>
  )
}

/** Header facts of the catalogue entry. */
function GeneralCard({ detail }: { detail: ActivityNavigationDto }) {
  const { t } = useTranslation()
  const activity = detail.activity
  const none = t('common.none')

  return (
    <div className="card h-100">
      <div className="card-header">
        <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
          {t('activity.detail.general')}
        </h2>
      </div>
      <div className="card-body">
        <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
          <Term label={t('activity.fields.activityType')}>
            {t(`enums.activityType.${activity.activityType}`)}
          </Term>
          <Term label={t('activity.fields.activityGroup')}>
            {detail.activityGroup?.displayName ?? none}
          </Term>
          <Term label={t('activity.fields.period')}>{detail.period?.displayName ?? none}</Term>
          <Term label={t('activity.fields.defaultActivity')}>
            {activity.defaultActivity ? t('common.yes') : t('common.no')}
          </Term>
          <Term label={t('activity.fields.defaultCount')}>{activity.defaultCount}</Term>
          <Term label={t('activity.fields.defaultStartMonthOffset')}>
            {activity.defaultStartMonthOffset}
          </Term>
          <Term label={t('activity.fields.defaultElementCondition')}>
            {activity.defaultElementCondition}
          </Term>
          <Term label={t('activity.fields.orderNo')}>{activity.orderNo ?? none}</Term>
          <Term label={t('activity.fields.scope')}>
            <span className={activity.tenantId == null ? 'badge-light-info' : 'badge-light-primary'}>
              {activity.tenantId == null ? t('activity.scope.shared') : t('activity.scope.private')}
            </span>
          </Term>
          <Term label={t('activity.fields.status')}>
            <span className={activity.isActive ? 'badge-light-success' : 'badge-light-danger'}>
              {activity.isActive ? t('common.active') : t('common.passive')}
            </span>
          </Term>
        </dl>
      </div>
    </div>
  )
}

/** One `<dt>`/`<dd>` pair of a definition list. */
function Term({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <>
      <dt className="col-sm-5" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-7">{children}</dd>
    </>
  )
}
