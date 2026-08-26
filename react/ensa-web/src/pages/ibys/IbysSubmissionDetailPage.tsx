import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { Field, Modal } from '@/components/Form'
import { IbysSubmissionStatus } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import {
  IBYS_STATUS_BADGE,
  IBYS_SUBMISSION_STATUSES,
  useIbysQueryDetail,
  useUpdateIbysStatus,
  type IbysSubmittedFormDto,
} from './api'

/**
 * IBYS submission detail.
 *
 * SECURITY. The notification XML and the e-signed payload are absent from every DTO in this
 * module by design; the screen states only whether each exists. The examination forms attached
 * to a submission are shown as clinical-free summaries, so submission tracking never becomes a
 * back door into health records — the form's own detail screen stays the only way in.
 */
export default function IbysSubmissionDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const [isStatusOpen, setIsStatusOpen] = useState(false)

  const { data, isLoading, error } = useIbysQueryDetail(Number(id))

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const query = data.query
  const title = query.queryNo ?? t('ibys.list.noQueryNo')

  const formColumns: Column<IbysSubmittedFormDto>[] = [
    {
      key: 'reportType',
      header: t('ibys.fields.reportType'),
      render: (form) => t(`enums.medicalReportType.${form.reportType}`),
    },
    {
      key: 'examinationDate',
      header: t('ibys.fields.examinationDate'),
      render: (form) => formatDate(form.examinationDate) ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('ibys.fields.status'),
      align: 'center',
      render: (form) => (
        <span className={IBYS_STATUS_BADGE[form.ibysStatus]}>
          {t(`enums.ibysSubmissionStatus.${form.ibysStatus}`)}
        </span>
      ),
    },
    {
      key: 'message',
      header: t('ibys.fields.statusMessage'),
      render: (form) =>
        [form.ibysStatusCode, form.ibysStatusMessage].filter(Boolean).join(' — ') ||
        t('common.none'),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '80px',
      render: (form) => (
        <Link
          to={`/medical-examinations/${form.id}`}
          className="btn btn-sm btn-icon btn-light"
          aria-label={t('ibys.detail.openForm')}
        >
          <span aria-hidden="true">→</span>
        </Link>
      ),
    },
  ]

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/ibys" className="text-decoration-none">
              {t('ibys.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {title}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={title}
        description={t('ibys.detail.subtitle', {
          type: t(`enums.ibysQueryType.${query.queryType}`),
          date: formatDate(query.submissionDate) ?? t('common.none'),
        })}
        action={
          <button
            className="btn btn-light-primary"
            type="button"
            onClick={() => setIsStatusOpen(true)}
          >
            {t('ibys.detail.updateStatus')}
          </button>
        }
      />

      <div className="card mb-4">
        <div className="card-body">
          <div className="mb-4">
            <span className={IBYS_STATUS_BADGE[query.status]}>
              {t(`enums.ibysSubmissionStatus.${query.status}`)}
            </span>
            {query.statusCode !== 0 && (
              <span className="ms-2" style={{ color: 'var(--kt-gray-500)' }}>
                {t('ibys.fields.statusCode')}: {query.statusCode}
              </span>
            )}
          </div>

          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <Term label={t('ibys.fields.queryType')}>
              {t(`enums.ibysQueryType.${query.queryType}`)}
            </Term>
            <Term label={t('ibys.fields.company')}>
              {data.company?.displayName ?? t('common.none')}
            </Term>
            <Term label={t('ibys.fields.employee')}>
              {data.employee?.displayName ?? t('common.none')}
            </Term>
            <Term label={t('ibys.fields.submissionDate')}>
              {formatDate(query.submissionDate) ?? t('common.none')}
            </Term>
            <Term label={t('ibys.fields.groupId')}>{query.groupId ?? t('common.none')}</Term>
            <Term label={t('ibys.fields.ibysVersion')}>
              {query.ibysVersion ?? t('common.none')}
            </Term>
            <Term label={t('ibys.fields.timeStamp')}>{query.timeStamp ?? t('common.none')}</Term>
            <Term label={t('ibys.fields.approver')}>
              {data.approverFullName ?? t('common.none')}
            </Term>
            <Term label={t('ibys.fields.message')}>{query.ibysMessage ?? t('common.none')}</Term>
            <Term label={t('ibys.fields.payload')}>
              <span className={query.hasXmlData ? 'badge-light-success' : 'badge-light-primary'}>
                {query.hasXmlData ? t('ibys.payload.xmlPresent') : t('ibys.payload.xmlAbsent')}
              </span>
              <span
                className={`ms-2 ${
                  query.hasSignedData ? 'badge-light-success' : 'badge-light-primary'
                }`}
              >
                {query.hasSignedData
                  ? t('ibys.payload.signaturePresent')
                  : t('ibys.payload.signatureAbsent')}
              </span>
            </Term>
          </dl>

          <p className="mb-0 mt-3" style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
            {t('ibys.detail.payloadNotice')}
          </p>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h2 className="h6 fw-bold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
            {t('ibys.detail.formsTitle')}
          </h2>
        </div>
        <div className="card-body p-0">
          <DataTable
            label={t('ibys.detail.formsTitle')}
            columns={formColumns}
            rows={data.examinationForms}
            rowKey={(form) => form.id}
            emptyMessage={t('ibys.detail.noForms')}
          />
        </div>
      </div>

      <StatusDialog
        isOpen={isStatusOpen}
        onClose={() => setIsStatusOpen(false)}
        queryId={query.id}
        currentStatus={query.status}
        currentQueryNo={query.queryNo}
      />
    </>
  )
}

