import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Package } from 'lucide-react'
import { AuthLayout } from '../components/AuthLayout'
import { clearPendingLogin } from '../api/authApi'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../lib/apiClient'
import { getPendingLogin } from '../lib/authStorage'
import { loadTurnstileScript } from '../lib/turnstile'
import '../App.css'

export function LoginPage() {
  const { login, completeTurnstileLogin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  /** Honeypot: deve ficar vazio. */
  const [website, setWebsite] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [awaitingVerification, setAwaitingVerification] = useState(() => getPendingLogin() !== null)
  const [scriptReady, setScriptReady] = useState(false)

  const containerRef = useRef<HTMLDivElement>(null)
  const widgetIdRef = useRef<string | null>(null)
  const submittingRef = useRef(false)

  const siteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY?.trim() ?? ''

  const runComplete = useCallback(
    async (turnstileToken: string) => {
      if (submittingRef.current) return
      submittingRef.current = true
      setError(null)
      setPending(true)
      try {
        await completeTurnstileLogin(turnstileToken)
        navigate(from, { replace: true })
      } catch (err) {
        setError(getApiErrorMessage(err))
        if (widgetIdRef.current && window.turnstile?.reset) {
          window.turnstile.reset(widgetIdRef.current)
        }
      } finally {
        setPending(false)
        submittingRef.current = false
      }
    },
    [completeTurnstileLogin, from, navigate],
  )

  useEffect(() => {
    if (!awaitingVerification) return
    if (!getPendingLogin()) {
      setAwaitingVerification(false)
      return
    }

    if (import.meta.env.DEV) {
      void runComplete('')
      return
    }

    if (!siteKey) {
      setError('Configure VITE_TURNSTILE_SITE_KEY (chave do site Cloudflare Turnstile).')
      return
    }

    let cancelled = false
    setScriptReady(false)

    void loadTurnstileScript()
      .then(() => {
        if (!cancelled) setScriptReady(true)
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Não foi possível carregar a verificação.')
        }
      })

    return () => {
      cancelled = true
    }
  }, [awaitingVerification, runComplete, siteKey])

  useEffect(() => {
    if (!awaitingVerification || import.meta.env.DEV) return
    if (!scriptReady || !siteKey || !containerRef.current || !window.turnstile) return

    const el = containerRef.current
    el.replaceChildren()

    const id = window.turnstile.render(el, {
      sitekey: siteKey,
      appearance: 'always',
      size: 'flexible',
      callback: (token) => {
        void runComplete(token)
      },
      'expired-callback': () => {
        setError('A verificação expirou. Resolva novamente.')
      },
      'error-callback': () => {
        setError('Não foi possível carregar a verificação. Atualize a página.')
      },
    })
    widgetIdRef.current = id

    return () => {
      if (widgetIdRef.current && window.turnstile?.remove) {
        window.turnstile.remove(widgetIdRef.current)
      }
      widgetIdRef.current = null
    }
  }, [awaitingVerification, runComplete, scriptReady, siteKey])

  const resetVerification = useCallback(() => {
    clearPendingLogin()
    setAwaitingVerification(false)
    setScriptReady(false)
    setError(null)
  }, [])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setPending(true)
    try {
      await login(userName.trim(), password, website)
      if (import.meta.env.DEV) {
        await runComplete('')
      } else {
        setAwaitingVerification(true)
      }
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setPending(false)
    }
  }

  return (
    <AuthLayout>
      <div className="auth-card">
        <div className="auth-card-header">
          <div className="sidebar-brand-icon auth-card-icon auth-card-icon--mobile-only">
            <Package />
          </div>
          <h1 className="auth-card-title">{awaitingVerification ? 'Verificação de segurança' : 'Entrar'}</h1>
          <p className="auth-card-subtitle">
            {awaitingVerification
              ? 'Login efetuado. Conclua a verificação Cloudflare Turnstile para continuar.'
              : 'Utilize o seu nome de utilizador e palavra-passe.'}
          </p>
        </div>

        {error ? <div className="auth-error">{error}</div> : null}

        {awaitingVerification ? (
          <div className="auth-form">
            <div className="turnstile-verify-body turnstile-verify-body--inline">
              <div ref={containerRef} className="turnstile-widget-wrap turnstile-widget-wrap--full" />
              {pending ? <p className="auth-card-subtitle">A validar…</p> : null}
            </div>
            <div className="form-actions auth-form-actions">
              <button type="button" className="btn ghost auth-submit" onClick={resetVerification} disabled={pending}>
                Voltar e alterar credenciais
              </button>
            </div>
          </div>
        ) : (
          <>
            <form onSubmit={handleSubmit} className="auth-form">
              <div className="auth-honeypot" aria-hidden="true">
                <label htmlFor="login-website">Website</label>
                <input
                  id="login-website"
                  name="website"
                  type="text"
                  tabIndex={-1}
                  autoComplete="off"
                  value={website}
                  onChange={(e) => setWebsite(e.target.value)}
                />
              </div>
              <div className="form-field">
                <label className="form-label" htmlFor="login-user">
                  Nome de utilizador
                </label>
                <input
                  id="login-user"
                  name="userName"
                  autoComplete="username"
                  value={userName}
                  onChange={(e) => setUserName(e.target.value)}
                  required
                  minLength={2}
                />
              </div>
              <div className="form-field">
                <label className="form-label" htmlFor="login-pass">
                  Palavra-passe
                </label>
                <input
                  id="login-pass"
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  minLength={6}
                />
              </div>
              <div className="form-actions auth-form-actions">
                <button type="submit" className="btn primary auth-submit" disabled={pending}>
                  {pending ? 'A entrar…' : 'Entrar'}
                </button>
              </div>
            </form>

            <p className="auth-footer">
              Ainda não tem conta? <Link to="/register">Criar conta</Link>
            </p>
          </>
        )}
      </div>
    </AuthLayout>
  )
}
