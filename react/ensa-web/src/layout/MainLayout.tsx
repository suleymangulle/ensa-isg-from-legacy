import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import Sidebar from './Sidebar'
import Header from './Header'

export default function MainLayout() {
  const { t } = useTranslation()
  const [isSidebarOpen, setIsSidebarOpen] = useState(true)

  return (
    <div className="d-flex" style={{ minHeight: '100vh' }}>
      <Sidebar isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

      <div
        className="flex-grow-1 d-flex flex-column"
        style={{
          marginInlineStart: isSidebarOpen
            ? 'var(--kt-sidebar-width)'
            : 'var(--kt-sidebar-width-collapsed)',
          transition: 'margin 0.25s ease',
          minWidth: 0,
        }}
      >
        <Header onMenuToggle={() => setIsSidebarOpen((open) => !open)} />

        <main className="flex-grow-1 p-4 p-lg-5">
          <Outlet />
        </main>

        <footer
          className="px-4 px-lg-5 py-3 text-center text-md-start"
          style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}
        >
          {t('app.footer', { year: new Date().getFullYear() })}
        </footer>
      </div>
    </div>
  )
}
