import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Badge, Button, Card, CheckBox, Input, Select } from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { Modal } from '@/components/Form'
import { errorMessage } from '@/api/http'
import {
  flattenPermissions,
  useSaveUserPermissions,
  useUserLookup,
  usePermissionTree,
  useUserPermissions,
  type PermissionTreeNodeDto,
} from './api'

/** What the administrator has chosen for one permission, before saving. */
type Override = 'inherit' | 'grant' | 'deny'

const OVERRIDES: Override[] = ['inherit', 'grant', 'deny']

/**
 * Module a permission belongs to, taken from its target (`Ensa.Company.Create` -> `Company`).
 *
 * The catalogue is grouped this way rather than by `ParentPermissionId`: the seeder currently
 * emits all 171 entries as roots with no parent link, so the "tree" endpoint returns a flat
 * list and the target string is the only thing that actually carries the grouping.
 */
function moduleOf(node: PermissionTreeNodeDto): string {
  const parts = node.permissionTarget.split('.')
  return parts.length > 1 ? parts[1] : parts[0]
}

/** A permission whose name repeats its target is the module's own "access this area" entry. */
function isModuleDefault(node: PermissionTreeNodeDto): boolean {
  return node.permissionName === node.permissionTarget
}

interface PermissionGroup {
  key: string
  nodes: PermissionTreeNodeDto[]
}

/**
 * Permission assignment screen.
 *
 * The catalogue holds 171 entries, so it is fetched once through `GET api/permission/tree` and
 * rendered from memory — never one request per row. Editing is deliberately explicit: each row
 * carries a three-state override (inherit / grant / deny) rather than a checkbox, because "not
 * ticked" hides the difference between *not granted here* and *explicitly denied*, and a denial
 * overrides every grant the staff role brings.
 */
