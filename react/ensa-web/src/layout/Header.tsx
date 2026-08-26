import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/auth/AuthContext'
import LanguageSwitcher from '@/components/LanguageSwitcher'
import { initials } from '@/utils/format'

export default function Header({ onMenuToggle }: { onMenuToggle: () => void }) {
  const { t } = useTranslation()
  const { user, signOut } = useAuth()
  const navigate = useNavigate()

  const avatarInitials = initials(user?.fullName ?? '?')

  function handleSignOut() {
    signOut()
    navigate('/login', { replace: true })
  }

  return (
    <header
      className="d-flex align-items-center gap-3 px-4 px-lg-5 bg-white sticky-top"
      style={{
        height: 'var(--kt-header-height)',
        borderBottom: '1px solid var(--kt-border-color)',
        zIndex: 1020,
      }}
    >
      <button
        type="button"
        className="btn btn-sm btn-light-primary"
        onClick={onMenuToggle}
        aria-label={t('nav.toggleMenu')}
      >
        ☰
      </button>

      <div className="ms-auto d-flex align-items-center gap-3">
        <LanguageSwitcher />

        <div className="dropdown">
          <button
            className="btn d-flex align-items-center gap-2 border-0"
            data-bs-toggle="dropdown"
            aria-expanded="false"
            aria-label={t('nav.userMenu')}
            type="button"
          >
            <span
              className="d-inline-flex align-items-center justify-content-center fw-semibold"
              style={{
                width: 38,
                height: 38,
                borderRadius: 9,
                backgroundColor: 'var(--kt-primary-light)',
                color: 'var(--kt-primary)',
                fontSize: '0.875rem',
              }}
              aria-hidden="true"
            >
              {avatarInitials}
            </span>
            <span className="d-none d-sm-flex flex-column align-items-start lh-sm">
              <span
                className="fw-semibold"
                style={{ color: 'var(--kt-gray-800)', fontSize: '0.9375rem' }}
              >
                {user?.fullName}
              </span>
              <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
                {user?.email ?? user?.userName}
              </span>
            </span>
          </button>

          <ul
            className="dropdown-menu dropdown-menu-end shadow border-0 mt-2"
            style={{ minWidth: 200 }}
          >
            <li>
              <button className="dropdown-item" type="button" onClick={() => navigate('/')}>
                {t('common.profile')}
              </button>
            </li>
            <li>
              <hr className="dropdown-divider" />
            </li>
            <li>
              <button className="dropdown-item text-danger" type="button" onClick={handleSignOut}>
                {t('common.signOut')}
              </button>
            </li>
          </ul>
        </div>
      </div>
    </header>
  )
}
