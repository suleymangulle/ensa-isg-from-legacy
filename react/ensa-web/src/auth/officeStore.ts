/**
 * The selected office: where it is remembered, and how the HTTP client reads it.
 *
 * Two things live here rather than in the React context, and for two different reasons.
 *
 * The **store** persists the selection so a reload does not drop the user back into a different
 * office. It is a convenience and nothing more: the server decides which offices exist and which
 * ones this user may work in, and `OfficeProvider` throws away any stored value the server's answer
 * does not contain. A tampered value cannot widen anything — the office context header is validated
 * on every single request.
 *
 * The **accessor** exists because `src/api/http.ts` is a plain axios instance created at module
 * scope; it cannot call a React hook, and importing the provider from it would close the import
 * cycle `http -> OfficeContext -> office api -> http`. So the provider pushes the resolved value
 * down here and the interceptor reads it, which keeps the dependency pointing one way.
 */

/** The office scope of a request: one office, every office the user may use, or none at all. */
export type OfficeScopeValue = number | 'all' | null

/** Storage key prefix; the user and tenant are appended. */
const STORAGE_PREFIX = 'ensa.office_id'

/** The neutral value the API accepts for "every office I may use" (`EnsaHttpHeaders.AllOfficesValue`). */
export const ALL_OFFICES = 'all'

/**
 * The storage key for one identity.
 *
 * Namespaced by tenant and user because this is a browser, and browsers are shared: sign out, sign
 * in as somebody else, and a single global key would hand the second person the first person's
 * office. They may not even have that office — the server would refuse every request until they
 * found the switcher.
 */
function storageKey(tenantId: number | undefined, userId: number): string {
  return `${STORAGE_PREFIX}.${tenantId ?? 'host'}.${userId}`
}

function safeGet(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

function safeSet(key: string, value: string): void {
  try {
    localStorage.setItem(key, value)
  } catch {
    /* private window / storage disabled — the selection simply does not survive a reload */
  }
}

function safeRemove(key: string): void {
  try {
    localStorage.removeItem(key)
  } catch {
    /* ignore */
  }
}

export const officeStore = {
  /** The remembered selection for one identity, or `null` when there is none or it is unreadable. */
  read(tenantId: number | undefined, userId: number): OfficeScopeValue {
    const raw = safeGet(storageKey(tenantId, userId))
    if (!raw) return null
    if (raw === ALL_OFFICES) return ALL_OFFICES

    const parsed = Number(raw)
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null
  },

  write(tenantId: number | undefined, userId: number, value: OfficeScopeValue): void {
    const key = storageKey(tenantId, userId)
    if (value === null) {
      safeRemove(key)
      return
    }
    safeSet(key, String(value))
  },

  clear(tenantId: number | undefined, userId: number): void {
    safeRemove(storageKey(tenantId, userId))
  },

  /**
   * Forgets every remembered office, for every identity in this browser.
   *
   * Used on sign-out: the next person to sign in here must start from the server's answer, not from
   * whatever the last one was looking at.
   */
  clearAll(): void {
    try {
      const keys: string[] = []
      for (let index = 0; index < localStorage.length; index += 1) {
        const key = localStorage.key(index)
        if (key?.startsWith(STORAGE_PREFIX)) keys.push(key)
      }
      keys.forEach(safeRemove)
    } catch {
      /* ignore */
    }
  },
}

/**
 * The value the request interceptor puts in `X-Ensa-OfficeId`.
 *
 * `null` means "send no header", which the API reads as "no office context" and answers exactly as
 * it did before offices existed. It is deliberately the initial value: until the permitted offices
 * have been fetched there is nothing trustworthy to send, and sending a guess would be refused.
 */
let currentScope: OfficeScopeValue = null

export const officeAccessor = {
  /** The header value for the next request, or `null` for none. */
  get(): string | null {
    return currentScope === null ? null : String(currentScope)
  },

  set(value: OfficeScopeValue): void {
    currentScope = value
  },

  reset(): void {
    currentScope = null
  },
}
