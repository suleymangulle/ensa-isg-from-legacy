import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Switch } from 'rich-react-component'
import DataTable, { PageTitle, Pagination, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { IncidentType } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import IncidentFormModal from './IncidentFormModal'
import {
  OBSERVATION_ENDPOINTS,
  useCompanyLookup,
  useIncidentDetail,
  useIncidentList,
  type IncidentListDto,
} from './api'
import {
  AlertPanel,
  FilterSelect,
  INCIDENT_TYPE_BADGE,
  RowActions,
  enumValues,
} from './components'

const PAGE_SIZE = 20

/** Only these incident types carry a statutory SSI notification obligation. */
function requiresSsiNotification(type: IncidentType): boolean {
  return type === IncidentType.WorkAccident || type === IncidentType.OccupationalDisease
}

export default function IncidentListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [incidentType, setIncidentType] = useState('')
  const [onlyPending, setOnlyPending] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [deleting, setDeleting] = useState<IncidentListDto | null>(null)

  const companies = useCompanyLookup()

  const { data, isLoading, error } = useIncidentList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'IncidentDate DESC',
    filter: search,
    companyId: companyId ? Number(companyId) : undefined,
    incidentType: incidentType ? (Number(incidentType) as IncidentType) : undefined,
    onlySsiNotificationPending: onlyPending || undefined,
  })

  // One extra request for the whole screen — the banner needs the count, not the rows.
  const pending = useIncidentList({
    skipCount: 0,
    maxResultCount: 1,
    sorting: 'IncidentDate ASC',
    companyId: companyId ? Number(companyId) : undefined,
    onlySsiNotificationPending: true,
  })

  // The edit dialog needs the full record; the row only carries the grid projection.
  const editing = useIncidentDetail(editingId ?? undefined)

  const remove = useDelete(OBSERVATION_ENDPOINTS.incident, {
    onSuccess: () => setDeleting(null),
  })

  function resetPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const pendingCount = pending.data?.totalCount ?? 0

  const columns: Column<IncidentListDto>[] = [
    {
      key: 'accent',
      header: '',
      width: '6px',
      render: (row) => (
        <span
          aria-hidden="true"
          className="d-block rounded"
          style={{
            width: 4,
            height: 34,
            backgroundColor: row.ssiNotificationOverdue
              ? 'var(--kt-danger)'
              : requiresSsiNotification(row.incidentType) && !row.ssiNotificationDate
                ? 'var(--kt-warning)'
                : 'transparent',
          }}
        />
      ),
    },
    {
      key: 'incidentDate',
      header: t('incident.fields.incidentDate'),
      render: (row) => (
        <Link to={`/incidents/${row.id}`} className="fw-semibold text-decoration-none">
          {formatDate(row.incidentDate) ?? t('common.none')}
        </Link>
      ),
    },
    {
      key: 'company',
      header: t('incident.fields.company'),
      render: (row) => row.companyName ?? t('common.none'),
    },
    {
      key: 'department',
      header: t('incident.fields.department'),
      render: (row) => row.departmentName ?? t('common.none'),
    },
    {
      key: 'incidentType',
      header: t('incident.fields.incidentType'),
      render: (row) => (
        <Badge variant={INCIDENT_TYPE_BADGE[row.incidentType]}>
          {t(`enums.incidentType.${row.incidentType}`)}
        </Badge>
      ),
    },
    {
      key: 'accidentType',
      header: t('incident.fields.accidentType'),
      render: (row) => t(`enums.accidentType.${row.accidentType}`),
    },
    {
      key: 'lostWorkDays',
      header: t('incident.fields.lostWorkDaysShort'),
      align: 'end',
      render: (row) => row.lostWorkDays ?? t('common.none'),
    },
    {
      key: 'ssi',
      header: t('incident.fields.ssiStatus'),
      render: (row) => <SsiStatus row={row} />,
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '120px',
      render: (row) => (
        <RowActions
          editLabel={t('incident.list.editAction')}
          deleteLabel={t('incident.list.deleteAction')}
          onEdit={() => setEditingId(row.id)}
          onDelete={() => setDeleting(row)}
        />
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('incident.list.title')}
        description={t('incident.list.description')}
        action={
          <Button variant="primary" onClick={() => setIsCreating(true)}>
            {t('incident.list.create')}
          </Button>
        }
      />

      {pendingCount > 0 && (
        <AlertPanel tone="danger">
          <div>
            <strong className="d-block">
              {t('incident.ssi.bannerTitle', { total: pendingCount })}
            </strong>
            <span>{t('incident.ssi.bannerDescription')}</span>
          </div>
          <Button variant="danger" size="sm"
            onClick={() => {
              setOnlyPending(true)
              setPage(1)
            }}
            disabled={onlyPending}
          >
            {t('incident.ssi.showPending')}
          </Button>
        </AlertPanel>
      )}

      <Card
        
        header={
          <SearchBar
            value={search}
            onChange={resetPage(setSearch)}
            placeholder={t('incident.list.searchPlaceholder')}
          >
            <FilterSelect
              id="incident-filter-company"
              label={t('incident.fields.company')}
              value={companyId}
              onChange={resetPage(setCompanyId)}
              width={220}
            >
              <option value="">{t('incident.list.allCompanies')}</option>
              {companies.data?.items.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.displayName}
                </option>
              ))}
            </FilterSelect>

            <FilterSelect
              id="incident-filter-type"
              label={t('incident.fields.incidentType')}
              value={incidentType}
              onChange={resetPage(setIncidentType)}
            >
              <option value="">{t('incident.list.allTypes')}</option>
              {enumValues(IncidentType).map((value) => (
                <option key={value} value={value}>
                  {t(`enums.incidentType.${value}`)}
                </option>
              ))}
            </FilterSelect>

            <Switch
              id="incident-filter-pending"
              checked={onlyPending}
              onChange={(checked) => {
                setOnlyPending(checked)
                setPage(1)
              }}
              label={t('incident.list.onlyPending')}
            />
          </SearchBar>
        }
        footer={
          data && data.totalCount > 0 ? (
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          ) : undefined
        }
      >
        <DataTable
          label={t('incident.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(row) => row.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('incident.list.empty')}
        />
      </Card>

      {isCreating && <IncidentFormModal onClose={() => setIsCreating(false)} />}

      {editingId !== null && editing.data && (
        <IncidentFormModal incident={editing.data.incident} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={deleting !== null}
        title={t('incident.list.deleteTitle')}
        message={t('incident.list.deleteMessage', {
          date: formatDate(deleting?.incidentDate) ?? '',
          company: deleting?.companyName ?? '',
        })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
      />
    </>
  )
}

/** SSI notification state of one row, as a colour-coded badge. */
function SsiStatus({ row }: { row: IncidentListDto }) {
  const { t } = useTranslation()

  if (row.ssiNotificationDate) {
    return (
      <Badge variant="success">
        {t('incident.ssi.notified', { date: formatDate(row.ssiNotificationDate) ?? '' })}
      </Badge>
    )
  }

  if (!requiresSsiNotification(row.incidentType)) {
    return <span style={{ color: 'var(--kt-gray-500)' }}>{t('incident.ssi.notRequired')}</span>
  }

  if (row.ssiNotificationOverdue) {
    return <Badge variant="danger" className="fw-bold">{t('incident.ssi.overdue')}</Badge>
  }

  return (
    <Badge variant="warning">
      {t('incident.ssi.dueBy', { date: formatDate(row.latestSsiNotificationDate) ?? '' })}
    </Badge>
  )
}
