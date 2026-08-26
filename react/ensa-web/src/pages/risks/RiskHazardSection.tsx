import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import DataTable, { type Column } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, controlClass } from '@/components/Form'
import { HazardSourceType, type RiskAssessmentMethod } from '@/api/endpoints'
import { errorMessage } from '@/api/http'
import { formatDate } from '@/utils/format'
import {
  RISK_LEVEL_BADGE,
  useAddControlMeasure,
  useAddHazard,
  useCompleteControlMeasure,
  useEmployeeLookup,
  useRemoveHazard,
  useUpdateHazard,
  type CreateControlMeasureDto,
  type IdentifiedHazardNavigationDto,
  type SaveIdentifiedHazardDto,
} from './api'
import {
  byRiskDescending,
  fromDateInput,
  previewScore,
  ratingScale,
  toDateInput,
  todayInput,
} from './helpers'

interface Props {
  reportId: number
  companyId: number
  method: RiskAssessmentMethod
  hazards: IdentifiedHazardNavigationDto[]
}

/**
 * The hazard register of a report: every identified hazard with its rating, the resulting risk
 * level, and the control measures attached to it.
 */
export default function RiskHazardSection({ reportId, companyId, method, hazards }: Props) {
  const { t } = useTranslation()
  const [editing, setEditing] = useState<IdentifiedHazardNavigationDto | null>(null)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<IdentifiedHazardNavigationDto | null>(null)
  const [measuresOf, setMeasuresOf] = useState<IdentifiedHazardNavigationDto | null>(null)

  const remove = useRemoveHazard(reportId)

  const rows = useMemo(
    () =>
      [...hazards].sort((left, right) =>
        byRiskDescending(left.identifiedHazard, right.identifiedHazard),
      ),
    [hazards],
  )

  // The dialogs are re-read from the freshly fetched list, so a save is reflected immediately.
  const openMeasures = measuresOf
    ? (rows.find((row) => row.identifiedHazard.id === measuresOf.identifiedHazard.id) ?? null)
    : null

  const columns: Column<IdentifiedHazardNavigationDto>[] = [
    {
      key: 'hazardTag',
      header: t('riskAssessment.hazard.fields.hazardTag'),
      render: (row) => (
        <div>
          <div className="fw-semibold" style={{ color: 'var(--kt-gray-800)' }}>
            {row.identifiedHazard.hazardTag}
          </div>
          {row.identifiedHazard.activityDescription && (
            <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
              {row.identifiedHazard.activityDescription}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'riskTag',
      header: t('riskAssessment.hazard.fields.riskTag'),
      render: (row) => row.identifiedHazard.riskTag ?? t('common.none'),
    },
    {
      key: 'rating',
      header: t('riskAssessment.hazard.fields.rating'),
      align: 'center',
      render: (row) => (
        <span style={{ color: 'var(--kt-gray-600)', whiteSpace: 'nowrap' }}>
          {formatRating(row.identifiedHazard, method)}
        </span>
      ),
    },
    {
      key: 'riskScore',
      header: t('riskAssessment.hazard.fields.riskScore'),
      align: 'center',
      render: (row) => (
        <div className="d-flex flex-column align-items-center gap-1">
          <span className="fw-bold" style={{ color: 'var(--kt-gray-900)' }}>
            {row.identifiedHazard.riskScore}
          </span>
          <span className={RISK_LEVEL_BADGE[row.identifiedHazard.riskLevel]}>
            {t(`enums.riskLevel.${row.identifiedHazard.riskLevel}`)}
          </span>
        </div>
      ),
    },
    {
      key: 'residual',
      header: t('riskAssessment.hazard.fields.residualRiskScore'),
      align: 'center',
      render: (row) =>
        row.identifiedHazard.residualRiskScore == null ? (
          <span style={{ color: 'var(--kt-gray-500)' }}>{t('common.none')}</span>
        ) : (
          <div className="d-flex flex-column align-items-center gap-1">
            <span className="fw-bold" style={{ color: 'var(--kt-gray-900)' }}>
              {row.identifiedHazard.residualRiskScore}
            </span>
            <span className={RISK_LEVEL_BADGE[row.identifiedHazard.residualRiskLevel]}>
              {t(`enums.riskLevel.${row.identifiedHazard.residualRiskLevel}`)}
            </span>
          </div>
        ),
    },
    {
      key: 'owner',
      header: t('riskAssessment.hazard.fields.ownerPerson'),
      render: (row) => row.identifiedHazard.ownerPerson ?? t('common.none'),
    },
    {
      key: 'deadline',
      header: t('riskAssessment.hazard.fields.deadlineDate'),
      render: (row) => formatDate(row.identifiedHazard.deadlineDate) ?? t('common.none'),
    },
    {
      key: 'measures',
      header: t('riskAssessment.hazard.fields.controlMeasures'),
      align: 'center',
      render: (row) => {
        const open = row.controlMeasures.filter((measure) => !measure.isCompleted).length
        return (
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => setMeasuresOf(row)}
            aria-label={t('riskAssessment.measure.manageFor', {
              name: row.identifiedHazard.hazardTag,
            })}
          >
            {t('riskAssessment.measure.counter', {
              total: row.controlMeasures.length,
              open,
            })}
          </button>
        )
      },
    },
    {
      key: 'actions',
      header: t('common.actions'),
      align: 'end',
      width: '150px',
      render: (row) => (
        <div className="d-flex justify-content-end gap-2">
          <button
            type="button"
            className="btn btn-sm btn-light-primary"
            onClick={() => setEditing(row)}
            aria-label={t('riskAssessment.hazard.editFor', {
              name: row.identifiedHazard.hazardTag,
            })}
          >
            {t('common.edit')}
          </button>
          <button
            type="button"
            className="btn btn-sm btn-light-danger"
            onClick={() => setPendingDelete(row)}
            aria-label={t('riskAssessment.hazard.deleteFor', {
              name: row.identifiedHazard.hazardTag,
            })}
          >
            {t('common.delete')}
          </button>
        </div>
      ),
    },
  ]

  return (
    <>
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <div>
          <h2 className="h6 fw-semibold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
            {t('riskAssessment.hazard.title')}
          </h2>
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
            {t('riskAssessment.hazard.description', { count: rows.length })}
          </p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setCreateOpen(true)}>
          {t('riskAssessment.hazard.add')}
        </button>
      </div>

      <DataTable
        label={t('riskAssessment.hazard.title')}
        columns={columns}
        rows={rows}
        rowKey={(row) => row.identifiedHazard.id}
        emptyMessage={t('riskAssessment.hazard.empty')}
      />

      <HazardModal
        key={editing ? `edit-${editing.identifiedHazard.id}` : 'create'}
        reportId={reportId}
        method={method}
        isOpen={isCreateOpen || !!editing}
        hazard={editing}
        onClose={() => {
          setCreateOpen(false)
          setEditing(null)
        }}
      />

      <ControlMeasureModal
        key={openMeasures ? `measures-${openMeasures.identifiedHazard.id}` : 'measures'}
        companyId={companyId}
        hazard={openMeasures}
        onClose={() => setMeasuresOf(null)}
      />

      <ConfirmDialog
        isOpen={!!pendingDelete}
        title={t('riskAssessment.hazard.deleteTitle')}
        message={t('riskAssessment.hazard.deleteMessage', {
          name: pendingDelete?.identifiedHazard.hazardTag ?? '',
        })}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() =>
          pendingDelete &&
          remove.mutate(pendingDelete.identifiedHazard.id, {
            onSuccess: () => setPendingDelete(null),
          })
        }
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/** `L × F × S` for Fine-Kinney, `L × S` for the matrices — shown as the raw inputs. */
function formatRating(
  hazard: { likelihood: number; frequency: number; severity: number },
  method: RiskAssessmentMethod,
): string {
  const scale = ratingScale(method)
  return scale?.usesFrequency
    ? `${hazard.likelihood} × ${hazard.frequency} × ${hazard.severity}`
    : `${hazard.likelihood} × ${hazard.severity}`
}

// ---------------------------------------------------------------
// Hazard form
// ---------------------------------------------------------------

function emptyHazard(): SaveIdentifiedHazardDto {
  return {
    hazardTag: '',
    activityDescription: null,
    ownerPerson: null,
    riskTag: null,
    measure: null,
    likelihood: 0,
    severity: 0,
    frequency: 0,
    comment: null,
    residualLikelihood: null,
    residualSeverity: null,
    residualFrequency: null,
    residualComment: null,
    sourceType: HazardSourceType.Manual,
    deadlineDate: null,
  }
}

function toForm(hazard: IdentifiedHazardNavigationDto): SaveIdentifiedHazardDto {
  const source = hazard.identifiedHazard
  return {
    hazardCategoryId: source.hazardCategoryId,
    hazardId: source.hazardId,
    hazardTag: source.hazardTag,
    activityDescription: source.activityDescription,
    ownerPerson: source.ownerPerson,
    riskTag: source.riskTag,
    measure: source.measure,
    likelihood: source.likelihood,
    severity: source.severity,
    frequency: source.frequency,
    comment: source.comment,
    residualLikelihood: source.residualLikelihood,
    residualSeverity: source.residualSeverity,
    residualFrequency: source.residualFrequency,
    residualComment: source.residualComment,
    sourceType: source.sourceType,
    sourceId: source.sourceId,
    documentId: source.documentId,
    deadlineDate: toDateInput(source.deadlineDate),
  }
}

function HazardModal({
  reportId,
  method,
  isOpen,
  hazard,
  onClose,
}: {
  reportId: number
  method: RiskAssessmentMethod
  isOpen: boolean
  hazard: IdentifiedHazardNavigationDto | null
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<SaveIdentifiedHazardDto>(() =>
    hazard ? toForm(hazard) : emptyHazard(),
  )
  const [validation, setValidation] = useState<Record<string, string>>({})

  const add = useAddHazard(reportId)
  const update = useUpdateHazard(reportId)
  const pending = add.isPending || update.isPending
  const failure = add.error ?? update.error

  const scale = ratingScale(method)
  const preview = previewScore(method, form.likelihood, form.frequency, form.severity)
  const residualPreview = previewScore(
    method,
    form.residualLikelihood ?? 0,
    form.residualFrequency ?? 0,
    form.residualSeverity ?? 0,
  )

  function patch(changes: Partial<SaveIdentifiedHazardDto>) {
    setForm((current) => ({ ...current, ...changes }))
  }

  function submit() {
    const errors: Record<string, string> = {}
    if (!form.hazardTag.trim()) errors.hazardTag = t('validation.required')
    if (!form.likelihood) errors.likelihood = t('validation.required')
    if (!form.severity) errors.severity = t('validation.required')
    if (scale?.usesFrequency && !form.frequency) errors.frequency = t('validation.required')
    setValidation(errors)
    if (Object.keys(errors).length) return

    const input: SaveIdentifiedHazardDto = {
      ...form,
      deadlineDate: fromDateInput(form.deadlineDate ?? ''),
    }

    if (hazard) {
      update.mutate({ hazardId: hazard.identifiedHazard.id, input }, { onSuccess: onClose })
    } else {
      add.mutate(input, { onSuccess: onClose })
    }
  }

  return (
    <Modal
      title={hazard ? t('riskAssessment.hazard.editTitle') : t('riskAssessment.hazard.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={pending}
      error={failure ? errorMessage(failure) : null}
      size="xl"
    >
      <div className="row g-3">
        <Field
          label={t('riskAssessment.hazard.fields.hazardTag')}
          htmlFor="hazardTag"
          required
          error={validation.hazardTag}
          className="col-md-6"
        >
          <input
            id="hazardTag"
            className={controlClass('form-control', validation.hazardTag)}
            value={form.hazardTag}
            onChange={(event) => patch({ hazardTag: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.hazard.fields.riskTag')}
          htmlFor="riskTag"
          className="col-md-6"
        >
          <input
            id="riskTag"
            className="form-control"
            value={form.riskTag ?? ''}
            onChange={(event) => patch({ riskTag: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.hazard.fields.activityDescription')}
          htmlFor="activityDescription"
          className="col-12"
        >
          <textarea
            id="activityDescription"
            className="form-control"
            rows={2}
            value={form.activityDescription ?? ''}
            onChange={(event) => patch({ activityDescription: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.hazard.fields.measure')}
          htmlFor="measure"
          hint={t('riskAssessment.hazard.measureHint')}
          className="col-12"
        >
          <textarea
            id="measure"
            className="form-control"
            rows={2}
            value={form.measure ?? ''}
            onChange={(event) => patch({ measure: event.target.value })}
          />
        </Field>

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-0 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('riskAssessment.hazard.currentRating')}
          </h3>
        </div>

        <RatingInput
          id="likelihood"
          label={t('riskAssessment.hazard.fields.likelihood')}
          values={scale?.likelihood}
          value={form.likelihood}
          error={validation.likelihood}
          onChange={(value) => patch({ likelihood: value })}
        />

        {scale?.usesFrequency !== false && (
          <RatingInput
            id="frequency"
            label={t('riskAssessment.hazard.fields.frequency')}
            values={scale?.frequency}
            value={form.frequency}
            error={validation.frequency}
            onChange={(value) => patch({ frequency: value })}
          />
        )}

        <RatingInput
          id="severity"
          label={t('riskAssessment.hazard.fields.severity')}
          values={scale?.severity}
          value={form.severity}
          error={validation.severity}
          onChange={(value) => patch({ severity: value })}
        />

        <div className="col-md-3 d-flex align-items-end">
          <p className="mb-2" style={{ color: 'var(--kt-gray-600)', fontSize: '0.875rem' }}>
            {preview == null
              ? t('riskAssessment.hazard.previewUnavailable')
              : t('riskAssessment.hazard.preview', { score: preview })}
          </p>
        </div>

        <div className="col-12">
          <h3 className="h6 fw-semibold mb-0 mt-2" style={{ color: 'var(--kt-gray-900)' }}>
            {t('riskAssessment.hazard.residualRating')}
          </h3>
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
            {t('riskAssessment.hazard.residualHint')}
          </p>
        </div>

        <RatingInput
          id="residualLikelihood"
          label={t('riskAssessment.hazard.fields.likelihood')}
          values={scale?.likelihood}
          value={form.residualLikelihood ?? 0}
          onChange={(value) => patch({ residualLikelihood: value || null })}
        />

        {scale?.usesFrequency !== false && (
          <RatingInput
            id="residualFrequency"
            label={t('riskAssessment.hazard.fields.frequency')}
            values={scale?.frequency}
            value={form.residualFrequency ?? 0}
            onChange={(value) => patch({ residualFrequency: value || null })}
          />
        )}

        <RatingInput
          id="residualSeverity"
          label={t('riskAssessment.hazard.fields.severity')}
          values={scale?.severity}
          value={form.residualSeverity ?? 0}
          onChange={(value) => patch({ residualSeverity: value || null })}
        />

        <div className="col-md-3 d-flex align-items-end">
          <p className="mb-2" style={{ color: 'var(--kt-gray-600)', fontSize: '0.875rem' }}>
            {residualPreview == null
              ? t('riskAssessment.hazard.previewUnavailable')
              : t('riskAssessment.hazard.preview', { score: residualPreview })}
          </p>
        </div>

        <Field
          label={t('riskAssessment.hazard.fields.ownerPerson')}
          htmlFor="ownerPerson"
          className="col-md-4"
        >
          <input
            id="ownerPerson"
            className="form-control"
            value={form.ownerPerson ?? ''}
            onChange={(event) => patch({ ownerPerson: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.hazard.fields.deadlineDate')}
          htmlFor="deadlineDate"
          className="col-md-4"
        >
          <input
            id="deadlineDate"
            type="date"
            className="form-control"
            value={form.deadlineDate ?? ''}
            onChange={(event) => patch({ deadlineDate: event.target.value })}
          />
        </Field>

        <Field
          label={t('riskAssessment.hazard.fields.comment')}
          htmlFor="comment"
          className="col-md-4"
        >
          <input
            id="comment"
            className="form-control"
            value={form.comment ?? ''}
            onChange={(event) => patch({ comment: event.target.value })}
          />
        </Field>
      </div>
    </Modal>
  )
}

/**
 * One rating input.
 *
 * Methods with a fixed scale get a drop-down of exactly the permitted values, which is what stops
 * a Fine-Kinney severity of 4 — a value that scale does not have — from being entered at all.
 */
function RatingInput({
  id,
  label,
  values,
  value,
  error,
  onChange,
}: {
  id: string
  label: string
  values: number[] | undefined
  value: number
  error?: string
  onChange: (next: number) => void
}) {
  const { t } = useTranslation()

  return (
    <Field label={label} htmlFor={id} error={error} className="col-md-3">
      {values?.length ? (
        <select
          id={id}
          className={controlClass('form-select', error)}
          value={value || ''}
          onChange={(event) => onChange(Number(event.target.value))}
        >
          <option value="">{t('riskAssessment.hazard.selectValue')}</option>
          {values.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      ) : (
        <input
          id={id}
          type="number"
          min={0}
          step="any"
          className={controlClass('form-control', error)}
          value={value || ''}
          onChange={(event) => onChange(Number(event.target.value))}
        />
      )}
    </Field>
  )
}

// ---------------------------------------------------------------
// Control measures
// ---------------------------------------------------------------

function ControlMeasureModal({
  companyId,
  hazard,
  onClose,
}: {
  companyId: number
  hazard: IdentifiedHazardNavigationDto | null
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [form, setForm] = useState<CreateControlMeasureDto>({
    measure: '',
    deadlineDate: null,
    ownerCompanyEmployeeId: null,
  })
  const [validation, setValidation] = useState<string | undefined>()

  const employees = useEmployeeLookup(hazard ? companyId : undefined)
  const add = useAddControlMeasure()
  const complete = useCompleteControlMeasure()

  const failure = add.error ?? complete.error

  function submit() {
    if (!hazard) return
    if (!form.measure.trim()) {
      setValidation(t('validation.required'))
      return
    }
    setValidation(undefined)

    add.mutate(
      {
        hazardId: hazard.identifiedHazard.id,
        input: { ...form, deadlineDate: fromDateInput(form.deadlineDate ?? '') },
      },
      {
        onSuccess: () => setForm({ measure: '', deadlineDate: null, ownerCompanyEmployeeId: null }),
      },
    )
  }

  return (
    <Modal
      title={t('riskAssessment.measure.title')}
      isOpen={!!hazard}
      onClose={onClose}
      onSubmit={submit}
      isBusy={add.isPending}
      confirmLabel={t('riskAssessment.measure.add')}
      error={failure ? errorMessage(failure) : null}
      size="lg"
    >
      <p className="fw-semibold mb-3" style={{ color: 'var(--kt-gray-800)' }}>
        {hazard?.identifiedHazard.hazardTag}
      </p>

      {hazard?.controlMeasures.length ? (
        <ul className="list-unstyled mb-4 d-flex flex-column gap-2">
          {hazard.controlMeasures.map((measure) => (
            <li
              key={measure.id}
              className="d-flex flex-wrap align-items-center justify-content-between gap-2 p-3"
              style={{ backgroundColor: 'var(--kt-gray-100)', borderRadius: '0.475rem' }}
            >
              <div>
                <div style={{ color: 'var(--kt-gray-800)' }}>{measure.measure}</div>
                <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>
                  {measure.deadlineDate
                    ? t('riskAssessment.measure.deadline', {
                        date: formatDate(measure.deadlineDate),
                      })
                    : t('riskAssessment.measure.noDeadline')}
                </div>
              </div>
              {measure.isCompleted ? (
                <span className="badge-light-success">
                  {t('riskAssessment.measure.completedOn', {
                    date: formatDate(measure.completionDate) ?? '',
                  })}
                </span>
              ) : (
                <button
                  type="button"
                  className="btn btn-sm btn-light-success"
                  disabled={complete.isPending}
                  onClick={() =>
                    complete.mutate({
                      controlMeasureId: measure.id,
                      completionDate: todayInput(),
                    })
                  }
                >
                  {t('riskAssessment.measure.complete')}
                </button>
              )}
            </li>
          ))}
        </ul>
      ) : (
        <p className="mb-4" style={{ color: 'var(--kt-gray-500)' }}>
          {t('riskAssessment.measure.empty')}
        </p>
      )}

      <div className="row g-3">
        <Field
          label={t('riskAssessment.measure.fields.measure')}
          htmlFor="newMeasure"
          required
          error={validation}
          className="col-12"
        >
          <textarea
            id="newMeasure"
            className={controlClass('form-control', validation)}
            rows={2}
            value={form.measure}
            onChange={(event) => setForm((current) => ({ ...current, measure: event.target.value }))}
          />
        </Field>

        <Field
          label={t('riskAssessment.measure.fields.deadlineDate')}
          htmlFor="measureDeadline"
          className="col-md-6"
        >
          <input
            id="measureDeadline"
            type="date"
            className="form-control"
            value={form.deadlineDate ?? ''}
            onChange={(event) =>
              setForm((current) => ({ ...current, deadlineDate: event.target.value }))
            }
          />
        </Field>

        <Field
          label={t('riskAssessment.measure.fields.owner')}
          htmlFor="measureOwner"
          className="col-md-6"
        >
          <select
            id="measureOwner"
            className="form-select"
            value={form.ownerCompanyEmployeeId ?? ''}
            onChange={(event) =>
              setForm((current) => ({
                ...current,
                ownerCompanyEmployeeId: event.target.value ? Number(event.target.value) : null,
              }))
            }
          >
            <option value="">{t('riskAssessment.measure.noOwner')}</option>
            {employees.data?.items.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.displayName}
              </option>
            ))}
          </select>
        </Field>
      </div>
    </Modal>
  )
}
