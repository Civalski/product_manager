/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
  readonly VITE_TURNSTILE_SITE_KEY: string
  readonly VITE_ENABLE_HTTP_LOG_VIEWER?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
