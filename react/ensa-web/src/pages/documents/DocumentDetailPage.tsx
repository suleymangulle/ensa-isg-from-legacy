import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { errorMessage } from '@/api/http'
import { downloadFile } from '@/api/download'
import { formatDate, formatFileSize } from '@/utils/format'
import {
  documentContentPath,
  useDocumentDetail,
} from './api'

/**
 * `GET api/document/{id}/detail` — the document with its category and owning company.
 *
 * The payload is fetched through `downloadFile`, which attaches the bearer token; the storage
 * coordinates stay server-side and are
 * kept off the DTO on purpose. The disabled button says so rather than leaving the reader to
 * wonder where the file is.
 */
export default function DocumentDetailPage() {
  const { t } = useTranslation()
  const [isDownloading, setIsDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const { id } = useParams()

  const { data, isLoading, error } = useDocumentDetail(Number(id))

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const document = data.document
  const none = t('common.none')
  const size = formatFileSize(document.sizeBytes) ?? none

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/documents" className="text-decoration-none">
              {t('document.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {document.documentName}
          </li>
        </ol>
      </nav>

      {downloadError && <ErrorPanel message={downloadError} />}

      <PageTitle
        title={document.documentName || t('document.detail.fallbackTitle')}
        description={t('document.detail.subtitle', { size })}
        action={
          <button
            type="button"
            className="btn btn-light"
            disabled={isDownloading}
            aria-label={t('document.detail.download')}
            onClick={async () => {
              setIsDownloading(true)
              setDownloadError(null)
              try {
                await downloadFile(documentContentPath(document.id), document.documentName)
              } catch (cause) {
                setDownloadError(errorMessage(cause))
              } finally {
                setIsDownloading(false)
              }
            }}
          >
            {t('document.detail.download')}
          </button>
        }
      />

      <div
        className="alert border-0"
        style={{ backgroundColor: 'var(--kt-primary-light)', color: 'var(--kt-primary)' }}
      >
        {t('document.list.metadataOnlyNotice')}
      </div>

      <div className="card">
        <div className="card-body">
          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <Term label={t('document.fields.category')}>
              {data.category?.displayName ?? none}
            </Term>
            <Term label={t('document.fields.company')}>{data.company?.displayName ?? none}</Term>
            <Term label={t('document.fields.ownerType')}>
              {t(`enums.documentOwnerType.${document.ownerType}`)}
            </Term>
            <Term label={t('document.fields.ownerRecordId')}>
              {document.ownerRecordId ?? none}
            </Term>
            <Term label={t('document.fields.extension')}>
              {document.extension ? (
                <span className="badge-light-primary text-uppercase">{document.extension}</span>
              ) : (
                none
              )}
            </Term>
            <Term label={t('document.fields.contentType')}>{document.contentType ?? none}</Term>
            <Term label={t('document.fields.sizeBytes')}>{size}</Term>
            <Term label={t('document.fields.sha256')}>
              {document.sha256 ? (
                <code className="text-break">{document.sha256}</code>
              ) : (
                none
              )}
            </Term>
            <Term label={t('document.fields.creationTime')}>
              {formatDate(document.creationTime) ?? none}
            </Term>
            <Term label={t('document.fields.lastModificationTime')}>
              {formatDate(document.lastModificationTime) ?? none}
            </Term>
            <Term label={t('document.fields.status')}>
              <span className={document.isActive ? 'badge-light-success' : 'badge-light-danger'}>
                {document.isActive ? t('common.active') : t('common.passive')}
              </span>
            </Term>
          </dl>
        </div>
      </div>
    </>
  )
}

/** One `<dt>`/`<dd>` pair of the definition list. */
function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="col-sm-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-9">{children}</dd>
    </>
  )
}
