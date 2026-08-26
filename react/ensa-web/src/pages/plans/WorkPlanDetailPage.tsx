import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import DataTable, { ErrorPanel, PageTitle, Spinner, type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, controlClass } from '@/components/Form'
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
          <span className={PLAN_LINE_STATUS_BADGE[status]}>
            {t(`enums.planLineStatus.${status}`)}
          </span>
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
          <span className={APPROVAL_STATUS_BADGE[status]}>
            {t(`enums.approvalStatus.${status}`)}
          </span>
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
              <button
                className="btn btn-light-primary"
                type="button"
                onClick={() => setGenerateOpen(true)}
              >
                {t('workPlan.detail.generate')}
              </button>
            )}
            <button className="btn btn-primary" type="button" onClick={() => setLineCreateOpen(true)}>
              {t('workPlan.line.create')}
            </button>
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
          <div className="card">
            <div className="card-header d-flex flex-wrap align-items-center justify-content-between gap-2">
              <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-900)' }}>
                {t('workPlan.detail.lines')}
              </h2>
              {!hasLines && (
                <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
                  {t('workPlan.detail.generateHint')}
                </span>
              )}
            </div>
            <div className="card-body p-0">
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
            </div>
          </div>
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
    <div className="card">
      <div className="card-body">
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
              <span className="badge-light-info">
                {t('workPlan.detail.completionValue', { value: completionPercentage })}
              </span>
            )}
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
      <span className="badge-light-success" title={t('workPlan.line.lockedHint')}>
        {t('workPlan.line.locked')}
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
          aria-label={t('workPlan.line.submitAria', { name: entry.activityName })}
        >
          {t('workPlan.line.submit')}
        </button>
      )}
      {isAwaitingDecision(status) && (
        <>
          <button
            type="button"
            className="btn btn-sm btn-light-success"
            disabled={isBusy}
            onClick={onApprove}
            aria-label={t('workPlan.line.approveAria', { name: entry.activityName })}
          >
            {t('workPlan.line.approve')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            disabled={isBusy}
            onClick={onReject}
            aria-label={t('workPlan.line.rejectAria', { name: entry.activityName })}
          >
            {t('workPlan.line.reject')}
          </button>
        </>
      )}
      <button
        type="button"
        className="btn btn-sm btn-light"
        onClick={onEdit}
        aria-label={t('workPlan.line.editAria', { name: entry.activityName })}
      >
        {t('common.edit')}
      </button>
      <button
        type="button"
        className="btn btn-sm btn-light-danger"
        onClick={onDelete}
        aria-label={t('workPlan.line.deleteAria', { name: entry.activityName })}
      >
        {t('common.delete')}
      </button>
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
      <Field
        label={t('workPlan.line.reasonLabel')}
        htmlFor="work-reject-reason"
        required
        error={reasonError}
      >
        <textarea
          id="work-reject-reason"
          rows={3}
          className={controlClass('form-control', reasonError)}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
        />
      </Field>
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
      <Field label={t('workPlan.line.fields.year')} htmlFor="generate-year" required>
        <input
          id="generate-year"
          type="number"
          min={2000}
          max={2200}
          className="form-control"
          value={year}
          onChange={(event) => setYear(Number(event.target.value) || defaultYear)}
        />
      </Field>
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
        <Field
          label={t('workPlan.line.fields.activity')}
          htmlFor="work-line-activity"
          required
          error={activityError}
          className="col-md-6"
        >
          <select
            id="work-line-activity"
            className={controlClass('form-select', activityError)}
            value={model.activityId || ''}
            onChange={(event) => setModel({ ...model, activityId: Number(event.target.value) || 0 })}
          >
            <option value="">{t('workPlan.line.selectActivity')}</option>
            {activities.data?.items.map((activity) => (
              <option key={activity.id} value={activity.id}>
                {activity.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('workPlan.line.fields.period')}
          htmlFor="work-line-period"
          className="col-md-6"
        >
          <select
            id="work-line-period"
            className="form-select"
            value={model.periodId ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                periodId: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {periods.data?.items.map((period) => (
              <option key={period.id} value={period.id}>
                {period.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('workPlan.line.fields.year')}
          htmlFor="work-line-year"
          required
          className="col-md-3"
        >
          <input
            id="work-line-year"
            type="number"
            min={2000}
            max={2200}
            className="form-control"
            value={model.year}
            onChange={(event) =>
              setModel({ ...model, year: Number(event.target.value) || defaultYear })
            }
          />
        </Field>

        <Field
          label={t('workPlan.line.fields.month')}
          htmlFor="work-line-month"
          className="col-md-3"
        >
          <select
            id="work-line-month"
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
          label={t('workPlan.line.fields.status')}
          htmlFor="work-line-status"
          className="col-md-3"
        >
          <select
            id="work-line-status"
            className="form-select"
            value={model.status ?? PlanLineStatus.Planned}
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
          label={t('workPlan.line.fields.performedDate')}
          htmlFor="work-line-performed"
          className="col-md-3"
        >
          <input
            id="work-line-performed"
            type="date"
            className="form-control"
            value={model.performedDate ?? ''}
            onChange={(event) => setModel({ ...model, performedDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('workPlan.line.fields.instructor')}
          htmlFor="work-line-instructor"
          className="col-md-6"
        >
          <select
            id="work-line-instructor"
            className="form-select"
            value={model.instructorUserId ?? ''}
            onChange={(event) =>
              setModel({
                ...model,
                instructorUserId: event.target.value === '' ? null : Number(event.target.value),
              })
            }
          >
            <option value="">{t('common.none')}</option>
            {users.data?.items.map((user) => (
              <option key={user.id} value={user.id}>
                {user.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('workPlan.line.fields.instructorNationalId')}
          htmlFor="work-line-instructor-id"
          hint={t('workPlan.line.instructorHint')}
          className="col-md-6"
        >
          <input
            id="work-line-instructor-id"
            className="form-control"
            value={model.instructorNationalId ?? ''}
            onChange={(event) => setModel({ ...model, instructorNationalId: event.target.value })}
          />
        </Field>

        <Field
          label={t('workPlan.line.fields.description')}
          htmlFor="work-line-description"
          className="col-12"
        >
          <textarea
            id="work-line-description"
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