/** Records the result IBYS returned for a submission. */
function StatusDialog({
  isOpen,
  onClose,
  queryId,
  currentStatus,
  currentQueryNo,
}: {
  isOpen: boolean
  onClose: () => void
  queryId: number
  currentStatus: IbysSubmissionStatus
  currentQueryNo?: string | null
}) {
  const { t } = useTranslation()
  const [status, setStatus] = useState<IbysSubmissionStatus>(currentStatus)
  const [message, setMessage] = useState('')
  const [submissionNumber, setSubmissionNumber] = useState(currentQueryNo ?? '')

  const update = useUpdateIbysStatus()

  return (
    <Modal
      title={t('ibys.detail.updateStatus')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={() =>
        update.mutate(
          {
            id: queryId,
            input: {
              status,
              message: message.trim() || null,
              submissionNumber: submissionNumber.trim() || null,
            },
          },
          { onSuccess: onClose },
        )
      }
      isBusy={update.isPending}
      error={update.error ? errorMessage(update.error) : null}
    >
      <div className="row g-3">
        <Field label={t('ibys.fields.status')} htmlFor="ibys-status" required>
          <select
            id="ibys-status"
            className="form-select"
            value={status}
            onChange={(event) => setStatus(Number(event.target.value))}
          >
            {IBYS_SUBMISSION_STATUSES.map((item) => (
              <option key={item} value={item}>
                {t(`enums.ibysSubmissionStatus.${item}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('ibys.fields.queryNo')}
          htmlFor="ibys-submission-number"
          hint={t('ibys.detail.submissionNumberHint')}
        >
          <input
            id="ibys-submission-number"
            className="form-control"
            value={submissionNumber}
            onChange={(event) => setSubmissionNumber(event.target.value)}
          />
        </Field>

        <Field label={t('ibys.fields.message')} htmlFor="ibys-message">
          <textarea
            id="ibys-message"
            className="form-control"
            rows={3}
            value={message}
            onChange={(event) => setMessage(event.target.value)}
          />
        </Field>
      </div>
    </Modal>
  )
}

/** One `<dt>`/`<dd>` pair of the definition list. */
function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="col-sm-4 col-lg-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-8 col-lg-9">{children}</dd>
    </>
  )
}
