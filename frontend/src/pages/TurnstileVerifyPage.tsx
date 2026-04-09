import { useCallback, useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Shield } from 'lucide-react'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../lib/apiClient'
import { getPendingLogin } from '../lib/authStorage'
import '../App.css'

const TURNSTILE_SCRIPT = 'https://challenges.cloudflare.com/turnstile/v0/api.js'

function loadTurnstileScript(): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve()
  if (window.turnstile) return Promise.resolve()
  return new Promise((resolve, reject) => {
    const existing = document.querySelector(`script[src="${TURNSTILE_SCRIPT}"]`)
    if (existing) {
      existing.addEventListener('load', () => resolve())
      existing.addEventListener('error', () => reject(new Error('Falha ao carregar Turnstile.')))
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

export function TurnstileVerifyPage() {
  const { completeTurnstileLogin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const containerRef = useRef<HTMLDivElement>(null)
  const widgetIdRef = useRef<string | null>(null)
  const submittingRef = useRef(false)

  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [scriptReady, setScriptReady] = useState(false)

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
    if (!getPendingLogin()) {
      navigate('/login', { replace: true })
      return
    }

    if (!siteKey) {
      setError('Configure VITE_TURNSTILE_SITE_KEY (chave do site Cloudflare Turnstile).')
      return
    }

    let cancelled = false

    void loadTurnstileScript()
      .then(() => {
        if (!cancelled) setScriptReady(true)
      })
      .catch((e: unknown) => {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Não foi possível carregar a verificação.')
      })

    return () => {
      cancelled = true
    }
  }, [navigate, siteKey])

  useEffect(() => {
    if (!scriptReady || !siteKey || !containerRef.current || !window.turnstile) return

    const el = containerRef.current
    el.replaceChildren()

    const id = window.turnstile.render(el, {
      sitekey: siteKey,
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
  }, [runComplete, scriptReady, siteKey])

  return (
    <AuthLayout>
      <div className="auth-card">
        <div className="auth-card-header">
          <div className="sidebar-brand-icon auth-card-icon auth-card-icon--mobile-only">
            <Shield />
          </div>
          <h1 className="auth-card-title">Verificação de segurança</h1>
          <p className="auth-card-subtitle">
            Confirme que não é um robô para continuar para a aplicação.
          </p>
        </div>

        {error ? <div className="auth-error">{error}</div> : null}

        <div className="turnstile-verify-body">
          <div ref={containerRef} className="turnstile-widget-wrap" />
          {pending ? <p className="auth-card-subtitle">A validar…</p> : null}
        </div>

        <p className="auth-footer">
          <button
            type="button"
            className="btn ghost"
            onClick={() => navigate('/login', { replace: true })}
          >
            Voltar ao login
          </button>
        </p>
      </div>
    </AuthLayout>
  )
}
