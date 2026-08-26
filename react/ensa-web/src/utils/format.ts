import { formatLocale } from '@/i18n'

/**
 * Formats an ISO date coming from the API in the active language.
 * Returns `null` for missing or unparsable values so the caller can fall back
 * to its own placeholder (`common.none`).
 */
export function formatDate(value: string | null | undefined): string | null {
  if (!value) return null
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return null
  return date.toLocaleDateString(formatLocale())
}

/** Whole days between now and the given date; negative when the date has passed. */
export function daysUntil(value: string): number {
  const target = new Date(value).getTime()
  if (Number.isNaN(target)) return 0
  return Math.ceil((target - Date.now()) / 86_400_000)
}

/** First letters of the first two words, used for the avatar badge. */
export function initials(fullName: string): string {
  return fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toLocaleUpperCase(formatLocale()) ?? '')
    .join('')
}

/**
 * Formats a monetary amount in the active language.
 *
 * Amounts are `decimal` server-side and every total, VAT figure and grand total is computed by
 * the API — this only presents what came back. Never compute a figure here that the user will
 * read as authoritative.
 */
export function formatMoney(
  value: number | null | undefined,
  options: { currency?: string; withSymbol?: boolean } = {},
): string | null {
  if (value === null || value === undefined || Number.isNaN(value)) return null

  const { currency = 'TRY', withSymbol = false } = options

  return new Intl.NumberFormat(formatLocale(), {
    style: withSymbol ? 'currency' : 'decimal',
    currency: withSymbol ? currency : undefined,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

/** Formats a plain number in the active language. */
export function formatNumber(
  value: number | null | undefined,
  fractionDigits = 0,
): string | null {
  if (value === null || value === undefined || Number.isNaN(value)) return null

  return new Intl.NumberFormat(formatLocale(), {
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(value)
}

/** Formats a byte count as KB / MB / GB in the active language. */
export function formatFileSize(bytes: number | null | undefined): string | null {
  if (bytes === null || bytes === undefined || Number.isNaN(bytes)) return null

  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let size = bytes
  let unit = 0

  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024
    unit += 1
  }

  const digits = unit === 0 ? 0 : 1
  return `${formatNumber(size, digits)} ${units[unit]}`
}

/** Formats an ISO timestamp as date and time in the active language. */
export function formatDateTime(value: string | null | undefined): string | null {
  if (!value) return null
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return null
  return date.toLocaleString(formatLocale())
}

/**
 * Quantity with up to four decimals — invoice lines are priced per fractional unit, so a
 * whole-number format would silently round a real quantity away.
 */
export function formatQuantity(value: number | null | undefined): string | null {
  if (value === null || value === undefined || Number.isNaN(value)) return null

  return new Intl.NumberFormat(formatLocale(), {
    minimumFractionDigits: 0,
    maximumFractionDigits: 4,
  }).format(value)
}
