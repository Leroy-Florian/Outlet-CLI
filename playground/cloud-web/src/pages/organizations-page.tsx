import { Effect } from 'effect'
import { useState, type FormEvent } from 'react'
import { navigate } from '../router'
import { OutletApi, useEffectFn, useEffectQuery } from '../runtime'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { Label } from '../components/ui/label'

export function OrganizationsPage() {
  const orgs = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.listOrganizations()), [])
  const create = useEffectFn((slug: string, name: string) => Effect.flatMap(OutletApi, (api) => api.createOrganization(slug, name)))

  const [slug, setSlug] = useState('')
  const [name, setName] = useState('')

  const list = orgs.data ?? []
  const error = create.error ?? orgs.error

  async function onCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      await create.run(slug, name)
      setSlug('')
      setName('')
      orgs.refresh()
    } catch {
      // create.error rendered below
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">Your organizations</h1>

      <ul className="flex flex-col gap-2">
        {list.map((org) => (
          <li key={org.organizationId}>
            <button
              onClick={() => navigate(`/orgs/${org.organizationId}`)}
              className="hover:bg-muted/60 flex w-full items-center justify-between rounded-md border px-4 py-3 text-left"
            >
              <span className="font-medium">{org.name}</span>
              <span className="text-muted-foreground text-sm">/{org.slug} · {org.role}</span>
            </button>
          </li>
        ))}
        {list.length === 0 && !orgs.loading && (
          <p className="text-muted-foreground text-sm">No organizations yet — create your first below.</p>
        )}
      </ul>

      <form onSubmit={onCreate} className="flex max-w-md flex-col gap-2 rounded-md border p-4">
        <Label htmlFor="slug">New organization</Label>
        <Input id="slug" placeholder="slug (e.g. acme)" value={slug} onChange={(e) => setSlug(e.target.value)} required />
        <Input placeholder="display name" value={name} onChange={(e) => setName(e.target.value)} required />
        <Button type="submit" disabled={create.running} className="w-auto px-3">Create</Button>
        {error && <p className="text-destructive text-sm">{error.message}</p>}
      </form>
    </div>
  )
}
