import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import {
  clearAuth,
  clearPendingLogin,
  getStoredUserName,
  persistAuth,
  persistPendingLogin,
  completeTurnstile as apiCompleteTurnstile,
  login as apiLogin,
  register as apiRegister,
} from '../api/authApi'
import { getPendingLogin } from '../lib/authStorage'

type AuthContextValue = {
  userName: string | null
  isAuthenticated: boolean
  login: (userName: string, password: string, honeypot?: string) => Promise<void>
  completeTurnstileLogin: (turnstileToken: string) => Promise<void>
  register: (userName: string, password: string, honeypot?: string, turnstileToken?: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [userName, setUserName] = useState<string | null>(() => getStoredUserName())

  const logout = useCallback(() => {
    clearAuth()
    clearPendingLogin()
    setUserName(null)
  }, [])

  const login = useCallback(async (u: string, p: string, honeypot = '') => {
    const res = await apiLogin(u, p, honeypot)
    persistPendingLogin(res)
  }, [])

  const completeTurnstileLogin = useCallback(async (turnstileToken: string) => {
    const pending = getPendingLogin()
    if (!pending) throw new Error('Sessão de verificação em falha ou expirada. Faça login novamente.')

    const res = await apiCompleteTurnstile(pending.pendingToken, turnstileToken)
    clearPendingLogin()
    persistAuth(res)
    setUserName(res.userName)
  }, [])

  const register = useCallback(async (u: string, p: string, honeypot = '', turnstileToken = '') => {
    const res = await apiRegister(u, p, honeypot, turnstileToken)
    persistAuth(res)
    setUserName(res.userName)
  }, [])

  const value = useMemo(
    () => ({
      userName,
      isAuthenticated: !!userName,
      login,
      completeTurnstileLogin,
      register,
      logout,
    }),
    [userName, login, completeTurnstileLogin, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components -- hook + provider no mesmo ficheiro
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider.')
  return ctx
}
