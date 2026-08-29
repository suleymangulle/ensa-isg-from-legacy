import { useQuery } from '@tanstack/react-query'
import { http } from './http'

/**
 * The office switcher's data layer.
 *
 * Kept in `src/api` rather than in `src/pages/tenancy/api.ts` on purpose: that file is the tenancy
 * *module's* data layer, loaded with the office administration screens, and the switcher is part of
 * the shell. The shell must not depend on a feature module — and this endpoint is not the office
 * administration endpoint either. `api/office` returns the office directory and requires the
 * `Ensa.Office` permission; this one answers "which offices may I work in" and requires only a
 * session, which is the whole reason an ordinary member of staff can switch at all.
 */

/** `MyOfficeDto` — one office the signed-in user may work in. */
export interface MyOfficeDto {
  id: number
  name: string
  isHeadquarterOffice: boolean
}

/** `MyOfficesDto` — `GET api/account/offices`. */
export interface MyOfficesDto {
  items: MyOfficeDto[]
  /** The office to start on, or `null` to start on "all offices". Always one of `items`. */
  defaultOfficeId: number | null
  /** Whether the server grants this user the "Tüm Şubeler" scope. Never inferred on the client. */
  allOfficesAllowed: boolean
}

/** The query key the office list lives under. Survives an office switch — it is what recovers from one. */
export const MY_OFFICES_KEY = ['account', 'offices'] as const

/**
 * `GET api/account/offices`.
 *
 * Held for the session rather than refetched: which offices a person is assigned to changes when an
 * administrator reassigns them, not while they work. `retry: false` because the two ways this can
 * fail — no session, or the server being down — are both answered by signing in again rather than
 * by asking a second time, and the switcher has an error state that says so.
 */
export function useMyOffices(enabled: boolean) {
  return useQuery({
    queryKey: MY_OFFICES_KEY,
    enabled,
    retry: false,
    staleTime: Infinity,
    queryFn: async ({ signal }) => {
      const { data } = await http.get<MyOfficesDto>('/account/offices', { signal })
      return data
    },
  })
}
