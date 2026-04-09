const TOKEN_KEY = 'productstore.auth.token'
const USER_KEY = 'productstore.auth.userName'
const EXPIRES_KEY = 'productstore.auth.expiresAtUtc'

const PENDING_TOKEN_KEY = 'productstore.auth.pendingToken'
const PENDING_USER_KEY = 'productstore.auth.pendingUserName'
const PENDING_EXPIRES_KEY = 'productstore.auth.pendingExpiresAtUtc'

export type AuthResponse = {
  token: string
  userName: string
  expiresAtUtc: string
}

export type LoginPendingResponse = {
  pendingToken: string
  userName: string
  pendingExpiresAtUtc: string
}

function isExpired(isoUtc: string): boolean {
  const parsed = Date.parse(isoUtc)
  return Number.isFinite(parsed) && parsed <= Date.now()
}

export function getStoredAuth(): AuthResponse | null {
  const token = localStorage.getItem(TOKEN_KEY)
  const userName = localStorage.getItem(USER_KEY)
  const expiresAtUtc = localStorage.getItem(EXPIRES_KEY)

  if (!token || !userName || !expiresAtUtc) {
    clearAuth()
    return null
  }

  if (isExpired(expiresAtUtc)) {
    clearAuth()
    return null
  }

  return { token, userName, expiresAtUtc }
}

export function getStoredToken(): string | null {
  return getStoredAuth()?.token ?? null
}

export function getStoredUserName(): string | null {
  return getStoredAuth()?.userName ?? null
}

export function persistAuth(res: AuthResponse): void {
  localStorage.setItem(TOKEN_KEY, res.token)
  localStorage.setItem(USER_KEY, res.userName)
  localStorage.setItem(EXPIRES_KEY, res.expiresAtUtc)
}

export function clearAuth(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
  localStorage.removeItem(EXPIRES_KEY)
}

export function persistPendingLogin(res: LoginPendingResponse): void {
  sessionStorage.setItem(PENDING_TOKEN_KEY, res.pendingToken)
  sessionStorage.setItem(PENDING_USER_KEY, res.userName)
  sessionStorage.setItem(PENDING_EXPIRES_KEY, res.pendingExpiresAtUtc)
}

export function getPendingLogin(): LoginPendingResponse | null {
  const pendingToken = sessionStorage.getItem(PENDING_TOKEN_KEY)
  const userName = sessionStorage.getItem(PENDING_USER_KEY)
  const pendingExpiresAtUtc = sessionStorage.getItem(PENDING_EXPIRES_KEY)
  if (!pendingToken || !userName || !pendingExpiresAtUtc) return null
  if (isExpired(pendingExpiresAtUtc)) {
    clearPendingLogin()
    return null
  }
  return { pendingToken, userName, pendingExpiresAtUtc }
}

export function clearPendingLogin(): void {
  sessionStorage.removeItem(PENDING_TOKEN_KEY)
  sessionStorage.removeItem(PENDING_USER_KEY)
  sessionStorage.removeItem(PENDING_EXPIRES_KEY)
}
