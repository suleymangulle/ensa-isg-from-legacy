import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Button, Select, type BadgeVariant } from 'rich-react-component'
import type { LookupDto } from '@/api/endpoints'
import { CorrectiveActionStatus, IncidentType, RiskCategory } from '@/api/enums'

/**
 * Presentation helpers shared by the three screens of this module.
 *
 * Only the badge colour maps live in code — every label is resolved from the module locale
 * bundle by its numeric enum value, as `MODULES.md` rule 2 requires.
 */

export const INCIDENT_TYPE_BADGE: Record<IncidentType, BadgeVariant> = {
  [IncidentType.WorkAccident]: 'danger',
  [IncidentType.NearMiss]: 'warning',
  [IncidentType.OccupationalDisease]: 'info',
  [IncidentType.NoInjuryIncident]: 'primary',
}

export const RISK_CATEGORY_BADGE: Record<RiskCategory, BadgeVariant> = {
  [RiskCategory.Unspecified]: 'primary',
  [RiskCategory.WorkAccidentRisk]: 'danger',
  [RiskCategory.OccupationalDiseaseRisk]: 'warning',
  [RiskCategory.EnvironmentalRisk]: 'success',
  [RiskCategory.FireRisk]: 'info',
}

export const CORRECTIVE_ACTION_STATUS_BADGE: Record<CorrectiveActionStatus, BadgeVariant> = {
  [CorrectiveActionStatus.InProgress]: 'warning',
  [CorrectiveActionStatus.Closed]: 'success',
  [CorrectiveActionStatus.Cancelled]: 'primary',
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
      <Button variant="light" size="sm" 
        aria-label={editLabel}
        title={editLabel}
        onClick={onEdit}
      >
        <span aria-hidden="true">✎</span>
      </Button>
      <Button variant="light" size="sm" 
        aria-label={deleteLabel}
        title={deleteLabel}
        onClick={onDelete}
      >
        <span aria-hidden="true">🗑</span>
      </Button>
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
    <Select<number>
      id={id}
      label={label}
      required={required}
      error={error}
      helpText={hint}
      className={className}
      disabled={disabled || isLoading}
      placeholder={isLoading ? t('common.loading') : placeholder}
      options={items?.map((item) => ({ value: item.id, label: item.displayName })) ?? []}
      value={value ?? null}
      onChange={(next) => onChange(next ?? undefined)}
    />
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
    <Select<number>
      id={id}
      label={label}
      required={required}
      error={error}
      className={className}
      disabled={disabled}
      placeholder={placeholder}
      options={values.map((item) => ({ value: item, label: t(`${translationPrefix}.${item}`) }))}
      value={value ?? null}
      onChange={(next) => onChange(next ?? undefined)}
    />
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
