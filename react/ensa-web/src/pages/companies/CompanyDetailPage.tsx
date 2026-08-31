import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  HAZARD_CLASS_BADGE,
  useCompanyDetail,
  type CompanyNavigationDto,
  type CompanyWarningSummaryDto,
} from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { Avatar, Badge, Button, Card, Tabs, type TabItem } from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import CompanyFormModal from './CompanyFormModal'

const TABS = ['general', 'departments', 'specialists', 'branches'] as const

type TabKey = (typeof TABS)[number]

/** Counter fields of the warning summary paired with their label keys. */
const WARNING_FIELDS: { field: keyof CompanyWarningSummaryDto; labelKey: string }[] = [
  { field: 'isSafetyTrainingNoneCount', labelKey: 'company.warnings.isSafetyTrainingNone' },
  { field: 'isSafetyTrainingMissingCount', labelKey: 'company.warnings.isSafetyTrainingMissing' },
  { field: 'isHealthTrainingNoneCount', labelKey: 'company.warnings.isHealthTrainingNone' },
  { field: 'isHealthTrainingMissingCount', labelKey: 'company.warnings.isHealthTrainingMissing' },
  {
    field: 'preEmploymentHealthExaminationMissingCount',
    labelKey: 'company.warnings.preEmploymentHealthExaminationMissing',
  },
  {
    field: 'equipmentExaminationMissingCount',
    labelKey: 'company.warnings.equipmentExaminationMissing',
  },
]

export default function CompanyDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const [activeTab, setActiveTab] = useState<TabKey>('general')
  const [isEditing, setEditing] = useState(false)

  const { data, isLoading, error } = useCompanyDetail(Number(id))

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const company = data.company

  // The counter on the tab tells the user whether a section is worth opening; a tab that opens
  // onto "no records" is the small waste this removes.
  const counts: Partial<Record<TabKey, number>> = {
    departments: data.departments.length,
    specialists: data.assignedSpecialists.length,
    branches: data.branches.length,
  }

  const tabs: TabItem[] = [
    { key: 'general', label: t('company.detail.tabs.general'), content: <GeneralTab detail={data} /> },
    {
      key: 'departments',
      label: t('company.detail.tabs.departments'),
      badge: counts.departments,
      content: (
        <LookupList
          items={data.departments.map((item) => item.displayName)}
          emptyMessage={t('company.detail.emptyDepartments')}
        />
      ),
    },
    {
      key: 'specialists',
      label: t('company.detail.tabs.specialists'),
      badge: counts.specialists,
      content: <SpecialistList detail={data} />,
    },
    {
      key: 'branches',
      label: t('company.detail.tabs.branches'),
      badge: counts.branches,
      content: (
        <LookupList
          items={data.branches.map((item) => item.displayName)}
          emptyMessage={t('company.detail.emptyBranches')}
        />
      ),
    },
  ]

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/companies" className="text-decoration-none">
              {t('company.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {company.companyName}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={company.companyName || t('company.detail.fallbackTitle')}
        description={
          company.ssiNumber
            ? t('company.detail.ssiNumber', { value: company.ssiNumber })
            : undefined
        }
        action={
          <Button variant="light" onClick={() => setEditing(true)}>
            {t('common.edit')}
          </Button>
        }
      />

      <Card>
        <Tabs
          items={tabs}
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as TabKey)}
          variant="underline"
        />
      </Card>

      {isEditing && <CompanyFormModal company={company} onClose={() => setEditing(false)} />}
    </>
  )
}

