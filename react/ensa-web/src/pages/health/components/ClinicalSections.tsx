import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/http'
import {
  ExamFinding,
  HabitStatus,
  TriStateAnswer,
  type HabitType,
  type ImmunizationType,
  type LabTestType,
  type MedicalComplaintType,
  type PhysicalExamSystem,
  type WorkConditionType,
} from '@/api/enums'
import {
  COMPLAINT_TYPES,
  EXAM_FINDINGS,
  HABIT_STATUSES,
  HABIT_TYPES,
  IMMUNIZATION_TYPES,
  LAB_TEST_TYPES,
  PHYSICAL_EXAM_SYSTEMS,
  TRI_STATE_ANSWERS,
  WORK_CONDITION_TYPES,
  useSaveChildSet,
  type ChildSetKey,
  type MedicalExamComplaintDto,
  type MedicalExamHabitDto,
  type MedicalExamImmunizationDto,
  type MedicalExamLabTestDto,
  type MedicalExamPhysicalFindingDto,
  type MedicalExamWorkConditionDto,
  type SaveMedicalExamComplaintDto,
  type SaveMedicalExamHabitDto,
  type SaveMedicalExamImmunizationDto,
  type SaveMedicalExamLabTestDto,
  type SaveMedicalExamPhysicalFindingDto,
  type SaveMedicalExamWorkConditionDto,
} from '../api'

/**
 * The six clinical child sets of an EK-2 examination form.
 *
 * PRIVACY. Everything rendered here is special-category health data and reaches the browser
 * only through the single-record detail call. Each section is a self-contained editor with its
 * own save endpoint, so a physician can record one part of the examination without the rest
 * being in a valid state.
 *
 * Every child table is unique on (form, type) and each save replaces the whole set, so a
 * section renders one fixed row per enum member instead of a free "add row" list. Rows the
 * physician left untouched are not sent, which keeps the record free of empty clinical rows.
 */

/** Turns a list of child rows into a lookup keyed by its enum discriminator. */
function byKey<TRow, TKey extends number>(rows: TRow[], key: (row: TRow) => TKey) {
  const map = new Map<TKey, TRow>()
  for (const row of rows) map.set(key(row), row)
  return map
}

interface SectionProps {
  formId: number
  isReadOnly: boolean
}

interface SectionShellProps {
  title: string
  description: string
  isReadOnly: boolean
  isDirty: boolean
  isSaving: boolean
  error: unknown
  onSave: () => void
  onReset: () => void
  children: ReactNode
}

/** Section chrome: heading, save/undo actions and the failed-save message. */
function SectionShell({
  title,
  description,
  isReadOnly,
  isDirty,
  isSaving,
  error,
  onSave,
  onReset,
  children,
}: SectionShellProps) {
  const { t } = useTranslation()

  return (
    <section className="mb-4">
      <div className="d-flex flex-wrap align-items-start justify-content-between gap-2 mb-3">
        <div>
          <h2 className="h6 fw-bold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
            {title}
          </h2>
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
            {description}
          </p>
        </div>

        {!isReadOnly && (
          <div className="d-flex gap-2">
            <button
              type="button"
              className="btn btn-sm btn-light"
              disabled={!isDirty || isSaving}
              onClick={onReset}
            >
              {t('medicalExamination.sections.revert')}
            </button>
            <button
              type="button"
              className="btn btn-sm btn-primary"
              disabled={!isDirty || isSaving}
              onClick={onSave}
            >
              {isSaving && (
                <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
              )}
              {t('common.save')}
            </button>
          </div>
        )}
      </div>

      {error != null && (
        <div
          className="alert border-0"
          style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
          role="alert"
        >
          {errorMessage(error)}
        </div>
      )}

      <div className="table-responsive">{children}</div>
    </section>
  )
}

/** Small labelled control used inside a clinical table cell. */
function CellSelect({
  id,
  label,
  value,
  options,
  optionLabel,
  onChange,
  disabled,
}: {
  id: string
  label: string
  value: number
  options: number[]
  optionLabel: (option: number) => string
  onChange: (next: number) => void
  disabled: boolean
}) {
  return (
    <>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <select
        id={id}
        className="form-select form-select-sm"
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(Number(event.target.value))}
      >
        {options.map((option) => (
          <option key={option} value={option}>
            {optionLabel(option)}
          </option>
        ))}
      </select>
    </>
  )
}

