import { Effect } from 'effect'
import { AuthScreen } from './components/auth-screen'
import { Paywall } from './components/paywall'
import { Shell } from './components/shell'
import type { SessionUser } from './domain/models'
import { OrganizationPage } from './pages/organization-page'
import { OrganizationsPage } from './pages/organizations-page'
import { RegistryPage } from './pages/registry-page'
import { useHashRoute } from './router'
import { OutletApi, useEffectFn, useEffectQuery } from './runtime'

export function App() {
  const session = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.me()), [])
  const logout = useEffectFn(() => Effect.flatMap(OutletApi, (api) => api.logout()))
  const route = useHashRoute()

  if (session.loading) return null

  const user = session.data

  async function doLogout() {
    await logout.run().catch(() => undefined)
    session.refresh()
  }

  if (!user) {
    return <AuthScreen onAuthenticated={() => session.refresh()} />
  }

  if (user.plan !== 'Pro') {
    return <Paywall user={user} onUpgraded={() => session.refresh()} onLogout={() => void doLogout()} />
  }

  return (
    <Shell user={user} onLogout={() => void doLogout()}>
      {renderRoute(route, user)}
    </Shell>
  )
}

function renderRoute(route: string, user: SessionUser) {
  const match = /^\/orgs\/([^/]+)(\/registry)?$/.exec(route)
  if (match) {
    const organizationId = match[1]
    return match[2]
      ? <RegistryPage organizationId={organizationId} />
      : <OrganizationPage organizationId={organizationId} currentUserId={user.userId} />
  }
  return <OrganizationsPage />
}
