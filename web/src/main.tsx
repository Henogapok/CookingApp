import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import './app/styles/global.css'
import { App } from './app/App.tsx'
import { AuthProvider } from './app/auth/AuthProvider.tsx'
import { connectApiClientToAuth } from './app/auth/userManager.ts'
import { queryClient } from './app/query/queryClient.ts'

connectApiClientToAuth()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </AuthProvider>
  </StrictMode>,
)
