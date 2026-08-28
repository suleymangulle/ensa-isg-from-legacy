import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Select } from 'rich-react-component'
import DataTable, { Pagination, PageTitle, type Column } from '@/components/DataTable'
import { ConfirmDialog, SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { useEntity, useLookup } from '@/api/endpoints'
import { ActivityType } from '@/api/enums'
import {
  ACTIVITY_TYPES,
  RESOURCES,
  useActivityList,
  type ActivityDto,
  type ActivityListDto,
} from './api'
import ActivityFormModal from './ActivityFormModal'

const PAGE_SIZE = 20

/**
 * Activity catalogue.
 *
 * Activities are the master data a work plan line points at, and they form a tree: a parent
 * heading with the individual items beneath it. Parent names come from one lookup request plus
 * the rows already on screen — never a request per row — and the tree itself is walked either by
 * filtering on a parent here or through the detail page.
 */
export default function ActivityListPage() {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [activityType, setActivityType] = useState<ActivityType | null>(null)
  const [parentFilter, setParentFilter] = useState<number | null>(null)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<ActivityListDto | null>(null)

  const { data, isLoading, error } = useActivityList({
    skipCount: (page - 1) * PAGE_SIZE,
    maxResultCount: PAGE_SIZE,
    sorting: 'OrderNo ASC, ActivityName ASC',
    filter: search,
    activityType,
    parentActivityId: parentFilter,
  })

  const lookup = useLookup(RESOURCES.activity)
  const { data: editing } = useEntity<ActivityDto>(RESOURCES.activity, editingId ?? undefined)
  const remove = useDelete(RESOURCES.activity, { onSuccess: () => setDeleting(null) })

  /** Id → name, assembled once from the lookup and the rows on screen. */
  const names = useMemo(() => {
    const map = new Map<number, string>()
    for (const item of lookup.data?.items ?? []) map.set(item.id, item.displayName)
    for (const row of data?.items ?? []) map.set(row.id, row.activityName)
    return map
  }, [lookup.data, data])

  function resetToFirstPage<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value)
      setPage(1)
    }
  }

  const columns: Column<ActivityListDto>[] = [
    {
      key: 'activityName',
      header: t('activity.fields.activityName'),
      render: (activity) => (
        <Link to={`/activities/${activity.id}`} className="fw-semibold text-decoration-none">
          {activity.activityName}
        </Link>
      ),
    },
    {
      key: 'activityCode',
      header: t('activity.fields.activityCode'),
      render: (activity) => activity.activityCode ?? t('common.none'),
    },
    {
      key: 'parent',
      header: t('activity.fields.parentActivity'),
      render: (activity) =>
        activity.parentActivityId ? (
          <Link
            to={`/activities/${activity.parentActivityId}`}
            className="text-decoration-none"
          >
            {names.get(activity.parentActivityId) ?? t('activity.unnamedParent')}
          </Link>
        ) : (
          <Badge variant="primary">{t('activity.root')}</Badge>
        ),
    },
    {
      key: 'activityType',
      header: t('activity.fields.activityType'),
      render: (activity) => t(`enums.activityType.${activity.activityType}`),
    },
    {
      key: 'defaultActivity',
      header: t('activity.fields.defaultActivity'),
      align: 'center',
      render: (activity) =>
        activity.defaultActivity ? (
          <Badge variant="info">{t('common.yes')}</Badge>
        ) : (
          t('common.no')
        ),
    },
    {
      key: 'orderNo',
      header: t('activity.fields.orderNo'),
      align: 'end',
      render: (activity) => activity.orderNo ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('activity.fields.status'),
      align: 'center',
      render: (activity) => (
        <Badge variant={activity.isActive ? 'success' : 'danger'}>
          {activity.isActive ? t('common.active') : t('common.passive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '180px',
      render: (activity) => (
        <div className="d-flex justify-content-end flex-wrap gap-1">
          <Button variant="light" size="sm"
            onClick={() => resetToFirstPage(setParentFilter)(activity.id)}
            aria-label={t('activity.list.childrenAria', { name: activity.activityName })}
          >
            {t('activity.list.children')}
          </Button>
          <Button variant="light" size="sm"
            onClick={() => setEditingId(activity.id)}
            aria-label={t('activity.list.editAria', { name: activity.activityName })}
          >
            {t('common.edit')}
          </Button>
          <Button variant="light" size="sm" 
            onClick={() => setDeleting(activity)}
            aria-label={t('activity.list.deleteAria', { name: activity.activityName })}
          >
            {t('common.delete')}
          </Button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageTitle
        title={t('activity.list.title')}
        description={t('activity.list.description')}
        action={
          <Button variant="primary" onClick={() => setCreateOpen(true)}>
            {t('activity.list.create')}
          </Button>
        }
      />

      <Card
        
        header={
          <SearchBar
            value={search}
            onChange={resetToFirstPage(setSearch)}
            placeholder={t('activity.list.searchPlaceholder')}
          >
            <div>
              <label htmlFor="activity-type-filter" className="visually-hidden">
                {t('activity.fields.activityType')}
              </label>
              <Select
                id="activity-type-filter"
                placeholder={t('activity.list.allTypes')}
                options={ACTIVITY_TYPES.map((value) => ({
                  value,
                  label: t(`enums.activityType.${value}`),
                }))}
                value={activityType}
                onChange={resetToFirstPage(setActivityType)}
              />
            </div>
            {parentFilter !== null && (
              <Button variant="light"
                onClick={() => resetToFirstPage(setParentFilter)(null)}
              >
                {t('activity.list.clearParent', {
                  name: names.get(parentFilter) ?? t('activity.unnamedParent'),
                })}
              </Button>
            )}
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
          label={t('activity.list.title')}
          columns={columns}
          rows={data?.items}
          rowKey={(activity) => activity.id}
          isLoading={isLoading}
          error={error ? errorMessage(error) : null}
          emptyMessage={t('activity.list.empty')}
        />
      </Card>

      {isCreateOpen && (
        <ActivityFormModal parentActivityId={parentFilter} onClose={() => setCreateOpen(false)} />
      )}

      {editingId !== null && editing && (
        <ActivityFormModal activity={editing} onClose={() => setEditingId(null)} />
      )}

      <ConfirmDialog
        isOpen={deleting !== null}
        title={t('activity.list.deleteTitle')}
        message={t('activity.list.deleteMessage', { name: deleting?.activityName ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}
