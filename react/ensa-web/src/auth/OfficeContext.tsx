import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { MY_OFFICES_KEY, useMyOffices, type MyOfficeDto } from '@/api/office'
import { useAuth } from './AuthContext'
import { ALL_OFFICES, officeAccessor, officeStore, type OfficeScopeValue } from './officeStore'

/**
 * The office (branch) the application is working in.
 *
 * The server is the authority on **which** offices exist and which ones this user may have; this
 * context only decides which of them is selected right now. That split is what makes a stored
 * selection safe to keep in `localStorage`: a value the server's list does not contain is dropped
 * on sight, and even a value that survives that check is validated again by the API on every
 * request it is sent with.
 *
 * Selecting an office is not a save. Nothing is written to the database — the choice travels as the
 * `X-Ensa-OfficeId` header on subsequent requests, and switching never touches the tenant.
 */

/** What the shell is currently scoped to. */
export interface OfficeContextValue {
  /** The offices this user may work in, ordered by name. Empty means no switcher is shown. */
  offices: MyOfficeDto[]
  /** The selected office, `'all'` for every permitted office, or `null` when there is no context. */
  scope: OfficeScopeValue
  /** The selected office record, when exactly one is selected. */
  activeOffice: MyOfficeDto | null
  /** Whether the server grants the "Tüm Şubeler" scope. */
  allOfficesAllowed: boolean
  /** Whether there is anything to switch between. */
  canSwitch: boolean
  /** False until the permitted offices have been resolved. Office-scoped screens must wait for it. */
  isReady: boolean
  isLoading: boolean
  /** Set when the office list could not be fetched. The scope is left alone rather than widened. */
  error: unknown
  /** True while a switch is being applied, so the control can refuse a second one. */
  isSwitching: boolean
  selectOffice: (value: OfficeScopeValue) => Promise<void>
  retry: () => void
}

const OfficeContext = createContext<OfficeContextValue | null>(null)

/**
 * Query keys that survive an office switch.
 *
 * Everything else is dropped, because the office context header goes out with every API call and
 * any of them may answer differently under a different office. Keeping a short, explicit list of
 * exceptions is the only version of this that stays correct as screens are added — an allow-list of
 * "office-scoped" keys would silently miss the next one somebody writes.
 *
 * - `account` — the caller's own account: their profile, their permissions, and the office list
 *   itself. None of it depends on which office is selected, and dropping the office list would
 *   delete the very answer the switch was made from.
 * - `lookup` — host reference data (provinces, occupation codes, plans). Identical for every
 *   organization, let alone every office, and cached for an hour on purpose.
 */
const OFFICE_INDEPENDENT_KEYS = ['account', 'lookup']

function survivesOfficeSwitch(key: readonly unknown[]): boolean {
  return typeof key[0] === 'string' && OFFICE_INDEPENDENT_KEYS.includes(key[0])
}

