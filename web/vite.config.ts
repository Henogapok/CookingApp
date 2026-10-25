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
})
