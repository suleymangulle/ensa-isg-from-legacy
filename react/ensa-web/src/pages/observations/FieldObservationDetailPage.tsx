import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import CorrectiveActionFormModal from './CorrectiveActionFormModal'
import FieldObservationFormModal from './FieldObservationFormModal'
import FieldObservationLineModal from './FieldObservationLineModal'
import {
  OBSERVATION_ENDPOINTS,
  useFieldObservationReportDetail,
  useRemoveObservationLine,
  type FieldObservationLineDto,
  type FieldObservationLineNavigationDto,
} from './api'
import {
  AlertPanel,
  CORRECTIVE_ACTION_STATUS_BADGE,
  EmptyHint,
  RISK_CATEGORY_BADGE,
  Term,
} from './components'

export default function FieldObservationDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const reportId = Number(id)

  const [isEditing, setIsEditing] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [isAddingLine, setIsAddingLine] = useState(false)
  const [editingLine, setEditingLine] = useState<FieldObservationLineDto | null>(null)
  const [deletingLine, setDeletingLine] = useState<FieldObservationLineDto | null>(null)
  const [raisingActionFor, setRaisingActionFor] = useState<FieldObservationLineDto | null>(null)

  const { data, isLoading, error } = useFieldObservationReportDetail(reportId)

  const removeReport = useDelete(OBSERVATION_ENDPOINTS.fieldObservationReport, {
    onSuccess: () => navigate('/field-observations'),
  })
  const removeLine = useRemoveObservationLine(reportId, () => setDeletingLine(null))

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const report = data.report
  const none = t('common.none')
  const overdueCount = data.lines.filter((entry) => entry.line.isOverdue).length

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/field-observations" className="text-decoration-none">
              {t('fieldObservation.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {formatDate(report.date) ?? t('fieldObservation.detail.fallbackTitle')}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={t('fieldObservation.detail.title', { date: formatDate(report.date) ?? '' })}
        description={data.company?.displayName ?? undefined}
        action={
          <div className="d-flex gap-2">
            <button
              className="btn btn-light-primary"
              type="button"
              onClick={() => setIsEditing(true)}
            >
              {t('common.edit')}
            </button>
            <button
              className="btn btn-light-danger"
              type="button"
              onClick={() => setIsDeleting(true)}
            >
              {t('common.delete')}
            </button>
          </div>
        }
      />

      {overdueCount > 0 && (
        <div className="mb-4">
          <AlertPanel tone="danger">
            <div>
              <strong className="d-block">
                {t('fieldObservation.lines.overdueBanner', { total: overdueCount })}
              </strong>
              <span>{t('fieldObservation.lines.overdueDescription')}</span>
            </div>
          </AlertPanel>
        </div>
      )}

      <div className="card mb-4">
        <div className="card-body">
          <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
            {t('fieldObservation.detail.general')}
          </h2>
          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <Term label={t('fieldObservation.fields.company')}>
              {data.company?.displayName ?? none}
            </Term>
            <Term label={t('fieldObservation.fields.department')}>
              {data.department?.displayName ?? none}
            </Term>
            <Term label={t('fieldObservation.fields.date')}>
              {formatDate(report.date) ?? none}
            </Term>
            <Term label={t('fieldObservation.fields.lineCount')}>{data.lines.length}</Term>
          </dl>
        </div>
      </div>

      <div className="card">
        <div className="card-header d-flex flex-wrap align-items-center justify-content-between gap-2 pt-4 border-0">
          <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
            {t('fieldObservation.lines.title')}
          </h2>
          <button
            type="button"
            className="btn btn-sm btn-primary"
            onClick={() => setIsAddingLine(true)}
          >
            {t('fieldObservation.lines.create')}
          </button>
        </div>

        <div className="card-body">
          {data.lines.length === 0 ? (
            <EmptyHint message={t('fieldObservation.lines.empty')} />
          ) : (
            <ul className="list-unstyled mb-0 d-flex flex-column gap-3">
              {data.lines.map((entry) => (
                <LineCard
                  key={entry.line.id}
                  entry={entry}
                  onEdit={() => setEditingLine(entry.line)}
                  onDelete={() => setDeletingLine(entry.line)}
                  onRaiseAction={() => setRaisingActionFor(entry.line)}
                />
              ))}
            </ul>
          )}
        </div>
      </div>

      {isEditing && (
        <FieldObservationFormModal report={report} onClose={() => setIsEditing(false)} />
      )}

      {isAddingLine && (
        <FieldObservationLineModal
          reportId={reportId}
          companyId={report.companyId}
          onClose={() => setIsAddingLine(false)}
        />
      )}

      {editingLine && (
        <FieldObservationLineModal
          reportId={reportId}
          companyId={report.companyId}
          line={editingLine}
          onClose={() => setEditingLine(null)}
        />
      )}

      {raisingActionFor && (
        <CorrectiveActionFormModal
          defaultCompanyId={report.companyId}
          fieldObservationLineId={raisingActionFor.id}
          onClose={() => setRaisingActionFor(null)}
        />
      )}

      <ConfirmDialog
        isOpen={isDeleting}
        title={t('fieldObservation.list.deleteTitle')}
        message={t('fieldObservation.list.deleteMessage', {
          date: formatDate(report.date) ?? '',
          company: data.company?.displayName ?? '',
        })}
        isBusy={removeReport.isPending}
        error={removeReport.error ? errorMessage(removeReport.error) : null}
        onCancel={() => setIsDeleting(false)}
        onConfirm={() => removeReport.mutate(reportId)}
      />

      <ConfirmDialog
        isOpen={deletingLine !== null}
        title={t('fieldObservation.lines.deleteTitle')}
        message={t('fieldObservation.lines.deleteMessage', {
          nonConformity: deletingLine?.nonConformity ?? '',
        })}
        isBusy={removeLine.isPending}
        error={removeLine.error ? errorMessage(removeLine.error) : null}
        onCancel={() => setDeletingLine(null)}
        onConfirm={() => deletingLine && removeLine.mutate(deletingLine.id)}
      />
    </>
  )
}

