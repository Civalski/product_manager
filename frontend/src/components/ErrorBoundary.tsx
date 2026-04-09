import { Component, type ErrorInfo, type ReactNode } from 'react'
import { AlertTriangle, RefreshCw } from 'lucide-react'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('ErrorBoundary caught:', error, info.componentStack)
  }

  private handleReload = () => {
    window.location.reload()
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null })
  }

  render() {
    if (!this.state.hasError) return this.props.children

    return (
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: 'var(--bg-base)',
          padding: '24px',
        }}
      >
        <div
          style={{
            maxWidth: 440,
            width: '100%',
            background: 'var(--bg-surface)',
            border: '1px solid var(--border-default)',
            borderRadius: 'var(--radius-xl)',
            padding: 'clamp(24px, 5vw, 36px)',
            boxShadow: 'var(--shadow-lg)',
            textAlign: 'center',
          }}
        >
          <div
            style={{
              width: 48,
              height: 48,
              borderRadius: 'var(--radius-lg)',
              background: 'var(--danger-muted)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 16px',
              color: 'var(--danger)',
            }}
          >
            <AlertTriangle size={24} />
          </div>
          <h1
            style={{
              fontSize: '1.2rem',
              fontWeight: 700,
              color: 'var(--text-primary)',
              margin: '0 0 8px',
            }}
          >
            Algo correu mal
          </h1>
          <p
            style={{
              fontSize: '0.9rem',
              color: 'var(--text-secondary)',
              lineHeight: 1.5,
              margin: '0 0 20px',
            }}
          >
            Ocorreu um erro inesperado na aplicação. Tente recarregar a página.
          </p>
          {import.meta.env.DEV && this.state.error && (
            <pre
              style={{
                textAlign: 'left',
                fontSize: '0.75rem',
                fontFamily: 'var(--font-mono)',
                background: 'var(--bg-elevated)',
                border: '1px solid var(--border-default)',
                borderRadius: 'var(--radius-md)',
                padding: '10px 12px',
                maxHeight: 160,
                overflow: 'auto',
                color: 'var(--danger)',
                marginBottom: 20,
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
              }}
            >
              {this.state.error.message}
              {this.state.error.stack && `\n${this.state.error.stack}`}
            </pre>
          )}
          <div style={{ display: 'flex', gap: 8, justifyContent: 'center' }}>
            <button type="button" className="btn" onClick={this.handleReset}>
              Tentar novamente
            </button>
            <button type="button" className="btn primary" onClick={this.handleReload}>
              <RefreshCw size={15} />
              Recarregar
            </button>
          </div>
        </div>
      </div>
    )
  }
}
