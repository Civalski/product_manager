import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
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
})
