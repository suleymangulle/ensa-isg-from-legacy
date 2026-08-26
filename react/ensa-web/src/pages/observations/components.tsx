import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type { LookupDto } from '@/api/endpoints'
import { CorrectiveActionStatus, IncidentType, RiskCategory } from '@/api/enums'
import { Field, controlClass } from '@/components/Form'

/**
 * Presentation helpers shared by the three screens of this module.
 *
 * Only the badge colour maps live in code — every label is resolved from the module locale
 * bundle by its numeric enum value, as `MODULES.md` rule 2 requires.
 */

export const INCIDENT_TYPE_BADGE: Record<IncidentType, string> = {
  [IncidentType.WorkAccident]: 'badge-light-danger',
  [IncidentType.NearMiss]: 'badge-light-warning',
  [IncidentType.OccupationalDisease]: 'badge-light-info',
  [IncidentType.NoInjuryIncident]: 'badge-light-primary',
}

export const RISK_CATEGORY_BADGE: Record<RiskCategory, string> = {
  [RiskCategory.Unspecified]: 'badge-light-primary',
  [RiskCategory.WorkAccidentRisk]: 'badge-light-danger',
  [RiskCategory.OccupationalDiseaseRisk]: 'badge-light-warning',
  [RiskCategory.EnvironmentalRisk]: 'badge-light-success',
  [RiskCategory.FireRisk]: 'badge-light-info',
}

export const CORRECTIVE_ACTION_STATUS_BADGE: Record<CorrectiveActionStatus, string> = {
  [CorrectiveActionStatus.InProgress]: 'badge-light-warning',
  [CorrectiveActionStatus.Closed]: 'badge-light-success',
  [CorrectiveActionStatus.Cancelled]: 'badge-light-primary',
}

/** Numeric values of an enum object, ready to feed a `<select>`. */
export function enumValues(source: Record<string, string | number>): number[] {
  return Object.values(source).filter((value): value is number => typeof value === 'number')
}

/** Trims an ISO timestamp down to the `yyyy-MM-dd` an `<input type="date">` accepts. */
export function toDateInput(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/** `yyyy-MM-dd` back to the payload value; empty input becomes `null`. */
export function fromDateInput(value: string): string | null {
  return value ? value : null
}

/** One `<dt>` / `<dd>` pair of a detail definition list. */
export function Term({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="col-sm-4 col-lg-3" style={{ color: 'var(--kt-gray-500)', fontWeight: 500 }}>
        {label}
      </dt>
      <dd className="col-sm-8 col-lg-9">{children}</dd>
    </>
  )
}

/** Muted placeholder shown where a collection is empty. */
export function EmptyHint({ message }: { message: string }) {
  return (
    <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
      {message}
    </p>
  )
}

/**
 * Attention banner used for overdue deadlines and pending statutory notifications.
 *
 * The tone drives the tinted background and the accent bar, not the text colour: `--kt-warning`
 * on `--kt-warning-light` is far below the contrast floor, so the copy always uses the dark
 * grey and stays readable in all three tones.
 */
export function AlertPanel({
  tone,
  children,
}: {
  tone: 'danger' | 'warning' | 'info'
  children: ReactNode
}) {
  return (
    <div
      className="alert d-flex flex-wrap align-items-center justify-content-between gap-3"
      style={{
        backgroundColor: `var(--kt-${tone}-light)`,
        color: 'var(--kt-gray-800)',
        // Set here rather than with `border-0`, whose `!important` would beat the accent bar.
        border: '0 solid transparent',
        borderInlineStartWidth: 4,
        borderInlineStartColor: `var(--kt-${tone})`,
      }}
      role="status"
    >
      {children}
    </div>
  )
}

/** Edit and delete buttons of a table row; icon-only, so both carry an `aria-label`. */
export function RowActions({
  editLabel,
  deleteLabel,
  onEdit,
  onDelete,
}: {
  editLabel: string
  deleteLabel: string
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <div className="d-flex justify-content-end gap-1">
      <button
        type="button"
        className="btn btn-sm btn-light-primary"
        aria-label={editLabel}
        title={editLabel}
        onClick={onEdit}
      >
        <span aria-hidden="true">✎</span>
      </button>
      <button
        type="button"
        className="btn btn-sm btn-light-danger"
        aria-label={deleteLabel}
        title={deleteLabel}
        onClick={onDelete}
      >
        <span aria-hidden="true">🗑</span>
      </button>
    </div>
  )
}

/** A `<select>` bound to a lookup list, wrapped in the shared `Field`. */
export function LookupField({
  id,
  label,
  value,
  onChange,
  items,
  isLoading,
  placeholder,
  required,
  error,
  hint,
  disabled,
  className,
}: {
  id: string
  label: string
  value: number | undefined
  onChange: (next: number | undefined) => void
  items: LookupDto[] | undefined
  isLoading?: boolean
  placeholder: string
  required?: boolean
  error?: string
  hint?: string
  disabled?: boolean
  className?: string
}) {
  const { t } = useTranslation()

  return (
    <Field label={label} htmlFor={id} required={required} error={error} hint={hint} className={className}>
      <select
        id={id}
        className={controlClass('form-select', error)}
        value={value ?? ''}
        disabled={disabled || isLoading}
        aria-invalid={error ? true : undefined}
        onChange={(event) => onChange(event.target.value ? Number(event.target.value) : undefined)}
      >
        <option value="">{isLoading ? t('common.loading') : placeholder}</option>
        {items?.map((item) => (
          <option key={item.id} value={item.id}>
            {item.displayName}
          </option>
        ))}
      </select>
    </Field>
  )
}

/** A `<select>` over the numeric values of a backend enum. */
export function EnumField({
  id,
  label,
  value,
  onChange,
  values,
  translationPrefix,
  placeholder,
  required,
  error,
  className,
  disabled,
}: {
  id: string
  label: string
  value: number | undefined
  onChange: (next: number | undefined) => void
  values: number[]
  /** For example `enums.incidentType`; the numeric value is appended. */
  translationPrefix: string
  placeholder?: string
  required?: boolean
  error?: string
  className?: string
  disabled?: boolean
}) {
  const { t } = useTranslation()

  return (
    <Field label={label} htmlFor={id} required={required} error={error} className={className}>
      <select
        id={id}
        className={controlClass('form-select', error)}
        value={value ?? ''}
        disabled={disabled}
        aria-invalid={error ? true : undefined}
        onChange={(event) => onChange(event.target.value === '' ? undefined : Number(event.target.value))}
      >
        {placeholder !== undefined && <option value="">{placeholder}</option>}
        {values.map((item) => (
          <option key={item} value={item}>
            {t(`${translationPrefix}.${item}`)}
          </option>
        ))}
      </select>
    </Field>
  )
}

/** Toolbar filter select; compact, so it carries a `visually-hidden` label rather than a `Field`. */
export function FilterSelect({
  id,
  label,
  value,
  onChange,
  children,
  width = 190,
}: {
  id: string
  label: string
  value: string
  onChange: (next: string) => void
  children: ReactNode
  width?: number
}) {
  return (
    <div style={{ minWidth: width }}>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <select
        id={id}
        className="form-select"
        value={value}
        aria-label={label}
        onChange={(event) => onChange(event.target.value)}
      >
        {children}
      </select>
    </div>
  )
}
