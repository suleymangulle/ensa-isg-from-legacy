import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { http, type ListResult } from '@/api/http'
import { officeAccessor, officeStore } from './officeStore'
import { decodeToken, tokenStore } from './tokenStore'

export interface UserInfo {
  id: number
  userName: string
  email?: string
  fullName: string
  tenantId?: number
  /** Set when the user belongs to one client workplace; their data is scoped to it. */
  companyId?: number
  roles: string[]
  permissions: string[]
}

interface AuthContextValue {
  user: UserInfo | null
  /** False until the stored token has been inspected on first render. */
  isReady: boolean
  signIn: (userName: string, password: string) => Promise<void>
  signOut: () => void
  hasPermission: (permission: string) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

/** Normalises a claim to an array — it may arrive as a single value or a list. */
function claimList(payload: Record<string, unknown>, key: string): string[] {
  const value = payload[key]
  if (Array.isArray(value)) return value.map(String)
  if (typeof value === 'string') return [value]
  return []
}

function userFromToken(token: string): UserInfo | null {
  const payload = decodeToken(token)
  if (!payload) return null

  const id = Number(payload.sub ?? payload.nameid ?? 0)
  if (!id) return null

  const name = String(payload.given_name ?? '')
  const lastName = String(payload.family_name ?? '')
  const userName = String(payload.name ?? payload.preferred_username ?? '')

  return {
    id,
    userName,
    email: payload.email ? String(payload.email) : undefined,
    fullName: [name, lastName].filter(Boolean).join(' ') || userName,
    tenantId: payload['ensa:tenantId'] ? Number(payload['ensa:tenantId']) : undefined,
    companyId: payload['ensa:companyId'] ? Number(payload['ensa:companyId']) : undefined,
    roles: [
      ...claimList(payload, 'role'),
      ...claimList(payload, 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'),
    ],
    permissions: [],
  }
}

/**
 * Business permissions are not in the token, deliberately: a token that carried them would keep
 * answering with what was true when it was issued, for as long as it lives. They are fetched from
 * the API instead, where they are re-evaluated per request.
 *
 * This drives what the interface offers, never what it is allowed to do -- the server checks every
 * call regardless, so an empty answer here degrades the menu, it does not open anything.
 */
async function fetchPermissions(): Promise<string[]> {
  try {
    const { data } = await http.get<ListResult<string>>('/account/permissions')
    return data.items ?? []
  } catch {
    return []
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [isReady, setIsReady] = useState(false)

  useEffect(() => {
    const token = tokenStore.getAccessToken()
    if (!token) {
      setIsReady(true)
      return
    }

    const signedIn = userFromToken(token)
    setUser(signedIn)

    if (!signedIn) {
      setIsReady(true)
      return
    }

    let cancelled = false
    void fetchPermissions().then(permissions => {
      if (!cancelled) setUser(current => (current ? { ...current, permissions } : current))
      setIsReady(true)
    })

    return () => {
      cancelled = true
    }
  }, [])

  const signIn = useCallback(async (userName: string, password: string) => {
    const response = await tokenStore.signIn(userName, password)
    const signedIn = userFromToken(response.access_token)
    setUser(signedIn ? { ...signedIn, permissions: await fetchPermissions() } : signedIn)
  }, [])

  const signOut = useCallback(() => {
    tokenStore.clear()
    // The office context goes with the session. The accessor is cleared so no request can still
    // carry the previous user's office, and the remembered selections are dropped because a browser
    // is shared: the next person to sign in here must start from the server's answer for them.
    officeAccessor.reset()
    officeStore.clearAll()
    setUser(null)
  }, [])

  const hasPermission = useCallback(
    (permission: string) => user?.permissions.includes(permission) ?? false,
    [user],
  )

  const value = useMemo(
    () => ({ user, isReady, signIn, signOut, hasPermission }),
    [user, isReady, signIn, signOut, hasPermission],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth can only be used inside AuthProvider.')
  return context
}