function CellInput({
  id,
  label,
  value,
  onChange,
  disabled,
  type = 'text',
  placeholder,
}: {
  id: string
  label: string
  value: string
  onChange: (next: string) => void
  disabled: boolean
  type?: 'text' | 'number' | 'date'
  placeholder?: string
}) {
  return (
    <>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <input
        id={id}
        type={type}
        className="form-control form-control-sm"
        value={value}
        disabled={disabled}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
      />
    </>
  )
}

/** Wires a child set editor to its own PUT endpoint. */
function useChildSetEditor<TState, TInput>(
  formId: number,
  childSet: ChildSetKey,
  initial: TState,
  toPayload: (state: TState) => TInput[],
) {
  const [state, setState] = useState(initial)
  const [isDirty, setIsDirty] = useState(false)
  const mutation = useSaveChildSet<TInput>(formId, childSet)

  return {
    state,
    isDirty,
    isSaving: mutation.isPending,
    error: mutation.error,
    update(next: TState) {
      setState(next)
      setIsDirty(true)
    },
    reset() {
      setState(initial)
      setIsDirty(false)
      mutation.reset()
    },
    save() {
      mutation.mutate(toPayload(state), { onSuccess: () => setIsDirty(false) })
    },
  }
}

function blank(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value)
}

function parseOptionalInt(value: string): number | null {
  const parsed = Number.parseInt(value, 10)
  return Number.isNaN(parsed) ? null : parsed
}

// ---------------------------------------------------------------
// 1. Complaints and personal history
// ---------------------------------------------------------------

interface ComplaintState {
  answer: TriStateAnswer
  description: string
}

export function ComplaintsSection({
  formId,
  isReadOnly,
  rows,
}: SectionProps & { rows: MedicalExamComplaintDto[] }) {
  const { t } = useTranslation()
  const existing = byKey(rows, (row) => row.complaintType)

  const initial = new Map<MedicalComplaintType, ComplaintState>(
    COMPLAINT_TYPES.map((type) => [
      type,
      {
        answer: existing.get(type)?.answer ?? TriStateAnswer.Unspecified,
        description: existing.get(type)?.description ?? '',
      },
    ]),
  )

  const editor = useChildSetEditor<
    Map<MedicalComplaintType, ComplaintState>,
    SaveMedicalExamComplaintDto
  >(formId, 'complaints', initial, (state) =>
    COMPLAINT_TYPES.flatMap((type) => {
      const row = state.get(type)!
      const isFilled = row.answer !== TriStateAnswer.Unspecified || row.description.trim() !== ''
      return isFilled
        ? [
            {
              complaintType: type,
              answer: row.answer,
              description: row.description.trim() || null,
            },
          ]
        : []
    }),
  )

  function patch(type: MedicalComplaintType, changes: Partial<ComplaintState>) {
    const next = new Map(editor.state)
    next.set(type, { ...next.get(type)!, ...changes })
    editor.update(next)
  }

  return (
    <SectionShell
      title={t('medicalExamination.sections.complaints.title')}
      description={t('medicalExamination.sections.complaints.description')}
      isReadOnly={isReadOnly}
      isDirty={editor.isDirty}
      isSaving={editor.isSaving}
      error={editor.error}
      onSave={editor.save}
      onReset={editor.reset}
    >
      <table
        className="table table-sm align-middle mb-0"
        aria-label={t('medicalExamination.sections.complaints.title')}
      >
        <thead>
          <tr>
            <th scope="col">{t('medicalExamination.sections.complaints.item')}</th>
            <th scope="col" style={{ width: 160 }}>
              {t('medicalExamination.sections.answer')}
            </th>
            <th scope="col">{t('medicalExamination.sections.note')}</th>
          </tr>
        </thead>
        <tbody>
          {COMPLAINT_TYPES.map((type) => {
            const row = editor.state.get(type)!
            return (
              <tr key={type}>
                <th scope="row" className="fw-normal">
                  {t(`enums.medicalComplaintType.${type}`)}
                </th>
                <td>
                  <CellSelect
                    id={`complaint-answer-${type}`}
                    label={t('medicalExamination.sections.answer')}
                    value={row.answer}
                    options={TRI_STATE_ANSWERS}
                    optionLabel={(option) => t(`enums.triStateAnswer.${option}`)}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { answer: next as TriStateAnswer })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`complaint-note-${type}`}
                    label={t('medicalExamination.sections.note')}
                    value={row.description}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { description: next })}
                  />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </SectionShell>
  )
}

