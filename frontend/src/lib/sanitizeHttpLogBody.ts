/** Chaves cujo valor não deve aparecer no painel de logs HTTP (credenciais, tokens). */
const SENSITIVE_KEY_LOWERCASE = new Set([
  'password',
  'currentpassword',
  'newpassword',
  'confirmpassword',
  'turnstiletoken',
  'pendingtoken',
  'token',
  'accesstoken',
  'refreshtoken',
  'idtoken',
  'secret',
  'clientsecret',
  'authorization',
])

function isSensitiveKey(key: string): boolean {
  const k = key.toLowerCase()
  if (SENSITIVE_KEY_LOWERCASE.has(k)) return true
  if (k.includes('password')) return true
  if (k.endsWith('secret')) return true
  return false
}

const REDACTED = '[omitido]'

function redactDeep(value: unknown): unknown {
  if (value === null || value === undefined) return value
  if (Array.isArray(value)) return value.map(redactDeep)
  if (typeof value === 'object') {
    const o = value as Record<string, unknown>
    const out: Record<string, unknown> = {}
    for (const [key, v] of Object.entries(o)) {
      out[key] = isSensitiveKey(key) ? REDACTED : redactDeep(v)
    }
    return out
  }
  return value
}

/** Reduz padrões JSON comuns quando o parse falha (ex.: corpo quase JSON). */
function redactSensitiveJsonLiterals(text: string): string {
  return text
    .replace(/"password"\s*:\s*"[^"\\]*(?:\\.[^"\\]*)*"/gi, `"password":"${REDACTED}"`)
    .replace(/"turnstileToken"\s*:\s*"[^"\\]*(?:\\.[^"\\]*)*"/gi, `"turnstileToken":"${REDACTED}"`)
    .replace(/"pendingToken"\s*:\s*"[^"\\]*(?:\\.[^"\\]*)*"/gi, `"pendingToken":"${REDACTED}"`)
    .replace(/"token"\s*:\s*"[^"\\]*(?:\\.[^"\\]*)*"/gi, `"token":"${REDACTED}"`)
}

/**
 * Prepara texto de corpo HTTP para o painel de debug: remove segredos de objetos JSON.
 */
export function sanitizeHttpLogBodyPreview(text: string | undefined): string | undefined {
  if (text == null || text === '') return undefined
  const trimmed = text.trim()
  if (!trimmed) return undefined
  try {
    const parsed = JSON.parse(trimmed) as unknown
    return JSON.stringify(redactDeep(parsed))
  } catch {
    return redactSensitiveJsonLiterals(trimmed)
  }
}
