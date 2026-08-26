import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, controlClass } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { PLAN_LINE_STATUS_BADGE, useLookup } from '@/api/endpoints'
import { ApprovalStatus, PlanLineStatus, TrainingLocation, TrainingType } from '@/api/enums'
import { formatDate } from '@/utils/format'
import {
  APPROVAL_STATUS_BADGE,
  PLAN_LINE_STATUSES,
  RESOURCES,
  TRAINING_LOCATIONS,
  TRAINING_TYPES,
  canSubmit,
  isApproved,
  isAwaitingDecision,
  useDeletePlanLine,
  useIncompleteTrainingPlanLines,
  usePlanLineWorkflow,
  useSavePlanLine,
  useTrainingPlanDetail,
  type SaveTrainingPlanLineDto,
  type TrainingPlanLineNavigationDto,
  type TrainingPlanNavigationDto,
} from './api'

/** ISO date (`YYYY-MM-DD`) as an `<input type="date">` wants it. */
function toDateInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : ''
}

/**
 * One annual training plan: its cover page, its lines and the per-line approval workflow.
 *
 * A line moves Draft → SubmittedForApproval → Approved or Rejected. Once approved it is
 * statutory evidence, so the screen renders it as frozen rather than offering an edit that the
 * API would refuse; a rejected line shows the reason the approver recorded.
 */