// ---------------------------------------------------------------
// 2. Working conditions
// ---------------------------------------------------------------

export function WorkConditionsSection({
  formId,
  isReadOnly,
  rows,
}: SectionProps & { rows: MedicalExamWorkConditionDto[] }) {
  const { t } = useTranslation()
  const existing = byKey(rows, (row) => row.conditionType)

  const initial = new Map<WorkConditionType, TriStateAnswer>(
    WORK_CONDITION_TYPES.map((type) => [
      type,
      existing.get(type)?.suitable ?? TriStateAnswer.Unspecified,
    ]),
  )

  const editor = useChildSetEditor<
    Map<WorkConditionType, TriStateAnswer>,
    SaveMedicalExamWorkConditionDto
  >(formId, 'workConditions', initial, (state) =>
    WORK_CONDITION_TYPES.flatMap((type) => {
      const suitable = state.get(type)!
      return suitable === TriStateAnswer.Unspecified
        ? []
        : [{ conditionType: type, suitable }]
    }),
  )

  return (
    <SectionShell
      title={t('medicalExamination.sections.workConditions.title')}
      description={t('medicalExamination.sections.workConditions.description')}
      isReadOnly={isReadOnly}
      isDirty={editor.isDirty}
      isSaving={editor.isSaving}
      error={editor.error}
      onSave={editor.save}
      onReset={editor.reset}
    >
      <table
        className="table table-sm align-middle mb-0"
        aria-label={t('medicalExamination.sections.workConditions.title')}
      >
        <thead>
          <tr>
            <th scope="col">{t('medicalExamination.sections.workConditions.item')}</th>
            <th scope="col" style={{ width: 200 }}>
              {t('medicalExamination.sections.workConditions.suitable')}
            </th>
          </tr>
        </thead>
        <tbody>
          {WORK_CONDITION_TYPES.map((type) => (
            <tr key={type}>
              <th scope="row" className="fw-normal">
                {t(`enums.workConditionType.${type}`)}
              </th>
              <td>
                <CellSelect
                  id={`work-condition-${type}`}
                  label={t('medicalExamination.sections.workConditions.suitable')}
                  value={editor.state.get(type)!}
                  options={TRI_STATE_ANSWERS}
                  optionLabel={(option) => t(`enums.triStateAnswer.${option}`)}
                  disabled={isReadOnly}
                  onChange={(next) => {
                    const map = new Map(editor.state)
                    map.set(type, next as TriStateAnswer)
                    editor.update(map)
                  }}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </SectionShell>
  )
}

// ---------------------------------------------------------------
// 3. Habits
// ---------------------------------------------------------------

interface HabitState {
  status: HabitStatus
  dailyQuantity: string
  durationYear: string
  cessationYearBefore: string
  description: string
}

export function HabitsSection({
  formId,
  isReadOnly,
  rows,
}: SectionProps & { rows: MedicalExamHabitDto[] }) {
  const { t } = useTranslation()
  const existing = byKey(rows, (row) => row.habitType)

  const initial = new Map<HabitType, HabitState>(
    HABIT_TYPES.map((type) => {
      const row = existing.get(type)
      return [
        type,
        {
          status: row?.status ?? HabitStatus.Unspecified,
          dailyQuantity: blank(row?.dailyQuantity),
          durationYear: blank(row?.durationYear),
          cessationYearBefore: blank(row?.cessationYearBefore),
          description: row?.description ?? '',
        },
      ]
    }),
  )

  const editor = useChildSetEditor<Map<HabitType, HabitState>, SaveMedicalExamHabitDto>(
    formId,
    'habits',
    initial,
    (state) =>
      HABIT_TYPES.flatMap((type) => {
        const row = state.get(type)!
        if (row.status === HabitStatus.Unspecified) return []
        return [
          {
            habitType: type,
            status: row.status,
            dailyQuantity: parseOptionalInt(row.dailyQuantity),
            durationYear: parseOptionalInt(row.durationYear),
            cessationYearBefore: parseOptionalInt(row.cessationYearBefore),
            description: row.description.trim() || null,
          },
        ]
      }),
  )

  function patch(type: HabitType, changes: Partial<HabitState>) {
    const next = new Map(editor.state)
    next.set(type, { ...next.get(type)!, ...changes })
    editor.update(next)
  }

  return (
    <SectionShell
      title={t('medicalExamination.sections.habits.title')}
      description={t('medicalExamination.sections.habits.description')}
      isReadOnly={isReadOnly}
      isDirty={editor.isDirty}
      isSaving={editor.isSaving}
      error={editor.error}
      onSave={editor.save}
      onReset={editor.reset}
    >
      <table
        className="table table-sm align-middle mb-0"
        aria-label={t('medicalExamination.sections.habits.title')}
      >
        <thead>
          <tr>
            <th scope="col">{t('medicalExamination.sections.habits.item')}</th>
            <th scope="col" style={{ width: 180 }}>
              {t('medicalExamination.sections.habits.status')}
            </th>
            <th scope="col" style={{ width: 130 }}>
              {t('medicalExamination.sections.habits.dailyQuantity')}
            </th>
            <th scope="col" style={{ width: 130 }}>
              {t('medicalExamination.sections.habits.durationYear')}
            </th>
            <th scope="col" style={{ width: 150 }}>
              {t('medicalExamination.sections.habits.cessationYearBefore')}
            </th>
            <th scope="col">{t('medicalExamination.sections.note')}</th>
          </tr>
        </thead>
        <tbody>
          {HABIT_TYPES.map((type) => {
            const row = editor.state.get(type)!
            return (
              <tr key={type}>
                <th scope="row" className="fw-normal">
                  {t(`enums.habitType.${type}`)}
                </th>
                <td>
                  <CellSelect
                    id={`habit-status-${type}`}
                    label={t('medicalExamination.sections.habits.status')}
                    value={row.status}
                    options={HABIT_STATUSES}
                    optionLabel={(option) => t(`enums.habitStatus.${option}`)}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { status: next as HabitStatus })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`habit-quantity-${type}`}
                    label={t('medicalExamination.sections.habits.dailyQuantity')}
                    type="number"
                    value={row.dailyQuantity}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { dailyQuantity: next })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`habit-duration-${type}`}
                    label={t('medicalExamination.sections.habits.durationYear')}
                    type="number"
                    value={row.durationYear}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { durationYear: next })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`habit-cessation-${type}`}
                    label={t('medicalExamination.sections.habits.cessationYearBefore')}
                    type="number"
                    value={row.cessationYearBefore}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { cessationYearBefore: next })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`habit-note-${type}`}
                    label={t('medicalExamination.sections.note')}
                    value={row.description}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { description: next })}
                  />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </SectionShell>
  )
}

