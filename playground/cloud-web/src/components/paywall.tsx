import { Effect } from 'effect'
import type { SessionUser } from '../domain/models'
import { OutletApi, useEffectFn } from '../runtime'
import { Button } from './ui/button'

export function Paywall({ user, onUpgraded, onLogout }: { user: SessionUser; onUpgraded: () => void; onLogout: () => void }) {
  const subscribe = useEffectFn(() => Effect.flatMap(OutletApi, (api) => api.subscribe()))

  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-6 p-6 text-center">
      <div className="bg-primary text-primary-foreground flex size-10 items-center justify-center rounded-md font-bold">O</div>
      <div className="max-w-md">
        <h1 className="text-2xl font-bold">Outlet Cloud is a Pro plan</h1>
        <p className="text-muted-foreground mt-2 text-sm text-balance">
          Organizations, members and private registries live in Outlet Cloud (Pro). The public registry stays free — pull it any
          time with the <code>outlet</code> CLI.
        </p>
      </div>
      <div className="flex w-full max-w-xs flex-col gap-2">
        <Button
          disabled={subscribe.running}
          onClick={async () => {
            try {
              await subscribe.run()
              onUpgraded()
            } catch {
              // subscribe.error rendered below
            }
          }}
        >
          {subscribe.running ? 'Upgrading…' : 'Upgrade to Pro'}
        </Button>
        <Button variant="outline" onClick={onLogout}>Log out</Button>
        {subscribe.error && <p className="text-destructive text-sm">{subscribe.error.message}</p>}
      </div>
      <p className="text-muted-foreground text-xs">Signed in as {user.email}</p>
    </div>
  )
}
