import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge, Button, Card, CheckBox, Input, NumberInput, Select, TextArea } from 'rich-react-component'
import { ErrorPanel, PageTitle, Spinner } from '@/components/DataTable'
import { ConfirmDialog, Modal } from '@/components/Form'
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
  const [userId, setUserId] = useState<number | null>(null)
  const [companyId, setCompanyId] = useState<number | null>(null)
  const [operationType, setOperationType] = useState<VisitType | null>(null)
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
    userId ?? undefined,
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
      if (companyId !== null && visit.companyId !== companyId) return false
      if (operationType !== null && visit.operationType !== operationType) return false
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
          <Button variant="primary"
            onClick={() => {
              setEditingId(undefined)
              setIsEditorOpen(true)
            }}
          >
            {t('visit.list.create')}
          </Button>
        }
      />

      <Card
        className="mb-4"
      >
          <div className="row g-3 align-items-end">
            <div className="col-auto">
              <div className="btn-group" role="group" aria-label={t('visit.filters.moveRange')}>
                <Button variant="light"
                  onClick={() => moveMonth(-1)}
                  aria-label={t('visit.filters.previousMonth')}
                >
                  ‹
                </Button>
                <Button variant="light"
                  onClick={() => {
                    const range = monthRange(new Date())
                    setFrom(range.from)
                    setTo(range.to)
                  }}
                >
                  {t('visit.filters.thisMonth')}
                </Button>
                <Button variant="light"
                  onClick={() => moveMonth(1)}
                  aria-label={t('visit.filters.nextMonth')}
                >
                  ›
                </Button>
              </div>
            </div>

            <Input
              id="visit-from"
              label={t('visit.filters.from')}
              className="col-sm-6 col-md-2"
              value={from}
              onChange={setFrom}
              inputProps={{ type: 'date' }}
            />

            <Input
              id="visit-to"
              label={t('visit.filters.to')}
              error={isRangeValid ? undefined : t('visit.filters.invalidRange')}
              className="col-sm-6 col-md-2"
              value={to}
              onChange={setTo}
              inputProps={{ type: 'date' }}
            />

            <Select<number>
              id="visit-user"
              label={t('visit.filters.user')}
              className="col-sm-6 col-md-2"
              placeholder={t('visit.filters.allUsers')}
              options={(users.data?.items ?? []).map((user) => ({
                value: user.id,
                label: user.displayName,
              }))}
              value={userId}
              onChange={setUserId}
            />

            <Select<number>
              id="visit-company"
              label={t('visit.filters.company')}
              className="col-sm-6 col-md-2"
              placeholder={t('visit.filters.allCompanies')}
              options={(companies.data?.items ?? []).map((company) => ({
                value: company.id,
                label: company.displayName,
              }))}
              value={companyId}
              onChange={setCompanyId}
            />

            <Select<VisitType>
              id="visit-type"
              label={t('visit.filters.operationType')}
              className="col-sm-6 col-md-2"
              placeholder={t('visit.filters.allTypes')}
              options={VISIT_TYPES.map((value) => ({
                value,
                label: t(`enums.visitType.${value}`),
              }))}
              value={operationType}
              onChange={setOperationType}
            />

            <Select
              id="visit-completed"
              label={t('visit.filters.completed')}
              className="col-sm-6 col-md-2"
              placeholder={t('common.all')}
              options={[
                { value: 'true', label: t('visit.filters.onlyCompleted') },
                { value: 'false', label: t('visit.filters.onlyPlanned') },
              ]}
              value={completed === '' ? null : completed}
              onChange={(value) => setCompleted(value ?? '')}
            />
          </div>
        
      </Card>

      {!isRangeValid ? (
        <ErrorPanel message={t('visit.filters.invalidRange')} />
      ) : calendar.isLoading ? (
        <Spinner />
      ) : calendar.error ? (
        <ErrorPanel message={errorMessage(calendar.error)} />
      ) : days.length === 0 ? (
        <Card >
          <div className="text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
            {t('visit.list.empty')}
          </div>
        </Card>
      ) : (
        <div className="d-flex flex-column gap-3">
          {days.map((group) => (
            <section key={group.day} aria-label={formatDayHeading(group.day)}>
              <Card
                header={
                  <div className="d-flex align-items-center justify-content-between w-100">
                    <h2 className="h6 fw-semibold mb-0" style={{ color: 'var(--kt-gray-800)' }}>
                      {formatDayHeading(group.day)}
                    </h2>
                    <Badge variant="primary">
                      {t('visit.list.dayCount', { count: group.items.length })}
                    </Badge>
                  </div>
                }
              >
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
                    <Badge variant="info">
                      {t(`enums.visitType.${visit.operationType}`)}
                    </Badge>
                    <span style={{ color: 'var(--kt-gray-600)' }}>
                      {visit.userFullName ?? t('visit.list.userFallback', { id: visit.userId })}
                    </span>
                    <Badge variant={visit.completed ? 'success' : 'warning'}>
                      {visit.completed ? t('visit.list.completed') : t('visit.list.planned')}
                    </Badge>
                    <span className="d-flex gap-2 ms-auto">
                      <Button variant="light" size="sm" 
                        onClick={() => {
                          setEditingId(visit.id)
                          setIsEditorOpen(true)
                        }}
                        aria-label={t('visit.list.editAria', { title: visit.title })}
                      >
                        {t('common.edit')}
                      </Button>
                      <Button variant="light" size="sm" 
                        onClick={() => setDeleting(visit)}
                        aria-label={t('visit.list.deleteAria', { title: visit.title })}
                      >
                        {t('common.delete')}
                      </Button>
                    </span>
                    </li>
                  ))}
                </ul>
              </Card>
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
  companyId: number | null
  userId: number | null
  visitDate: string
  start: string
  end: string
  operationType: VisitType
  description: string
  scheduledWeek: number | null
  scheduledMonth: number | null
  regionCode: number | null
  otherCompanyDistanceKm: number | null
  completed: boolean
}

function emptyEditor(): EditorState {
  return {
    companyId: null,
    userId: null,
    visitDate: toDateInput(new Date()),
    start: '',
    end: '',
    operationType: VisitType.RoutineVisit,
    description: '',
    scheduledWeek: null,
    scheduledMonth: null,
    regionCode: null,
    otherCompanyDistanceKm: null,
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
            companyId: visit.companyId,
            userId: visit.userId,
            visitDate: visit.visitDate.slice(0, 10),
            start: toDateTimeInput(visit.start),
            end: toDateTimeInput(visit.end),
            operationType: visit.operationType,
            description: visit.description ?? '',
            scheduledWeek: visit.scheduledWeek ?? null,
            scheduledMonth: visit.scheduledMonth ?? null,
            regionCode: visit.regionCode ?? null,
            otherCompanyDistanceKm: visit.otherCompanyDistanceKm ?? null,
            completed: visit.completed,
          }
        : emptyEditor(),
    )
  }, [isOpen, visit])

  const create = useCreate<CreateVisitDto, VisitDto>(VISIT, { onSuccess: onClose })
  const update = useUpdate<UpdateVisitDto, VisitDto>(VISIT, { onSuccess: onClose })
  const mutation = visit ? update : create

  function submit() {
    const nextErrors: Partial<Record<keyof EditorState, string>> = {}
    if (state.companyId === null || state.companyId < 1) {
      nextErrors.companyId = t('visit.editor.companyRequired')
    }
    if (!state.visitDate) nextErrors.visitDate = t('visit.editor.dateRequired')
    if (state.start && state.end && state.end < state.start) {
      nextErrors.end = t('visit.editor.endBeforeStart')
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length) return

    const base: CreateVisitDto = {
      companyId: state.companyId as number,
      userId: state.userId,
      visitDate: state.visitDate,
      start: state.start || null,
      end: state.end || null,
      operationType: state.operationType,
      description: state.description.trim() || null,
      scheduledWeek: state.scheduledWeek,
      scheduledMonth: state.scheduledMonth,
      regionCode: state.regionCode,
      otherCompanyDistanceKm: state.otherCompanyDistanceKm,
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
        <Select<number>
          id="visit-editor-company"
          label={t('visit.fields.company')}
          required
          error={errors.companyId}
          className="col-md-6"
          placeholder={t('visit.editor.selectCompany')}
          options={(companies.data?.items ?? []).map((company) => ({
            value: company.id,
            label: company.displayName,
          }))}
          value={state.companyId}
          onChange={(value) => setState((s) => ({ ...s, companyId: value }))}
        />

        <Select<number>
          id="visit-editor-user"
          label={t('visit.fields.user')}
          helpText={t('visit.editor.userHint')}
          className="col-md-6"
          placeholder={t('visit.editor.currentUser')}
          options={(users.data?.items ?? []).map((user) => ({
            value: user.id,
            label: user.displayName,
          }))}
          value={state.userId}
          onChange={(value) => setState((s) => ({ ...s, userId: value }))}
        />

        <Input
          id="visit-editor-date"
          label={t('visit.fields.visitDate')}
          required
          error={errors.visitDate}
          className="col-md-4"
          value={state.visitDate}
          onChange={(value) => setState((s) => ({ ...s, visitDate: value }))}
          inputProps={{ type: 'date' }}
        />

        <Input
          id="visit-editor-start"
          label={t('visit.fields.start')}
          className="col-md-4"
          value={state.start}
          onChange={(value) => setState((s) => ({ ...s, start: value }))}
          inputProps={{ type: 'datetime-local' }}
        />

        <Input
          id="visit-editor-end"
          label={t('visit.fields.end')}
          error={errors.end}
          className="col-md-4"
          value={state.end}
          onChange={(value) => setState((s) => ({ ...s, end: value }))}
          inputProps={{ type: 'datetime-local' }}
        />

        <Select<VisitType>
          id="visit-editor-type"
          label={t('visit.fields.operationType')}
          className="col-md-6"
          options={VISIT_TYPES.map((value) => ({ value, label: t(`enums.visitType.${value}`) }))}
          value={state.operationType}
          onChange={(value) =>
            setState((s) => ({ ...s, operationType: value ?? s.operationType }))
          }
        />

        <NumberInput
          id="visit-editor-distance"
          label={t('visit.fields.otherCompanyDistanceKm')}
          className="col-md-6"
          min={0}
          step={0.01}
          value={state.otherCompanyDistanceKm}
          onChange={(value) => setState((s) => ({ ...s, otherCompanyDistanceKm: value }))}
        />

        <NumberInput
          id="visit-editor-week"
          label={t('visit.fields.scheduledWeek')}
          className="col-md-4"
          min={1}
          max={53}
          value={state.scheduledWeek}
          onChange={(value) => setState((s) => ({ ...s, scheduledWeek: value }))}
        />

        <NumberInput
          id="visit-editor-month"
          label={t('visit.fields.scheduledMonth')}
          className="col-md-4"
          min={1}
          max={12}
          value={state.scheduledMonth}
          onChange={(value) => setState((s) => ({ ...s, scheduledMonth: value }))}
        />

        <NumberInput
          id="visit-editor-region"
          label={t('visit.fields.regionCode')}
          className="col-md-4"
          value={state.regionCode}
          onChange={(value) => setState((s) => ({ ...s, regionCode: value }))}
        />

        <TextArea
          id="visit-editor-description"
          label={t('visit.fields.description')}
          rows={3}
          value={state.description}
          onChange={(value) => setState((s) => ({ ...s, description: value }))}
        />

        {visit && (
          <div className="col-12">
            <CheckBox
              id="visit-editor-completed"
              label={t('visit.fields.completed')}
              checked={state.completed}
              onChange={(checked) => setState((s) => ({ ...s, completed: checked }))}
            />
          </div>
        )}
      </div>
    </Modal>
  )
}