function GeneralTab({ detail }: { detail: CompanyNavigationDto }) {
  const { t } = useTranslation()
  const company = detail.company
  const none = t('common.none')

  const cityDistrict =
    [detail.city?.displayName, detail.district?.displayName].filter(Boolean).join(' / ') || none

  return (
    <>
      <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
        <Term label={t('company.fields.hazardClass')}>
          <Badge variant={HAZARD_CLASS_BADGE[company.hazardClass]}>
            {t(`enums.hazardClass.${company.hazardClass}`)}
          </Badge>
        </Term>

        <Term label={t('company.fields.workplaceType')}>
          {t(`enums.workplaceType.${company.workplaceType}`)}
        </Term>

        {detail.headquarterCompany && (
          <Term label={t('company.fields.headquarterCompany')}>
            {detail.headquarterCompany.displayName}
          </Term>
        )}

        <Term label={t('company.fields.cityDistrict')}>{cityDistrict}</Term>
        <Term label={t('company.fields.address')}>{company.address ?? none}</Term>
        <Term label={t('company.fields.authorizedPerson')}>{company.authorizedPerson ?? none}</Term>
        <Term label={t('company.fields.phone')}>{company.phone ?? none}</Term>
        <Term label={t('company.fields.email')}>{company.email ?? none}</Term>
        <Term label={t('company.fields.taxOffice')}>{company.taxOffice ?? none}</Term>
        <Term label={t('company.fields.taxNumber')}>{company.taxNumber ?? none}</Term>
        {detail.office && <Term label={t('company.fields.office')}>{detail.office.displayName}</Term>}
        <Term label={t('company.fields.activeEmployeeCount')}>{detail.activeEmployeeCount}</Term>
        <Term label={t('company.fields.status')}>
          <Badge variant={company.isActive ? 'success' : 'danger'}>
            {company.isActive ? t('common.active') : t('common.passive')}
          </Badge>
        </Term>
        {company.notes && (
          <Term label={t('company.fields.note')}>{company.notes}</Term>
        )}
      </dl>

      {detail.warningSummary && <WarningSummary summary={detail.warningSummary} />}
    </>
  )
}

function WarningSummary({ summary }: { summary: CompanyWarningSummaryDto }) {
  const { t } = useTranslation()
  const rows = WARNING_FIELDS.filter(({ field }) => Number(summary[field]) > 0)
  if (!rows.length) return null

  return (
    <div className="mt-4 pt-4" style={{ borderTop: '1px solid var(--kt-border-color)' }}>
      <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
        {t('company.detail.warningSummary')}
      </h2>
      <ul className="list-unstyled mb-3 d-flex flex-wrap gap-2">
        {rows.map(({ field, labelKey }) => (
          <li key={field}>
            <Badge variant="warning">
              {t(labelKey)}: {String(summary[field])}
            </Badge>
          </li>
        ))}
      </ul>
      <p className="mb-0 fw-semibold" style={{ color: 'var(--kt-gray-700)' }}>
        {t('company.detail.totalMissing', { count: summary.totalMissing })}
      </p>
    </div>
  )
}

function SpecialistList({ detail }: { detail: CompanyNavigationDto }) {
  const { t } = useTranslation()

  if (!detail.assignedSpecialists.length) {
    return (
      <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
        {t('company.detail.emptySpecialists')}
      </p>
    )
  }

  return (
    <ul className="list-unstyled mb-0 d-flex flex-column gap-2">
      {detail.assignedSpecialists.map((specialist) => (
        <li key={specialist.id} className="d-flex flex-wrap align-items-center gap-2">
          <Avatar name={specialist.fullName} size="sm" />
          <span className="fw-semibold" style={{ color: 'var(--kt-gray-800)' }}>
            {specialist.fullName}
          </span>
          <Badge variant="info">{t(`enums.staffRole.${specialist.staffRole}`)}</Badge>
          <Badge variant={specialist.isActive ? 'success' : 'danger'}>
            {specialist.isActive ? t('common.active') : t('common.passive')}
          </Badge>
        </li>
      ))}
    </ul>
  )
}

function LookupList({ items, emptyMessage }: { items: string[]; emptyMessage: string }) {
  if (!items.length) {
    return (
      <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
        {emptyMessage}
      </p>
    )
  }

  return (
    <ul className="list-unstyled mb-0 d-flex flex-wrap gap-2">
      {items.map((item) => (
        <li key={item}>
          <Badge variant="primary">{item}</Badge>
        </li>
      ))}
    </ul>
  )
}

/** One `<dt>`/`<dd>` pair of the definition list. */
function Term({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <>
      <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-9">{children}</dd>
    </>
  )
}
