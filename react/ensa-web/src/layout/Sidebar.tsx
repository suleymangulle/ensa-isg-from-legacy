import { useMemo } from 'react'
import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/auth/AuthContext'
import { moduleNavigation } from '@/modules/registry'

export default function Sidebar({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const { hasPermission } = useAuth()

  // Modules contribute their own entries; see src/modules/registry.ts. Entries the signed-in
  // user has no permission for are dropped here so the menu matches what the API will actually
  // allow — the API is still the control, this only stops the menu promising what it refuses.
  const sections = useMemo(() => moduleNavigation(hasPermission), [hasPermission])

  return (
    <aside
      className="position-fixed top-0 start-0 h-100 d-flex flex-column"
      aria-label={t('nav.sidebar')}
      style={{
        width: isOpen ? 'var(--kt-sidebar-width)' : 'var(--kt-sidebar-width-collapsed)',
        backgroundColor: 'var(--kt-dark)',
        transition: 'width 0.25s ease',
        zIndex: 1030,
        overflowX: 'hidden',
      }}
    >
      <div
        className="d-flex align-items-center gap-2 px-4"
        style={{
          height: 'var(--kt-header-height)',
          borderBottom: '1px solid rgba(255,255,255,0.06)',
        }}
      >
        <span
          className="d-inline-flex align-items-center justify-content-center fw-bold flex-shrink-0"
          style={{
            width: 34,
            height: 34,
            borderRadius: 9,
            backgroundColor: 'var(--kt-primary)',
            color: '#fff',
            fontSize: 15,
          }}
          aria-hidden="true"
        >
          {t('app.initial')}
        </span>
        {isOpen && <span className="text-white fw-bold fs-5">{t('app.shortName')}</span>}
        {isOpen && (
          <button
            type="button"
            className="btn btn-sm ms-auto d-lg-none text-white border-0"
            onClick={onClose}
            aria-label={t('nav.closeMenu')}
          >
            ×
          </button>
        )}
      </div>

      <nav className="flex-grow-1 overflow-auto py-3">
        {sections.map((section) => (
          <div key={section.group}>
            {isOpen && (
              <div
                className="px-4 pt-4 pb-2 text-uppercase fw-semibold"
                style={{
                  color: 'var(--kt-gray-600)',
                  fontSize: '0.6875rem',
                  letterSpacing: '0.08em',
                }}
              >
                {t(`nav.group.${section.group}`)}
              </div>
            )}
            {section.entries.map((entry) => {
              const to = entry.path === '' ? '/' : `/${entry.path}`
              return (
                <NavLink
                  key={entry.path}
                  to={to}
                  end={to === '/'}
                  onClick={onClose}
                  className="d-flex align-items-center gap-3 px-4 py-2 text-decoration-none"
                  style={({ isActive }) => ({
                    color: isActive ? '#fff' : 'var(--kt-gray-500)',
                    backgroundColor: isActive ? 'rgba(62,151,255,0.14)' : 'transparent',
                    borderInlineStart: `3px solid ${isActive ? 'var(--kt-primary)' : 'transparent'}`,
                    fontWeight: isActive ? 600 : 500,
                    fontSize: '0.9375rem',
                  })}
                  title={isOpen ? undefined : t(entry.labelKey)}
                  aria-label={isOpen ? undefined : t(entry.labelKey)}
                >
                  <span style={{ width: 18, textAlign: 'center', flexShrink: 0 }} aria-hidden="true">
                    {entry.icon}
                  </span>
                  {isOpen && <span className="text-truncate">{t(entry.labelKey)}</span>}
                </NavLink>
              )
            })}
          </div>
        ))}
      </nav>
    </aside>
  )
}
