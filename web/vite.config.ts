import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    // '@/entities/recipe' вместо '../../../entities/recipe'. Тот же путь прописан в tsconfig.app.json (paths) — для TypeScript.
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    // Запросы на /api/… dev-сервер пересылает в Api — для браузера это тот же origin (localhost:5173), CORS не нужен.
    // В проде то же самое сделает Caddy. secure: false — dev-сертификат .NET Node.js не доверяет (браузеру он доверен).
    proxy: {
      '/api': {
        target: 'https://localhost:7211',
        secure: false,
      },
    },
  },
})