/** One non-conformity line with its measures, owner, deadline and derived corrective actions. */
function LineCard({
  entry,
  onEdit,
  onDelete,
  onRaiseAction,
}: {
  entry: FieldObservationLineNavigationDto
  onEdit: () => void
  onDelete: () => void
  onRaiseAction: () => void
}) {
  const { t } = useTranslation()
  const { line } = entry
  const none = t('common.none')

  return (
    <li
      className="p-3 rounded"
      style={{
        backgroundColor: 'var(--kt-gray-100)',
        borderInlineStart: `4px solid ${line.isOverdue ? 'var(--kt-danger)' : 'var(--kt-gray-300)'}`,
      }}
    >
      <div className="d-flex flex-wrap align-items-start justify-content-between gap-2 mb-2">
        <div className="d-flex flex-wrap align-items-center gap-2">
          <span className={RISK_CATEGORY_BADGE[line.riskCategory]}>
            {t(`enums.riskCategory.${line.riskCategory}`)}
          </span>
          {line.isOverdue && (
            <span className="badge-light-danger fw-bold">
              {t('fieldObservation.lines.overdue')}
            </span>
          )}
        </div>
        <div className="d-flex gap-1">
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={onRaiseAction}
            aria-label={t('fieldObservation.lines.raiseAction')}
            title={t('fieldObservation.lines.raiseAction')}
          >
            {t('fieldObservation.lines.raiseAction')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={onEdit}
            aria-label={t('fieldObservation.lines.editAction')}
            title={t('fieldObservation.lines.editAction')}
          >
            <span aria-hidden="true">✎</span>
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={onDelete}
            aria-label={t('fieldObservation.lines.deleteAction')}
            title={t('fieldObservation.lines.deleteAction')}
          >
            <span aria-hidden="true">🗑</span>
          </button>
        </div>
      </div>

      <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
        <Term label={t('fieldObservation.lines.nonConformity')}>{line.nonConformity}</Term>
        <Term label={t('fieldObservation.lines.measures')}>{line.measures ?? none}</Term>
        <Term label={t('fieldObservation.lines.owner')}>
          {entry.ownerEmployee?.displayName ?? line.owner ?? none}
        </Term>
        <Term label={t('fieldObservation.lines.deadlineDate')}>
          {formatDate(line.deadlineDate) ?? none}
        </Term>
      </dl>

      {entry.correctiveActions.length > 0 && (
        <div className="mt-3 pt-3" style={{ borderTop: '1px solid var(--kt-border-color)' }}>
          <h3 className="h6 mb-2" style={{ color: 'var(--kt-gray-700)' }}>
            {t('fieldObservation.lines.derivedActions')}
          </h3>
          <ul className="list-unstyled mb-0 d-flex flex-column gap-1">
            {entry.correctiveActions.map((action) => (
              <li key={action.id} className="d-flex flex-wrap align-items-center gap-2">
                <Link
                  to={`/corrective-actions/${action.id}`}
                  className="fw-semibold text-decoration-none"
                >
                  {action.finding}
                </Link>
                <span className={CORRECTIVE_ACTION_STATUS_BADGE[action.operationResult]}>
                  {t(`enums.correctiveActionStatus.${action.operationResult}`)}
                </span>
                {action.isOverdue && (
                  <span className="badge-light-danger">
                    {t('correctiveAction.overdue.badge')}
                  </span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </li>
  )
}
