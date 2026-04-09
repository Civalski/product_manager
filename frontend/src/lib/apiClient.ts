import type { ProblemDetails } from '../types/product'
import { appendHttpLog } from './httpLog'
import { sanitizeHttpLogBodyPreview } from './sanitizeHttpLogBody'

import { getStoredToken } from './authStorage'

const configuredBaseUrl = ((import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '').trim()

if (import.meta.env.PROD && configuredBaseUrl === '') {
  throw new Error('VITE_API_BASE_URL é obrigatório no build de produção.')
}

const baseUrl = () => configuredBaseUrl

const BODY_PREVIEW_MAX = 4000

function truncateBody(s: string): string {
  if (s.length <= BODY_PREVIEW_MAX) return s
  return `${s.slice(0, BODY_PREVIEW_MAX)}…`
}

function requestBodyPreview(body: RequestInit['body']): string | undefined {
  if (body == null || body === '') return undefined
  if (typeof body === 'string') return truncateBody(body)
  return '[corpo não textual]'
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(message: string, status: number, problem: ProblemDetails | null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

function parseProblem(data: unknown): ProblemDetails | null {
  if (!data || typeof data !== 'object') return null
  return data as ProblemDetails
}

export function formatApiErrors(problem: ProblemDetails | null): string {
  if (!problem) return 'Erro desconhecido.'
  if (problem.detail) return problem.detail
  if (problem.errors) {
    const lines = Object.entries(problem.errors).flatMap(([key, msgs]) =>
      msgs.map((m) => `${key}: ${m}`),
    )
    if (lines.length) return lines.join('\n')
  }
  if (problem.title) return problem.title
  return 'Solicitação inválida.'
}

/** Mensagem para exibir ao usuário (rede, ApiError ou genérico). */
export function getApiErrorMessage(e: unknown): string {
  if (e instanceof ApiError) {
    if (e.status === 429) {
      return 'Demasiadas tentativas. Aguarde cerca de um minuto e tente novamente.'
    }
    if (e.problem) return formatApiErrors(e.problem)
    return e.message
  }
  if (e instanceof Error) return e.message
  return 'Erro inesperado.'
}

export async function apiJson<T>(
  path: string,
  init?: RequestInit,
): Promise<T | undefined> {
  const url = `${baseUrl()}${path}`
  const method = (init?.method ?? 'GET').toUpperCase()
  const token = getStoredToken()
  const headers: HeadersInit = {
    Accept: 'application/json',
    ...(init?.headers ?? {}),
  }
  const skipAuth = path.startsWith('/api/auth/')
  if (token && !skipAuth && !(init?.headers as Record<string, string>)?.Authorization) {
    ;(headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
  }
  if (init?.body !== undefined && !(init.headers as Record<string, string>)?.['Content-Type']) {
    ;(headers as Record<string, string>)['Content-Type'] = 'application/json'
  }

  const t0 = performance.now()
  const reqPreview = requestBodyPreview(init?.body)

  const pushLog = (opts: { status: number; responseText: string; error?: string }) => {
    appendHttpLog({
      method,
      url,
      status: opts.status,
      durationMs: Math.round(performance.now() - t0),
      requestBody: sanitizeHttpLogBodyPreview(reqPreview),
      responseBody: opts.responseText
        ? sanitizeHttpLogBodyPreview(truncateBody(opts.responseText))
        : undefined,
      error: opts.error,
    })
  }

  let res: Response
  try {
    res = await fetch(url, { ...init, headers })
  } catch (err) {
    const base = baseUrl()
    const hint =
      base === ''
        ? ' Inicie a API (na pasta backend: dotnet run) na porta 5127 e mantenha o Vite em npm run dev.'
        : ` Verifique se a API está acessível em ${base}.`
    const reason = err instanceof Error ? err.message : String(err)
    const msg = `Falha de rede (não foi possível contatar a API).${hint} (${reason})`
    pushLog({ status: 0, responseText: '', error: msg })
    throw new ApiError(msg, 0, null)
  }
  const text = await res.text()
  pushLog({ status: res.status, responseText: text })

  let data: unknown = null
  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      data = { detail: text }
    }
  }

  if (res.status === 204) return undefined

  if (!res.ok) {
    const problem = parseProblem(data)
    const message =
      problem?.detail ??
      problem?.title ??
      (typeof data === 'object' && data && 'title' in data
        ? String((data as { title?: string }).title)
        : res.statusText)
    throw new ApiError(String(message), res.status, problem)
  }

  return data as T
}
