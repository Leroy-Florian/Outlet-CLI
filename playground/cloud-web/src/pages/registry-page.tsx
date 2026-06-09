import { Effect } from 'effect'
import { useState } from 'react'
import { navigate } from '../router'
import { OutletApi, useEffectFn, useEffectQuery } from '../runtime'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'

const fieldClass =
  'min-h-28 rounded-md border border-input bg-transparent px-3 py-2 font-mono text-sm outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50'

export function RegistryPage({ organizationId }: { organizationId: string }) {
  const detail = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.getOrganization(organizationId)), [organizationId])
  const published = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.listPublished(organizationId)), [organizationId])
  const publish = useEffectFn((name: string, path: string, content: string) =>
    Effect.flatMap(OutletApi, (api) =>
      api.publish(organizationId, {
        name,
        manifest: { name, type: 'outlet:adapter', concern: 'misc', targetFrameworks: ['net10.0'], files: [{ path, target: 'adapter' }] },
        files: [{ path, content }],
      }),
    ),
  )

  const [error, setError] = useState<string | null>(null)
  const [itemName, setItemName] = useState('')
  const [filePath, setFilePath] = useState('')
  const [fileContent, setFileContent] = useState('')

  if (detail.loading && detail.data === null) {
    return <p className="text-muted-foreground text-sm">Loading…</p>
  }

  const org = detail.data
  if (org === null) {
    return <p className="text-destructive text-sm">{detail.error?.message ?? 'Failed to load organization.'}</p>
  }

  const canManage = org.role === 'Owner' || org.role === 'Admin'

  return (
    <div className="flex flex-col gap-6">
      <div>
        <button onClick={() => navigate(`/orgs/${organizationId}`)} className="text-muted-foreground text-sm hover:underline">← {org.name}</button>
        <h1 className="mt-2 text-2xl font-bold">Private registry</h1>
        <p className="text-muted-foreground text-sm">/{org.slug}</p>
      </div>

      {error && <p className="text-destructive text-sm">{error}</p>}

      <section className="flex flex-col gap-3">
        <h2 className="font-semibold">Published items</h2>
        <ul className="flex flex-col gap-2">
          {(published.data ?? []).map((item) => (
            <li key={item.name} className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
              <span className="font-medium">{item.name}</span>
              <span className="text-muted-foreground">{item.fileCount} file(s)</span>
            </li>
          ))}
          {(published.data ?? []).length === 0 && <p className="text-muted-foreground text-sm">No items published yet.</p>}
        </ul>
      </section>

      {canManage && (
        <section className="flex flex-col gap-3">
          <h2 className="font-semibold">Publish an item</h2>
          <form
            className="flex flex-col gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              setError(null)
              publish
                .run(itemName, filePath, fileContent)
                .then(() => {
                  setItemName('')
                  setFilePath('')
                  setFileContent('')
                  published.refresh()
                })
                .catch((caught: unknown) => setError(caught instanceof Error ? caught.message : 'Failed to publish.'))
            }}
          >
            <Input placeholder="item name (e.g. email-smtp)" value={itemName} onChange={(e) => setItemName(e.target.value)} required />
            <Input placeholder="file path (e.g. SmtpEmailSender.cs)" value={filePath} onChange={(e) => setFilePath(e.target.value)} required />
            <textarea className={fieldClass} placeholder="// file content" value={fileContent} onChange={(e) => setFileContent(e.target.value)} required />
            <Button type="submit" disabled={publish.running} className="w-auto px-3">Publish</Button>
          </form>
        </section>
      )}

      <p className="text-muted-foreground text-xs">
        Pull from the CLI: point <code>outlet.json</code> at this org&apos;s registry URL with a read-scoped token, then <code>outlet list</code>.
      </p>
    </div>
  )
}
