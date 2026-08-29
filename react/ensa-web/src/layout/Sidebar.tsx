import { useMemo } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Avatar, Sidebar as RichSidebar, Text, type SidebarItem } from 'rich-react-component'
import { useAuth } from '@/auth/AuthContext'
import { moduleNavigation, NAV_GROUP_ICONS } from '@/modules/registry'

/**
 * Main navigation, drawn by the library's `Sidebar`.
 *
 * The library owns the tree: one recursive `items` model, one authoritative
 * `activeKey` (ancestors are derived, never passed in) and one expansion
 * state. What it deliberately does not own is routing — which entry matches
 * the current URL, and how to reach it without reloading the application —
 * so `activeKey` is computed here and `renderLink` hands the library the
 * router's own `Link`.
 *
 * Entries carry a real `href` rather than an `onClick` that navigates. That is
 * the difference between a link and a button to everything outside React:
 * ctrl-click, middle-click, "open in new tab" and the browser's status bar all
 * work again, and the library only calls `preventDefault()` for a disabled
 * item.
 *
 * Which entries exist at all is still the modules' answer
 * (`src/modules/registry.ts`), filtered by permission: the menu never promises
 * a screen the API will refuse.
 */
export interface SidebarProps {
  /** Desktop rail state. Owned by `MainLayout`, which also draws the toggle. */
  collapsed: boolean
  onCollapsedChange: (collapsed: boolean) => void
  /** Mobile drawer state. The library renders the backdrop and the dialog. */
  mobileOpen: boolean
  onMobileOpenChange: (open: boolean) => void
}

/** The route a navigation entry points at. `''` is the dashboard. */
function entryHref(path: string) {
  return path === '' ? '/' : `/${path}`
}

export default function Sidebar({
  collapsed,
  onCollapsedChange,
  mobileOpen,
  onMobileOpenChange,
}: SidebarProps) {
  const { t } = useTranslation()
  const { hasPermission } = useAuth()
  const { pathname } = useLocation()

  const sections = useMemo(() => moduleNavigation(hasPermission), [hasPermission])

  const items = useMemo<SidebarItem[]>(
    () =>
      sections.map((section) => ({
        type: 'group',
        key: `group.${section.group}`,
        label: t(`nav.group.${section.group}`),
        icon: NAV_GROUP_ICONS[section.group],
        // A heading that can be closed: this is what the old per-group
        // accordion did, expressed as one property of the one tree.
        collapsible: true,
        children: section.entries.map((entry) => ({
          type: 'link',
          key: entry.path,
          href: entryHref(entry.path),
          label: t(entry.labelKey),
          icon: entry.icon,
        })),
      })),
    [sections, t],
  )

  /** Every group starts open, which is the menu this application has always shown. */
  const defaultExpandedKeys = useMemo(
    () => sections.map((section) => `group.${section.group}`),
    [sections],
  )

  /**
   * Re-seeds the expansion state when the set of groups changes.
   *
   * `defaultExpandedKeys` is an initial value, read once. The menu is empty on
   * the first render — the permission list is still in flight — so without
   * this the defaults would be applied to a tree with no groups in it, and the
   * menu would arrive with every group shut. The identity only changes when a
   * group appears or disappears, which in a session means once, as the
   * permissions land.
   */
  const groupIdentity = defaultExpandedKeys.join('|')

  /**
   * The deepest entry the current URL actually belongs to.
   *
   * Longest match, and only on a whole path segment: `/companies` must not
   * light up while the user is on `/company-employees`, and a detail route
   * such as `/companies/12` still belongs to `/companies`.
   */
  const activeKey = useMemo(() => {
    let best: string | undefined
    let bestLength = -1

    for (const section of sections) {
      for (const entry of section.entries) {
        const href = entryHref(entry.path)
        const matches =
          href === '/' ? pathname === '/' : pathname === href || pathname.startsWith(`${href}/`)

        if (matches && href.length > bestLength) {
          best = entry.path
          bestLength = href.length
        }
      }
    }
    return best
  }, [sections, pathname])

  return (
    <RichSidebar
      key={groupIdentity}
      // Hidden below `lg` while the drawer is closed — and never while it is
      // open, because the drawer reuses this very element.
      className={mobileOpen ? undefined : 'd-none d-lg-block'}
      items={items}
      activeKey={activeKey}
      defaultExpandedKeys={defaultExpandedKeys}
      // Opens the branch leading to the active entry on arrival, and leaves it
      // closable by hand afterwards.
      expandActivePath
      // The library can draw its own collapse control; it is left off because
      // this application already has one in the header, and two buttons with
      // the same accessible name is worse than either of them alone.
      collapsed={collapsed}
      onCollapsedChange={onCollapsedChange}
      mobileOpen={mobileOpen}
      onMobileOpenChange={onMobileOpenChange}
      mobileLabel={t('nav.sidebar')}
      closeMobileOnSelect
      navLabel={t('nav.sidebar')}
      header={
        <div className="d-flex align-items-center gap-2">
          <Avatar name={t('app.initial')} size="sm" />
          {!collapsed && (
            <Text weight="bold" size="lg">
              {t('app.shortName')}
            </Text>
          )}
        </div>
      }
      renderLink={({ item, href, children, ...anchorProps }) => (
        <Link to={href} {...anchorProps}>
          {children}
        </Link>
      )}
    />
  )
}