// ---------------------------------------------------------------
// 4. Physical examination findings
// ---------------------------------------------------------------

interface FindingState {
  finding: ExamFinding
  description: string
}

export function PhysicalFindingsSection({
  formId,
  isReadOnly,
  rows,
}: SectionProps & { rows: MedicalExamPhysicalFindingDto[] }) {
  const { t } = useTranslation()
  const existing = byKey(rows, (row) => row.system)

  const initial = new Map<PhysicalExamSystem, FindingState>(
    PHYSICAL_EXAM_SYSTEMS.map((system) => [
      system,
      {
        finding: existing.get(system)?.finding ?? ExamFinding.Unspecified,
        description: existing.get(system)?.description ?? '',
      },
    ]),
  )

  const editor = useChildSetEditor<
    Map<PhysicalExamSystem, FindingState>,
    SaveMedicalExamPhysicalFindingDto
  >(formId, 'physicalFindings', initial, (state) =>
    PHYSICAL_EXAM_SYSTEMS.flatMap((system) => {
      const row = state.get(system)!
      const isFilled = row.finding !== ExamFinding.Unspecified || row.description.trim() !== ''
      return isFilled
        ? [{ system, finding: row.finding, description: row.description.trim() || null }]
        : []
    }),
  )

  function patch(system: PhysicalExamSystem, changes: Partial<FindingState>) {
    const next = new Map(editor.state)
    next.set(system, { ...next.get(system)!, ...changes })
    editor.update(next)
  }

  return (
    <SectionShell
      title={t('medicalExamination.sections.physicalFindings.title')}
      description={t('medicalExamination.sections.physicalFindings.description')}
      isReadOnly={isReadOnly}
      isDirty={editor.isDirty}
      isSaving={editor.isSaving}
      error={editor.error}
      onSave={editor.save}
      onReset={editor.reset}
    >
      <table
        className="table table-sm align-middle mb-0"
        aria-label={t('medicalExamination.sections.physicalFindings.title')}
      >
        <thead>
          <tr>
            <th scope="col">{t('medicalExamination.sections.physicalFindings.item')}</th>
            <th scope="col" style={{ width: 180 }}>
              {t('medicalExamination.sections.physicalFindings.finding')}
            </th>
            <th scope="col">{t('medicalExamination.sections.note')}</th>
          </tr>
        </thead>
        <tbody>
          {PHYSICAL_EXAM_SYSTEMS.map((system) => {
            const row = editor.state.get(system)!
            return (
              <tr key={system}>
                <th scope="row" className="fw-normal">
                  {t(`enums.physicalExamSystem.${system}`)}
                </th>
                <td>
                  <CellSelect
                    id={`finding-${system}`}
                    label={t('medicalExamination.sections.physicalFindings.finding')}
                    value={row.finding}
                    options={EXAM_FINDINGS}
                    optionLabel={(option) => t(`enums.examFinding.${option}`)}
                    disabled={isReadOnly}
                    onChange={(next) => patch(system, { finding: next as ExamFinding })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`finding-note-${system}`}
                    label={t('medicalExamination.sections.note')}
                    value={row.description}
                    disabled={isReadOnly}
                    onChange={(next) => patch(system, { description: next })}
                  />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </SectionShell>
  )
}

// ---------------------------------------------------------------
// 5. Laboratory and diagnostic tests
// ---------------------------------------------------------------

interface LabTestState {
  isCompleted: boolean
  result: string
  date: string
}

export function LabTestsSection({
  formId,
  isReadOnly,
  rows,
}: SectionProps & { rows: MedicalExamLabTestDto[] }) {
  const { t } = useTranslation()
  const existing = byKey(rows, (row) => row.labTestType)

  const initial = new Map<LabTestType, LabTestState>(
    LAB_TEST_TYPES.map((type) => {
      const row = existing.get(type)
      return [
        type,
        {
          isCompleted: row?.isCompleted ?? false,
          result: row?.result ?? '',
          date: row?.date ? row.date.slice(0, 10) : '',
        },
      ]
    }),
  )

  const editor = useChildSetEditor<Map<LabTestType, LabTestState>, SaveMedicalExamLabTestDto>(
    formId,
    'labTests',
    initial,
    (state) =>
      LAB_TEST_TYPES.flatMap((type) => {
        const row = state.get(type)!
        const isFilled = row.isCompleted || row.result.trim() !== '' || row.date !== ''
        return isFilled
          ? [
              {
                labTestType: type,
                isCompleted: row.isCompleted,
                result: row.result.trim() || null,
                date: row.date || null,
              },
            ]
          : []
      }),
  )

  function patch(type: LabTestType, changes: Partial<LabTestState>) {
    const next = new Map(editor.state)
    next.set(type, { ...next.get(type)!, ...changes })
    editor.update(next)
  }

  return (
    <SectionShell
      title={t('medicalExamination.sections.labTests.title')}
      description={t('medicalExamination.sections.labTests.description')}
      isReadOnly={isReadOnly}
      isDirty={editor.isDirty}
      isSaving={editor.isSaving}
      error={editor.error}
      onSave={editor.save}
      onReset={editor.reset}
    >
      <table
        className="table table-sm align-middle mb-0"
        aria-label={t('medicalExamination.sections.labTests.title')}
      >
        <thead>
          <tr>
            <th scope="col">{t('medicalExamination.sections.labTests.item')}</th>
            <th scope="col" style={{ width: 120 }}>
              {t('medicalExamination.sections.labTests.isCompleted')}
            </th>
            <th scope="col" style={{ width: 170 }}>
              {t('medicalExamination.sections.labTests.date')}
            </th>
            <th scope="col">{t('medicalExamination.sections.labTests.result')}</th>
          </tr>
        </thead>
        <tbody>
          {LAB_TEST_TYPES.map((type) => {
            const row = editor.state.get(type)!
            return (
              <tr key={type}>
                <th scope="row" className="fw-normal">
                  {t(`enums.labTestType.${type}`)}
                </th>
                <td>
                  <div className="form-check mb-0">
                    <input
                      id={`lab-completed-${type}`}
                      className="form-check-input"
                      type="checkbox"
                      checked={row.isCompleted}
                      disabled={isReadOnly}
                      onChange={(event) => patch(type, { isCompleted: event.target.checked })}
                    />
                    <label className="form-check-label visually-hidden" htmlFor={`lab-completed-${type}`}>
                      {t('medicalExamination.sections.labTests.isCompleted')}
                    </label>
                  </div>
                </td>
                <td>
                  <CellInput
                    id={`lab-date-${type}`}
                    label={t('medicalExamination.sections.labTests.date')}
                    type="date"
                    value={row.date}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { date: next })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`lab-result-${type}`}
                    label={t('medicalExamination.sections.labTests.result')}
                    value={row.result}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { result: next })}
                  />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </SectionShell>
  )
}

// ---------------------------------------------------------------
// 6. Immunisations
// ---------------------------------------------------------------

interface ImmunizationState {
  date: string
  description: string
}

export function ImmunizationsSection({
  formId,
  isReadOnly,
  rows,
}: SectionProps & { rows: MedicalExamImmunizationDto[] }) {
  const { t } = useTranslation()
  const existing = byKey(rows, (row) => row.immunizationType)

  const initial = new Map<ImmunizationType, ImmunizationState>(
    IMMUNIZATION_TYPES.map((type) => {
      const row = existing.get(type)
      return [
        type,
        {
          date: row?.date ? row.date.slice(0, 10) : '',
          description: row?.description ?? '',
        },
      ]
    }),
  )

  const editor = useChildSetEditor<
    Map<ImmunizationType, ImmunizationState>,
    SaveMedicalExamImmunizationDto
  >(formId, 'immunizations', initial, (state) =>
    IMMUNIZATION_TYPES.flatMap((type) => {
      const row = state.get(type)!
      const isFilled = row.date !== '' || row.description.trim() !== ''
      return isFilled
        ? [
            {
              immunizationType: type,
              date: row.date || null,
              description: row.description.trim() || null,
            },
          ]
        : []
    }),
  )

  function patch(type: ImmunizationType, changes: Partial<ImmunizationState>) {
    const next = new Map(editor.state)
    next.set(type, { ...next.get(type)!, ...changes })
    editor.update(next)
  }

  return (
    <SectionShell
      title={t('medicalExamination.sections.immunizations.title')}
      description={t('medicalExamination.sections.immunizations.description')}
      isReadOnly={isReadOnly}
      isDirty={editor.isDirty}
      isSaving={editor.isSaving}
      error={editor.error}
      onSave={editor.save}
      onReset={editor.reset}
    >
      <table
        className="table table-sm align-middle mb-0"
        aria-label={t('medicalExamination.sections.immunizations.title')}
      >
        <thead>
          <tr>
            <th scope="col">{t('medicalExamination.sections.immunizations.item')}</th>
            <th scope="col" style={{ width: 170 }}>
              {t('medicalExamination.sections.immunizations.date')}
            </th>
            <th scope="col">{t('medicalExamination.sections.note')}</th>
          </tr>
        </thead>
        <tbody>
          {IMMUNIZATION_TYPES.map((type) => {
            const row = editor.state.get(type)!
            return (
              <tr key={type}>
                <th scope="row" className="fw-normal">
                  {t(`enums.immunizationType.${type}`)}
                </th>
                <td>
                  <CellInput
                    id={`immunization-date-${type}`}
                    label={t('medicalExamination.sections.immunizations.date')}
                    type="date"
                    value={row.date}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { date: next })}
                  />
                </td>
                <td>
                  <CellInput
                    id={`immunization-note-${type}`}
                    label={t('medicalExamination.sections.note')}
                    value={row.description}
                    disabled={isReadOnly}
                    onChange={(next) => patch(type, { description: next })}
                  />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </SectionShell>
  )
}
