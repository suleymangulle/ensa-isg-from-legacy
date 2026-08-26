import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
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
            <button
              type="button"
              className="btn btn-primary"
              disabled={preview.total === 0}
              onClick={() => setConfirmOpen(true)}
            >
              {t('permission.page.review', { count: preview.total })}
            </button>
          ) : undefined
        }
      />

      <div className="card mb-4">
        <div className="card-body">
          <div className="row g-3 align-items-end">
            <div className="col-md-4">
              <label htmlFor="permission-user-filter" className="form-label fw-semibold">
                {t('permission.page.searchUser')}
              </label>
              <input
                id="permission-user-filter"
                type="search"
                className="form-control"
                value={userFilter}
                placeholder={t('permission.page.searchUserPlaceholder')}
                onChange={(event) => setUserFilter(event.target.value)}
              />
            </div>

            <div className="col-md-5">
              <label htmlFor="permission-user" className="form-label fw-semibold">
                {t('permission.page.subject')}
              </label>
              <select
                id="permission-user"
                className="form-select"
                value={userIdParam ?? ''}
                onChange={(event) => selectUser(event.target.value)}
              >
                <option value="">{t('permission.page.subjectPlaceholder')}</option>
                {users.data?.items.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.displayName}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-3">
              <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
                {t('permission.page.roleScopeNote')}
              </p>
            </div>
          </div>
        </div>
      </div>

      {tree.error && <ErrorPanel message={errorMessage(tree.error)} />}
      {permissions.error && <ErrorPanel message={errorMessage(permissions.error)} />}

      {!userId && !tree.error && (
        <div className="card">
          <div className="card-body text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
            {t('permission.page.selectSubject')}
          </div>
        </div>
      )}

      {userId && (tree.isLoading || permissions.isLoading) && <Spinner />}

      {userId && tree.data && permissions.data && (
        <>
          {isSystemAdministrator && (
            <div
              className="alert border-0"
              style={{ backgroundColor: 'var(--kt-warning-light)', color: 'var(--kt-warning)' }}
              role="alert"
            >
              {t('permission.page.systemAdministratorNote')}
            </div>
          )}

          <div className="card mb-4">
            <div className="card-body">
              <div className="row g-3 align-items-end">
                <div className="col-md-4">
                  <label htmlFor="permission-search" className="form-label fw-semibold">
                    {t('permission.page.searchPermission')}
                  </label>
                  <input
                    id="permission-search"
                    type="search"
                    className="form-control"
                    value={search}
                    placeholder={t('permission.page.searchPermissionPlaceholder')}
                    onChange={(event) => setSearch(event.target.value)}
                  />
                </div>

                <div className="col-md-3">
                  <div className="form-check">
                    <input
                      id="permission-only-changed"
                      type="checkbox"
                      className="form-check-input"
                      checked={onlyChanged}
                      onChange={(event) => setOnlyChanged(event.target.checked)}
                    />
                    <label className="form-check-label" htmlFor="permission-only-changed">
                      {t('permission.page.onlyChanged')}
                    </label>
                  </div>
                </div>

                <div className="col-md-2">
                  <button
                    type="button"
                    className="btn btn-light w-100"
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
                  </button>
                </div>

                <div className="col-md-3 text-md-end">
                  <span className="badge-light-success me-2">
                    {t('permission.page.effectiveCount', { count: effectiveNow.size })}
                  </span>
                  <span className="badge-light-primary">
                    {t('permission.page.catalogueCount', { count: tree.data.totalCount })}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {groups.length === 0 ? (
            <div className="card">
              <div className="card-body text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
                {t('permission.page.noMatch')}
              </div>
            </div>
          ) : (
            groups.map((group) => {
              const isOpen = isFiltered || (expanded[group.key] ?? false)
              const effectiveInGroup = group.nodes.filter((node) =>
                effectiveNow.has(node.id),
              ).length
              const changedInGroup = group.nodes.filter(
                (node) => (draft[node.id] ?? 'inherit') !== (baseline[node.id] ?? 'inherit'),
              ).length

              return (
                <div className="card mb-3" key={group.key}>
                  <div className="card-header">
                    <button
                      type="button"
                      className="btn btn-link p-0 text-decoration-none fw-bold"
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
                    </button>

                    <span className="ms-auto d-inline-flex gap-2">
                      {changedInGroup > 0 && (
                        <span className="badge-light-primary">
                          {t('permission.page.changedCount', { count: changedInGroup })}
                        </span>
                      )}
                      <span className="badge-light-success">
                        {t('permission.page.groupSummary', {
                          effective: effectiveInGroup,
                          total: group.nodes.length,
                        })}
                      </span>
                    </span>
                  </div>

                  {isOpen && (
                    <div className="card-body p-0" id={`permission-group-${group.key}`}>
                      <div className="table-responsive">
                        <table
                          className="table table-hover align-middle mb-0"
                          aria-label={t('permission.page.tableLabel', { module: group.key })}
                        >
                          <thead>
                            <tr>
                              <th scope="col">{t('permission.fields.permission')}</th>
                              <th scope="col">{t('permission.fields.target')}</th>
                              <th scope="col">{t('permission.fields.type')}</th>
                              <th scope="col" className="text-center">
                                {t('permission.fields.current')}
                              </th>
                              <th scope="col" className="text-end" style={{ width: 210 }}>
                                {t('permission.fields.override')}
                              </th>
                            </tr>
                          </thead>
                          <tbody>
                            {group.nodes.map((node) => {
                              const state = draft[node.id] ?? 'inherit'
                              const savedState = baseline[node.id] ?? 'inherit'
                              const isChanged = state !== savedState
                              const label = isModuleDefault(node)
                                ? t('permission.fields.defaultAccess', { module: group.key })
                                : node.permissionName

                              return (
                                <tr key={node.id}>
                                  <th scope="row" className="fw-normal">
                                    <span
                                      className={isChanged ? 'fw-semibold' : undefined}
                                      style={{
                                        color: isChanged
                                          ? 'var(--kt-primary)'
                                          : 'var(--kt-gray-800)',
                                      }}
                                    >
                                      {label}
                                    </span>
                                    {node.permissionDescription && (
                                      <div
                                        style={{
                                          color: 'var(--kt-gray-500)',
                                          fontSize: '0.8125rem',
                                        }}
                                      >
                                        {node.permissionDescription}
                                      </div>
                                    )}
                                  </th>
                                  <td
                                    style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}
                                  >
                                    {node.permissionTarget}
                                  </td>
                                  <td>{t(`enums.permissionType.${node.permissionType}`)}</td>
                                  <td className="text-center">
                                    <span
                                      className={
                                        effectiveNow.has(node.id)
                                          ? 'badge-light-success'
                                          : 'badge-light-danger'
                                      }
                                    >
                                      {effectiveNow.has(node.id)
                                        ? t('permission.state.effective')
                                        : t('permission.state.notEffective')}
                                    </span>
                                    {savedState !== 'inherit' && (
                                      <span className="badge-light-primary ms-1">
                                        {t(`permission.override.${savedState}`)}
                                      </span>
                                    )}
                                  </td>
                                  <td className="text-end">
                                    <select
                                      className="form-select form-select-sm"
                                      style={{ maxWidth: 190, marginInlineStart: 'auto' }}
                                      value={state}
                                      disabled={isSystemAdministrator}
                                      aria-label={t('permission.page.overrideLabel', {
                                        name: label,
                                      })}
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
                                  </td>
                                </tr>
                              )
                            })}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}
                </div>
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
