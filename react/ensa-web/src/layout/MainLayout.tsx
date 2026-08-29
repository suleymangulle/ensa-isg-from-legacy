import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Container, Divider, Flex, Text } from 'rich-react-component'
import Sidebar from './Sidebar'
import Header from './Header'

/**
 * The application shell: navigation beside the screen, the top bar above it.
 *
 * Two navigation states, kept apart because they answer different questions:
 * `collapsed` is the desktop rail — the menu is still there, reduced to its
 * icons — and `mobileOpen` is the drawer, which only exists on a screen too
 * narrow to hold the aside at all. Collapsing on a laptop must not decide what
 * happens on a phone, so neither value is derived from the other, and the
 * library keeps them separate for the same reason.
 *
 * The sidebar is no longer unmounted to hide it: the library's `Sidebar` now
 * has a real collapsed rail and a real drawer, both of which keep the menu
 * reachable and keep its expansion state intact.
 */
export default function MainLayout() {
  const { t } = useTranslation()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const [isMobileOpen, setIsMobileOpen] = useState(false)

  return (
    <Flex align="stretch" gap={0} className="min-vh-100">
      {/* The library's Sidebar is its own `<nav>` landmark; wrapping it in a
          second one would announce the menu twice. */}
      <Sidebar
        collapsed={isCollapsed}
        onCollapsedChange={setIsCollapsed}
        mobileOpen={isMobileOpen}
        onMobileOpenChange={setIsMobileOpen}
      />

      <Flex direction="column" grow className="min-w-0">
        <Header
          isSidebarCollapsed={isCollapsed}
          onSidebarCollapseToggle={() => setIsCollapsed((collapsed) => !collapsed)}
          onMobileMenuOpen={() => setIsMobileOpen(true)}
        />

        <main className="flex-grow-1">
          <Container fluid className="py-4">
            <Outlet />
          </Container>
        </main>

        <Divider />

        <Container fluid className="pb-3">
          <Text size="sm" tone="muted">
            {t('app.footer', { year: new Date().getFullYear() })}
          </Text>
        </Container>
      </Flex>
    </Flex>
  )
}
