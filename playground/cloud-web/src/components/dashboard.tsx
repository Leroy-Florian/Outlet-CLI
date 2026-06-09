import type { SessionUser } from '../App'
import { Button } from './ui/button'

/** The logged-in view: proof the SSO session is established. */
export function Dashboard({ user, onLogout }: { user: SessionUser; onLogout: () => Promise<void> | void }) {
  return (
    <div className="min-h-svh">
      <header className="flex items-center justify-between border-b px-6 py-4">
        <div className="flex items-center gap-2 font-medium">
          <div className="bg-primary text-primary-foreground flex size-6 items-center justify-center rounded-md text-xs">O</div>
          Outlet Cloud
        </div>
        <div className="flex items-center gap-3 text-sm">
          <span className="text-muted-foreground">{user.email}</span>
          <Button variant="outline" className="w-auto px-3" onClick={() => void onLogout()}>
            Log out
          </Button>
        </div>
      </header>

      <main className="mx-auto max-w-3xl p-6 md:p-10">
        <h1 className="text-2xl font-bold">Welcome back, {user.displayName}.</h1>
        <p className="text-muted-foreground mt-1 text-sm">
          You are signed in to Outlet Cloud. Manage your organizations and personal access tokens below.
        </p>

        <div className="mt-8 grid gap-4 sm:grid-cols-2">
          <section className="rounded-lg border p-5">
            <h2 className="font-medium">Organizations</h2>
            <p className="text-muted-foreground mt-1 text-sm">
              Create an organization to host a private registry and invite members.
            </p>
            <Button className="mt-4 w-auto px-3">New organization</Button>
          </section>

          <section className="rounded-lg border p-5">
            <h2 className="font-medium">Personal access tokens</h2>
            <p className="text-muted-foreground mt-1 text-sm">
              Generate a scoped token for the CLI / CI to pull your private registry.
            </p>
            <Button variant="outline" className="mt-4 w-auto px-3">Generate token</Button>
          </section>
        </div>
      </main>
    </div>
  )
}
