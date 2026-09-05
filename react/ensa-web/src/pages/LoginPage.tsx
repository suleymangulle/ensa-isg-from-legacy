import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Alert, Button, Card, Input, PasswordInput } from 'rich-react-component'
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
    <div className="d-flex" style={{ minHeight: '100vh' }}>
      {/* Sign-in form. First in the DOM so the form is what a screen reader
          and a narrow viewport both reach first. */}
      <div
        className="d-flex flex-column flex-grow-1 min-vw-0"
        style={{ backgroundColor: 'var(--kt-body-bg)' }}
      >
        <div className="d-flex justify-content-end p-3">
          <LanguageSwitcher />
        </div>

        <div className="d-flex flex-column align-items-center justify-content-center flex-grow-1 px-3 pb-5">
          <div style={{ width: '100%', maxWidth: 400 }}>
            {/* Mobile-only brand mark: the panel below is hidden at this width. */}
            <div className="d-lg-none text-center mb-4">
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
              <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
                {t('app.tagline')}
              </p>
            </div>

            <Card className="shadow-sm">
              <div className="p-4 p-sm-5">
                <div className="mb-4">
                  <h1 className="fw-bold mb-1" style={{ fontSize: '1.5rem', color: 'var(--kt-gray-900)' }}>
                    {t('auth.signInTitle')}
                  </h1>
                  <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
                    {t('auth.signInHint')}
                  </p>
                </div>

                {error && (
                  <Alert variant="danger" className="border-0 mb-3">
                    {error}
                  </Alert>
                )}

                <form onSubmit={handleSubmit} noValidate>
                  <div className="mb-3">
                    <Input
                      id="userName"
                      name="userName"
                      label={t('auth.userName')}
                      required
                      autoFocus
                      inputProps={{ autoComplete: 'username' }}
                      value={userName}
                      onChange={setUserName}
                    />
                  </div>

                  <div className="mb-4">
                    <PasswordInput
                      id="password"
                      name="password"
                      label={t('auth.password')}
                      required
                      value={password}
                      onChange={setPassword}
                    />
                  </div>

                  <Button
                    variant="primary"
                    size="lg"
                    className="w-100"
                    type="submit"
                    loading={isSubmitting}
                    disabled={isSubmitting || !userName || !password}
                  >
                    {isSubmitting ? t('auth.signingIn') : t('auth.signIn')}
                  </Button>
                </form>
              </div>
            </Card>
          </div>
        </div>
      </div>

      {/* Brand panel. Hidden below `lg`, where there is no room for a second column. */}
      <div
        className="d-none d-lg-flex flex-column align-items-center text-center p-5"
        style={{
          width: '44%',
          minWidth: 420,
          background: 'linear-gradient(155deg, var(--kt-primary), var(--kt-primary-active))',
        }}
      >
        <div className="d-flex align-items-center gap-2">
          <span
            className="d-inline-flex align-items-center justify-content-center fw-bold"
            style={{
              width: 36,
              height: 36,
              borderRadius: 10,
              backgroundColor: 'rgba(255, 255, 255, .16)',
              color: '#fff',
              fontSize: 16,
            }}
            aria-hidden="true"
          >
            {t('app.initial')}
          </span>
          <span className="fw-bold text-white fs-5">{t('app.shortName')}</span>
        </div>

        {/* Purely decorative — illustrative figures, not live data. */}
        <div
          className="position-relative flex-grow-1 d-flex align-items-center"
          style={{ width: '100%', maxWidth: 320 }}
          aria-hidden="true"
        >
          <div style={{ position: 'relative', width: '100%', height: 230 }}>
            <div
              style={{
                position: 'absolute',
                left: 0,
                top: 18,
                width: 190,
                padding: '16px 18px',
                borderRadius: 16,
                background: '#fff',
                boxShadow: '0 20px 40px -12px rgba(11, 42, 89, .45)',
                textAlign: 'left',
              }}
            >
              <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--kt-gray-500)' }}>
                {t('nav.companies')}
              </div>
              <div style={{ fontSize: 28, fontWeight: 800, color: 'var(--kt-gray-900)' }}>1.248</div>
              <div
                style={{
                  height: 6,
                  borderRadius: 3,
                  background: 'var(--kt-primary-light)',
                  marginTop: 10,
                  overflow: 'hidden',
                }}
              >
                <div style={{ width: '72%', height: '100%', background: 'var(--kt-primary)' }} />
              </div>
            </div>

            <div
              style={{
                position: 'absolute',
                right: 0,
                top: 0,
                width: 118,
                padding: '12px 14px',
                borderRadius: 14,
                background: '#fff',
                boxShadow: '0 16px 30px -10px rgba(11, 42, 89, .4)',
                textAlign: 'left',
              }}
            >
              <div style={{ fontSize: 18 }}>✔</div>
              <div style={{ fontSize: 18, fontWeight: 800, color: 'var(--kt-gray-900)' }}>98%</div>
              <div style={{ fontSize: 11, color: 'var(--kt-gray-500)' }}>{t('nav.riskAssessments')}</div>
            </div>

            <div
              style={{
                position: 'absolute',
                left: 55,
                bottom: 0,
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                width: 175,
                padding: '12px 16px',
                borderRadius: 14,
                background: '#fff',
                boxShadow: '0 16px 30px -10px rgba(11, 42, 89, .4)',
                textAlign: 'left',
              }}
            >
              <div
                style={{
                  width: 34,
                  height: 34,
                  borderRadius: '50%',
                  background: 'conic-gradient(var(--kt-primary) 0 75%, var(--kt-primary-light) 0 100%)',
                  flexShrink: 0,
                }}
              />
              <div>
                <div style={{ fontSize: 16, fontWeight: 800, color: 'var(--kt-gray-900)', lineHeight: 1.1 }}>
                  54.466
                </div>
                <div style={{ fontSize: 11, color: 'var(--kt-gray-500)' }}>{t('nav.employees')}</div>
              </div>
            </div>
          </div>
        </div>

        <div>
          <h2 className="fw-bold text-white mb-3" style={{ fontSize: '1.75rem', lineHeight: 1.3 }}>
            {t('auth.brandHeadline')}
          </h2>
          <p className="mb-0" style={{ color: 'rgba(255, 255, 255, .85)' }}>
            {t('auth.brandBody')}
          </p>
        </div>

        <p className="mb-0 mt-4 small" style={{ color: 'rgba(255, 255, 255, .7)' }}>
          {t('app.footer', { year: new Date().getFullYear() })}
        </p>
      </div>
    </div>
  )
}