export default function PermissionMatrixPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()

  const userIdParam = searchParams.get('userId')
  const userId = userIdParam ? Number(userIdParam) : undefined

  const [userFilter, setUserFilter] = useState('')
  const [search, setSearch] = useState('')
  const [onlyChanged, setOnlyChanged] = useState(false)
  const [expanded, setExpanded] = useState<Record<string, boolean>>({})
  const [isConfirmOpen, setConfirmOpen] = useState(false)
  const [draft, setDraft] = useState<Record<number, Override>>({})
  const [draftUserId, setDraftUserId] = useState<number | null>(null)

  const users = useUserLookup(userFilter)
  const tree = usePermissionTree()
  const permissions = useUserPermissions(userId)
  const save = useSaveUserPermissions(userId ?? 0)

  // The draft mirrors the saved overrides until the administrator changes something, and is
  // rebuilt whenever a different user is selected.
  useEffect(() => {
    const data = permissions.data
    if (!data || draftUserId === data.userId) return

    const next: Record<number, Override> = {}
    for (const id of data.grantedPermissionIds) next[id] = 'grant'
    for (const id of data.deniedPermissionIds) next[id] = 'deny'
    setDraft(next)
    setDraftUserId(data.userId)
  }, [permissions.data, draftUserId])

  const baseline = useMemo(() => {
    const map: Record<number, Override> = {}
    for (const id of permissions.data?.grantedPermissionIds ?? []) map[id] = 'grant'
    for (const id of permissions.data?.deniedPermissionIds ?? []) map[id] = 'deny'
    return map
  }, [permissions.data])

  const effectiveNow = useMemo(
    () => new Set((permissions.data?.effectivePermissions ?? []).map((item) => item.id)),
    [permissions.data],
  )

  const allNodes = useMemo(
    () => flattenPermissions(tree.data?.roots ?? []),
    [tree.data],
  )

  /** The whole catalogue grouped by module, in catalogue order. */
  const allGroups = useMemo(() => {
    const map = new Map<string, PermissionTreeNodeDto[]>()
    for (const node of allNodes) {
      const key = moduleOf(node)
      const bucket = map.get(key)
      if (bucket) bucket.push(node)
      else map.set(key, [node])
    }
    return [...map.entries()]
      .map(([key, nodes]) => ({ key, nodes }))
      .sort((left, right) => left.key.localeCompare(right.key))
  }, [allNodes])

  /** The groups after the search and "only changed" filters, empty groups dropped. */
  const groups: PermissionGroup[] = useMemo(() => {
    const term = search.trim().toLocaleLowerCase()

    return allGroups
      .map((group) => ({
        key: group.key,
        nodes: group.nodes.filter((node) => {
          const matchesSearch =
            term === '' ||
            node.permissionName.toLocaleLowerCase().includes(term) ||
            node.permissionTarget.toLocaleLowerCase().includes(term)

          const state = draft[node.id] ?? 'inherit'
          const savedState = baseline[node.id] ?? 'inherit'
          return matchesSearch && (!onlyChanged || state !== savedState)
        }),
      }))
      .filter((group) => group.nodes.length > 0)
  }, [allGroups, search, onlyChanged, draft, baseline])

  /** What the pending save changes, in the terms the administrator cares about. */
  const preview = useMemo(() => {
    const gains: PermissionTreeNodeDto[] = []
    const losses: PermissionTreeNodeDto[] = []
    const cleared: PermissionTreeNodeDto[] = []

    for (const node of allNodes) {
      const state = draft[node.id] ?? 'inherit'
      const savedState = baseline[node.id] ?? 'inherit'
      if (state === savedState) continue

      if (state === 'grant' && !effectiveNow.has(node.id)) gains.push(node)
      else if (state === 'deny' && effectiveNow.has(node.id)) losses.push(node)
      else cleared.push(node)
    }

    return { gains, losses, cleared, total: gains.length + losses.length + cleared.length }
  }, [allNodes, draft, baseline, effectiveNow])

  // A search or a "only changed" filter has already narrowed the list, so the matching groups
  // open by themselves; browsing the full 171-row catalogue starts collapsed instead.
  const isFiltered = search.trim() !== '' || onlyChanged

  function selectUser(nextId: string) {
    setSearchParams(nextId ? { userId: nextId } : {})
    setDraftUserId(null)
    setDraft({})
    setSearch('')
    setOnlyChanged(false)
    setExpanded({})
  }

  function setOverride(permissionId: number, value: Override) {
    setDraft((previous) => {
      const next = { ...previous }
      if (value === 'inherit') delete next[permissionId]
      else next[permissionId] = value
      return next
    })
  }

  function submit() {
    const granted: number[] = []
    const denied: number[] = []
    for (const [key, value] of Object.entries(draft)) {
      if (value === 'grant') granted.push(Number(key))
      else if (value === 'deny') denied.push(Number(key))
    }

    save.mutate(
      { grantedPermissionIds: granted, deniedPermissionIds: denied },
      { onSuccess: () => setConfirmOpen(false) },
    )
  }

  const isSystemAdministrator = permissions.data?.systemAdministrator ?? false

  return (
    <>
      <PageTitle
        title={t('permission.page.title')}
        description={t('permission.page.description')}
        action={
          userId && !isSystemAdministrator ? (
            <Button variant="primary"
              disabled={preview.total === 0}
              onClick={() => setConfirmOpen(true)}
            >
              {t('permission.page.review', { count: preview.total })}
            </Button>
          ) : undefined
        }
      />

      <Card className="mb-4">
        <div className="row g-3 align-items-end">
          <Input
            id="permission-user-filter"
            type="search"
            label={t('permission.page.searchUser')}
            className="col-md-4"
            value={userFilter}
            placeholder={t('permission.page.searchUserPlaceholder')}
            onChange={setUserFilter}
          />

          <Select
            id="permission-user"
            label={t('permission.page.subject')}
            className="col-md-5"
            placeholder={t('permission.page.subjectPlaceholder')}
            options={
              users.data?.items.map((user) => ({
                value: String(user.id),
                label: user.displayName,
              })) ?? []
            }
            value={userIdParam ?? null}
            onChange={(value) => selectUser(value ?? '')}
          />

          <div className="col-md-3">
            <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
              {t('permission.page.roleScopeNote')}
            </p>
          </div>
        </div>
      </Card>

      {tree.error && <ErrorPanel message={errorMessage(tree.error)} />}
      {permissions.error && <ErrorPanel message={errorMessage(permissions.error)} />}

      {!userId && !tree.error && (
        <Card>
          <div className="text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
            {t('permission.page.selectSubject')}
          </div>
        </Card>
      )}

      {userId && (tree.isLoading || permissions.isLoading) && <Spinner />}

      {userId && tree.data && permissions.data && (
        <>
          {isSystemAdministrator && (
            <Alert variant="warning">{t('permission.page.systemAdministratorNote')}</Alert>
          )}

          <Card className="mb-4">
            <div className="row g-3 align-items-end">
              <Input
                id="permission-search"
                type="search"
                label={t('permission.page.searchPermission')}
                className="col-md-4"
                value={search}
                placeholder={t('permission.page.searchPermissionPlaceholder')}
                onChange={setSearch}
              />

              <CheckBox
                id="permission-only-changed"
                className="col-md-3"
                checked={onlyChanged}
                onChange={setOnlyChanged}
                label={t('permission.page.onlyChanged')}
              />

              <div className="col-md-2">
                <Button variant="light" className="w-100"
                  disabled={isFiltered}
                  onClick={() =>
                    setExpanded(
                      Object.values(expanded).some(Boolean)
                        ? {}
                        : Object.fromEntries(allGroups.map((group) => [group.key, true])),
                    )
                  }
                >
                  {Object.values(expanded).some(Boolean)
                    ? t('permission.page.collapseAll')
                    : t('permission.page.expandAll')}
                </Button>
              </div>

              <div className="col-md-3 text-md-end">
                <Badge variant="success" className="me-2">
                  {t('permission.page.effectiveCount', { count: effectiveNow.size })}
                </Badge>
                <Badge variant="primary">
                  {t('permission.page.catalogueCount', { count: tree.data.totalCount })}
                </Badge>
              </div>
            </div>
          </Card>

          {groups.length === 0 ? (
            <Card>
              <div className="text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
                {t('permission.page.noMatch')}
              </div>
            </Card>
          ) : (
            groups.map((group) => {
              const isOpen = isFiltered || (expanded[group.key] ?? false)
              const effectiveInGroup = group.nodes.filter((node) =>
                effectiveNow.has(node.id),
              ).length
              const changedInGroup = group.nodes.filter(
                (node) => (draft[node.id] ?? 'inherit') !== (baseline[node.id] ?? 'inherit'),
              ).length

              // Built per group (not hoisted) so each column's `render` can close over this
              // group's `t(...)` arguments (e.g. the override <select>'s aria-label names the
              // group) without threading the group through every row.
              const permissionColumns: Column<PermissionTreeNodeDto>[] = [
                {
                  key: 'permission',
                  header: t('permission.fields.permission'),
                  render: (node) => {
                    const state = draft[node.id] ?? 'inherit'
                    const savedState = baseline[node.id] ?? 'inherit'
                    const isChanged = state !== savedState
                    const label = isModuleDefault(node)
                      ? t('permission.fields.defaultAccess', { module: group.key })
                      : node.permissionName
                    return (
                      <>
                        <span
                          className={isChanged ? 'fw-semibold' : undefined}
                          style={{ color: isChanged ? 'var(--kt-primary)' : 'var(--kt-gray-800)' }}
                        >
                          {label}
                        </span>
                        {node.permissionDescription && (
                          <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
                            {node.permissionDescription}
                          </div>
                        )}
                      </>
                    )
                  },
                },
                {
                  key: 'target',
                  header: t('permission.fields.target'),
                  render: (node) => (
                    <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
                      {node.permissionTarget}
                    </span>
                  ),
                },
                {
                  key: 'type',
                  header: t('permission.fields.type'),
                  render: (node) => t(`enums.permissionType.${node.permissionType}`),
                },
                {
                  key: 'current',
                  header: t('permission.fields.current'),
                  align: 'center',
                  render: (node) => {
                    const savedState = baseline[node.id] ?? 'inherit'
                    return (
                      <>
                        <Badge variant={effectiveNow.has(node.id) ? 'success' : 'danger'}>
                          {effectiveNow.has(node.id)
                            ? t('permission.state.effective')
                            : t('permission.state.notEffective')}
                        </Badge>
                        {savedState !== 'inherit' && (
                          <Badge variant="primary" className="ms-1">
                            {t(`permission.override.${savedState}`)}
                          </Badge>
                        )}
                      </>
                    )
                  },
                },
                {
                  key: 'override',
                  header: t('permission.fields.override'),
                  align: 'end',
                  width: '210px',
                  // Kept as a raw <select>, not the library's `Select`: `Select` always wraps
                  // its control in a `mb-3` field div (see FieldShell), which would inflate this
                  // cell's height inside the data grid row — a visible regression the rest of
                  // the row (plain badges/text) doesn't share.
                  render: (node) => {
                    const state = draft[node.id] ?? 'inherit'
                    const label = isModuleDefault(node)
                      ? t('permission.fields.defaultAccess', { module: group.key })
                      : node.permissionName
                    return (
                      <select
                        className="form-select form-select-sm"
                        style={{ maxWidth: 190, marginInlineStart: 'auto' }}
                        value={state}
                        disabled={isSystemAdministrator}
                        aria-label={t('permission.page.overrideLabel', { name: label })}
                        onChange={(event) =>
                          setOverride(node.id, event.target.value as Override)
                        }
                      >
                        {OVERRIDES.map((option) => (
                          <option key={option} value={option}>
                            {t(`permission.override.${option}`)}
                          </option>
                        ))}
                      </select>
                    )
                  },
                },
              ]

              return (
                <Card
                  className="mb-3"
                  key={group.key}
                  header={
                    <>
                      <Button variant="link" className="p-0 text-decoration-none fw-bold"
                        style={{ color: 'var(--kt-gray-900)' }}
                        aria-expanded={isOpen}
                        aria-controls={`permission-group-${group.key}`}
                        disabled={isFiltered}
                        onClick={() =>
                          setExpanded((previous) => ({
                            ...previous,
                            [group.key]: !(previous[group.key] ?? false),
                          }))
                        }
                      >
                        <span aria-hidden="true" className="me-2">
                          {isOpen ? '▾' : '▸'}
                        </span>
                        {group.key}
                      </Button>

                      <span className="ms-auto d-inline-flex gap-2">
                        {changedInGroup > 0 && (
                          <Badge variant="primary">
                            {t('permission.page.changedCount', { count: changedInGroup })}
                          </Badge>
                        )}
                        <Badge variant="success">
                          {t('permission.page.groupSummary', {
                            effective: effectiveInGroup,
                            total: group.nodes.length,
                          })}
                        </Badge>
                      </span>
                    </>
                  }
                >
                  {isOpen && (
                    <div id={`permission-group-${group.key}`}>
                      <DataTable
                        label={t('permission.page.tableLabel', { module: group.key })}
                        columns={permissionColumns}
                        rows={group.nodes}
                        rowKey={(node) => node.id}
                      />
                    </div>
                  )}
                </Card>
              )
            })
          )}
        </>
      )}

      <Modal
        title={t('permission.confirm.title')}
        isOpen={isConfirmOpen}
        onClose={() => setConfirmOpen(false)}
        onSubmit={submit}
        isBusy={save.isPending}
        error={save.error ? errorMessage(save.error) : null}
        confirmLabel={t('permission.confirm.apply')}
        size="lg"
      >
        <p style={{ color: 'var(--kt-gray-600)' }}>{t('permission.confirm.intro')}</p>

        <PreviewList
          title={t('permission.confirm.gains', { count: preview.gains.length })}
          nodes={preview.gains}
          color="var(--kt-success)"
          emptyMessage={t('permission.confirm.noGains')}
        />
        <PreviewList
          title={t('permission.confirm.losses', { count: preview.losses.length })}
          nodes={preview.losses}
          color="var(--kt-danger)"
          emptyMessage={t('permission.confirm.noLosses')}
        />
        <PreviewList
          title={t('permission.confirm.cleared', { count: preview.cleared.length })}
          nodes={preview.cleared}
          color="var(--kt-gray-600)"
          emptyMessage={t('permission.confirm.noCleared')}
        />

        <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
          {t('permission.confirm.absoluteNote')}
        </p>
      </Modal>
    </>
  )
}

function PreviewList({
  title,
  nodes,
  color,
  emptyMessage,
}: {
  title: string
  nodes: PermissionTreeNodeDto[]
  color: string
  emptyMessage: string
}) {
  return (
    <section className="mb-3">
      <h3 className="h6 fw-bold mb-1" style={{ color }}>
        {title}
      </h3>
      {nodes.length === 0 ? (
        <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
          {emptyMessage}
        </p>
      ) : (
        <ul
          className="mb-0"
          style={{ color: 'var(--kt-gray-700)', fontSize: '0.875rem', maxHeight: 220, overflowY: 'auto' }}
        >
          {nodes.map((node) => (
            <li key={node.id}>
              {node.permissionName}{' '}
              <span style={{ color: 'var(--kt-gray-500)' }}>({node.permissionTarget})</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
