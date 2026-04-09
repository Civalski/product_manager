export interface HttpLogEntry {
  id: string
  at: string
  method: string
  url: string
  status: number
  durationMs: number
  error?: string
  requestBody?: string
  responseBody?: string
}

const MAX_LOGS = 80
let logs: HttpLogEntry[] = []
const listeners = new Set<() => void>()

function notify() {
  for (const l of listeners) l()
}

export function appendHttpLog(entry: Omit<HttpLogEntry, 'id' | 'at'>) {
  const full: HttpLogEntry = {
    ...entry,
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`,
    at: new Date().toISOString(),
  }
  logs = [full, ...logs].slice(0, MAX_LOGS)
  notify()
}

export function clearHttpLogs() {
  logs = []
  notify()
}

export function subscribeHttpLogs(cb: () => void) {
  listeners.add(cb)
  return () => listeners.delete(cb)
}

export function getHttpLogs(): readonly HttpLogEntry[] {
  return logs
}
