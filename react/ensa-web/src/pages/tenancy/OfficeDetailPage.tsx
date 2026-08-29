import { useMemo, useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card } from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { useLookup } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import OfficeFormModal from './OfficeFormModal'
import { TENANCY_RESOURCES, useOfficeDetail } from './api'

export default function OfficeDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()

  const [isEditOpen, setEditOpen] = useState(false)
  const [isDeleteOpen, setDeleteOpen] = useState(false)

  const { data, isLoading, error } = useOfficeDetail(Number(id))

  // The office record names its company only by id; the shared company lookup resolves it.
  const companies = useLookup('company')
  const companyName = useMemo(() => {
    const companyId = data?.office.companyId
    if (companyId == null) return null
    return (
      companies.data?.items.find((company) => company.id === companyId)?.displayName ?? null
    )
  }, [companies.data, data])

  const remove = useDelete(TENANCY_RESOURCES.office, {
    onSuccess: () => navigate('/offices', { replace: true }),
  })

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const office = data.office
  const none = t('common.none')

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/offices" className="text-decoration-none">
              {t('office.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {office.name}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={office.name}
        description={
          data.organization
            ? t('office.detail.subtitle', { organization: data.organization.displayName })
            : undefined
        }
        action={
          <div className="d-flex flex-wrap gap-2">
            <Button variant="primary" onClick={() => setEditOpen(true)}>
              {t('common.edit')}
            </Button>
            <Button variant="light" 
              onClick={() => setDeleteOpen(true)}
            >
              {t('common.delete')}
            </Button>
          </div>
        }
      />

      <div className="d-flex flex-wrap gap-2 mb-4">
        <Badge variant={office.isActive ? 'success' : 'danger'}>
          {office.isActive ? t('common.active') : t('common.passive')}
        </Badge>
        {office.headquarterOffice && (
          <Badge variant="primary">{t('office.badges.headquarter')}</Badge>
        )}
      </div>

      <div className="row g-4 mb-4">
        <CounterCard label={t('office.detail.userCount')} value={data.userCount} />
        <CounterCard
          label={t('office.detail.cashRegisterCount')}
          value={data.cashRegisterCount}
        />
      </div>

      <Card
        header={
          <h2 className="card-title h6 mb-0">{t('office.detail.general')}</h2>
        
        }
      >
          <div className="row">
            <Detail label={t('office.fields.organization')}>
              {data.organization?.displayName ?? t('common.host')}
            </Detail>
            <Detail label={t('office.fields.company')}>
              {office.companyId == null
                ? t('office.form.attachedToOrganization')
                : (companyName ??
                  t('office.list.unresolvedCompany', { id: office.companyId }))}
            </Detail>
            <Detail label={t('office.fields.phone')}>{office.phone ?? none}</Detail>
            <Detail label={t('office.fields.fax')}>{office.fax ?? none}</Detail>
            <Detail label={t('office.fields.city')}>{data.city?.displayName ?? none}</Detail>
            <Detail label={t('office.fields.district')}>
              {data.district?.displayName ?? none}
            </Detail>
            <Detail label={t('office.fields.address')}>{office.address ?? none}</Detail>
            <Detail label={t('office.fields.authorizedPerson')}>
              {office.authorizedPerson ?? none}
            </Detail>
            <Detail label={t('office.fields.authorizedEmail')}>
              {office.authorizedEmail ?? none}
            </Detail>
          </div>
        
      </Card>

      {isEditOpen && <OfficeFormModal office={office} onClose={() => setEditOpen(false)} />}

      <ConfirmDialog
        isOpen={isDeleteOpen}
        title={t('office.actions.deleteTitle')}
        message={t('office.actions.deleteMessage', { name: office.name })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setDeleteOpen(false)}
        onConfirm={() => remove.mutate(office.id)}
      />
    </>
  )
}

function CounterCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="col-sm-6 col-lg-4">
      <Card
        className="h-100"
      >
          <div
            className="text-uppercase fw-semibold mb-2"
            style={{ color: 'var(--kt-gray-500)', fontSize: '0.6875rem', letterSpacing: '0.06em' }}
          >
            {label}
          </div>
          <div className="fs-2 fw-bold" style={{ color: 'var(--kt-gray-900)' }}>
            {value}
          </div>
        
      </Card>
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
