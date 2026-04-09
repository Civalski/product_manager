import { useState } from 'react'
import { Link, Navigate, Outlet, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { LogOut, Moon, Package, LayoutList, PlusCircle, Sun, Tags } from 'lucide-react'
import { HttpLogTriggerButton, HttpLogViewer } from './components/HttpLogViewer'
import { ProtectedRoute } from './components/ProtectedRoute'
import { useAuth } from './contexts/AuthContext'
import { useTheme } from './hooks/useTheme'
import './App.css'
import { LoginPage } from './pages/LoginPage'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { ProductFormPage } from './pages/ProductFormPage'
import { ProductListPage } from './pages/ProductListPage'
import { RegisterPage } from './pages/RegisterPage'
import { TurnstileVerifyPage } from './pages/TurnstileVerifyPage'
import { CategoriesPage } from './pages/CategoriesPage'

const httpLogViewerEnabled =
  import.meta.env.DEV || (import.meta.env.VITE_ENABLE_HTTP_LOG_VIEWER ?? '').toLowerCase() === 'true'

function NavLink({ to, icon: Icon, children }: { to: string; icon: React.ElementType; children: React.ReactNode }) {
  const location = useLocation()
  const isActive = to === '/' ? location.pathname === '/' : location.pathname.startsWith(to)

  return (
    <Link to={to} className={isActive ? 'active' : ''}>
      <Icon />
      {children}
    </Link>
  )
}

function AppShell() {
  const { userName, logout } = useAuth()
  const navigate = useNavigate()
  const { theme, toggle } = useTheme()
  const [httpLogOpen, setHttpLogOpen] = useState(false)

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="sidebar-brand-icon">
            <Package />
          </div>
          <div className="sidebar-brand-text-block">
            <span className="sidebar-brand-text">ProductStore</span>
            <span className="sidebar-user-name" title={userName ?? ''}>
              {userName}
            </span>
          </div>
        </div>

        <div className="sidebar-section">
          <div className="sidebar-section-title">Menu</div>
          <nav className="sidebar-nav">
            <NavLink to="/" icon={LayoutList}>Produtos</NavLink>
            <NavLink to="/products/new" icon={PlusCircle}>Novo Produto</NavLink>
            <NavLink to="/categories" icon={Tags}>Categorias</NavLink>
          </nav>
        </div>

        <div className="sidebar-spacer" />

        <div className="sidebar-footer">
          <div className="sidebar-footer-row">
            {httpLogViewerEnabled && (
              <div className="sidebar-footer-slot">
                <HttpLogTriggerButton className="theme-toggle" onClick={() => setHttpLogOpen(true)} />
              </div>
            )}
            <div className="sidebar-footer-slot">
              <button
                type="button"
                className="theme-toggle"
                onClick={toggle}
                title={theme === 'light' ? 'Alternar para tema escuro' : 'Alternar para tema claro'}
              >
                {theme === 'light' ? <Moon size={16} /> : <Sun size={16} />}
              </button>
            </div>
            <div className="sidebar-footer-slot">
              <button
                type="button"
                className="btn ghost sidebar-logout"
                onClick={() => {
                  logout()
                  navigate('/login', { replace: true })
                }}
                title="Terminar sessão"
              >
                <LogOut size={16} />
                <span>Sair</span>
              </button>
            </div>
          </div>
        </div>
      </aside>

      {httpLogViewerEnabled && httpLogOpen ? <HttpLogViewer onClose={() => setHttpLogOpen(false)} /> : null}

      <main className="main">
        <div className="main-scroll">
          <Outlet />
        </div>
      </main>
    </div>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/verify-turnstile" element={<TurnstileVerifyPage />} />
      <Route
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route index element={<ProductListPage />} />
        <Route path="products/new" element={<ProductFormPage />} />
        <Route path="products/:id" element={<ProductDetailPage />} />
        <Route path="products/:id/edit" element={<ProductFormPage />} />
        <Route path="categories" element={<CategoriesPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

export default App
