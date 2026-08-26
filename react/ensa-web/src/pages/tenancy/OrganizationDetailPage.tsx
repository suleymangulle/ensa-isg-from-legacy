import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import OrganizationFormModal from './OrganizationFormModal'
import { TENANCY_RESOURCES, useOrganizationDetail, type OrganizationNavigationDto } from './api'

const TABS = ['general', 'offices', 'subscription'] as const

type TabKey = (typeof TABS)[number]

export default function OrganizationDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()

  const [activeTab, setActiveTab] = useState<TabKey>('general')
  const [isEditOpen, setEditOpen] = useState(false)
  const [isDeleteOpen, setDeleteOpen] = useState(false)

  const { data, isLoading, error } = useOrganizationDetail(Number(id))
  const remove = useDelete(TENANCY_RESOURCES.organization, {
    onSuccess: () => navigate('/organizations', { replace: true }),
  })

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const organization = data.organization

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/organizations" className="text-decoration-none">
              {t('organization.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {organization.name}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={organization.name}
        description={t('organization.detail.subtitle', { code: organization.code })}
        action={
          <div className="d-flex flex-wrap gap-2">
            <button type="button" className="btn btn-primary" onClick={() => setEditOpen(true)}>
              {t('common.edit')}
            </button>
            <button
              type="button"
              className="btn btn-light-danger"
              onClick={() => setDeleteOpen(true)}
            >
              {t('common.delete')}
            </button>
          </div>
        }
      />

      <div className="row g-4 mb-4">
        <QuotaCard
          label={t('organization.detail.activeUserCount')}
          value={data.activeUserCount}
          quota={organization.maximumUserCount}
        />
        <QuotaCard
          label={t('organization.detail.activeCompanyCount')}
          value={data.activeCompanyCount}
          quota={organization.maximumCompanyCount}
        />
        <QuotaCard label={t('organization.detail.officeCount')} value={data.officeCount} />
      </div>

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
                  {t(`organization.detail.tabs.${tab}`)}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="card-body">
          {activeTab === 'general' && <GeneralTab detail={data} />}
          {activeTab === 'offices' && <OfficesTab detail={data} />}
          {activeTab === 'subscription' && <SubscriptionTab detail={data} />}
        </div>
      </div>

      {isEditOpen && (
        <OrganizationFormModal organization={organization} onClose={() => setEditOpen(false)} />
      )}

      <ConfirmDialog
        isOpen={isDeleteOpen}
        title={t('organization.actions.deleteTitle')}
        message={t('organization.actions.deleteMessage', { name: organization.name })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setDeleteOpen(false)}
        onConfirm={() => remove.mutate(organization.id)}
      />
    </>
  )
}

