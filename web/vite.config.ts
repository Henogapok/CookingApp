import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // PWA: манифест (установка на домашний экран как приложение) и Service Worker (мгновенный запуск,
    // оболочка без сети, тихое обновление). В dev Service Worker выключен — иначе мешал бы hot reload.
    VitePWA({
      // Новая версия скачивается в фоне и включается при следующем открытии — без кнопки «Обновить».
      registerType: 'autoUpdate',
      includeAssets: ['favicon.ico', 'icon.svg', 'apple-touch-icon-180x180.png'],
      manifest: {
        name: 'Cooking — рецепты',
        short_name: 'Cooking',
        description: 'Рецепты семьи: КБЖУ, стоимость, пересчёт порций',
        lang: 'ru',
        start_url: '/',
        scope: '/',
        // standalone — без адресной строки, как отдельное приложение.
        display: 'standalone',
        // Фон экрана запуска и цвет панели — «Шалфей», тема по умолчанию.
        background_color: '#e9eee4',
        theme_color: '#2f5d43',
        icons: [
          { src: 'pwa-64x64.png', sizes: '64x64', type: 'image/png' },
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
          { src: 'maskable-icon-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // Любой адрес приложения (/recipes/…, /auth/callback) без сети открывается из кэша: роутер разберётся сам.
        navigateFallback: '/index.html',
        // Кроме Api: его ответы кэширует TanStack Query, Service Worker их не трогает — всегда сеть.
        navigateFallbackDenylist: [/^\/api\//],
      },
    }),
  ],
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
