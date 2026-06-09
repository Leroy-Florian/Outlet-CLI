import { Effect } from 'effect'
import { useState, type FormEvent } from 'react'
import type { SessionUser } from '../domain/models'
import { OutletApi, useEffectFn, useEffectQuery } from '../runtime'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'
import { OrgPanel } from './org-panel'

export function Dashboard({ user, onLogout }: { user: SessionUser; onLogout: () => Promise<void> | void }) {
  const orgs = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.listOrganizations()), [])
  const create = useEffectFn((slug: string, name: string) =>
    Effect.flatMap(OutletApi, (api) => api.createOrganization(slug, name)),
  )

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [slug, setSlug] = useState('')
  const [name, setName] = useState('')

  const list = orgs.data ?? []
  const selected = selectedId ?? list[0]?.organizationId ?? null
  const error = create.error ?? orgs.error

  async function onCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      await create.run(slug, name)
      setSlug('')
      setName('')
      orgs.refresh()
    } catch {
      // create.error is rendered below.
    }
  }

  return (
    <div className="min-h-svh">
      <header className="flex items-center justify-between border-b px-6 py-4">
        <div className="flex items-center gap-2 font-medium">
          <div className="bg-primary text-primary-foreground flex size-6 items-center justify-center rounded-md text-xs">O</div>
          Outlet Cloud
        </div>
        <div className="flex items-center gap-3 text-sm">
          <span className="text-muted-foreground">{user.email}</span>
          <Button variant="outline" className="w-auto px-3" onClick={() => void onLogout()}>Log out</Button>
        </div>
      </header>

      <main className="mx-auto grid max-w-5xl gap-8 p-6 md:grid-cols-[260px_1fr] md:p-10">
        <aside className="flex flex-col gap-4">
          <h2 className="text-sm font-semibold tracking-wide uppercase">Organizations</h2>
          <nav className="flex flex-col gap-1">
            {list.map((org) => (
              <button
                key={org.organizationId}
                onClick={() => setSelectedId(org.organizationId)}
                className={`flex items-center justify-between rounded-md px-3 py-2 text-left text-sm ${org.organizationId === selected ? 'bg-muted font-medium' : 'hover:bg-muted/60'}`}
              >
                {org.name}
                <span className="text-muted-foreground text-xs">{org.role}</span>
              </button>
            ))}
            {list.length === 0 && !orgs.loading && <p className="text-muted-foreground text-sm">No organizations yet.</p>}
          </nav>

          <form onSubmit={onCreate} className="mt-2 flex flex-col gap-2 border-t pt-4">
            <Label htmlFor="slug">New organization</Label>
            <Input id="slug" placeholder="slug (e.g. acme)" value={slug} onChange={(e) => setSlug(e.target.value)} required />
            <Input placeholder="display name" value={name} onChange={(e) => setName(e.target.value)} required />
            <Button type="submit" disabled={create.running}>Create</Button>
          </form>

          {error && <p className="text-destructive text-sm">{error.message}</p>}
        </aside>

        <section>
          {selected ? (
            <OrgPanel key={selected} organizationId={selected} currentUserId={user.userId} />
          ) : (
            <p className="text-muted-foreground text-sm">Select or create an organization to manage members, tokens and your private registry.</p>
          )}
        </section>
      </main>
    </div>
  )
}
