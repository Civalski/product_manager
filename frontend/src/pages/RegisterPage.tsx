import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Package } from 'lucide-react'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../lib/apiClient'
import '../App.css'

const TURNSTILE_SCRIPT = 'https://challenges.cloudflare.com/turnstile/v0/api.js'

function loadTurnstileScript(): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve()
  if (window.turnstile) return Promise.resolve()
  return new Promise((resolve, reject) => {
    const existing = document.querySelector(
      `script[src="${TURNSTILE_SCRIPT}"]`,
    ) as HTMLScriptElement | null
    if (existing) {
      if (window.turnstile) {
        resolve()
        return
      }
      const onLoad = () => {
        existing.removeEventListener('load', onLoad)
        resolve()
      }
      existing.addEventListener('load', onLoad)
      setTimeout(() => {
        if (window.turnstile) resolve()
        else reject(new Error('Falha ao carregar Turnstile.'))
      }, 10_000)
      return
    }
    const script = document.createElement('script')
    script.src = TURNSTILE_SCRIPT
    script.async = true
    script.onload = () => resolve()
    script.onerror = () => reject(new Error('Falha ao carregar Turnstile.'))
    document.head.appendChild(script)
  })
}

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [website, setWebsite] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  const turnstileTokenRef = useRef('')
  const containerRef = useRef<HTMLDivElement>(null)
  const widgetIdRef = useRef<string | null>(null)
  const [scriptReady, setScriptReady] = useState(false)

  const siteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY?.trim() ?? ''
  const isDev = import.meta.env.DEV

  useEffect(() => {
    if (isDev || !siteKey) return
    let cancelled = false
    void loadTurnstileScript()
      .then(() => {
        if (!cancelled) setScriptReady(true)
      })
      .catch((e: unknown) => {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Não foi possível carregar a verificação.')
      })
    return () => {
      cancelled = true
    }
  }, [isDev, siteKey])

  const handleToken = useCallback((token: string) => {
    turnstileTokenRef.current = token
  }, [])

  useEffect(() => {
    if (!scriptReady || !siteKey || !containerRef.current || !window.turnstile) return

    const el = containerRef.current
    el.replaceChildren()

    const id = window.turnstile.render(el, {
      sitekey: siteKey,
      appearance: 'always',
      size: 'normal',
      callback: handleToken,
      'expired-callback': () => {
        turnstileTokenRef.current = ''
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
  }, [handleToken, scriptReady, siteKey])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    if (!isDev && !turnstileTokenRef.current) {
      setError('Complete a verificação de segurança antes de registar.')
      return
    }

    setPending(true)
    try {
      await register(userName.trim(), password, website, turnstileTokenRef.current)
      navigate('/', { replace: true })
    } catch (err) {
      setError(getApiErrorMessage(err))
      if (widgetIdRef.current && window.turnstile?.reset) {
        window.turnstile.reset(widgetIdRef.current)
      }
      turnstileTokenRef.current = ''
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
          <h1 className="auth-card-title">Criar conta</h1>
        </div>

        <form onSubmit={handleSubmit} className="auth-form">
          {error ? <div className="auth-error">{error}</div> : null}
          <div className="auth-honeypot" aria-hidden="true">
            <label htmlFor="reg-website">Website</label>
            <input
              id="reg-website"
              name="website"
              type="text"
              tabIndex={-1}
              autoComplete="off"
              value={website}
              onChange={(e) => setWebsite(e.target.value)}
            />
          </div>
          <div className="form-field">
            <label className="form-label" htmlFor="reg-user">
              Nome de utilizador
            </label>
            <input
              id="reg-user"
              name="userName"
              autoComplete="username"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              required
              minLength={2}
            />
          </div>
          <div className="form-field">
            <label className="form-label" htmlFor="reg-pass">
              Palavra-passe
            </label>
            <input
              id="reg-pass"
              name="password"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={8}
              title="Mínimo 8 caracteres, com número e símbolo"
            />
          </div>
          {!isDev && siteKey && (
            <div className="turnstile-widget-wrap" ref={containerRef} />
          )}
          <div className="form-actions auth-form-actions">
            <button type="submit" className="btn primary auth-submit" disabled={pending}>
              {pending ? 'A criar…' : 'Registar'}
            </button>
          </div>
        </form>

        <p className="auth-footer">
          Já tem conta? <Link to="/login">Entrar</Link>
        </p>
      </div>
    </AuthLayout>
  )
}
