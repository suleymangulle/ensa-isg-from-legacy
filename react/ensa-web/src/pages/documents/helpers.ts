import { DocumentOwnerType } from '@/api/enums'

/**
 * Presentation helpers of the documents module.
 *
 * Formatting itself belongs to the shared `@/utils/format` bundle — `formatFileSize` renders
 * the byte counts on these screens. What lives here is documents-only logic: the owner-type
 * option list, and the local digest the duplicate check is run against.
 */

/** Every `DocumentOwnerType` value, for the owner drop-downs. */
export const OWNER_TYPES: DocumentOwnerType[] = Object.values(DocumentOwnerType).filter(
  (value): value is DocumentOwnerType => typeof value === 'number',
)

/** Extension without the leading dot, or `null` when the name carries none. */
export function extensionOf(fileName: string): string | null {
  const dot = fileName.lastIndexOf('.')
  if (dot <= 0 || dot === fileName.length - 1) return null
  return fileName.slice(dot + 1).toLowerCase()
}

/**
 * SHA-256 digest of a file, as 64 lowercase hex characters — the shape the API stores.
 *
 * The file never leaves the browser: there is no upload endpoint, so the digest is computed
 * locally purely so the duplicate check has something to ask about.
 */
export async function sha256OfFile(file: File): Promise<string> {
  const buffer = await file.arrayBuffer()
  const digest = await crypto.subtle.digest('SHA-256', buffer)
  return Array.from(new Uint8Array(digest))
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('')
}

/** Whether the browser exposes WebCrypto — it needs a secure context (HTTPS or localhost). */
export function canHashLocally(): boolean {
  return typeof crypto !== 'undefined' && !!crypto.subtle
}

/** Years offered by the archive period filter: this year and the nine before it. */
export function recentYears(count = 10): number[] {
  const thisYear = new Date().getFullYear()
  return Array.from({ length: count }, (_, index) => thisYear - index)
}

/** 1..12, for the month drop-downs. */
export const MONTHS: number[] = Array.from({ length: 12 }, (_, index) => index + 1)
