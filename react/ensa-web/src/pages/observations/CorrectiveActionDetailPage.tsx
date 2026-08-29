import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card } from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog } from '@/components/Form'
import { CorrectiveActionStatus } from '@/api/enums'
import { errorMessage } from '@/api/http'
import { useDelete } from '@/api/mutations'
import { formatDate } from '@/utils/format'
import CorrectiveActionCloseModal from './CorrectiveActionCloseModal'
import CorrectiveActionFormModal from './CorrectiveActionFormModal'
import { OBSERVATION_ENDPOINTS, useCorrectiveActionDetail } from './api'
import {
  AlertPanel,
  CORRECTIVE_ACTION_STATUS_BADGE,
  RISK_CATEGORY_BADGE,
  Term,
} from './components'

export default function CorrectiveActionDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const actionId = Number(id)

  const [isEditing, setIsEditing] = useState(false)
  const [isClosing, setIsClosing] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const { data, isLoading, error } = useCorrectiveActionDetail(actionId)

  const remove = useDelete(OBSERVATION_ENDPOINTS.correctiveAction, {
    onSuccess: () => navigate('/corrective-actions'),
  })

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const action = data.correctiveAction
  const none = t('common.none')
  const isOpen = action.operationResult === CorrectiveActionStatus.InProgress

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/corrective-actions" className="text-decoration-none">
              {t('correctiveAction.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {t('correctiveAction.detail.breadcrumb', { id: action.id })}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={action.finding}
        description={data.company?.displayName ?? undefined}
        action={
          <div className="d-flex gap-2">
            {isOpen && (
              <Button variant="success" onClick={() => setIsClosing(true)}>
                {t('correctiveAction.detail.close')}
              </Button>
            )}
            <Button variant="light" 
              onClick={() => setIsEditing(true)}
            >
              {t('common.edit')}
            </Button>
            <Button variant="light" 
              onClick={() => setIsDeleting(true)}
            >
              {t('common.delete')}
            </Button>
          </div>
        }
      />

      {action.isOverdue && (
        <div className="mb-4">
          <AlertPanel tone="danger">
            <div>
              <strong className="d-block">{t('correctiveAction.overdue.detailTitle')}</strong>
              <span>
                {t('correctiveAction.overdue.since', {
                  date: formatDate(action.deadlineDate) ?? none,
                })}
              </span>
            </div>
          </AlertPanel>
        </div>
      )}

      <Card
        className="mb-4"
      >
          <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
            {t('correctiveAction.detail.general')}
          </h2>
          <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
            <Term label={t('correctiveAction.fields.status')}>
              <Badge variant={CORRECTIVE_ACTION_STATUS_BADGE[action.operationResult]}>
                {t(`enums.correctiveActionStatus.${action.operationResult}`)}
              </Badge>
            </Term>
            <Term label={t('correctiveAction.fields.riskCategory')}>
              <Badge variant={RISK_CATEGORY_BADGE[action.riskCategory]}>
                {t(`enums.riskCategory.${action.riskCategory}`)}
              </Badge>
            </Term>
            <Term label={t('correctiveAction.fields.company')}>
              {data.company?.displayName ?? none}
            </Term>
            <Term label={t('correctiveAction.fields.owner')}>
              {data.ownerEmployee?.displayName ?? action.owner ?? none}
            </Term>
            <Term label={t('correctiveAction.fields.findingDate')}>
              {formatDate(action.findingDate) ?? none}
            </Term>
            <Term label={t('correctiveAction.fields.deadlineDate')}>
              {formatDate(action.deadlineDate) ?? none}
            </Term>
            <Term label={t('correctiveAction.fields.recommendation')}>
              {action.recommendation ?? none}
            </Term>
            <Term label={t('correctiveAction.fields.source')}>{action.source ?? none}</Term>
            <Term label={t('correctiveAction.fields.result')}>{action.result ?? none}</Term>
            <Term label={t('correctiveAction.fields.resultDate')}>
              {formatDate(action.resultDate) ?? none}
            </Term>
          </dl>
        
      </Card>

      {data.sourceFieldObservationLine && (
        <Card>
            <h2 className="h6 fw-semibold mb-3" style={{ color: 'var(--kt-gray-900)' }}>
              {t('correctiveAction.detail.sourceLine')}
            </h2>
            <dl className="row mb-3" style={{ fontSize: '0.9375rem' }}>
              <Term label={t('fieldObservation.lines.nonConformity')}>
                {data.sourceFieldObservationLine.nonConformity}
              </Term>
              <Term label={t('fieldObservation.lines.measures')}>
                {data.sourceFieldObservationLine.measures ?? none}
              </Term>
              <Term label={t('fieldObservation.lines.deadlineDate')}>
                {formatDate(data.sourceFieldObservationLine.deadlineDate) ?? none}
              </Term>
            </dl>
            <Link
              to={`/field-observations/${data.sourceFieldObservationLine.fieldObservationReportId}`}
              className="btn btn-sm"
            >
              {t('correctiveAction.detail.openSourceReport')}
            </Link>
          
        </Card>
      )}

      {isEditing && (
        <CorrectiveActionFormModal action={action} onClose={() => setIsEditing(false)} />
      )}

      {isClosing && (
        <CorrectiveActionCloseModal actionId={actionId} onClose={() => setIsClosing(false)} />
      )}

      <ConfirmDialog
        isOpen={isDeleting}
        title={t('correctiveAction.list.deleteTitle')}
        message={t('correctiveAction.list.deleteMessage', { finding: action.finding })}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
        onCancel={() => setIsDeleting(false)}
        onConfirm={() => remove.mutate(actionId)}
      />
    </>
  )
}
