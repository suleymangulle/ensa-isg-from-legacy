import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Alert,
  Button,
  Card,
  Input,
  NumberInput,
  ProgressBar,
  Select,
  TextArea,
  type BadgeVariant,
  type SelectOption,
} from 'rich-react-component'
import type { LookupDto } from '@/api/endpoints'
import { AssignmentType, HazardClass, StaffRole } from '@/api/enums'
import { formatNumber } from '@/utils/format'

/**
 * Presentation helpers shared by the three reporting screens.
 *
 * Only the badge colour maps live in code — every label is resolved from the module locale
 * bundle by its numeric enum value, as `MODULES.md` rule 2 requires.
 */

// ---------------------------------------------------------------
// Badge colours
// ---------------------------------------------------------------

/**
 * Hazard-class colours, repeated here rather than imported from `@/api/endpoints` because the
 * breakdown summary needs the *solid* variants for its progress bars, not the light badges.
 *
 * Feeds `<ProgressBar variant>`, so the palette is a `BadgeVariant` rather than a raw CSS colour;
 * `Unspecified` has no dedicated Metronic hue, so it falls back to `secondary`.
 */
export const HAZARD_CLASS_BAR: Record<HazardClass, BadgeVariant> = {
  [HazardClass.Unspecified]: 'secondary',
  [HazardClass.LowHazard]: 'success',
  [HazardClass.Hazardous]: 'warning',
  [HazardClass.VeryHazardous]: 'danger',
}

export const ASSIGNMENT_TYPE_BADGE: Record<AssignmentType, BadgeVariant> = {
  [AssignmentType.Unspecified]: 'primary',
  [AssignmentType.InboundAssignment]: 'info',
  [AssignmentType.OutboundAssignment]: 'warning',
}

export const STAFF_ROLE_BADGE: Record<StaffRole, BadgeVariant> = {
  [StaffRole.Unspecified]: 'primary',
  [StaffRole.OccupationalSafetySpecialist]: 'info',
  [StaffRole.WorkplacePhysician]: 'success',
  [StaffRole.OtherHealthPersonnel]: 'warning',
  [StaffRole.OfficeStaff]: 'primary',
  [StaffRole.Customer]: 'primary',
  [StaffRole.OfficeAdministrator]: 'primary',
  [StaffRole.OrganizationAdministrator]: 'primary',
  [StaffRole.SystemAdministrator]: 'primary',
}

// ---------------------------------------------------------------
// Value helpers
// ---------------------------------------------------------------

/** Numeric values of an enum object, ready to feed a `<select>`. */
export function enumValues(source: Record<string, string | number>): number[] {
  return Object.values(source).filter((value): value is number => typeof value === 'number')
}

