import { apiJson } from '../lib/apiClient'
import type { AuthResponse, LoginPendingResponse } from '../lib/authStorage'

export type { AuthResponse, LoginPendingResponse } from '../lib/authStorage'

export {
  clearAuth,
  clearPendingLogin,
  getPendingLogin,
  getStoredToken,
  getStoredUserName,
  persistAuth,
  persistPendingLogin,
} from '../lib/authStorage'

export async function register(
  userName: string,
  password: string,
  website = '',
): Promise<AuthResponse> {
  const data = await apiJson<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ userName, password, website }),
  })
  if (!data) throw new Error('Resposta vazia.')
  return data
}

export async function login(userName: string, password: string, website = ''): Promise<LoginPendingResponse> {
  const data = await apiJson<LoginPendingResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ userName, password, website }),
  })
  if (!data) throw new Error('Resposta vazia.')
  return data
}

export async function completeTurnstile(pendingToken: string, turnstileToken: string): Promise<AuthResponse> {
  const data = await apiJson<AuthResponse>('/api/auth/complete-turnstile', {
    method: 'POST',
    body: JSON.stringify({ pendingToken, turnstileToken }),
  })
  if (!data) throw new Error('Resposta vazia.')
  return data
}
