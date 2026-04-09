import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Package } from 'lucide-react'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../lib/apiClient'
import { getPendingLogin } from '../lib/authStorage'
import '../App.css'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  useEffect(() => {
    const pendingLogin = getPendingLogin()
    if (pendingLogin) {
      navigate('/verify-turnstile', { state: { from }, replace: true })
    }
  }, [from, navigate])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setPending(true)
    try {
      await login(userName.trim(), password)
      navigate('/verify-turnstile', { state: { from }, replace: true })
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
          <h1 className="auth-card-title">Entrar</h1>
          <p className="auth-card-subtitle">Utilize o seu nome de utilizador e palavra-passe.</p>
        </div>

        <form onSubmit={handleSubmit} className="auth-form">
          {error ? <div className="auth-error">{error}</div> : null}
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
      </div>
    </AuthLayout>
  )
}
