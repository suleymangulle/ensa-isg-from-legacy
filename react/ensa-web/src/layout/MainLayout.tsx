import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Container, Divider, Flex, Text } from 'rich-react-component'
import Sidebar from './Sidebar'
import Header from './Header'

/**
 * The application shell: navigation beside the screen, the top bar above it.
 *
 * Built from the library's layout primitives — `Flex` for the two columns and the vertical stack,
 * `Container` for the screen's own width, `Divider` and `Text` for the footer. The toggle simply
 * mounts or unmounts the sidebar: the library's `Sidebar` has no collapsed rail of its own, and a
 * hidden-but-present menu is worse than one that is not there.
 */
export default function MainLayout() {
  const { t } = useTranslation()
  const [isSidebarOpen, setIsSidebarOpen] = useState(true)

  return (
    <Flex align="stretch" gap={0} className="min-vh-100">
      {isSidebarOpen && (
        <nav
          aria-label={t('nav.sidebar')}
          className="flex-shrink-0 border-end"
          style={{ width: 'var(--kt-sidebar-width)' }}
        >
          <Sidebar />
        </nav>
      )}

      <Flex direction="column" grow className="min-w-0">
        <Header onMenuToggle={() => setIsSidebarOpen((open) => !open)} />

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
