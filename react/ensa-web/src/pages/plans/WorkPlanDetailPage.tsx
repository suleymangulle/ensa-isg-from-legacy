import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, Input, NumberInput, Select, TextArea, Tooltip } from 'rich-react-component'
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
  canSubmit,
  isApproved,
  isAwaitingDecision,
  useDeleteWorkPlanLine,
  useGenerateDefaultLines,
  usePeriodLookup,
  useSaveWorkPlanLine,
  useWorkPlanCompletion,
  useWorkPlanDetail,
  useWorkPlanLineWorkflow,
  type SaveWorkPlanLineDto,
  type WorkPlanLineNavigationDto,
  type WorkPlanNavigationDto,
} from './api'

/** ISO date (`YYYY-MM-DD`) as an `<input type="date">` wants it. */
function toDateInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : ''
}

/**
 * One annual work plan: its cover page, its lines and the per-line approval workflow.
 *
 * "Generate default lines" scaffolds an empty plan from the default activity catalogue. The API
 * refuses a second run, so the button disappears as soon as the plan has a line — the screen
 * renders the rule rather than letting the call fail.
 */
export default function WorkPlanDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const planId = Number(id)

  const { data, isLoading, error } = useWorkPlanDetail(planId)
  const completion = useWorkPlanCompletion(planId)

  const [isLineCreateOpen, setLineCreateOpen] = useState(false)
  const [editingLine, setEditingLine] = useState<WorkPlanLineNavigationDto | null>(null)
  const [deletingLine, setDeletingLine] = useState<WorkPlanLineNavigationDto | null>(null)
  const [rejectingLine, setRejectingLine] = useState<WorkPlanLineNavigationDto | null>(null)
  const [isGenerateOpen, setGenerateOpen] = useState(false)

  const removeLine = useDeleteWorkPlanLine(planId)
  const workflow = useWorkPlanLineWorkflow(planId)

  if (isLoading) return <Spinner />
  if (error) return <ErrorPanel message={errorMessage(error)} />
  if (!data) return <ErrorPanel message={t('errors.notFound')} />

  const lines = data.lines
  const hasLines = lines.length > 0

  const columns: Column<WorkPlanLineNavigationDto>[] = [
    {
      key: 'activityName',
      header: t('workPlan.line.fields.activity'),
      render: (entry) => (
        <>
          <span className="fw-semibold d-block">{entry.activityName}</span>
          {entry.line.approvalStatus === ApprovalStatus.Rejected && entry.line.rejectionReason && (
            <span
              className="d-block mt-1"
              style={{ color: 'var(--kt-danger)', fontSize: '0.8125rem' }}
            >
              {t('workPlan.line.rejectionReason', { reason: entry.line.rejectionReason })}
            </span>
          )}
        </>
      ),
    },
    {
      key: 'period',
      header: t('workPlan.line.fields.period'),
      render: (entry) => {
        const month = entry.line.month ? t(`enums.month.${entry.line.month}`) : ''
        return `${month} ${entry.line.year}`.trim()
      },
    },
    {
      key: 'performedDate',
      header: t('workPlan.line.fields.performedDate'),
      render: (entry) => formatDate(entry.line.performedDate) ?? t('common.none'),
    },
    {
      key: 'instructor',
      header: t('workPlan.line.fields.instructor'),
      render: (entry) => entry.instructorUserFullName ?? t('common.none'),
    },
    {
      key: 'status',
      header: t('workPlan.line.fields.status'),
      align: 'center',
      render: (entry) => {
        const status = entry.line.status ?? PlanLineStatus.Planned
        return (
          <Badge variant={PLAN_LINE_STATUS_BADGE[status]}>
            {t(`enums.planLineStatus.${status}`)}
          </Badge>
        )
      },
    },
    {
      key: 'approvalStatus',
      header: t('workPlan.line.fields.approvalStatus'),
      align: 'center',
      render: (entry) => {
        const status = entry.line.approvalStatus ?? ApprovalStatus.Draft
        return (
          <Badge variant={APPROVAL_STATUS_BADGE[status]}>
            {t(`enums.approvalStatus.${status}`)}
          </Badge>
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
            <Link to="/work-plans" className="text-decoration-none">
              {t('workPlan.list.title')}
            </Link>
          </li>
          <li className="breadcrumb-item active" aria-current="page">
            {data.company?.displayName ?? t('workPlan.detail.fallbackTitle')}
          </li>
        </ol>
      </nav>

      <PageTitle
        title={data.company?.displayName ?? t('workPlan.detail.fallbackTitle')}
        description={t('workPlan.detail.description', {
          year: new Date(data.workPlan.startDate).getFullYear(),
        })}
        action={
          <div className="d-flex gap-2">
            {!hasLines && (
              <Button variant="light" 
                onClick={() => setGenerateOpen(true)}
              >
                {t('workPlan.detail.generate')}
              </Button>
            )}
            <Button variant="primary" onClick={() => setLineCreateOpen(true)}>
              {t('workPlan.line.create')}
            </Button>
          </div>
        }
      />

      <div className="row g-4">
        <div className="col-12">
          <HeaderCard
            detail={data}
            completionPercentage={completion.data?.completionPercentage}
          />
        </div>

        <div className="col-12">
          <Card
            
            header={
            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
              <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                {t('workPlan.detail.lines')}
              </h2>
              {!hasLines && (
                <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
                  {t('workPlan.detail.generateHint')}
                </span>
              )}
            
            </div>
            }
          >
              {workflow.error && (
                <div className="p-4 pb-0">
                  <ErrorPanel message={errorMessage(workflow.error)} />
                </div>
              )}
              <DataTable
                label={t('workPlan.detail.lines')}
                columns={columns}
                rows={lines}
                rowKey={(entry) => entry.line.id}
                emptyMessage={t('workPlan.detail.emptyLines')}
              />
            
          </Card>
        </div>
      </div>

      {isLineCreateOpen && (
        <LineFormModal
          planId={planId}
          defaultYear={new Date(data.workPlan.startDate).getFullYear()}
          onClose={() => setLineCreateOpen(false)}
        />
      )}

      {editingLine && (
        <LineFormModal
          planId={planId}
          entry={editingLine}
          defaultYear={new Date(data.workPlan.startDate).getFullYear()}
          onClose={() => setEditingLine(null)}
        />
      )}

      {isGenerateOpen && (
        <GenerateModal
          planId={planId}
          defaultYear={new Date(data.workPlan.startDate).getFullYear()}
          onClose={() => setGenerateOpen(false)}
        />
      )}

      {rejectingLine && (
        <RejectModal
          activityName={rejectingLine.activityName}
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
        title={t('workPlan.line.deleteTitle')}
        message={t('workPlan.line.deleteMessage', { name: deletingLine?.activityName ?? '' })}
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

/** Cover-page facts of the plan plus the completion figure the API computes. */
function HeaderCard({
  detail,
  completionPercentage,
}: {
  detail: WorkPlanNavigationDto
  completionPercentage?: number
}) {
  const { t } = useTranslation()
  const plan = detail.workPlan
  const none = t('common.none')

  return (
    <Card>
        <dl className="row mb-0" style={{ fontSize: '0.9375rem' }}>
          <Term label={t('workPlan.fields.documentNo')}>{plan.documentNo ?? none}</Term>
          <Term label={t('workPlan.fields.revisionNo')}>{plan.revisionNo ?? none}</Term>
          <Term label={t('workPlan.fields.startDate')}>{formatDate(plan.startDate) ?? none}</Term>
          <Term label={t('workPlan.fields.publicationDate')}>
            {formatDate(plan.publicationDate) ?? none}
          </Term>
          <Term label={t('workPlan.fields.specialist')}>{detail.specialistFullName ?? none}</Term>
          <Term label={t('workPlan.fields.physician')}>{detail.physicianFullName ?? none}</Term>
          <Term label={t('workPlan.fields.approver')}>{detail.approverFullName ?? none}</Term>
          <Term label={t('workPlan.detail.completion')}>
            {completionPercentage == null ? (
              none
            ) : (
              <Badge variant="info">
                {t('workPlan.detail.completionValue', { value: completionPercentage })}
              </Badge>
            )}
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
  entry: WorkPlanLineNavigationDto
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
      <Tooltip content={t('workPlan.line.lockedHint')}>
        <Badge variant="success">
          {t('workPlan.line.locked')}
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
          aria-label={t('workPlan.line.submitAria', { name: entry.activityName })}
        >
          {t('workPlan.line.submit')}
        </Button>
      )}
      {isAwaitingDecision(status) && (
        <>
          <Button variant="light" size="sm" 
            disabled={isBusy}
            onClick={onApprove}
            aria-label={t('workPlan.line.approveAria', { name: entry.activityName })}
          >
            {t('workPlan.line.approve')}
          </Button>
          <Button variant="light" size="sm" 
            disabled={isBusy}
            onClick={onReject}
            aria-label={t('workPlan.line.rejectAria', { name: entry.activityName })}
          >
            {t('workPlan.line.reject')}
          </Button>
        </>
      )}
      <Button variant="light" size="sm"
        onClick={onEdit}
        aria-label={t('workPlan.line.editAria', { name: entry.activityName })}
      >
        {t('common.edit')}
      </Button>
      <Button variant="light" size="sm" 
        onClick={onDelete}
        aria-label={t('workPlan.line.deleteAria', { name: entry.activityName })}
      >
        {t('common.delete')}
      </Button>
    </div>
  )
}

/** Rejection needs a reason; the API stores it and the table shows it on the line. */
function RejectModal({
  activityName,
  isBusy,
  error,
  onCancel,
  onConfirm,
}: {
  activityName: string
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
      title={t('workPlan.line.rejectTitle', { name: activityName })}
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
      confirmLabel={t('workPlan.line.reject')}
      error={error}
    >
      <TextArea
        id="work-reject-reason"
        label={t('workPlan.line.reasonLabel')}
        required
        error={reasonError}
        rows={3}
        value={reason}
        onChange={setReason}
      />
    </Modal>
  )
}

/** Scaffolds an empty plan from the default activity catalogue. */
function GenerateModal({
  planId,
  defaultYear,
  onClose,
}: {
  planId: number
  defaultYear: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const generate = useGenerateDefaultLines(planId)
  const [year, setYear] = useState(defaultYear)

  return (
    <Modal
      title={t('workPlan.detail.generateTitle')}
      isOpen
      onClose={onClose}
      onSubmit={() => generate.mutate(year, { onSuccess: onClose })}
      isBusy={generate.isPending}
      confirmLabel={t('workPlan.detail.generate')}
      error={generate.error ? errorMessage(generate.error) : null}
    >
      <p style={{ color: 'var(--kt-gray-500)' }}>{t('workPlan.detail.generateDescription')}</p>
      <NumberInput
        id="generate-year"
        label={t('workPlan.line.fields.year')}
        required
        min={2000}
        max={2200}
        value={year}
        onChange={(value) => setYear(value ?? defaultYear)}
      />
    </Modal>
  )
}

/** Create/edit dialog of a work plan line. */
function LineFormModal({
  planId,
  entry,
  defaultYear,
  onClose,
}: {
  planId: number
  entry?: WorkPlanLineNavigationDto
  defaultYear: number
  onClose: () => void
}) {
  const { t } = useTranslation()
  const activities = useLookup(RESOURCES.activity)
  const users = useLookup(RESOURCES.user)
  const periods = usePeriodLookup()
  const save = useSaveWorkPlanLine(planId)
  const [activityError, setActivityError] = useState<string | undefined>()
  const line = entry?.line
  const [model, setModel] = useState<SaveWorkPlanLineDto>(() => ({
    activityId: line?.activityId ?? 0,
    periodId: line?.periodId ?? null,
    year: line?.year ?? defaultYear,
    month: line?.month ?? null,
    status: line?.status ?? PlanLineStatus.Planned,
    performedDate: toDateInput(line?.performedDate) || null,
    description: line?.description ?? '',
    instructorNationalId: line?.instructorNationalId ?? '',
    instructorUserId: line?.instructorUserId ?? null,
    isActive: line?.isActive ?? true,
  }))

  function submit() {
    if (!model.activityId) {
      setActivityError(t('common.required'))
      return
    }
    setActivityError(undefined)
    save.mutate(
      {
        lineId: line?.id,
        input: {
          ...model,
          performedDate: model.performedDate || null,
          description: model.description?.trim() || null,
          instructorNationalId: model.instructorNationalId?.trim() || null,
        },
      },
      { onSuccess: onClose },
    )
  }

  return (
    <Modal
      title={line ? t('workPlan.line.editTitle') : t('workPlan.line.createTitle')}
      isOpen
      onClose={onClose}
      onSubmit={submit}
      isBusy={save.isPending}
      error={save.error ? errorMessage(save.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Select
          id="work-line-activity"
          label={t('workPlan.line.fields.activity')}
          required
          error={activityError}
          className="col-md-6"
          placeholder={t('workPlan.line.selectActivity')}
          options={
            activities.data?.items.map((activity) => ({
              value: activity.id,
              label: activity.displayName,
            })) ?? []
          }
          value={model.activityId || null}
          onChange={(value) => setModel({ ...model, activityId: value ?? 0 })}
        />

        <Select
          id="work-line-period"
          label={t('workPlan.line.fields.period')}
          className="col-md-6"
          placeholder={t('common.none')}
          options={
            periods.data?.items.map((period) => ({ value: period.id, label: period.displayName })) ?? []
          }
          value={model.periodId ?? null}
          onChange={(value) => setModel({ ...model, periodId: value })}
        />

        <NumberInput
          id="work-line-year"
          label={t('workPlan.line.fields.year')}
          required
          className="col-md-3"
          min={2000}
          max={2200}
          value={model.year}
          onChange={(value) => setModel({ ...model, year: value ?? defaultYear })}
        />

        <Select
          id="work-line-month"
          label={t('workPlan.line.fields.month')}
          className="col-md-3"
          placeholder={t('common.none')}
          options={Array.from({ length: 12 }, (_, index) => index + 1).map((month) => ({
            value: month,
            label: t(`enums.month.${month}`),
          }))}
          value={model.month ?? null}
          onChange={(value) => setModel({ ...model, month: value })}
        />

        <Select
          id="work-line-status"
          label={t('workPlan.line.fields.status')}
          className="col-md-3"
          options={PLAN_LINE_STATUSES.map((value) => ({
            value,
            label: t(`enums.planLineStatus.${value}`),
          }))}
          value={model.status ?? PlanLineStatus.Planned}
          onChange={(value) => setModel({ ...model, status: value ?? PlanLineStatus.Planned })}
        />

        <Input
          id="work-line-performed"
          label={t('workPlan.line.fields.performedDate')}
          className="col-md-3"
          value={model.performedDate ?? ''}
          inputProps={{ type: 'date' }}
          onChange={(value) => setModel({ ...model, performedDate: value })}
        />

        <Select
          id="work-line-instructor"
          label={t('workPlan.line.fields.instructor')}
          className="col-md-6"
          placeholder={t('common.none')}
          options={
            users.data?.items.map((user) => ({ value: user.id, label: user.displayName })) ?? []
          }
          value={model.instructorUserId ?? null}
          onChange={(value) => setModel({ ...model, instructorUserId: value })}
        />

        <Input
          id="work-line-instructor-id"
          label={t('workPlan.line.fields.instructorNationalId')}
          helpText={t('workPlan.line.instructorHint')}
          className="col-md-6"
          value={model.instructorNationalId ?? ''}
          onChange={(value) => setModel({ ...model, instructorNationalId: value })}
        />

        <TextArea
          id="work-line-description"
          label={t('workPlan.line.fields.description')}
          rows={2}
          className="col-12"
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
