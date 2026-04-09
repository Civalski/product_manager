import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const isProdBuild = command === 'build' && mode !== 'development'

  if (isProdBuild) {
    const missing = ['VITE_API_BASE_URL', 'VITE_TURNSTILE_SITE_KEY'].filter(
      (name) => !env[name]?.trim(),
    )
    if (missing.length > 0) {
      throw new Error(
        `Build de produção bloqueado. Defina as variáveis obrigatórias: ${missing.join(', ')}`,
      )
    }
  }

  return {
    plugins: [react()],
    // Evita duas cópias de React (lucide-react usa hooks no Icon); sem isto, ícones quebram e o app pode falhar.
    resolve: {
      dedupe: ['react', 'react-dom'],
    },
    logLevel: 'warn',
    server: {
      proxy: {
        // No dev, requisições relativas /api/* vão para a API e evitam CORS.
        '/api': {
          target: 'http://127.0.0.1:5127',
          changeOrigin: true,
        },
      },
    },
  }
})