export function OfficeProvider({ children }: { children: ReactNode }) {
  const { user, isReady: isSessionReady } = useAuth()
  const queryClient = useQueryClient()

  const [scope, setScope] = useState<OfficeScopeValue>(null)
  const [isResolved, setIsResolved] = useState(false)
  const [isSwitching, setIsSwitching] = useState(false)

  // Identity, not the whole user: the office list has to be resolved again when the person or the
  // organization changes, and only then.
  const userId = user?.id
  const tenantId = user?.tenantId

  const query = useMyOffices(Boolean(isSessionReady && userId))

  const offices = useMemo(() => query.data?.items ?? [], [query.data])
  const allOfficesAllowed = query.data?.allOfficesAllowed ?? false

  /**
   * Applies a scope to the accessor the HTTP client reads, and remembers it.
   *
   * Both happen together, always, so the header the next request carries and the value a reload
   * would restore can never disagree.
   */
  const apply = useCallback(
    (value: OfficeScopeValue) => {
      officeAccessor.set(value)
      if (userId) officeStore.write(tenantId, userId, value)
      setScope(value)
    },
    [tenantId, userId],
  )

  /**
   * Resolves the initial scope from the server's answer and whatever was remembered.
   *
   * Order: the stored value if the server still lists it, then the server's own default, then the
   * only office there is, then "all offices" if it is allowed. Anything left over means the user has
   * no office to work in, and the scope stays empty — which sends no header and leaves their
   * requests scoped by tenant, exactly as before this feature existed.
   */
  const resolvedFor = useRef<string | null>(null)

  useEffect(() => {
    if (!isSessionReady) return

    if (!userId) {
      // Signed out: forget everything, including the header the next request would carry.
      officeAccessor.reset()
      resolvedFor.current = null
      setScope(null)
      setIsResolved(false)
      return
    }

    if (query.isPending) return

    const identity = `${tenantId ?? 'host'}:${userId}`

    // The list is fetched once per identity; re-resolving on every render would fight the user's
    // own selection.
    if (resolvedFor.current === identity) return

    if (query.isError) {
      // No trustworthy list, so no office context. Requests fall back to tenant scope rather than
      // going out with a guess the server would refuse.
      officeAccessor.reset()
      setScope(null)
      setIsResolved(true)
      resolvedFor.current = identity
      return
    }

    const permitted = query.data?.items ?? []
    const stored = officeStore.read(tenantId, userId)

    const storedIsUsable =
      stored === ALL_OFFICES
        ? (query.data?.allOfficesAllowed ?? false)
        : typeof stored === 'number' && permitted.some(office => office.id === stored)

    if (stored !== null && !storedIsUsable) {
      // The user was moved out of that office, or it was closed. Drop it silently and fall through
      // to the server's answer — the alternative is a switcher pointing at an office every request
      // would be refused for.
      officeStore.clear(tenantId, userId)
    }

    const serverDefault = query.data?.defaultOfficeId ?? null

    const initial: OfficeScopeValue = storedIsUsable
      ? stored
      : serverDefault !== null && permitted.some(office => office.id === serverDefault)
        ? serverDefault
        : permitted.length === 1
          ? permitted[0].id
          : query.data?.allOfficesAllowed
            ? ALL_OFFICES
            : null

    apply(initial)
    setIsResolved(true)
    resolvedFor.current = identity
  }, [apply, isSessionReady, query.data, query.isError, query.isPending, tenantId, userId])

  /**
   * Switches office.
   *
   * The order matters and is the whole reason this is not two lines in the component:
   *
   * 1. **Cancel** what is in flight, so a response fetched under the previous office cannot land
   *    after the switch and be read as the new one's data.
   * 2. **Remove** the office-dependent cache. Removing rather than invalidating, because an
   *    invalidated query keeps serving its old rows until the refetch arrives — and those rows are
   *    the previous office's.
   * 3. **Then** move the scope. Any query that remounts now goes out with the new header.
   */
  const selectOffice = useCallback(
    async (value: OfficeScopeValue) => {
      if (value === scope) return

      setIsSwitching(true)
      try {
        await queryClient.cancelQueries({
          predicate: query_ => !survivesOfficeSwitch(query_.queryKey),
        })

        queryClient.removeQueries({
          predicate: query_ => !survivesOfficeSwitch(query_.queryKey),
        })

        apply(value)
      } finally {
        setIsSwitching(false)
      }
    },
    [apply, queryClient, scope],
  )

  const retry = useCallback(() => {
    resolvedFor.current = null
    void queryClient.refetchQueries({ queryKey: MY_OFFICES_KEY })
  }, [queryClient])

  const activeOffice = useMemo(
    () => (typeof scope === 'number' ? (offices.find(office => office.id === scope) ?? null) : null),
    [offices, scope],
  )

  const value = useMemo<OfficeContextValue>(
    () => ({
      offices,
      scope,
      activeOffice,
      allOfficesAllowed,
      // One office and no "all" option is a control with a single choice, which the legacy shell
      // did not render either (`Model.OfisList.Count > 1`).
      canSwitch: offices.length > 1 || (offices.length === 1 && allOfficesAllowed),
      isReady: isResolved || !userId,
      isLoading: query.isPending,
      error: query.isError ? query.error : null,
      isSwitching,
      selectOffice,
      retry,
    }),
    [
      activeOffice,
      allOfficesAllowed,
      isResolved,
      isSwitching,
      offices,
      query.error,
      query.isError,
      query.isPending,
      retry,
      scope,
      selectOffice,
      userId,
    ],
  )

  return <OfficeContext.Provider value={value}>{children}</OfficeContext.Provider>
}

export function useOffice(): OfficeContextValue {
  const context = useContext(OfficeContext)
  if (!context) throw new Error('useOffice can only be used inside OfficeProvider.')
  return context
}

/**
 * A stable token for the current office scope, for use inside a query key.
 *
 * Belt and braces beside the cache reset in `selectOffice`: with the scope in the key, a response
 * that somehow escapes cancellation lands under the key it was requested with rather than under the
 * one the screen is now reading.
 */
export function useOfficeScopeKey(): string {
  const { scope } = useOffice()
  return scope === null ? 'no-office' : String(scope)
}