/** Percentage of a total, clamped to 0–100; returns 0 when the total is zero. */
export function percentOf(value: number, total: number): number {
  if (!total) return 0
  return Math.min(100, Math.max(0, Math.round((value / total) * 100)))
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

/** `yyyy-MM-dd` back to the payload value; an empty input becomes `null`. */
export function fromDateInput(value: string): string | null {
  return value ? value : null
}

// ---------------------------------------------------------------
// Layout pieces
// ---------------------------------------------------------------

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
 * Neutral panel telling the user which filter still has to be chosen before a request fires.
 *
 * `Alert` only has an assertive `role="alert"`, unlike the polite `role="status"` this used to
 * carry — an accepted trade-off of routing every message box through the one library component.
 */
export function GateHint({ message }: { message: string }) {
  return (
    <Alert variant="info" className="mb-0">
      {message}
    </Alert>
  )
}

/** Card of one headline figure. The value is plain text, so a screen reader reads it as such. */
export function SummaryCard({
  label,
  value,
  hint,
  tone = 'primary',
  icon,
}: {
  label: string
  value: string
  hint?: string
  tone?: 'primary' | 'success' | 'warning' | 'danger' | 'info'
  icon: string
}) {
  return (
    <Card className="h-100">
      <div className="d-flex align-items-center gap-3">
        <span
          className="d-inline-flex align-items-center justify-content-center flex-shrink-0"
          style={{
            width: 48,
            height: 48,
            borderRadius: 12,
            fontSize: 20,
            backgroundColor: `var(--kt-${tone}-light)`,
            color: `var(--kt-${tone})`,
          }}
          aria-hidden="true"
        >
          {icon}
        </span>
        <div className="min-w-0">
          <div
            className="fw-bold"
            style={{ fontSize: '1.5rem', color: 'var(--kt-gray-900)', lineHeight: 1.2 }}
          >
            {value}
          </div>
          <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>{label}</div>
          {hint && (
            <div style={{ color: 'var(--kt-gray-400)', fontSize: '0.8125rem' }}>{hint}</div>
          )}
        </div>
      </div>
    </Card>
  )
}

/**
 * One row of a distribution summary: a label, a progress bar and the figure itself.
 *
 * The count and the share are already in the text next to the bar, so the bar itself adds no
 * information a screen reader needs — but `ProgressBar` still reports it via `role="progressbar"`
 * plus `aria-valuenow`/`-valuemin`/`-valuemax`, which the hand-rolled `.progress` markup this used
 * to render never did.
 */
export function DistributionRow({
  label,
  value,
  total,
  variant,
  shareLabel,
}: {
  label: string
  value: number
  total: number
  variant: BadgeVariant
  /** Already translated share text, e.g. "%42". */
  shareLabel: string
}) {
  const percent = percentOf(value, total)

  return (
    <div className="mb-3">
      <div className="d-flex align-items-center justify-content-between gap-2 mb-1">
        <span className="fw-semibold" style={{ color: 'var(--kt-gray-800)' }}>
          {label}
        </span>
        <span style={{ color: 'var(--kt-gray-600)', fontSize: '0.875rem' }}>
          {formatNumber(value)} · {shareLabel}
        </span>
      </div>
      <ProgressBar value={percent} variant={variant} />
    </div>
  )
}

/**
 * The two facts a statutory report is filed under: which workplace, and for which period.
 *
 * They are pulled out of the detail list and given their own banner because a report whose
 * workplace or reporting period is ambiguous is not a usable statutory document — on screen or
 * on the printout, where this block is the first thing under the title.
 */
export function ReportPeriodBanner({
  companyLabel,
  companyName,
  periodLabel,
  periodValue,
  extraLabel,
  extraValue,
}: {
  companyLabel: string
  companyName: string
  periodLabel: string
  periodValue: string
  extraLabel?: string
  extraValue?: string
}) {
  return (
    // `Card` takes no inline `style`, so the accent bar is rebuilt from Bootstrap's border
    // utilities instead of the original `borderLeft: '4px solid var(--kt-primary)'`: only the
    // start side keeps a border, `border-4` widens it, `border-primary` colours it — and since
    // `$primary` is Metronic's blue (see `metronic.scss`), `border-primary` renders the same hex
    // the inline style did.
    <Card className="mb-4 border-top-0 border-end-0 border-bottom-0 border-start border-4 border-primary">
      <div className="row g-3">
        <div className="col-12 col-md">
          <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>{companyLabel}</div>
          <div
            className="fw-bold"
            style={{ color: 'var(--kt-gray-900)', fontSize: '1.125rem', lineHeight: 1.3 }}
          >
            {companyName}
          </div>
        </div>
        <div className="col-12 col-md-auto">
          <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>{periodLabel}</div>
          <div
            className="fw-bold"
            style={{ color: 'var(--kt-gray-900)', fontSize: '1.125rem', lineHeight: 1.3 }}
          >
            {periodValue}
          </div>
        </div>
        {extraLabel && (
          <div className="col-12 col-md-auto">
            <div style={{ color: 'var(--kt-gray-500)', fontSize: '0.8125rem' }}>{extraLabel}</div>
            <div
              className="fw-bold"
              style={{ color: 'var(--kt-gray-900)', fontSize: '1.125rem', lineHeight: 1.3 }}
            >
              {extraValue}
            </div>
          </div>
        )}
      </div>
    </Card>
  )
}

/** Edit and delete buttons of a table row; icon-only, so both carry an `aria-label`. */
export function RowActions({
  editLabel,
  deleteLabel,
  onEdit,
  onDelete,
  extra,
}: {
  editLabel: string
  deleteLabel: string
  onEdit: () => void
  onDelete: () => void
  extra?: ReactNode
}) {
  return (
    <div className="d-flex justify-content-end gap-1 d-print-none">
      {extra}
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

/** Button that hands the page to the browser's print dialog. Hidden in the printout itself. */
export function PrintButton() {
  const { t } = useTranslation()

  return (
    <Button variant="light" className="d-print-none"
      onClick={() => window.print()}
    >
      <span aria-hidden="true" className="me-2">
        ⎙
      </span>
      {t('reports.common.print')}
    </Button>
  )
}

/**
 * Print rules for the reporting screens.
 *
 * The legacy reports were printed on paper and filed, so the modern screens have to print
 * cleanly too. No dependency is involved: the chrome (`.d-print-none` on the toolbar, the
 * sidebar, the row actions) is dropped by Bootstrap's own utilities and this block flattens
 * the cards, forces black-on-white text and keeps tables from splitting across pages.
 */
export function ReportPrintStyles() {
  return (
    <style>{`
      @media print {
        .report-print .card {
          border: 1px solid #000 !important;
          box-shadow: none !important;
          break-inside: avoid;
        }
        .report-print .card-header {
          background: transparent !important;
        }
        .report-print table {
          break-inside: auto;
          width: 100%;
        }
        .report-print tr {
          break-inside: avoid;
        }
        .report-print .table-responsive {
          overflow: visible !important;
        }
        .report-print .progress {
          border: 1px solid #000;
        }
        .report-print .report-print-heading {
          break-after: avoid;
        }
      }
    `}</style>
  )
}

// ---------------------------------------------------------------
// Form controls
// ---------------------------------------------------------------

/** A `<select>` bound to a lookup list. */
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
  /** For example `enums.activityReportType`; the numeric value is appended. */
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

/** A text, number or multi-line field, resolved to the matching library field component. */
export function TextField({
  id,
  label,
  value,
  onChange,
  type = 'text',
  required,
  error,
  hint,
  className,
  placeholder,
  rows,
  min,
  max,
}: {
  id: string
  label: string
  value: string
  onChange: (next: string) => void
  type?: 'text' | 'number' | 'date'
  required?: boolean
  error?: string
  hint?: string
  className?: string
  placeholder?: string
  /** Renders a `<textarea>` instead of an `<input>` when set. */
  rows?: number
  min?: number
  max?: number
}) {
  if (rows) {
    return (
      <TextArea
        id={id}
        rows={rows}
        label={label}
        required={required}
        error={error}
        helpText={hint}
        className={className}
        placeholder={placeholder}
        value={value}
        onChange={onChange}
      />
    )
  }

  if (type === 'number') {
    return (
      <NumberInput
        id={id}
        label={label}
        required={required}
        error={error}
        helpText={hint}
        className={className}
        placeholder={placeholder}
        min={min}
        max={max}
        value={value === '' ? null : Number(value)}
        onChange={(next) => onChange(next === null ? '' : String(next))}
      />
    )
  }

  return (
    <Input
      id={id}
      label={label}
      required={required}
      error={error}
      helpText={hint}
      className={className}
      placeholder={placeholder}
      value={value}
      onChange={onChange}
      inputProps={type === 'date' ? { type: 'date' } : undefined}
    />
  )
}

// ---------------------------------------------------------------
// Toolbar filters
// ---------------------------------------------------------------

/**
 * Toolbar filter select; compact, so it carries a `visually-hidden` label rather than the
 * library's own (visible) `label` prop. Values are kept as strings the same way the raw
 * `<select>` this replaces did, so an empty string still means "no filter chosen".
 */
export function FilterSelect({
  id,
  label,
  value,
  onChange,
  options,
  placeholder,
  width = 190,
}: {
  id: string
  label: string
  value: string
  onChange: (next: string) => void
  options: SelectOption<string>[]
  placeholder: string
  width?: number
}) {
  return (
    <div style={{ minWidth: width }}>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <Select
        id={id}
        options={options}
        placeholder={placeholder}
        value={value === '' ? null : value}
        onChange={(next) => onChange(next ?? '')}
      />
    </div>
  )
}

/** Toolbar date input. The label is visible, because an unlabelled date box is unreadable. */
export function FilterDate({
  id,
  label,
  value,
  onChange,
}: {
  id: string
  label: string
  value: string
  onChange: (next: string) => void
}) {
  return (
    <div className="d-flex align-items-center gap-2">
      <label
        htmlFor={id}
        className="form-label mb-0 text-nowrap"
        style={{ color: 'var(--kt-gray-600)', fontSize: '0.875rem' }}
      >
        {label}
      </label>
      <div style={{ minWidth: 150 }}>
        <Input id={id} inputProps={{ type: 'date' }} value={value} onChange={onChange} />
      </div>
    </div>
  )
}
