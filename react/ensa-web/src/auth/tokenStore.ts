import axios from 'axios'

const ACCESS_KEY = 'ensa.access_token'
const REFRESH_KEY = 'ensa.refresh_token'

/** OpenIddict `connect/token` response. */
export interface TokenResponse {
  access_token: string
  refresh_token?: string
  token_type: string
  expires_in: number
}

/**
 * The client this application is registered as in OpenIddict. A public client - the SPA runs in
 * a browser, where a secret would be readable by anyone who opens the developer tools - so this
 * identifies the application without authenticating it. The grants and scopes it may ask for are
 * declared once, on the server, against this id.
 */
const CLIENT_ID = 'ensa-spa'

/** The OpenIddict token endpoint expects `application/x-www-form-urlencoded`. */
const tokenClient = axios.create({
  baseURL: '/',
  headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
})

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
    /* private window / storage disabled — ignore silently */
  }
}

function safeRemove(key: string): void {
  try {
    localStorage.removeItem(key)
  } catch {
    /* ignore */
  }
}

export const tokenStore = {
  getAccessToken: () => safeGet(ACCESS_KEY),
  getRefreshToken: () => safeGet(REFRESH_KEY),

  save(response: TokenResponse) {
    safeSet(ACCESS_KEY, response.access_token)
    if (response.refresh_token) safeSet(REFRESH_KEY, response.refresh_token)
  },

  clear() {
    safeRemove(ACCESS_KEY)
    safeRemove(REFRESH_KEY)
  },

  /** Signs in with the `password` grant. */
  async signIn(userName: string, password: string): Promise<TokenResponse> {
    const body = new URLSearchParams({
      grant_type: 'password',
      client_id: CLIENT_ID,
      username: userName,
      password,
      scope: 'openid profile email roles offline_access ensa',
    })
    const { data } = await tokenClient.post<TokenResponse>('connect/token', body)
    tokenStore.save(data)
    return data
  },

  /** Runs the `refresh_token` grant. Returns null when it fails. */
  async refresh(): Promise<string | null> {
    const refreshToken = tokenStore.getRefreshToken()
    if (!refreshToken) return null
    try {
      const body = new URLSearchParams({
        grant_type: 'refresh_token',
        client_id: CLIENT_ID,
        refresh_token: refreshToken,
      })
      const { data } = await tokenClient.post<TokenResponse>('connect/token', body)
      tokenStore.save(data)
      return data.access_token
    } catch {
      tokenStore.clear()
      return null
    }
  },
}

/** Decodes the JWT payload — the signature is verified server side. */
export function decodeToken(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1]
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(decodeURIComponent(escape(json)))
  } catch {
    return null
  }
}