function QuotaCard({
  label,
  value,
  quota,
}: {
  label: string
  value: number
  quota?: number | null
}) {
  const { t } = useTranslation()

  return (
    <div className="col-sm-6 col-lg-4">
      <div className="card h-100">
        <div className="card-body">
          <div
            className="text-uppercase fw-semibold mb-2"
            style={{ color: 'var(--kt-gray-500)', fontSize: '0.6875rem', letterSpacing: '0.06em' }}
          >
            {label}
          </div>
          <div className="fs-2 fw-bold" style={{ color: 'var(--kt-gray-900)' }}>
            {value}
            {quota != null && (
              <span className="fs-6 fw-normal" style={{ color: 'var(--kt-gray-500)' }}>
                {' / '}
                {quota}
              </span>
            )}
          </div>
          {quota == null && (
            <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
              {t('organization.detail.unlimited')}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function Detail({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="col-md-6 col-xl-4 mb-4">
      <div
        className="text-uppercase fw-semibold mb-1"
        style={{ color: 'var(--kt-gray-500)', fontSize: '0.6875rem', letterSpacing: '0.06em' }}
      >
        {label}
      </div>
      <div style={{ color: 'var(--kt-gray-800)' }}>{children}</div>
    </div>
  )
}

function GeneralTab({ detail }: { detail: OrganizationNavigationDto }) {
  const { t } = useTranslation()
  const organization = detail.organization
  const none = t('common.none')

  return (
    <div className="row">
      <Detail label={t('organization.fields.code')}>{organization.code}</Detail>
      <Detail label={t('organization.fields.organizationType')}>
        {detail.organizationType?.displayName ?? none}
      </Detail>
      <Detail label={t('organization.fields.subscriptionPlan')}>
        {detail.subscriptionPlan?.displayName ?? none}
      </Detail>
      <Detail label={t('organization.fields.phone')}>{organization.phone ?? none}</Detail>
      <Detail label={t('organization.fields.email')}>{organization.email ?? none}</Detail>
      <Detail label={t('organization.fields.webUrl')}>{organization.webUrl ?? none}</Detail>
      <Detail label={t('organization.fields.taxOffice')}>{organization.taxTaxOffice ?? none}</Detail>
      <Detail label={t('organization.fields.taxNumber')}>{organization.taxNumber ?? none}</Detail>
      <Detail label={t('organization.fields.city')}>{detail.city?.displayName ?? none}</Detail>
      <Detail label={t('organization.fields.district')}>
        {detail.district?.displayName ?? none}
      </Detail>
      <Detail label={t('organization.fields.address')}>{organization.address ?? none}</Detail>
      <Detail label={t('organization.fields.authorizedFullName')}>
        {organization.authorizedFullName ?? none}
      </Detail>
      <Detail label={t('organization.fields.authorizedPhone')}>
        {organization.authorizedPhone ?? none}
      </Detail>
      <Detail label={t('organization.fields.authorizedEmail')}>
        {organization.authorizedEmail ?? none}
      </Detail>
      <Detail label={t('organization.fields.status')}>
        <span className={organization.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {organization.isActive ? t('common.active') : t('common.passive')}
        </span>
      </Detail>
    </div>
  )
}

function OfficesTab({ detail }: { detail: OrganizationNavigationDto }) {
  const { t } = useTranslation()

  if (detail.offices.length === 0) {
    return (
      <div className="text-center py-4" style={{ color: 'var(--kt-gray-500)' }}>
        {t('organization.detail.emptyOffices')}
      </div>
    )
  }

  return (
    <>
      {detail.headquarterOffice && (
        <p className="mb-3" style={{ color: 'var(--kt-gray-600)' }}>
          {t('organization.detail.headquarterOffice', {
            name: detail.headquarterOffice.displayName,
          })}
        </p>
      )}
      <ul className="list-unstyled mb-0">
        {detail.offices.map((office) => (
          <li key={office.id} className="mb-2">
            <Link to={`/offices/${office.id}`} className="text-decoration-none">
              {office.displayName}
            </Link>
            {!office.isActive && (
              <span className="badge-light-danger ms-2">{t('common.passive')}</span>
            )}
          </li>
        ))}
      </ul>
    </>
  )
}

function SubscriptionTab({ detail }: { detail: OrganizationNavigationDto }) {
  const { t } = useTranslation()
  const organization = detail.organization
  const contract = detail.currentContract
  const none = t('common.none')

  return (
    <div className="row">
      <Detail label={t('organization.fields.subscriptionStart')}>
        {formatDate(organization.subscriptionStart) ?? none}
      </Detail>
      <Detail label={t('organization.fields.subscriptionEnd')}>
        {formatDate(organization.subscriptionEnd) ?? t('organization.detail.openEnded')}
      </Detail>
      <Detail label={t('organization.fields.maximumUserCount')}>
        {organization.maximumUserCount ?? t('organization.detail.unlimited')}
      </Detail>
      <Detail label={t('organization.fields.maximumCompanyCount')}>
        {organization.maximumCompanyCount ?? t('organization.detail.unlimited')}
      </Detail>

      {contract ? (
        <>
          <Detail label={t('organization.fields.contractDate')}>
            {formatDate(contract.contractDate) ?? none}
          </Detail>
          <Detail label={t('organization.fields.contractStatus')}>
            {t(`enums.contractStatus.${contract.contractStatus}`)}
          </Detail>
          <Detail label={t('organization.fields.contractUserCount')}>{contract.userCount}</Detail>
          <Detail label={t('organization.fields.contractPaid')}>
            {contract.paid ? t('common.yes') : t('common.no')}
          </Detail>
        </>
      ) : (
        <div className="col-12" style={{ color: 'var(--kt-gray-500)' }}>
          {t('organization.detail.noContract')}
        </div>
      )}
    </div>
  )
}
