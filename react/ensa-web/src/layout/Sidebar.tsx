import { useMemo } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Avatar, Sidebar as RichSidebar, Text, type SidebarSection } from 'rich-react-component'
import { useAuth } from '@/auth/AuthContext'
import { moduleNavigation } from '@/modules/registry'

/**
 * Main navigation, drawn by the library's `Sidebar`.
 *
 * That component is deliberately routing-agnostic: it renders groups and leaves and reports a
 * click, and the two things it cannot know — which entry matches the current URL, and how to get
 * there without reloading the application — are supplied here. So every entry carries an
 * `onClick` that calls `navigate()` rather than an `href`, and `active` is computed from the
 * current location.
 *
 * Which entries exist at all is still the modules' answer (`src/modules/registry.ts`), filtered
 * by permission: the menu never promises a screen the API will refuse.
 *
 * Navigating does not close the menu: it is the persistent kind, and the toggle in the top bar is
 * the only thing that hides it.
 */
export default function Sidebar() {
  const { t } = useTranslation()
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const { pathname } = useLocation()

  const sections = useMemo<SidebarSection[]>(
    () =>
      moduleNavigation(hasPermission).map((section) => ({
        key: section.group,
        label: t(`nav.group.${section.group}`),
        defaultOpen: true,
        items: section.entries.map((entry) => {
          const to = entry.path === '' ? '/' : `/${entry.path}`
          return {
            key: entry.path,
            label: t(entry.labelKey),
            icon: entry.icon,
            active: to === '/' ? pathname === '/' : pathname.startsWith(to),
            onClick: () => navigate(to),
          }
        }),
      })),
    [hasPermission, navigate, pathname, t],
  )

  return (
    <RichSidebar
      className="h-100"
      header={
        <div className="d-flex align-items-center gap-2">
          <Avatar name={t('app.initial')} size="sm" />
          <Text weight="bold" size="lg">
            {t('app.shortName')}
          </Text>
        </div>
      }
      sections={sections}
    />
  )
}
