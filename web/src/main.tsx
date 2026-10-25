import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './app/styles/global.css'
import { App } from './app/App.tsx'
import { AuthProvider } from './app/auth/AuthProvider.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <App />
    </AuthProvider>
  </StrictMode>,
)
