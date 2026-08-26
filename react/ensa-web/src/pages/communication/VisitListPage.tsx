import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog, Field, Modal, controlClass } from '@/components/Form'
import { useCreate, useDelete, useUpdate } from '@/api/mutations'
import { errorMessage } from '@/api/http'
import { useEntity } from '@/api/endpoints'
import { VisitType } from '@/api/enums'
import {
  VISIT,
  useCompanyLookup,
  useUserLookup,
  useVisitCalendar,
  type CreateVisitDto,
  type UpdateVisitDto,
  type VisitCalendarDto,
  type VisitDto,
} from './api'
import {
  VISIT_TYPES,
  formatDayHeading,
  formatTime,
  groupByDay,
  monthRange,
  shiftMonth,
  toDateInput,
  toDateTimeInput,
} from './helpers'

/**
 * Workplace visit planner — the legacy `ZiyaretTakvimi.aspx`.
 *
 * The legacy screen was a hand-rolled week grid with a Google Maps pane. The modern equivalent
 * here is a **date-ranged day agenda**: one bounded request per range, entries grouped under
 * their day, filtered by specialist, workplace, visit type and completion. It keeps what the
 * calendar was actually used for — "what is on for this person in this period, and did it
 * happen?" — without a calendar widget the shared UI kit does not have.
 *
 * The data comes from `GET api/visit/calendar` rather than from the paged list, because only
 * the calendar shape carries the workplace name and the visiting user's name. The paged list
 * returns bare ids, which would mean resolving a name per row — exactly the per-row request
 * pattern this codebase forbids.
 */
