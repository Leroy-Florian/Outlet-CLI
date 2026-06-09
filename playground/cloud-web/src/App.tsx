import { useCallback, useEffect, useState } from 'react'
import { AuthScreen } from './components/auth-screen'
import { Dashboard } from './components/dashboard'

export type SessionUser = { userId: string; email: string; displayName: string }

export function App() {
  const [user, setUser] = useState<SessionUser | null>(null)
  const [ready, setReady] = useState(false)

  const refresh = useCallback(async () => {
    try {
      const response = await fetch('/api/auth/me', { credentials: 'include' })
      setUser(response.ok ? ((await response.json()) as SessionUser) : null)
    } catch {
      setUser(null)
    }
  }, [])

  useEffect(() => {
    void refresh().finally(() => setReady(true))
  }, [refresh])

  async function logout() {
    await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
    setUser(null)
  }

  if (!ready) return null

  return user ? <Dashboard user={user} onLogout={logout} /> : <AuthScreen onAuthenticated={refresh} />
}
