import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Package } from 'lucide-react'
import { AuthLayout } from '../components/AuthLayout'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../lib/apiClient'
import '../App.css'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  /** Honeypot: deve ficar vazio; utilizadores reais não o preenchem. */
  const [website, setWebsite] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setPending(true)
    try {
      await register(userName.trim(), password, website)
      navigate('/', { replace: true })
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
          <h1 className="auth-card-title">Criar conta</h1>
          <p className="auth-card-subtitle">
            Palavra-passe: mínimo 8 caracteres, com pelo menos um número e um símbolo (ex.: ! ? #).
          </p>
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
