import { useState } from 'react'
import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { Package, LayoutList, PlusCircle, Sun, Moon } from 'lucide-react'
import { HttpLogTriggerButton, HttpLogViewer } from './components/HttpLogViewer'
import { useTheme } from './hooks/useTheme'
import './App.css'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { ProductFormPage } from './pages/ProductFormPage'
import { ProductListPage } from './pages/ProductListPage'

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

function App() {
  const { theme, toggle } = useTheme()
  const [httpLogOpen, setHttpLogOpen] = useState(false)

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="sidebar-brand-icon">
            <Package />
          </div>
          <span className="sidebar-brand-text">ProductStore</span>
        </div>

        <div className="sidebar-section">
          <div className="sidebar-section-title">Menu</div>
          <nav className="sidebar-nav">
            <NavLink to="/" icon={LayoutList}>Produtos</NavLink>
            <NavLink to="/products/new" icon={PlusCircle}>Novo Produto</NavLink>
          </nav>
        </div>

        <div className="sidebar-spacer" />

        <div className="sidebar-footer">
          <div className="sidebar-footer-actions">
            <HttpLogTriggerButton onClick={() => setHttpLogOpen(true)} />
            <button
              type="button"
              className="theme-toggle"
              onClick={toggle}
              title={theme === 'light' ? 'Alternar para tema escuro' : 'Alternar para tema claro'}
            >
              {theme === 'light' ? <Moon size={16} /> : <Sun size={16} />}
            </button>
          </div>
        </div>
      </aside>

      <HttpLogViewer open={httpLogOpen} onClose={() => setHttpLogOpen(false)} />

      <main className="main">
        <div className="main-scroll">
          <Routes>
            <Route path="/" element={<ProductListPage />} />
            <Route path="/products/new" element={<ProductFormPage />} />
            <Route path="/products/:id" element={<ProductDetailPage />} />
            <Route path="/products/:id/edit" element={<ProductFormPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </div>
      </main>
    </div>
  )
}

export default App
