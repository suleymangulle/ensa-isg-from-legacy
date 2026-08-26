import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/auth/AuthContext'
import { errorMessage } from '@/api/http'
import LanguageSwitcher from '@/components/LanguageSwitcher'

export default function LoginPage() {
  const { t } = useTranslation()
  const { user, signIn } = useAuth()
  const navigate = useNavigate()

  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) return <Navigate to="/" replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await signIn(userName, password)
      navigate('/', { replace: true })
    } catch (err) {
      setError(errorMessage(err) || t('auth.invalidCredentials'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div
      className="d-flex align-items-center justify-content-center p-3"
      style={{ minHeight: '100vh', backgroundColor: 'var(--kt-body-bg)' }}
    >
      <div className="card shadow-sm w-100" style={{ maxWidth: 440 }}>
        <div className="card-body p-4 p-sm-5">
          <div className="d-flex justify-content-end mb-3">
            <LanguageSwitcher />
          </div>

          <div className="text-center mb-5">
            <span
              className="d-inline-flex align-items-center justify-content-center fw-bold mb-3"
              style={{
                width: 52,
                height: 52,
                borderRadius: 13,
                backgroundColor: 'var(--kt-primary)',
                color: '#fff',
                fontSize: 22,
              }}
              aria-hidden="true"
            >
              {t('app.initial')}
            </span>
            <h1 className="h4 fw-bold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
              {t('auth.signInTitle')}
            </h1>
            <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
              {t('app.tagline')}
            </p>
          </div>

          {error && (
            <div
              className="alert alert-danger border-0"
              style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
              role="alert"
            >
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-3">
              <label htmlFor="userName" className="form-label required">
                {t('auth.userName')}
              </label>
              <input
                id="userName"
                name="userName"
                className="form-control form-control-lg"
                value={userName}
                onChange={(event) => setUserName(event.target.value)}
                autoComplete="username"
                required
                autoFocus
              />
            </div>

            <div className="mb-4">
              <label htmlFor="password" className="form-label required">
                {t('auth.password')}
              </label>
              <input
                id="password"
                name="password"
                type="password"
                className="form-control form-control-lg"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="current-password"
                required
              />
            </div>

            <button
              type="submit"
              className="btn btn-primary btn-lg w-100"
              disabled={isSubmitting || !userName || !password}
            >
              {isSubmitting ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
                  {t('auth.signingIn')}
                </>
              ) : (
                t('auth.signIn')
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
