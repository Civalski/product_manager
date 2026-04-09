const TOKEN_KEY = 'productstore.auth.token'
const USER_KEY = 'productstore.auth.userName'

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

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function getStoredUserName(): string | null {
  return localStorage.getItem(USER_KEY)
}

export function persistAuth(res: AuthResponse): void {
  localStorage.setItem(TOKEN_KEY, res.token)
  localStorage.setItem(USER_KEY, res.userName)
}

export function clearAuth(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
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
  if (Number.isFinite(Date.parse(pendingExpiresAtUtc)) && Date.parse(pendingExpiresAtUtc) < Date.now()) {
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
