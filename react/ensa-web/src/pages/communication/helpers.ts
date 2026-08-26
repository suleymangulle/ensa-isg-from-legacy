import { formatLocale } from '@/i18n'
import {
  ContentFormat,
  MailPriority,
  MailStatus,
  MailType,
  MessageType,
  SupportTicketStatus,
  VisitType,
} from '@/api/enums'

/**
 * Presentation helpers of the communication module.
 *
 * Dates and times themselves come from the shared `@/utils/format` bundle (`formatDate`,
 * `formatDateTime`). What lives here is communication-only shaping: the enum option lists, the
 * day grouping the visit agenda is built from, and the `<input>` value conversions.
 */

/** Numeric values of an enum object, in declaration order. */
function valuesOf<T extends Record<string, string | number>>(source: T): number[] {
  return Object.values(source).filter((value): value is number => typeof value === 'number')
}

export const VISIT_TYPES = valuesOf(VisitType)
export const TICKET_STATUSES = valuesOf(SupportTicketStatus)
export const MESSAGE_TYPES = valuesOf(MessageType)
export const MAIL_STATUSES = valuesOf(MailStatus)
export const MAIL_PRIORITIES = valuesOf(MailPriority)
export const MAIL_TYPES = valuesOf(MailType)
export const CONTENT_FORMATS = valuesOf(ContentFormat)

/** Locale-aware time of day only — used inside a day group, where the date is the heading. */
export function formatTime(value: string | null | undefined): string | null {
  if (!value) return null
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return null
  return date.toLocaleTimeString(formatLocale(), { hour: '2-digit', minute: '2-digit' })
}

/** Long, locale-aware day heading, e.g. "Monday, 3 March 2026". */
export function formatDayHeading(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString(formatLocale(), {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })
}

/** `YYYY-MM-DD` in local time — the form the date inputs and the API range expect. */
export function toDateInput(date: Date): string {
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 10)
}

/** `YYYY-MM-DDTHH:mm` in local time, for `<input type="datetime-local">`. */
export function toDateTimeInput(value: string | null | undefined): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

/** First and last day of the month the given date falls in. */
export function monthRange(reference: Date): { from: string; to: string } {
  const first = new Date(reference.getFullYear(), reference.getMonth(), 1)
  const last = new Date(reference.getFullYear(), reference.getMonth() + 1, 0)
  return { from: toDateInput(first), to: toDateInput(last) }
}

/** Shifts a `YYYY-MM-DD` string by whole months, keeping it on the first of the month. */
export function shiftMonth(from: string, delta: number): Date {
  const date = new Date(`${from}T00:00:00`)
  if (Number.isNaN(date.getTime())) return new Date()
  return new Date(date.getFullYear(), date.getMonth() + delta, 1)
}

/** Groups calendar entries by calendar day, preserving the order they arrived in. */
export function groupByDay<T extends { start: string }>(items: T[]): { day: string; items: T[] }[] {
  const groups = new Map<string, T[]>()

  for (const item of items) {
    const date = new Date(item.start)
    const day = Number.isNaN(date.getTime()) ? item.start.slice(0, 10) : toDateInput(date)
    const bucket = groups.get(day)
    if (bucket) bucket.push(item)
    else groups.set(day, [item])
  }

  return [...groups.entries()]
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([day, entries]) => ({ day, items: entries }))
}

/** A short excerpt of a message body for a table cell. */
export function excerpt(text: string, maxLength = 90): string {
  const flat = text.replace(/\s+/g, ' ').trim()
  return flat.length > maxLength ? `${flat.slice(0, maxLength - 1)}…` : flat
}
