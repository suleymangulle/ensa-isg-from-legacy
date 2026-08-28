import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Card, Input, Tabs } from 'rich-react-component'
import DataTable, { PageTitle, type Column } from '@/components/DataTable'
import { HAZARD_CLASS_BADGE, type LookupDto } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import {
  useCertificates,
  useCities,
  useDistricts,
  useDuties,
  useNeighborhoods,
  useOccupationCodes,
  usePeriods,
  type OccupationCodeLookupDto,
  type PeriodLookupDto,
} from './api'

const TABS = ['locations', 'occupationCodes', 'duties', 'certificates', 'periods'] as const

type TabKey = (typeof TABS)[number]

/**
 * Reference data browser.
 *
 * Read-only on purpose: `LookupController` exposes seven GET endpoints and no write endpoint at
 * all — the catalogues are seeded with the database, so there is nothing to save here.
 */
export default function LookupListPage() {
  const { t } = useTranslation()
  const [activeTab, setActiveTab] = useState<TabKey>('locations')

  return (
    <>
      <PageTitle title={t('lookup.page.title')} description={t('lookup.page.description')} />

      <Alert variant="info" className="border-0">
        {t('lookup.page.readOnlyNote')}
      </Alert>

      <Card>
        <Tabs
          items={TABS.map((tab) => ({
            key: tab,
            label: t(`lookup.tabs.${tab}`),
            content:
              tab === 'locations' ? (
                <LocationsTab />
              ) : tab === 'occupationCodes' ? (
                <OccupationCodesTab />
              ) : tab === 'duties' ? (
                <SimpleLookupTab kind="duties" />
              ) : tab === 'certificates' ? (
                <SimpleLookupTab kind="certificates" />
              ) : (
                <PeriodsTab />
              ),
          }))}
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as TabKey)}
          variant="underline"
        />
      </Card>
    </>
  )
}

/** Province -> district -> neighbourhood drill-down; each level is a request of its own. */
function LocationsTab() {
  const { t } = useTranslation()
  const [cityId, setCityId] = useState<number | null>(null)
  const [districtId, setDistrictId] = useState<number | null>(null)

  const cities = useCities()
  const districts = useDistricts(cityId)
  const neighborhoods = useNeighborhoods(districtId)

  const columns: Column<LookupDto>[] = [
    { key: 'displayName', header: t('lookup.fields.displayName'), render: (row) => row.displayName },
    { key: 'code', header: t('lookup.fields.code'), render: (row) => row.code ?? t('common.none') },
  ]

  return (
    <div className="row g-4">
      <div className="col-lg-4">
        <h2 className="h6 fw-bold mb-2" style={{ color: 'var(--kt-gray-700)' }}>
          {t('lookup.locations.cities')}
        </h2>
        <label htmlFor="lookup-city" className="visually-hidden">
          {t('lookup.locations.selectCity')}
        </label>
        <select
          id="lookup-city"
          className="form-select mb-3"
          size={12}
          value={cityId ?? ''}
          onChange={(event) => {
            setCityId(Number(event.target.value))
            setDistrictId(null)
          }}
        >
          {cities.data?.items.map((city) => (
            <option key={city.id} value={city.id}>
              {city.displayName}
            </option>
          ))}
        </select>
        {cities.error && (
          <p style={{ color: 'var(--kt-danger)' }}>{errorMessage(cities.error)}</p>
        )}
      </div>

      <div className="col-lg-4">
        <h2 className="h6 fw-bold mb-2" style={{ color: 'var(--kt-gray-700)' }}>
          {t('lookup.locations.districts')}
        </h2>
        <label htmlFor="lookup-district" className="visually-hidden">
          {t('lookup.locations.selectDistrict')}
        </label>
        <select
          id="lookup-district"
          className="form-select mb-3"
          size={12}
          value={districtId ?? ''}
          disabled={!cityId}
          onChange={(event) => setDistrictId(Number(event.target.value))}
        >
          {districts.data?.items.map((district) => (
            <option key={district.id} value={district.id}>
              {district.displayName}
            </option>
          ))}
        </select>
        {!cityId && (
          <p style={{ color: 'var(--kt-gray-500)' }}>{t('lookup.locations.selectCityFirst')}</p>
        )}
      </div>

      <div className="col-lg-4">
        <h2 className="h6 fw-bold mb-2" style={{ color: 'var(--kt-gray-700)' }}>
          {t('lookup.locations.neighborhoods')}
        </h2>
        {!districtId ? (
          <p style={{ color: 'var(--kt-gray-500)' }}>
            {t('lookup.locations.selectDistrictFirst')}
          </p>
        ) : (
          <DataTable
            label={t('lookup.locations.neighborhoods')}
            columns={columns}
            rows={neighborhoods.data?.items}
            rowKey={(row) => row.id}
            isLoading={neighborhoods.isLoading}
            error={neighborhoods.error ? errorMessage(neighborhoods.error) : null}
            emptyMessage={t('lookup.page.empty')}
          />
        )}
      </div>
    </div>
  )
}

