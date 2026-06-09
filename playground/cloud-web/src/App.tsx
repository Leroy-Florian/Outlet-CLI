import { Effect } from 'effect'
import { AuthScreen } from './components/auth-screen'
import { Dashboard } from './components/dashboard'
import { OutletApi, useEffectFn, useEffectQuery } from './runtime'

export function App() {
  const session = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.me()), [])
  const logout = useEffectFn(() => Effect.flatMap(OutletApi, (api) => api.logout()))

  if (session.loading) return null

  const user = session.data
  if (user) {
    return (
      <Dashboard
        user={user}
        onLogout={async () => {
          await logout.run().catch(() => undefined)
          session.refresh()
        }}
      />
    )
  }

  return <AuthScreen onAuthenticated={() => session.refresh()} />
}
