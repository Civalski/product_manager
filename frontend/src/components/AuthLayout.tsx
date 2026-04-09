import { Package, Moon, Sun } from 'lucide-react'
import { useTheme } from '../hooks/useTheme'

type AuthLayoutProps = {
  children: React.ReactNode
}

export function AuthLayout({ children }: AuthLayoutProps) {
  const { theme, toggle } = useTheme()

  return (
    <div className="auth-shell">
      <aside className="auth-hero" aria-hidden="true">
        <div className="auth-hero-inner">
          <div className="auth-hero-logo">
            <div className="sidebar-brand-icon auth-hero-icon">
              <Package />
            </div>
            <span className="auth-hero-brand">ProductStore</span>
          </div>
          <p className="auth-hero-tagline">Inventário e produtos num só lugar.</p>
        </div>
      </aside>

      <div className="auth-panel">
        <div className="auth-page-top">
          <button
            type="button"
            className="theme-toggle"
            onClick={toggle}
            title={theme === 'light' ? 'Alternar para tema escuro' : 'Alternar para tema claro'}
          >
            {theme === 'light' ? <Moon size={16} /> : <Sun size={16} />}
          </button>
        </div>
        <div className="auth-page">{children}</div>
      </div>
    </div>
  )
}