/** NACE search. The list is long, so the endpoint is only asked once a term is entered. */
function OccupationCodesTab() {
  const { t } = useTranslation()
  const [filter, setFilter] = useState('')
  const occupationCodes = useOccupationCodes(filter)

  const columns: Column<OccupationCodeLookupDto>[] = [
    { key: 'code', header: t('lookup.fields.code'), render: (row) => row.code ?? t('common.none') },
    {
      key: 'displayName',
      header: t('lookup.fields.activity'),
      render: (row) => row.displayName,
    },
    {
      key: 'hazardClass',
      header: t('lookup.fields.hazardClass'),
      render: (row) => (
        <Badge variant={HAZARD_CLASS_BADGE[row.hazardClass]}>
          {t(`enums.hazardClass.${row.hazardClass}`)}
        </Badge>
      ),
    },
  ]

  return (
    <>
      <div style={{ maxWidth: 360 }}>
        <Input
          id="lookup-occupation-filter"
          type="search"
          label={t('lookup.occupationCodes.search')}
          placeholder={t('lookup.occupationCodes.searchPlaceholder')}
          value={filter}
          onChange={setFilter}
        />
      </div>

      <DataTable
        label={t('lookup.tabs.occupationCodes')}
        columns={columns}
        rows={occupationCodes.data?.items}
        rowKey={(row) => row.id}
        isLoading={occupationCodes.isLoading}
        error={occupationCodes.error ? errorMessage(occupationCodes.error) : null}
        emptyMessage={t('lookup.page.empty')}
      />
    </>
  )
}

function SimpleLookupTab({ kind }: { kind: 'duties' | 'certificates' }) {
  const { t } = useTranslation()
  const duties = useDuties()
  const certificates = useCertificates()
  const query = kind === 'duties' ? duties : certificates

  const columns: Column<LookupDto>[] = [
    { key: 'displayName', header: t('lookup.fields.displayName'), render: (row) => row.displayName },
    { key: 'code', header: t('lookup.fields.code'), render: (row) => row.code ?? t('common.none') },
    {
      key: 'isActive',
      header: t('lookup.fields.status'),
      align: 'center',
      render: (row) => (
        <Badge variant={row.isActive ? 'success' : 'danger'}>
          {row.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
  ]

  return (
    <DataTable
      label={t(`lookup.tabs.${kind}`)}
      columns={columns}
      rows={query.data?.items}
      rowKey={(row) => row.id}
      isLoading={query.isLoading}
      error={query.error ? errorMessage(query.error) : null}
      emptyMessage={t('lookup.page.empty')}
    />
  )
}

function PeriodsTab() {
  const { t } = useTranslation()
  const periods = usePeriods()

  const columns: Column<PeriodLookupDto>[] = [
    { key: 'displayName', header: t('lookup.fields.displayName'), render: (row) => row.displayName },
    {
      key: 'period',
      header: t('lookup.fields.period'),
      render: (row) =>
        t('lookup.periods.value', {
          value: row.periodValue,
          unit: t(`enums.periodUnit.${row.periodUnit}`),
        }),
    },
    {
      key: 'isActive',
      header: t('lookup.fields.status'),
      align: 'center',
      render: (row) => (
        <Badge variant={row.isActive ? 'success' : 'danger'}>
          {row.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
  ]

  return (
    <DataTable
      label={t('lookup.tabs.periods')}
      columns={columns}
      rows={periods.data?.items}
      rowKey={(row) => row.id}
      isLoading={periods.isLoading}
      error={periods.error ? errorMessage(periods.error) : null}
      emptyMessage={t('lookup.page.empty')}
    />
  )
}
