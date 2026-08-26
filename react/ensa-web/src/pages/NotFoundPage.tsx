import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

export default function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <div className="text-center py-5">
      <div
        className="fw-bold mb-2"
        style={{ fontSize: '4rem', color: 'var(--kt-gray-300)' }}
        aria-hidden="true"
      >
        {t('notFound.code')}
      </div>
      <h1 className="h4 fw-bold mb-2" style={{ color: 'var(--kt-gray-900)' }}>
        {t('notFound.title')}
      </h1>
      <p className="mb-4" style={{ color: 'var(--kt-gray-500)' }}>
        {t('notFound.description')}
      </p>
      <Link to="/" className="btn btn-primary">
        {t('notFound.back')}
      </Link>
    </div>
  )
}
