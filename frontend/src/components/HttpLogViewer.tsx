import { useCallback, useState, useSyncExternalStore } from 'react'
import { ScrollText, ChevronDown, ChevronRight } from 'lucide-react'
import { clearHttpLogs, getHttpLogs, subscribeHttpLogs } from '../lib/httpLog'
import type { HttpLogEntry } from '../lib/httpLog'

function useHttpLogs() {
  return useSyncExternalStore(subscribeHttpLogs, getHttpLogs, getHttpLogs)
}

function statusClass(status: number): string {
  if (status === 0) return 'http-log-status http-log-status--fail'
  if (status >= 200 && status < 300) return 'http-log-status http-log-status--ok'
  if (status >= 400 && status < 500) return 'http-log-status http-log-status--client'
  if (status >= 500) return 'http-log-status http-log-status--server'
  return 'http-log-status'
}

function formatTime(iso: string) {
  try {
    return new Date(iso).toLocaleTimeString('pt-BR', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    })
  } catch {
    return iso
  }
}

function LogRow({
  entry,
  expanded,
  onToggle,
}: {
  entry: HttpLogEntry
  expanded: boolean
  onToggle: () => void
}) {
  return (
    <div className="http-log-row">
      <button type="button" className="http-log-row-head" onClick={onToggle}>
        {expanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        <span className="http-log-time">{formatTime(entry.at)}</span>
        <span className="http-log-method">{entry.method}</span>
        <span className={statusClass(entry.status)}>{entry.status || '—'}</span>
        <span className="http-log-ms">{entry.durationMs} ms</span>
        <span className="http-log-url" title={entry.url}>
          {entry.url}
        </span>
      </button>
      {expanded && (
        <div className="http-log-row-detail">
          {entry.error && (
            <div className="http-log-block">
              <div className="http-log-block-title">Erro</div>
              <pre className="http-log-pre">{entry.error}</pre>
            </div>
          )}
          {entry.requestBody && (
            <div className="http-log-block">
              <div className="http-log-block-title">Corpo da requisição</div>
              <pre className="http-log-pre">{entry.requestBody}</pre>
            </div>
          )}
          {entry.responseBody && (
            <div className="http-log-block">
              <div className="http-log-block-title">Corpo da resposta</div>
              <pre className="http-log-pre">{entry.responseBody}</pre>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export function HttpLogViewer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const logs = useHttpLogs()
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const toggle = useCallback((id: string) => {
    setExpandedId((cur) => (cur === id ? null : id))
  }, [])

  const handleClear = useCallback(() => {
    clearHttpLogs()
    setExpandedId(null)
  }, [])

  if (!open) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal--http-log" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">Logs HTTP</div>
        <div className="modal-body http-log-modal-body">
          {logs.length === 0 ? (
            <p className="http-log-empty">Nenhuma requisição registrada nesta sessão.</p>
          ) : (
            <div className="http-log-list">
              {logs.map((entry) => (
                <LogRow
                  key={entry.id}
                  entry={entry}
                  expanded={expandedId === entry.id}
                  onToggle={() => toggle(entry.id)}
                />
              ))}
            </div>
          )}
        </div>
        <div className="modal-footer">
          <button type="button" className="btn" onClick={handleClear} disabled={logs.length === 0}>
            Limpar
          </button>
          <button type="button" className="btn primary" onClick={onClose}>
            Fechar
          </button>
        </div>
      </div>
    </div>
  )
}

export function HttpLogTriggerButton({ onClick }: { onClick: () => void }) {
  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={onClick}
      title="Visualizar logs HTTP"
      aria-label="Visualizar logs HTTP"
    >
      <ScrollText size={16} />
    </button>
  )
}
