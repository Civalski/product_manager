const TURNSTILE_SCRIPT = 'https://challenges.cloudflare.com/turnstile/v0/api.js'

export function loadTurnstileScript(): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve()
  if (window.turnstile) return Promise.resolve()

  return new Promise((resolve, reject) => {
    const existing = document.querySelector(`script[src="${TURNSTILE_SCRIPT}"]`) as HTMLScriptElement | null
    if (existing) {
      const err = new Error('Falha ao carregar Turnstile.')
      let pollId = 0
      let timeoutId = 0

      const cleanup = () => {
        window.clearInterval(pollId)
        window.clearTimeout(timeoutId)
        existing.removeEventListener('error', onError)
        existing.removeEventListener('load', onLoad)
      }

      const tryResolve = (): boolean => {
        if (!window.turnstile) return false
        cleanup()
        resolve()
        return true
      }

      const onLoad = () => {
        tryResolve()
      }

      const onError = () => {
        cleanup()
        reject(err)
      }

      if (tryResolve()) return

      existing.addEventListener('load', onLoad)
      existing.addEventListener('error', onError)

      // Script em cache: o evento `load` pode já ter disparado antes dos listeners.
      queueMicrotask(() => {
        tryResolve()
      })

      pollId = window.setInterval(() => {
        tryResolve()
      }, 50)

      timeoutId = window.setTimeout(() => {
        if (!tryResolve()) {
          cleanup()
          reject(err)
        }
      }, 10_000)

      return
    }

    const script = document.createElement('script')
    script.src = TURNSTILE_SCRIPT
    script.async = true
    script.onload = () => resolve()
    script.onerror = () => reject(new Error('Falha ao carregar Turnstile.'))
    document.head.appendChild(script)
  })
}
