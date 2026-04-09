import { useCallback, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { ScrollText, ChevronDown, ChevronLeft, ChevronRight } from 'lucide-react'
import { clearHttpLogs, getHttpLogs, subscribeHttpLogs } from '../lib/httpLog'
import type { HttpLogEntry } from '../lib/httpLog'

const LOGS_PER_PAGE = 10

type LogDetailSection = 'error' | 'request' | 'response'

function useHttpLogs() {
  return useSyncExternalStore(subscribeHttpLogs, getHttpLogs, getHttpLogs) as readonly HttpLogEntry[]
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

function LogDetailAccordion({
  section,
  title,
  open,
  onToggle,
  children,
}: {
  section: LogDetailSection
  title: string
  open: boolean
  onToggle: (s: LogDetailSection) => void
  children: ReactNode
}) {
  return (
    <div className="http-log-accordion">
      <button
        type="button"
        className="http-log-accordion-head"
        onClick={() => onToggle(section)}
        aria-expanded={open}
      >
        {open ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
        <span className="http-log-accordion-title">{title}</span>
      </button>
      {open && <div className="http-log-accordion-body">{children}</div>}
    </div>
  )
}

function LogRow({
  entry,
  expanded,
  openSection,
  onToggleRow,
  onToggleSection,
}: {
  entry: HttpLogEntry
  expanded: boolean
  openSection: LogDetailSection | null
  onToggleRow: () => void
  onToggleSection: (s: LogDetailSection) => void
}) {
  return (
    <div className="http-log-row">
      <button type="button" className="http-log-row-head" onClick={onToggleRow}>
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
            <LogDetailAccordion
              section="error"
              title="Erro"
              open={openSection === 'error'}
              onToggle={onToggleSection}
            >
              <pre className="http-log-pre">{entry.error}</pre>
            </LogDetailAccordion>
          )}
          {entry.requestBody && (
            <LogDetailAccordion
              section="request"
              title="Corpo da requisição"
              open={openSection === 'request'}
              onToggle={onToggleSection}
            >
              <pre className="http-log-pre">{entry.requestBody}</pre>
            </LogDetailAccordion>
          )}
          {entry.responseBody && (
            <LogDetailAccordion
              section="response"
              title="Corpo da resposta"
              open={openSection === 'response'}
              onToggle={onToggleSection}
            >
              <pre className="http-log-pre">{entry.responseBody}</pre>
            </LogDetailAccordion>
          )}
        </div>
      )}
    </div>
  )
}

export function HttpLogViewer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const logs = useHttpLogs()
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [detailSection, setDetailSection] = useState<LogDetailSection | null>(null)
  const [page, setPage] = useState(1)

  const totalPages = Math.max(1, Math.ceil(logs.length / LOGS_PER_PAGE))

  const pageNumbers = useMemo(() => {
    if (totalPages <= 7) {
      return Array.from({ length: totalPages }, (_, i) => i + 1)
    }
    const pages = new Set<number>()
    pages.add(1)
    pages.add(totalPages)
    for (let p = page - 1; p <= page + 1; p++) {
      if (p >= 1 && p <= totalPages) pages.add(p)
    }
    return [...pages].sort((a, b) => a - b)
  }, [page, totalPages])

  useEffect(() => {
    if (!open) return
    setPage(1)
    setExpandedId(null)
  }, [open])

  useEffect(() => {
    setExpandedId(null)
  }, [page])

  useEffect(() => {
    if (page > totalPages) setPage(totalPages)
  }, [logs.length, page, totalPages])

  useEffect(() => {
    setDetailSection(null)
  }, [expandedId])

  const toggleRow = useCallback((id: string) => {
    setExpandedId((cur) => (cur === id ? null : id))
  }, [])

  const toggleSection = useCallback((s: LogDetailSection) => {
    setDetailSection((cur) => (cur === s ? null : s))
  }, [])

  const handleClear = useCallback(() => {
    clearHttpLogs()
    setExpandedId(null)
    setDetailSection(null)
    setPage(1)
  }, [])

  const visibleLogs =
    logs.length === 0 ? [] : logs.slice((page - 1) * LOGS_PER_PAGE, page * LOGS_PER_PAGE)
  const rangeStart = logs.length === 0 ? 0 : (page - 1) * LOGS_PER_PAGE + 1
  const rangeEnd = logs.length === 0 ? 0 : Math.min(page * LOGS_PER_PAGE, logs.length)

  if (!open) return null

  return createPortal(
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal--http-log" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">Logs HTTP</div>
        <div className="modal-body http-log-modal-body">
          {logs.length === 0 ? (
            <p className="http-log-empty">Nenhuma requisição registrada nesta sessão.</p>
          ) : (
            <div className="http-log-list">
              {visibleLogs.map((entry) => (
                <LogRow
                  key={entry.id}
                  entry={entry}
                  expanded={expandedId === entry.id}
                  openSection={expandedId === entry.id ? detailSection : null}
                  onToggleRow={() => toggleRow(entry.id)}
                  onToggleSection={toggleSection}
                />
              ))}
            </div>
          )}
        </div>
        <div className="modal-footer modal-footer--http-log">
          {logs.length > 0 ? (
            <div className="http-log-footer-left">
              <span className="pagination-info http-log-footer-info">
                Mostrando {rangeStart}–{rangeEnd} de {logs.length} requisição(ões)
              </span>
              {totalPages > 1 && (
                <div className="pagination-controls http-log-footer-pages">
                  <button
                    type="button"
                    className="pagination-btn"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    aria-label="Página anterior"
                  >
                    <ChevronLeft />
                  </button>
                  {pageNumbers.map((p, idx) => {
                    const prev = pageNumbers[idx - 1]
                    const showEllipsis = idx > 0 && prev !== undefined && p - prev > 1
                    return (
                      <span key={p} className="http-log-pagination-slot">
                        {showEllipsis && (
                          <span className="http-log-pagination-ellipsis" aria-hidden>
                            …
                          </span>
                        )}
                        <button
                          type="button"
                          className={`pagination-btn ${p === page ? 'active' : ''}`}
                          onClick={() => setPage(p)}
                        >
                          {p}
                        </button>
                      </span>
                    )
                  })}
                  <button
                    type="button"
                    className="pagination-btn"
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    aria-label="Próxima página"
                  >
                    <ChevronRight />
                  </button>
                </div>
              )}
            </div>
          ) : (
            <span className="http-log-footer-spacer" aria-hidden />
          )}
          <div className="http-log-footer-actions">
            <button type="button" className="btn" onClick={handleClear} disabled={logs.length === 0}>
              Limpar
            </button>
            <button type="button" className="btn primary" onClick={onClose}>
              Fechar
            </button>
          </div>
        </div>
      </div>
    </div>,
    document.body,
  )
}

export function HttpLogTriggerButton({
  onClick,
  className,
}: {
  onClick: () => void
  className?: string
}) {
  return (
    <button
      type="button"
      className={className ?? 'theme-toggle'}
      onClick={onClick}
      title="Visualizar logs HTTP"
      aria-label="Visualizar logs HTTP"
    >
      <ScrollText size={16} />
    </button>
  )
}