export default function TrainingPlanDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const planId = Number(id)

  const { data, isLoading, error } = useTrainingPlanDetail(planId)
  const incomplete = useIncompleteTrainingPlanLines(planId)

  const [onlyIncomplete, setOnlyIncomplete] = useState(false)
  const [isLineCreateOpen, setLineCreateOpen] = useState(false)
  const [editingLine, setEditingLine] = useState<TrainingPlanLineNavigationDto | null>(null)
  const [deletingLine, setDeletingLine] = useState<TrainingPlanLineNavigationDto | null>(null)
  const [rejectingLine, setRejectingLine] = useState<TrainingPlanLineNavigationDto | null>(null)

  const removeLine = useDeletePlanLine(planId)
  const workflow = usePlanLineWorkflow(planId)

  const incompleteIds = useMemo(
    () => new Set(incomplete.data?.items.map((line) => line.id) ?? []),
    [incomplete.data],
  )

  const lines = useMemo(() => {
    const all = data?.lines ?? []
    return onlyIncomplete ? all.filter((entry) => incompleteIds.has(entry.line.id)) : all
  }, [data, onlyIncomplete, incompleteIds])

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const columns: Column<TrainingPlanLineNavigationDto>[] = [
    {
      key: 'trainingName',
      header: t('trainingPlans.line.fields.training'),
      render: (entry) => (
        <>
          <span className="fw-semibold d-block">{entry.trainingName}</span>
          {entry.line.approvalStatus === ApprovalStatus.Rejected && entry.line.rejectionReason && (
            <span
              className="d-block mt-1"
              style={{ color: 'var(--kt-danger)', fontSize: '0.8125rem' }}
            >
              {t('trainingPlans.line.rejectionReason', { reason: entry.line.rejectionReason })}
            </span>
          )}
        </>
      ),
    },
    {
      key: 'period',
      header: t('trainingPlans.line.fields.period'),
      render: (entry) => {
        if (!entry.line.year) return t('common.none')
        const month = entry.line.month ? t(`enums.month.${entry.line.month}`) : ''
        return `${month} ${entry.line.year}`.trim()
      },
    },
    {
      key: 'duration',
      header: t('trainingPlans.line.fields.duration'),
      align: 'end',
      render: (entry) => t('training.minutes', { count: entry.line.durationMinutes }),
    },
    {
      key: 'instructor',
      header: t('trainingPlans.line.fields.instructor'),
      render: (entry) =>
        entry.instructorUserFullName ?? entry.line.instructorFullName ?? t('common.none'),
    },
    {
      key: 'performedDate',
      header: t('trainingPlans.line.fields.performedDate'),
      render: (entry) => formatDate(entry.line.performedDate) ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('trainingPlans.line.fields.status'),
      align: 'center',
      render: (entry) => (
        <span className={PLAN_LINE_STATUS_BADGE[entry.line.status]}>
          {t(`enums.planLineStatus.${entry.line.status}`)}
        </span>
      ),
    },
    {
      key: 'approvalStatus',
      header: t('trainingPlans.line.fields.approvalStatus'),
      align: 'center',
      render: (entry) => {
        const status = entry.line.approvalStatus ?? ApprovalStatus.Draft
        return (
          <span className={APPROVAL_STATUS_BADGE[status]}>{t(`enums.approvalStatus.${status}`)}</span>
        )
      },
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '260px',
      render: (entry) => (
        <LineActions
          entry={entry}
          isBusy={workflow.isPending}
          onEdit={() => setEditingLine(entry)}
          onDelete={() => setDeletingLine(entry)}
          onSubmit={() => workflow.mutate({ lineId: entry.line.id, action: 'submit' })}
          onApprove={() => workflow.mutate({ lineId: entry.line.id, action: 'approve' })}
          onReject={() => setRejectingLine(entry)}
        />
      ),
    },
  ]

  return (
    <>
      <nav aria-label={t('nav.breadcrumb')} className="mb-3">
        <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
          <li className="breadcrumb-item">
            <Link to="/training-plans/plans" className="text-decoration-none">
              {t('trainingPlans.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {data.company?.displayName ?? t('trainingPlans.detail.fallbackTitle')}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={data.company?.displayName ?? t('trainingPlans.detail.fallbackTitle')}
        description={t('trainingPlans.detail.description', {
          year: new Date(data.trainingPlan.startDate).getFullYear(),
        })}
        action={
          <button className="btn btn-primary" type="button" onClick={() => setLineCreateOpen(true)}>
            {t('trainingPlans.line.create')}
          </button>
        }
      />

      <div className="row g-4">
        <div className="col-12">
          <HeaderCard detail={data} />
        </div>

        <div className="col-12">
          <div className="card">
            <div className="card-header d-flex flex-wrap align-items-center justify-content-between gap-2">
              <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                {t('trainingPlans.detail.lines')}
              </h2>
              <div className="d-flex align-items-center gap-3">
                <span className="badge-light-warning">
                  {t('trainingPlans.detail.incompleteCount', {
                    count: incomplete.data?.items.length ?? 0,
                  })}
                </span>
                <div className="form-check mb-0">
                  <input
                    id="only-incomplete"
                    type="checkbox"
                    className="form-check-input"
                    checked={onlyIncomplete}
                    onChange={(event) => setOnlyIncomplete(event.target.checked)}
                  />
                  <label className="form-check-label" htmlFor="only-incomplete">
                    {t('trainingPlans.detail.onlyIncomplete')}
                  </label>
                </div>
              </div>
            </div>
            <div className="card-body p-0">
              {workflow.error && (
                <div className="p-4 pb-0">
                  <ErrorPanel message={errorMessage(workflow.error)} />
                </div>
              )}
              <DataTable
                label={t('trainingPlans.detail.lines')}
                columns={columns}
                rows={lines}
                rowKey={(entry) => entry.line.id}
                error={incomplete.error ? errorMessage(incomplete.error) : null}
                emptyMessage={
                  onlyIncomplete
                    ? t('trainingPlans.detail.emptyIncomplete')
                    : t('trainingPlans.detail.emptyLines')
                }
              />
            </div>
          </div>
        </div>
      </div>

      {isLineCreateOpen && (
        <LineFormModal planId={planId} onClose={() => setLineCreateOpen(false)} />
      )}

      {editingLine && (
        <LineFormModal
          planId={planId}
          entry={editingLine}
          onClose={() => setEditingLine(null)}
        />
      )}

      {rejectingLine && (
        <RejectModal
          trainingName={rejectingLine.trainingName}
          isBusy={workflow.isPending}
          error={workflow.error ? errorMessage(workflow.error) : null}
          onCancel={() => setRejectingLine(null)}
          onConfirm={(reason) =>
            workflow.mutate(
              { lineId: rejectingLine.line.id, action: 'reject', reason },
              { onSuccess: () => setRejectingLine(null) },
            )
          }
        />
      )}

      <ConfirmDialog
        isOpen={deletingLine !== null}
        title={t('trainingPlans.line.deleteTitle')}
        message={t('trainingPlans.line.deleteMessage', { name: deletingLine?.trainingName ?? '' })}
        onCancel={() => setDeletingLine(null)}
        onConfirm={() =>
          deletingLine &&
          removeLine.mutate(deletingLine.line.id, { onSuccess: () => setDeletingLine(null) })
        }
        isBusy={removeLine.isPending}
        error={removeLine.error ? errorMessage(removeLine.error) : null}
      />
    </>
  )
}

/** Cover-page facts of the plan. */
function HeaderCard({ detail }: { detail: TrainingPlanNavigationDto }) {
  const { t } = useTranslation()
  const plan = detail.trainingPlan
  const none = t('common.none')

  return (
    <div className="card">
      <div className="card-body">
        <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
          <Term label={t('trainingPlans.fields.documentNo')}>{plan.documentNo ?? none}</Term>
          <Term label={t('trainingPlans.fields.revisionNo')}>{plan.revisionNo ?? none}</Term>
          <Term label={t('trainingPlans.fields.startDate')}>
            {formatDate(plan.startDate) ?? none}
          </Term>
          <Term label={t('trainingPlans.fields.publicationDate')}>
            {formatDate(plan.publicationDate) ?? none}
          </Term>
          <Term label={t('trainingPlans.fields.specialist')}>
            {detail.specialistFullName ?? none}
          </Term>
          <Term label={t('trainingPlans.fields.physician')}>
            {detail.physicianFullName ?? none}
          </Term>
          <Term label={t('trainingPlans.fields.approver')}>{detail.approverFullName ?? none}</Term>
          <Term label={t('trainingPlans.fields.status')}>
            <span className={plan.isActive ? 'badge-light-success' : 'badge-light-danger'}>
              {plan.isActive ? t('common.active') : t('common.passive')}
            </span>
          </Term>
        </dl>
      </div>
    </div>
  )
}

/** The buttons a line offers in its current approval state. */
function LineActions({
  entry,
  isBusy,
  onEdit,
  onDelete,
  onSubmit,
  onApprove,
  onReject,
}: {
  entry: TrainingPlanLineNavigationDto
  isBusy: boolean
  onEdit: () => void
  onDelete: () => void
  onSubmit: () => void
  onApprove: () => void
  onReject: () => void
}) {
  const { t } = useTranslation()
  const status = entry.line.approvalStatus

  if (isApproved(status)) {
    return (
      <span className="badge-light-success" title={t('trainingPlans.line.lockedHint')}>
        {t('trainingPlans.line.locked')}
      </span>
    )
  }

  return (
    <div className="d-flex justify-content-end flex-wrap gap-1">
      {canSubmit(status) && (
        <button
          type="button"
          className="btn btn-sm btn-light-primary"
          disabled={isBusy}
          onClick={onSubmit}
          aria-label={t('trainingPlans.line.submitAria', { name: entry.trainingName })}
        >
          {t('trainingPlans.line.submit')}
        </button>
      )}
      {isAwaitingDecision(status) && (
        <>
          <button
            type="button"
            className="btn btn-sm btn-light-success"
            disabled={isBusy}
            onClick={onApprove}
            aria-label={t('trainingPlans.line.approveAria', { name: entry.trainingName })}
          >
            {t('trainingPlans.line.approve')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            disabled={isBusy}
            onClick={onReject}
            aria-label={t('trainingPlans.line.rejectAria', { name: entry.trainingName })}
          >
            {t('trainingPlans.line.reject')}
          </button>
        </>
      )}
      <button
        type="button"
        className="btn btn-sm btn-light"
        onClick={onEdit}
        aria-label={t('trainingPlans.line.editAria', { name: entry.trainingName })}
      >
        {t('common.edit')}
      </button>
      <button
        type="button"
        className="btn btn-sm btn-light-danger"
        onClick={onDelete}
        aria-label={t('trainingPlans.line.deleteAria', { name: entry.trainingName })}
      >
        {t('common.delete')}
      </button>
    </div>
  )
}

/** Rejection needs a reason; the API stores it and the table shows it on the line. */
function RejectModal({
  trainingName,
  isBusy,
  error,
  onCancel,
  onConfirm,
}: {
  trainingName: string
  isBusy: boolean
  error: string | null
  onCancel: () => void
  onConfirm: (reason: string) => void
}) {
  const { t } = useTranslation()
  const [reason, setReason] = useState('')
  const [reasonError, setReasonError] = useState<string | undefined>()

  return (
    <Modal
      title={t('trainingPlans.line.rejectTitle', { name: trainingName })}
      isOpen
      onClose={onCancel}
      onSubmit={() => {
        if (!reason.trim()) {
          setReasonError(t('common.required'))
          return
        }
        setReasonError(undefined)
        onConfirm(reason.trim())
      }}
      isBusy={isBusy}
      confirmLabel={t('trainingPlans.line.reject')}
      error={error}
    >
      <Field
        label={t('trainingPlans.line.reasonLabel')}
        htmlFor="reject-reason"
        required
        error={reasonError}
      >
        <textarea
          id="reject-reason"
          rows={3}
          className={controlClass('form-control', reasonError)}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
        />
      </Field>
    </Modal>
  )
}

/** Create/edit dialog of a plan line. */
function LineFormModal({
  planId,
  entry,
  onClose,
}: {
  planId: number
  entry?: TrainingPlanLineNavigationDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const trainings = useLookup(RESOURCES.training)
  const save = useSavePlanLine(planId)
  const [trainingError, setTrainingError] = useState<string | undefined>()
  const line = entry?.line
  const [model, setModel] = useState<SaveTrainingPlanLineDto>(() => ({
    trainingId: line?.trainingId ?? 0,
    durationMinutes: line?.durationMinutes ?? 0,
    year: line?.year ?? new Date().getFullYear(),
    month: line?.month ?? null,
    status: line?.status ?? PlanLineStatus.Planned,
    performedDate: toDateInput(line?.performedDate) || null,
    source: line?.source ?? '',
    description: line?.description ?? '',
    instructorNationalId: line?.instructorNationalId ?? '',
    instructorTitle: line?.instructorTitle ?? '',
    instructorFullName: line?.instructorFullName ?? '',
    instructorUserId: line?.instructorUserId ?? null,
    trainingLocation: line?.trainingLocation ?? null,
    trainingType: line?.trainingType ?? null,
    isActive: line?.isActive ?? true,
  }))

  function submit() {
    if (!model.trainingId) {
      setTrainingError(t('common.required'))
      return
    }
    setTrainingError(undefined)
    save.mutate(
      {
        lineId: line?.id,
        input: {
          ...model,
          performedDate: model.performedDate || null,
          source: model.source?.trim() || null,
          description: model.description?.trim() || null,
          instructorNationalId: model.instructorNationalId?.trim() || null,
          instructorTitle: model.instructorTitle?.trim() || null,
          instructorFullName: model.instructorFullName?.trim() || null,
        },
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      title={line ? t('trainingPlans.line.editTitle') : t('trainingPlans.line.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={save.isPending}
      error={save.error ? errorMessage(save.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field
          label={t('trainingPlans.line.fields.training')}
          htmlFor="line-training"
          required
          error={trainingError}
          className="col-md-6"
        >
          <select
            id="line-training"
            className={controlClass('form-select', trainingError)}
            value={model.trainingId || ''}
            onChange={(event) => setModel({ ...model, trainingId: Number(event.target.value) || 0 })}
          >
            <option value="">{t('trainingPlans.line.selectTraining')}</option>
            {trainings.data?.items.map((training) => (
              <option key={training.id} value={training.id}>
                {training.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.line.fields.duration')}
          htmlFor="line-duration"
          className="col-md-2"
        >
          <input
            id="line-duration"
            type="number"
            min={0}
            className="form-control"
            value={model.durationMinutes}
            onChange={(event) =>
              setModel({ ...model, durationMinutes: Number(event.target.value) || 0 })
            }
          />
        </Field>

        <Field label={t('trainingPlans.line.fields.year')} htmlFor="line-year" className="col-md-2">
          <input
            id="line-year"
            type="number"
            min={2000}
            max={2200}
            className="form-control"
            value={model.year ?? ''}
            onChange={(event) =>
              setModel({ ...model, year: event.target.value === '' ? null : Number(event.target.value) })
            }
          />
        </Field>

        <Field
          label={t('trainingPlans.line.fields.month')}
          htmlFor="line-month"
          className="col-md-2"
        >
          <select
            id="line-month"
            className="form-select"
            value={model.month ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                month: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {Array.from({ length: 12 }, (_, index) => index + 1).map((month) => (
              <option key={month} value={month}>
                {t(`enums.month.${month}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.line.fields.status')}
          htmlFor="line-status"
          className="col-md-4"
        >
          <select
            id="line-status"
            className="form-select"
            value={model.status}
            onChange={(event) =>
              setModel({ ...model, status: Number(event.target.value) as PlanLineStatus })
            }
          >
            {PLAN_LINE_STATUSES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.planLineStatus.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.line.fields.performedDate')}
          htmlFor="line-performed"
          className="col-md-4"
        >
          <input
            id="line-performed"
            type="date"
            className="form-control"
            value={model.performedDate ?? ''}
            onChange={(event) => setModel({ ...model, performedDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.line.fields.trainingLocation')}
          htmlFor="line-location"
          className="col-md-4"
        >
          <select
            id="line-location"
            className="form-select"
            value={model.trainingLocation ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                trainingLocation:
                  event.target.value === ''
                    ? null
                    : (Number(event.target.value) as TrainingLocation),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {TRAINING_LOCATIONS.map((value) => (
              <option key={value} value={value}>
                {t(`enums.trainingLocation.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.line.fields.trainingType')}
          htmlFor="line-type"
          className="col-md-4"
        >
          <select
            id="line-type"
            className="form-select"
            value={model.trainingType ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                trainingType:
                  event.target.value === '' ? null : (Number(event.target.value) as TrainingType),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {TRAINING_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.trainingType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('trainingPlans.line.fields.instructorFullName')}
          htmlFor="line-instructor"
          hint={t('trainingPlans.line.instructorHint')}
          className="col-md-4"
        >
          <input
            id="line-instructor"
            className="form-control"
            value={model.instructorFullName ?? ''}
            onChange={(event) => setModel({ ...model, instructorFullName: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.line.fields.instructorTitle')}
          htmlFor="line-instructor-title"
          className="col-md-4"
        >
          <input
            id="line-instructor-title"
            className="form-control"
            value={model.instructorTitle ?? ''}
            onChange={(event) => setModel({ ...model, instructorTitle: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.line.fields.instructorNationalId')}
          htmlFor="line-instructor-id"
          className="col-md-4"
        >
          <input
            id="line-instructor-id"
            className="form-control"
            value={model.instructorNationalId ?? ''}
            onChange={(event) => setModel({ ...model, instructorNationalId: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.line.fields.source')}
          htmlFor="line-source"
          className="col-md-4"
        >
          <input
            id="line-source"
            className="form-control"
            value={model.source ?? ''}
            onChange={(event) => setModel({ ...model, source: event.target.value })}
          />
        </Field>

        <Field
          label={t('trainingPlans.line.fields.description')}
          htmlFor="line-description"
          className="col-12"
        >
          <textarea
            id="line-description"
            rows={2}
            className="form-control"
            value={model.description ?? ''}
            onChange={(event) => setModel({ ...model, description: event.target.value })}
          />
        </Field>
      </div>
    </Modal>
  )
}

/** One `<dt>`/`<dd>` pair of a definition list. */
function Term({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <>
      <dt className="col-sm-3 col-lg-2" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-9 col-lg-4">{children}</dd>
    </>
  )
}
