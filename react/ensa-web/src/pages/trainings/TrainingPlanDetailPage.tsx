import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  Badge,
  Button,
  Card,
  CheckBox,
  Input,
  NumberInput,
  Select,
  TextArea,
  Tooltip,
} from 'rich-react-component'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Modal } from '@/components/Form'
import { errorMessage } from '@/api/http'
import { PLAN_LINE_STATUS_BADGE, useLookup } from '@/api/endpoints'
import { ApprovalStatus, PlanLineStatus } from '@/api/enums'
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
        <Badge variant={PLAN_LINE_STATUS_BADGE[entry.line.status]}>
          {t(`enums.planLineStatus.${entry.line.status}`)}
        </Badge>
      ),
    },
    {
      key: 'approvalStatus',
      header: t('trainingPlans.line.fields.approvalStatus'),
      align: 'center',
      render: (entry) => {
        const status = entry.line.approvalStatus ?? ApprovalStatus.Draft
        return (
          <Badge variant={APPROVAL_STATUS_BADGE[status]}>{t(`enums.approvalStatus.${status}`)}</Badge>
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
          <Button variant="primary" onClick={() => setLineCreateOpen(true)}>
            {t('trainingPlans.line.create')}
          </Button>
        }
      />

      <div className="row g-4">
        <div className="col-12">
          <HeaderCard detail={data} />
        </div>

        <div className="col-12">
          <Card
            
            header={
            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
              <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                {t('trainingPlans.detail.lines')}
              </h2>
              <div className="d-flex align-items-center gap-3">
                <Badge variant="warning">
                  {t('trainingPlans.detail.incompleteCount', {
                    count: incomplete.data?.items.length ?? 0,
                  })}
                </Badge>
                <CheckBox
                  id="only-incomplete"
                  checked={onlyIncomplete}
                  onChange={setOnlyIncomplete}
                  label={t('trainingPlans.detail.onlyIncomplete')}
                />
              </div>
            
            </div>
            }
          >
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
            
          </Card>
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
    <Card>
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
            <Badge variant={plan.isActive ? 'success' : 'danger'}>
              {plan.isActive ? t('common.active') : t('common.passive')}
            </Badge>
          </Term>
        </dl>
      
    </Card>
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
      <Tooltip content={t('trainingPlans.line.lockedHint')}>
        <Badge variant="success">
          {t('trainingPlans.line.locked')}
        </Badge>
      </Tooltip>
    )
  }

  return (
    <div className="d-flex justify-content-end flex-wrap gap-1">
      {canSubmit(status) && (
        <Button variant="light" size="sm" 
          disabled={isBusy}
          onClick={onSubmit}
          aria-label={t('trainingPlans.line.submitAria', { name: entry.trainingName })}
        >
          {t('trainingPlans.line.submit')}
        </Button>
      )}
      {isAwaitingDecision(status) && (
        <>
          <Button variant="light" size="sm" 
            disabled={isBusy}
            onClick={onApprove}
            aria-label={t('trainingPlans.line.approveAria', { name: entry.trainingName })}
          >
            {t('trainingPlans.line.approve')}
          </Button>
          <Button variant="light" size="sm" 
            disabled={isBusy}
            onClick={onReject}
            aria-label={t('trainingPlans.line.rejectAria', { name: entry.trainingName })}
          >
            {t('trainingPlans.line.reject')}
          </Button>
        </>
      )}
      <Button variant="light" size="sm"
        onClick={onEdit}
        aria-label={t('trainingPlans.line.editAria', { name: entry.trainingName })}
      >
        {t('common.edit')}
      </Button>
      <Button variant="light" size="sm" 
        onClick={onDelete}
        aria-label={t('trainingPlans.line.deleteAria', { name: entry.trainingName })}
      >
        {t('common.delete')}
      </Button>
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
      <TextArea
        id="reject-reason"
        label={t('trainingPlans.line.reasonLabel')}
        required
        error={reasonError}
        rows={3}
        value={reason}
        onChange={setReason}
      />
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
        <Select
          id="line-training"
          label={t('trainingPlans.line.fields.training')}
          required
          error={trainingError}
          className="col-md-6"
          placeholder={t('trainingPlans.line.selectTraining')}
          options={
            trainings.data?.items.map((training) => ({
              value: training.id,
              label: training.displayName,
            })) ?? []
          }
          value={model.trainingId || null}
          onChange={(value) => setModel({ ...model, trainingId: value ?? 0 })}
        />

        <NumberInput
          id="line-duration"
          label={t('trainingPlans.line.fields.duration')}
          className="col-md-2"
          min={0}
          value={model.durationMinutes}
          onChange={(value) => setModel({ ...model, durationMinutes: value ?? 0 })}
        />

        <NumberInput
          id="line-year"
          label={t('trainingPlans.line.fields.year')}
          className="col-md-2"
          min={2000}
          max={2200}
          value={model.year ?? null}
          onChange={(value) => setModel({ ...model, year: value })}
        />

        <Select
          id="line-month"
          label={t('trainingPlans.line.fields.month')}
          className="col-md-2"
          placeholder={t('common.none')}
          options={Array.from({ length: 12 }, (_, index) => index + 1).map((month) => ({
            value: month,
            label: t(`enums.month.${month}`),
          }))}
          value={model.month ?? null}
          onChange={(value) => setModel({ ...model, month: value })}
        />

        <Select
          id="line-status"
          label={t('trainingPlans.line.fields.status')}
          className="col-md-4"
          options={PLAN_LINE_STATUSES.map((value) => ({
            value,
            label: t(`enums.planLineStatus.${value}`),
          }))}
          value={model.status}
          onChange={(value) => value !== null && setModel({ ...model, status: value })}
        />

        <Input
          id="line-performed"
          label={t('trainingPlans.line.fields.performedDate')}
          className="col-md-4"
          value={model.performedDate ?? ''}
          onChange={(value) => setModel({ ...model, performedDate: value })}
          inputProps={{ type: 'date' }}
        />

        <Select
          id="line-location"
          label={t('trainingPlans.line.fields.trainingLocation')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={TRAINING_LOCATIONS.map((value) => ({
            value,
            label: t(`enums.trainingLocation.${value}`),
          }))}
          value={model.trainingLocation ?? null}
          onChange={(value) => setModel({ ...model, trainingLocation: value })}
        />

        <Select
          id="line-type"
          label={t('trainingPlans.line.fields.trainingType')}
          className="col-md-4"
          placeholder={t('common.none')}
          options={TRAINING_TYPES.map((value) => ({
            value,
            label: t(`enums.trainingType.${value}`),
          }))}
          value={model.trainingType ?? null}
          onChange={(value) => setModel({ ...model, trainingType: value })}
        />

        <Input
          id="line-instructor"
          label={t('trainingPlans.line.fields.instructorFullName')}
          helpText={t('trainingPlans.line.instructorHint')}
          className="col-md-4"
          value={model.instructorFullName ?? ''}
          onChange={(value) => setModel({ ...model, instructorFullName: value })}
        />

        <Input
          id="line-instructor-title"
          label={t('trainingPlans.line.fields.instructorTitle')}
          className="col-md-4"
          value={model.instructorTitle ?? ''}
          onChange={(value) => setModel({ ...model, instructorTitle: value })}
        />

        <Input
          id="line-instructor-id"
          label={t('trainingPlans.line.fields.instructorNationalId')}
          className="col-md-4"
          value={model.instructorNationalId ?? ''}
          onChange={(value) => setModel({ ...model, instructorNationalId: value })}
        />

        <Input
          id="line-source"
          label={t('trainingPlans.line.fields.source')}
          className="col-md-4"
          value={model.source ?? ''}
          onChange={(value) => setModel({ ...model, source: value })}
        />

        <TextArea
          id="line-description"
          label={t('trainingPlans.line.fields.description')}
          className="col-12"
          rows={2}
          value={model.description ?? ''}
          onChange={(value) => setModel({ ...model, description: value })}
        />
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