export default function VisitListPage() {
  const { t } = useTranslation()

  const initialRange = useMemo(() => monthRange(new Date()), [])
  const [from, setFrom] = useState(initialRange.from)
  const [to, setTo] = useState(initialRange.to)
  const [userId, setUserId] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [operationType, setOperationType] = useState('')
  const [completed, setCompleted] = useState('')

  const [editingId, setEditingId] = useState<number | undefined>()
  const [isEditorOpen, setIsEditorOpen] = useState(false)
  const [deleting, setDeleting] = useState<VisitCalendarDto | null>(null)

  const users = useUserLookup()
  const companies = useCompanyLookup()

  const isRangeValid = !!from && !!to && from <= to
  const calendar = useVisitCalendar(
    isRangeValid ? from : '',
    isRangeValid ? to : '',
    userId === '' ? undefined : Number(userId),
  )

  const editing = useEntity<VisitDto>(VISIT, editingId)
  const remove = useDelete(VISIT, { onSuccess: () => setDeleting(null) })

  /**
   * Workplace, visit type and completion are narrowed in the browser: the calendar route takes
   * only a user and a date range, and one bounded request that is filtered locally beats three
   * that the API cannot answer.
   */
  const visible = useMemo(() => {
    const items = calendar.data?.items ?? []
    return items.filter((visit) => {
      if (companyId !== '' && visit.companyId !== Number(companyId)) return false
      if (operationType !== '' && visit.operationType !== Number(operationType)) return false
      if (completed !== '' && visit.completed !== (completed === 'true')) return false
      return true
    })
  }, [calendar.data, companyId, operationType, completed])

  const days = useMemo(() => groupByDay(visible), [visible])

  function moveMonth(delta: number) {
    const anchor = shiftMonth(from, delta)
    const range = monthRange(anchor)
    setFrom(range.from)
    setTo(range.to)
  }

  return (
    <>
      <PageTitle
        title={t('visit.list.title')}
        description={t('visit.list.description')}
        action={
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => {
              setEditingId(undefined)
              setIsEditorOpen(true)
            }}
          >
            {t('visit.list.create')}
          </button>
        }
      />

      <div className="card mb-4">
        <div className="card-body">
          <div className="row g-3 align-items-end">
            <div className="col-auto">
              <div className="btn-group" role="group" aria-label={t('visit.filters.moveRange')}>
                <button
                  type="button"
                  className="btn btn-light"
                  onClick={() => moveMonth(-1)}
                  aria-label={t('visit.filters.previousMonth')}
                >
                  ‹
                </button>
                <button
                  type="button"
                  className="btn btn-light"
                  onClick={() => {
                    const range = monthRange(new Date())
                    setFrom(range.from)
                    setTo(range.to)
                  }}
                >
                  {t('visit.filters.thisMonth')}
                </button>
                <button
                  type="button"
                  className="btn btn-light"
                  onClick={() => moveMonth(1)}
                  aria-label={t('visit.filters.nextMonth')}
                >
                  ›
                </button>
              </div>
            </div>

            <Field label={t('visit.filters.from')} htmlFor="visit-from" className="col-sm-6 col-md-2">
              <input
                id="visit-from"
                type="date"
                className="form-control"
                value={from}
                onChange={(event) => setFrom(event.target.value)}
              />
            </Field>

            <Field
              label={t('visit.filters.to')}
              htmlFor="visit-to"
              error={isRangeValid ? undefined : t('visit.filters.invalidRange')}
              className="col-sm-6 col-md-2"
            >
              <input
                id="visit-to"
                type="date"
                className={controlClass(
                  'form-control',
                  isRangeValid ? undefined : t('visit.filters.invalidRange'),
                )}
                value={to}
                onChange={(event) => setTo(event.target.value)}
              />
            </Field>

            <Field label={t('visit.filters.user')} htmlFor="visit-user" className="col-sm-6 col-md-2">
              <select
                id="visit-user"
                className="form-select"
                value={userId}
                onChange={(event) => setUserId(event.target.value)}
              >
                <option value="">{t('visit.filters.allUsers')}</option>
                {users.data?.items.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.displayName}
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label={t('visit.filters.company')}
              htmlFor="visit-company"
              className="col-sm-6 col-md-2"
            >
              <select
                id="visit-company"
                className="form-select"
                value={companyId}
                onChange={(event) => setCompanyId(event.target.value)}
              >
                <option value="">{t('visit.filters.allCompanies')}</option>
                {companies.data?.items.map((company) => (
                  <option key={company.id} value={company.id}>
                    {company.displayName}
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label={t('visit.filters.operationType')}
              htmlFor="visit-type"
              className="col-sm-6 col-md-2"
            >
              <select
                id="visit-type"
                className="form-select"
                value={operationType}
                onChange={(event) => setOperationType(event.target.value)}
              >
                <option value="">{t('visit.filters.allTypes')}</option>
                {VISIT_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {t(`enums.visitType.${value}`)}
                  </option>
                ))}
              </select>
            </Field>

            <Field
              label={t('visit.filters.completed')}
              htmlFor="visit-completed"
              className="col-sm-6 col-md-2"
            >
              <select
                id="visit-completed"
                className="form-select"
                value={completed}
                onChange={(event) => setCompleted(event.target.value)}
              >
                <option value="">{t('common.all')}</option>
                <option value="true">{t('visit.filters.onlyCompleted')}</option>
                <option value="false">{t('visit.filters.onlyPlanned')}</option>
              </select>
            </Field>
          </div>
        </div>
      </div>

      {!isRangeValid ? (
        <ErrorPanel message={t('visit.filters.invalidRange')} />
      ) : calendar.isLoading ? (
        <Spinner />
      ) : calendar.error ? (
        <ErrorPanel message={errorMessage(calendar.error)} />
      ) : days.length === 0 ? (
        <div className="card">
          <div className="card-body text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
            {t('visit.list.empty')}
          </div>
        </div>
      ) : (
        <div className="d-flex flex-column gap-3">
          {days.map((group) => (
            <section className="card" key={group.day} aria-label={formatDayHeading(group.day)}>
              <div className="card-header d-flex align-items-center justify-content-between">
                <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-800)' }}>
                  {formatDayHeading(group.day)}
                </h2>
                <span className="badge-light-primary">
                  {t('visit.list.dayCount', { count: group.items.length })}
                </span>
              </div>
              <ul className="list-unstyled mb-0">
                {group.items.map((visit) => (
                  <li
                    key={visit.id}
                    className="d-flex flex-wrap align-items-center gap-3 px-4 py-3"
                    style={{ borderTop: '1px solid var(--kt-border-color)' }}
                  >
                    <ColorDot color={visit.color} />
                    <span
                      className="fw-semibold"
                      style={{ minWidth: 88, color: 'var(--kt-gray-700)' }}
                    >
                      {formatTime(visit.start) ?? t('common.none')}
                      {visit.end && visit.end !== visit.start
                        ? ` – ${formatTime(visit.end) ?? ''}`
                        : ''}
                    </span>
                    <span className="flex-grow-1" style={{ minWidth: 220 }}>
                      <span className="fw-semibold d-block" style={{ color: 'var(--kt-gray-900)' }}>
                        {visit.companyName ?? t('visit.list.companyFallback', { id: visit.companyId })}
                      </span>
                      <span style={{ color: 'var(--kt-gray-600)' }}>{visit.title}</span>
                    </span>
                    <span className="badge-light-info">
                      {t(`enums.visitType.${visit.operationType}`)}
                    </span>
                    <span style={{ color: 'var(--kt-gray-600)' }}>
                      {visit.userFullName ?? t('visit.list.userFallback', { id: visit.userId })}
                    </span>
                    <span className={visit.completed ? 'badge-light-success' : 'badge-light-warning'}>
                      {visit.completed ? t('visit.list.completed') : t('visit.list.planned')}
                    </span>
                    <span className="d-flex gap-2 ms-auto">
                      <button
                        type="button"
                        className="btn btn-sm btn-light-primary"
                        onClick={() => {
                          setEditingId(visit.id)
                          setIsEditorOpen(true)
                        }}
                        aria-label={t('visit.list.editAria', { title: visit.title })}
                      >
                        {t('common.edit')}
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm btn-light-danger"
                        onClick={() => setDeleting(visit)}
                        aria-label={t('visit.list.deleteAria', { title: visit.title })}
                      >
                        {t('common.delete')}
                      </button>
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
      )}

      {isEditorOpen && (!editingId || editing.data) && (
        <VisitEditor
          isOpen
          visit={editingId ? editing.data : undefined}
          onClose={() => {
            setIsEditorOpen(false)
            setEditingId(undefined)
          }}
        />
      )}

      <ConfirmDialog
        isOpen={!!deleting}
        title={t('visit.list.deleteTitle')}
        message={t('visit.list.deleteMessage', { title: deleting?.title ?? '' })}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting.id)}
        isBusy={remove.isPending}
        error={remove.error ? errorMessage(remove.error) : null}
      />
    </>
  )
}

/**
 * The colour swatch of a calendar entry.
 *
 * The value is stored data, not a design decision, so it is rendered as it came back — but only
 * after it has been checked against a hex literal, so a stray value cannot inject a style.
 */
function ColorDot({ color }: { color?: string | null }) {
  const safe = color && /^#[0-9a-fA-F]{3,8}$/.test(color) ? color : null

  return (
    <span
      aria-hidden="true"
      style={{
        width: 10,
        height: 10,
        borderRadius: '50%',
        flex: '0 0 auto',
        backgroundColor: safe ?? 'var(--kt-gray-400)',
      }}
    />
  )
}

interface EditorState {
  companyId: string
  userId: string
  visitDate: string
  start: string
  end: string
  operationType: VisitType
  description: string
  scheduledWeek: string
  scheduledMonth: string
  regionCode: string
  otherCompanyDistanceKm: string
  completed: boolean
}

function emptyEditor(): EditorState {
  return {
    companyId: '',
    userId: '',
    visitDate: toDateInput(new Date()),
    start: '',
    end: '',
    operationType: VisitType.RoutineVisit,
    description: '',
    scheduledWeek: '',
    scheduledMonth: '',
    regionCode: '',
    otherCompanyDistanceKm: '',
    completed: false,
  }
}

function VisitEditor({
  isOpen,
  visit,
  onClose,
}: {
  isOpen: boolean
  visit?: VisitDto
  onClose: () => void
}) {
  const { t } = useTranslation()
  const [state, setState] = useState<EditorState>(emptyEditor)
  const [errors, setErrors] = useState<Partial<Record<keyof EditorState, string>>>({})

  const companies = useCompanyLookup()
  const users = useUserLookup()

  useEffect(() => {
    if (!isOpen) return
    setErrors({})
    setState(
      visit
        ? {
            companyId: visit.companyId.toString(),
            userId: visit.userId.toString(),
            visitDate: visit.visitDate.slice(0, 10),
            start: toDateTimeInput(visit.start),
            end: toDateTimeInput(visit.end),
            operationType: visit.operationType,
            description: visit.description ?? '',
            scheduledWeek: visit.scheduledWeek?.toString() ?? '',
            scheduledMonth: visit.scheduledMonth?.toString() ?? '',
            regionCode: visit.regionCode?.toString() ?? '',
            otherCompanyDistanceKm: visit.otherCompanyDistanceKm?.toString() ?? '',
            completed: visit.completed,
          }
        : emptyEditor(),
    )
  }, [isOpen, visit])

  const create = useCreate<CreateVisitDto, VisitDto>(VISIT, { onSuccess: onClose })
  const update = useUpdate<UpdateVisitDto, VisitDto>(VISIT, { onSuccess: onClose })
  const mutation = visit ? update : create

  function optionalInt(value: string): number | null {
    const parsed = Number(value)
    return value.trim() && Number.isFinite(parsed) ? Math.trunc(parsed) : null
  }

  function submit() {
    const nextErrors: Partial<Record<keyof EditorState, string>> = {}
    const companyId = Number(state.companyId)
    if (!state.companyId || !Number.isFinite(companyId) || companyId < 1) {
      nextErrors.companyId = t('visit.editor.companyRequired')
    }
    if (!state.visitDate) nextErrors.visitDate = t('visit.editor.dateRequired')
    if (state.start && state.end && state.end < state.start) {
      nextErrors.end = t('visit.editor.endBeforeStart')
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const distance = Number(state.otherCompanyDistanceKm)

    const base: CreateVisitDto = {
      companyId: Math.trunc(companyId),
      userId: optionalInt(state.userId),
      visitDate: state.visitDate,
      start: state.start || null,
      end: state.end || null,
      operationType: state.operationType,
      description: state.description.trim() || null,
      scheduledWeek: optionalInt(state.scheduledWeek),
      scheduledMonth: optionalInt(state.scheduledMonth),
      regionCode: optionalInt(state.regionCode),
      otherCompanyDistanceKm:
        state.otherCompanyDistanceKm.trim() && Number.isFinite(distance) ? distance : null,
    }

    if (visit) update.mutate({ id: visit.id, input: { ...base, completed: state.completed } })
    else create.mutate(base)
  }

  return (
    <Modal
      title={visit ? t('visit.editor.editTitle') : t('visit.editor.createTitle')}
      isOpen={isOpen}
      onClose={onClose}
      onSubmit={submit}
      isBusy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
      size="lg"
    >
      <div className="row g-3">
        <Field
          label={t('visit.fields.company')}
          htmlFor="visit-editor-company"
          required
          error={errors.companyId}
          className="col-md-6"
        >
          <select
            id="visit-editor-company"
            className={controlClass('form-select', errors.companyId)}
            value={state.companyId}
            onChange={(event) => setState((s) => ({ ...s, companyId: event.target.value }))}
          >
            <option value="">{t('visit.editor.selectCompany')}</option>
            {companies.data?.items.map((company) => (
              <option key={company.id} value={company.id}>
                {company.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('visit.fields.user')}
          htmlFor="visit-editor-user"
          hint={t('visit.editor.userHint')}
          className="col-md-6"
        >
          <select
            id="visit-editor-user"
            className="form-select"
            value={state.userId}
            onChange={(event) => setState((s) => ({ ...s, userId: event.target.value }))}
          >
            <option value="">{t('visit.editor.currentUser')}</option>
            {users.data?.items.map((user) => (
              <option key={user.id} value={user.id}>
                {user.displayName}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('visit.fields.visitDate')}
          htmlFor="visit-editor-date"
          required
          error={errors.visitDate}
          className="col-md-4"
        >
          <input
            id="visit-editor-date"
            type="date"
            className={controlClass('form-control', errors.visitDate)}
            value={state.visitDate}
            onChange={(event) => setState((s) => ({ ...s, visitDate: event.target.value }))}
          />
        </Field>

        <Field label={t('visit.fields.start')} htmlFor="visit-editor-start" className="col-md-4">
          <input
            id="visit-editor-start"
            type="datetime-local"
            className="form-control"
            value={state.start}
            onChange={(event) => setState((s) => ({ ...s, start: event.target.value }))}
          />
        </Field>

        <Field
          label={t('visit.fields.end')}
          htmlFor="visit-editor-end"
          error={errors.end}
          className="col-md-4"
        >
          <input
            id="visit-editor-end"
            type="datetime-local"
            className={controlClass('form-control', errors.end)}
            value={state.end}
            onChange={(event) => setState((s) => ({ ...s, end: event.target.value }))}
          />
        </Field>

        <Field
          label={t('visit.fields.operationType')}
          htmlFor="visit-editor-type"
          className="col-md-6"
        >
          <select
            id="visit-editor-type"
            className="form-select"
            value={state.operationType}
            onChange={(event) =>
              setState((s) => ({ ...s, operationType: Number(event.target.value) }))
            }
          >
            {VISIT_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`enums.visitType.${value}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field
          label={t('visit.fields.otherCompanyDistanceKm')}
          htmlFor="visit-editor-distance"
          className="col-md-6"
        >
          <input
            id="visit-editor-distance"
            type="number"
            min={0}
            step="0.01"
            className="form-control"
            value={state.otherCompanyDistanceKm}
            onChange={(event) =>
              setState((s) => ({ ...s, otherCompanyDistanceKm: event.target.value }))
            }
          />
        </Field>

        <Field
          label={t('visit.fields.scheduledWeek')}
          htmlFor="visit-editor-week"
          className="col-md-4"
        >
          <input
            id="visit-editor-week"
            type="number"
            min={1}
            max={53}
            className="form-control"
            value={state.scheduledWeek}
            onChange={(event) => setState((s) => ({ ...s, scheduledWeek: event.target.value }))}
          />
        </Field>

        <Field
          label={t('visit.fields.scheduledMonth')}
          htmlFor="visit-editor-month"
          className="col-md-4"
        >
          <input
            id="visit-editor-month"
            type="number"
            min={1}
            max={12}
            className="form-control"
            value={state.scheduledMonth}
            onChange={(event) => setState((s) => ({ ...s, scheduledMonth: event.target.value }))}
          />
        </Field>

        <Field
          label={t('visit.fields.regionCode')}
          htmlFor="visit-editor-region"
          className="col-md-4"
        >
          <input
            id="visit-editor-region"
            type="number"
            className="form-control"
            value={state.regionCode}
            onChange={(event) => setState((s) => ({ ...s, regionCode: event.target.value }))}
          />
        </Field>

        <Field label={t('visit.fields.description')} htmlFor="visit-editor-description">
          <textarea
            id="visit-editor-description"
            className="form-control"
            rows={3}
            value={state.description}
            onChange={(event) => setState((s) => ({ ...s, description: event.target.value }))}
          />
        </Field>

        {visit && (
          <div className="col-12">
            <div className="form-check">
              <input
                id="visit-editor-completed"
                type="checkbox"
                className="form-check-input"
                checked={state.completed}
                onChange={(event) => setState((s) => ({ ...s, completed: event.target.checked }))}
              />
              <label htmlFor="visit-editor-completed" className="form-check-label">
                {t('visit.fields.completed')}
              </label>
            </div>
          </div>
        )}
      </div>
    </Modal>
  )
}
