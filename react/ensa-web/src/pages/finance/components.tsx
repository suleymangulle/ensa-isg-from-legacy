import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import type { LookupDto } from '@/api/endpoints'
import { CashTransactionType, InvoiceType } from '@/api/enums'
import { Field, controlClass } from '@/components/Form'
import { formatMoney } from '@/utils/format'

/**
 * Presentation helpers shared by the finance screens.
 *
 * Only badge colour maps live in code, because they are styling; every label is resolved from
 * the module locale bundle by its numeric enum value, as `MODULES.md` rule 2 requires. Colours
 * are Metronic CSS variables and Bootstrap utility classes throughout — no new hex codes.
 */

/** Sale reads as money in, purchase and the two return types as money out. */
export const INVOICE_TYPE_BADGE: Record<InvoiceType, string> = {
  [InvoiceType.Sale]: 'badge-light-success',
  [InvoiceType.Purchase]: 'badge-light-info',
  [InvoiceType.SaleReturn]: 'badge-light-warning',
  [InvoiceType.PurchaseReturn]: 'badge-light-warning',
}

/** Inflow green, outflow red, carry-over neutral — the direction has to read at a glance. */
export const CASH_TRANSACTION_TYPE_BADGE: Record<CashTransactionType, string> = {
  [CashTransactionType.Inflow]: 'badge-light-success',
  [CashTransactionType.Outflow]: 'badge-light-danger',
  [CashTransactionType.CarryOver]: 'badge-light-primary',
}

/** Text colour for a signed amount: inflow green, outflow red. */
export function cashDirectionColor(type: CashTransactionType): string {
  if (type === CashTransactionType.Outflow) return 'var(--kt-danger)'
  if (type === CashTransactionType.Inflow) return 'var(--kt-success)'
  return 'var(--kt-gray-700)'
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

/** Today as `yyyy-MM-dd`, the default value of a new record's date field. */
export function todayInput(): string {
  return toDateInput(new Date().toISOString())
}

/** Calendar year of a `yyyy-MM-dd` input value, used when asking for an invoice number. */
export function yearOf(dateInput: string): number {
  const year = Number(dateInput.slice(0, 4))
  return Number.isFinite(year) && year > 1900 ? year : new Date().getFullYear()
}

/**
 * Parses a decimal typed into a number input.
 *
 * This is input parsing, not arithmetic on money: the value goes straight to the API, which
 * computes every figure the user reads as authoritative.
 */
export function parseDecimal(value: string): number {
  const parsed = Number(value.replace(',', '.'))
  return Number.isFinite(parsed) ? parsed : 0
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
 * A headline figure produced by the server — a total, a VAT total, a balance, a fine exposure.
 *
 * Amounts are rendered through `formatMoney`, and the currency is named once here rather than
 * repeated on every table row.
 */
export function MoneyStat({
  label,
  value,
  currency,
  tone = 'gray',
  emphasis,
}: {
  label: string
  value: number | null | undefined
  currency: string
  tone?: 'gray' | 'primary' | 'success' | 'danger' | 'warning'
  emphasis?: boolean
}) {
  const { t } = useTranslation()
  const color = tone === 'gray' ? 'var(--kt-gray-900)' : `var(--kt-${tone})`

  return (
    <div
      className="px-4 py-3 rounded"
      style={{ backgroundColor: tone === 'gray' ? 'var(--kt-gray-100)' : `var(--kt-${tone}-light)` }}
    >
      <div style={{ color: 'var(--kt-gray-600)', fontSize: '0.8125rem' }}>{label}</div>
      <div
        className={emphasis ? 'fw-bold' : 'fw-semibold'}
        style={{ color, fontSize: emphasis ? '1.5rem' : '1.125rem' }}
      >
        {formatMoney(value) ?? t('common.none')}{' '}
        <span style={{ fontSize: '0.75em', fontWeight: 500 }}>{currency}</span>
      </div>
    </div>
  )
}

/** Right-aligned money cell. The currency lives in the column header, not on the row. */
export function MoneyCell({
  value,
  color,
  bold,
}: {
  value: number | null | undefined
  color?: string
  bold?: boolean
}) {
  const { t } = useTranslation()
  return (
    <span
      className={bold ? 'fw-semibold' : undefined}
      style={{ color, fontVariantNumeric: 'tabular-nums' }}
    >
      {formatMoney(value) ?? t('common.none')}
    </span>
  )
}

/** Edit and delete buttons of a table row; icon-only, so both carry an `aria-label`. */
export function RowActions({
  editLabel,
  deleteLabel,
  onEdit,
  onDelete,
  disabled,
}: {
  editLabel: string
  deleteLabel: string
  onEdit?: () => void
  onDelete?: () => void
  disabled?: boolean
}) {
  return (
    <div className="d-flex justify-content-end gap-1">
      {onEdit && (
        <button
          type="button"
          className="btn btn-sm btn-light-primary"
          aria-label={editLabel}
          title={editLabel}
          disabled={disabled}
          onClick={onEdit}
        >
          <span aria-hidden="true">✎</span>
        </button>
      )}
      {onDelete && (
        <button
          type="button"
          className="btn btn-sm btn-light-danger"
          aria-label={deleteLabel}
          title={deleteLabel}
          disabled={disabled}
          onClick={onDelete}
        >
          <span aria-hidden="true">🗑</span>
        </button>
      )}
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
    <Field
      label={label}
      htmlFor={id}
      required={required}
      error={error}
      hint={hint}
      className={className}
    >
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
  /** For example `enums.invoiceType`; the numeric value is appended. */
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
        onChange={(event) =>
          onChange(event.target.value === '' ? undefined : Number(event.target.value))
        }
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
  width = 180,
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

/** Toolbar date filter; same compact treatment as `FilterSelect`. */
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
    <div style={{ minWidth: 160 }}>
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
      <input
        id={id}
        type="date"
        className="form-control"
        value={value}
        aria-label={label}
        title={label}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  )
}

/** Breadcrumb above a detail page. */
export function Breadcrumb({
  items,
  current,
}: {
  items: { label: string; to: string }[]
  current: string
}) {
  const { t } = useTranslation()

  return (
    <nav aria-label={t('nav.breadcrumb')} className="mb-3">
      <ol className="breadcrumb mb-0" style={{ fontSize: '0.875rem' }}>
        {items.map((item) => (
          <li className="breadcrumb-item" key={item.to}>
            <Link to={item.to} className="text-decoration-none">
              {item.label}
            </Link>
          </li>
        ))}
        <li className="breadcrumb-item active" aria-current="page">
          {current}
        </li>
      </ol>
    </nav>
  )
}
