import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { Pagination, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { SearchBar } from '@/components/Form'
import { errorMessage } from '@/api/http'
import {
  useMenuList,
  useMyMenu,
  type MenuElementNavigationDto,
  type MenuListDto,
  type MenuNodeNavigationDto,
} from './api'

const PAGE_SIZE = 20

/**
 * Menu administration.
 *
 * Two halves: the menu definitions the API stores, and a preview of the menu the signed-in user
 * would actually be served. `GET api/menu/my-menu` requires a layout type code — an empty one
 * answers 400 — and the API exposes no endpoint listing the codes, so the picker is built from
 * the codes present in the definition list and falls back to a free-text entry. Nothing is
 * requested until a code exists.
 */
export default function MenuListPage() {
  const { t } = useTranslation()

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [menuTypeCode, setMenuTypeCode] = useState('')

  const { data, isLoading, error } = useMenuList({ page, pageSize: PAGE_SIZE, filter: search })

  // Distinct layout codes seen in the definition list; the only source the API offers.
  const knownCodes = useMemo(() => {
    const codes = new Set<string>()
    for (const menu of data?.items ?? []) {
      if (menu.menuTypeCode) codes.add(menu.menuTypeCode)
    }
    return [...codes].sort((left, right) => left.localeCompare(right))
  }, [data])

  const myMenu = useMyMenu(menuTypeCode)

  const columns: Column<MenuListDto>[] = [
    {
      key: 'name',
      header: t('menu.fields.name'),
      render: (menu) => <span className="fw-semibold">{menu.name}</span>,
    },
    {
      key: 'menuTypeCode',
      header: t('menu.fields.menuTypeCode'),
      render: (menu) =>
        menu.menuTypeCode ? (
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => setMenuTypeCode(menu.menuTypeCode ?? '')}
            title={t('menu.actions.previewWithCode', { code: menu.menuTypeCode })}
          >
            {menu.menuTypeCode}
          </button>
        ) : (
          t('common.none')
        ),
    },
    {
      key: 'userTypeCode',
      header: t('menu.fields.userTypeCode'),
      render: (menu) => menu.userTypeCode ?? t('menu.fields.allUserTypes'),
    },
    {
      key: 'sortOrder',
      header: t('menu.fields.sortOrder'),
      align: 'end',
      render: (menu) => menu.sortOrder,
    },
    {
      key: 'status',
      header: t('menu.fields.status'),
      align: 'center',
      render: (menu) => (
        <span className={menu.isActive ? 'badge-light-success' : 'badge-light-danger'}>
          {menu.isActive ? t('common.active') : t('common.passive')}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageTitle title={t('menu.list.title')} description={t('menu.list.description')} />

      <div className="card mb-4">
        <div className="card-header">
          <h2 className="card-title h6 mb-0">{t('menu.list.definitions')}</h2>
        </div>

        <div className="card-header border-0 pt-4 pb-0 d-block">
          <SearchBar
            value={search}
            onChange={(value) => {
              setSearch(value)
              setPage(1)
            }}
            placeholder={t('menu.list.searchPlaceholder')}
          />
        </div>

        <div className="card-body p-0">
          <DataTable
            label={t('menu.list.definitions')}
            columns={columns}
            rows={data?.items}
            rowKey={(menu) => menu.id}
            isLoading={isLoading}
            error={error ? errorMessage(error) : null}
            emptyMessage={t('menu.list.empty')}
          />
        </div>

        {data && data.totalCount > 0 && (
          <div className="card-footer bg-transparent border-0 pt-0">
            <Pagination
              total={data.totalCount}
              page={page}
              pageSize={PAGE_SIZE}
              onPageChange={setPage}
            />
          </div>
        )}
      </div>

      <div className="card">
        <div className="card-header">
          <h2 className="card-title h6 mb-0">{t('menu.preview.title')}</h2>
        </div>

        <div className="card-body">
          <p style={{ color: 'var(--kt-gray-600)' }}>{t('menu.preview.description')}</p>

          <div className="row g-3 align-items-end mb-4">
            <div className="col-md-4">
              <label htmlFor="menu-type-code" className="form-label fw-semibold">
                {t('menu.preview.layoutCode')}
              </label>
              <input
                id="menu-type-code"
                className="form-control"
                list="menu-type-codes"
                value={menuTypeCode}
                placeholder={t('menu.preview.layoutCodePlaceholder')}
                onChange={(event) => setMenuTypeCode(event.target.value)}
              />
              <datalist id="menu-type-codes">
                {knownCodes.map((code) => (
                  <option key={code} value={code} />
                ))}
              </datalist>
              <div className="form-text" style={{ color: 'var(--kt-gray-500)' }}>
                {knownCodes.length > 0
                  ? t('menu.preview.knownCodes', { codes: knownCodes.join(', ') })
                  : t('menu.preview.noKnownCodes')}
              </div>
            </div>
          </div>

          {menuTypeCode.trim() === '' && (
            <div className="text-center py-4" style={{ color: 'var(--kt-gray-500)' }}>
              {t('menu.preview.selectCode')}
            </div>
          )}

          {myMenu.isLoading && <Spinner />}

          {myMenu.error && (
            <div
              className="alert border-0 mb-0"
              style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
              role="alert"
            >
              {errorMessage(myMenu.error)}
            </div>
          )}

          {myMenu.data && (
            <>
              <p className="fw-semibold mb-2" style={{ color: 'var(--kt-gray-800)' }}>
                {myMenu.data.menu.name}
                {myMenu.data.menuType && (
                  <span className="badge-light-primary ms-2">
                    {myMenu.data.menuType.displayName}
                  </span>
                )}
              </p>

              {myMenu.data.roots.length === 0 && myMenu.data.elementRoots.length === 0 ? (
                <div className="text-center py-4" style={{ color: 'var(--kt-gray-500)' }}>
                  {t('menu.preview.empty')}
                </div>
              ) : (
                <>
                  <NodeTree nodes={myMenu.data.roots} />
                  <ElementTree nodes={myMenu.data.elementRoots} />
                </>
              )}
            </>
          )}
        </div>
      </div>
    </>
  )
}

/** Renders the `MenuNode` tree as a nested list, so a screen reader keeps the hierarchy. */
function NodeTree({ nodes }: { nodes: MenuNodeNavigationDto[] }) {
  const { t } = useTranslation()
  if (nodes.length === 0) return null

  return (
    <ul style={{ color: 'var(--kt-gray-700)' }}>
      {nodes.map((node) => (
        <li key={node.id} className="mb-1">
          <span className="fw-semibold">{node.title}</span>
          {node.url && (
            <span className="ms-2" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
              {node.url}
            </span>
          )}
          {node.userHidden && (
            <span className="badge-light-warning ms-2">{t('menu.preview.userHidden')}</span>
          )}
          <NodeTree nodes={node.children} />
        </li>
      ))}
    </ul>
  )
}

/** Renders the legacy `MenuElement` tree, used by menus not built on the shared catalogue. */
function ElementTree({ nodes }: { nodes: MenuElementNavigationDto[] }) {
  if (nodes.length === 0) return null

  return (
    <ul style={{ color: 'var(--kt-gray-700)' }}>
      {nodes.map((node) => (
        <li key={node.id} className="mb-1">
          <span className="fw-semibold">{node.text}</span>
          {node.url && (
            <span className="ms-2" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
              {node.url}
            </span>
          )}
          <ElementTree nodes={node.children} />
        </li>
      ))}
    </ul>
  )
}
