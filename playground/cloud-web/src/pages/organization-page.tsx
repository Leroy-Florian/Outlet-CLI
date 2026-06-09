import { Effect } from 'effect'
import { useState } from 'react'
import type { Role } from '../domain/models'
import { navigate } from '../router'
import { OutletApi, useEffectFn, useEffectQuery } from '../runtime'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'

const ROLES: Role[] = ['Owner', 'Admin', 'Member']
const selectClass =
  'h-9 rounded-md border border-input bg-transparent px-2 text-sm outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50'

export function OrganizationPage({ organizationId, currentUserId }: { organizationId: string; currentUserId: string }) {
  const detail = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.getOrganization(organizationId)), [organizationId])
  const tokens = useEffectQuery(() => Effect.flatMap(OutletApi, (api) => api.listTokens(organizationId)), [organizationId])

  const addMember = useEffectFn((email: string, role: Role) => Effect.flatMap(OutletApi, (api) => api.addMember(organizationId, email, role)))
  const changeRole = useEffectFn((userId: string, role: Role) => Effect.flatMap(OutletApi, (api) => api.changeRole(organizationId, userId, role)))
  const removeMember = useEffectFn((userId: string) => Effect.flatMap(OutletApi, (api) => api.removeMember(organizationId, userId)))
  const issueToken = useEffectFn((name: string) => Effect.flatMap(OutletApi, (api) => api.issueToken(organizationId, name)))
  const revokeToken = useEffectFn((tokenId: string) => Effect.flatMap(OutletApi, (api) => api.revokeToken(organizationId, tokenId)))

  const [error, setError] = useState<string | null>(null)
  const [secret, setSecret] = useState<string | null>(null)
  const [memberEmail, setMemberEmail] = useState('')
  const [memberRole, setMemberRole] = useState<Role>('Member')
  const [tokenName, setTokenName] = useState('')

  async function guard(action: Promise<unknown>, after?: () => void) {
    setError(null)
    try {
      await action
      after?.()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Action failed.')
    }
  }

  if (detail.loading && detail.data === null) {
    return <p className="text-muted-foreground text-sm">Loading…</p>
  }

  const org = detail.data
  if (org === null) {
    return <p className="text-destructive text-sm">{detail.error?.message ?? 'Failed to load organization.'}</p>
  }

  const canManage = org.role === 'Owner' || org.role === 'Admin'

  return (
    <div className="flex flex-col gap-8">
      <div>
        <button onClick={() => navigate('/')} className="text-muted-foreground text-sm hover:underline">← Organizations</button>
        <h1 className="mt-2 text-2xl font-bold">{org.name}</h1>
        <p className="text-muted-foreground text-sm">
          /{org.slug} · you are <span className="font-medium">{org.role}</span>
        </p>
        <nav className="mt-3 flex gap-4 border-b pb-2 text-sm">
          <span className="font-medium">Members</span>
          <button onClick={() => navigate(`/orgs/${organizationId}/registry`)} className="text-muted-foreground hover:underline">
            Registry
          </button>
        </nav>
      </div>

      {error && <p className="text-destructive text-sm">{error}</p>}

      <section className="flex flex-col gap-3">
        <h2 className="font-semibold">Members</h2>
        <ul className="flex flex-col gap-2">
          {org.members.map((member) => (
            <li key={member.userId} className="flex items-center justify-between rounded-md border px-3 py-2">
              <div className="text-sm">
                <span className="font-medium">{member.displayName ?? member.email}</span>{' '}
                <span className="text-muted-foreground">{member.email}</span>
              </div>
              <div className="flex items-center gap-2">
                {canManage && member.userId !== currentUserId ? (
                  <>
                    <select
                      className={selectClass}
                      value={member.role}
                      onChange={(e) => void guard(changeRole.run(member.userId, e.target.value as Role), () => detail.refresh())}
                    >
                      {ROLES.map((role) => (
                        <option key={role} value={role}>{role}</option>
                      ))}
                    </select>
                    <Button variant="outline" className="w-auto px-2" onClick={() => void guard(removeMember.run(member.userId), () => detail.refresh())}>
                      Remove
                    </Button>
                  </>
                ) : (
                  <span className="text-muted-foreground text-sm">
                    {member.role}
                    {member.userId === currentUserId ? ' (you)' : ''}
                  </span>
                )}
              </div>
            </li>
          ))}
        </ul>

        {canManage && (
          <form
            className="flex gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              void guard(addMember.run(memberEmail, memberRole), () => {
                setMemberEmail('')
                detail.refresh()
              })
            }}
          >
            <Input placeholder="email to add" type="email" value={memberEmail} onChange={(e) => setMemberEmail(e.target.value)} required />
            <select className={selectClass} value={memberRole} onChange={(e) => setMemberRole(e.target.value as Role)}>
              {ROLES.map((role) => (
                <option key={role} value={role}>{role}</option>
              ))}
            </select>
            <Button type="submit" className="w-auto px-3">Add</Button>
          </form>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="font-semibold">Personal access tokens</h2>

        {secret && (
          <div className="border-primary bg-muted rounded-md border p-3 text-sm">
            <p className="font-medium">Copy your token now — it won&apos;t be shown again:</p>
            <code className="mt-1 block break-all">{secret}</code>
          </div>
        )}

        <ul className="flex flex-col gap-2">
          {(tokens.data ?? []).map((token) => (
            <li key={token.tokenId} className="flex items-center justify-between rounded-md border px-3 py-2">
              <div className="text-sm">
                <span className="font-medium">{token.name}</span>{' '}
                <span className="text-muted-foreground">{token.revoked ? 'revoked' : 'active'}</span>
              </div>
              {!token.revoked && (
                <Button variant="outline" className="w-auto px-2" onClick={() => void guard(revokeToken.run(token.tokenId), () => tokens.refresh())}>
                  Revoke
                </Button>
              )}
            </li>
          ))}
          {(tokens.data ?? []).length === 0 && <p className="text-muted-foreground text-sm">No tokens yet.</p>}
        </ul>

        <form
          className="flex gap-2"
          onSubmit={(e) => {
            e.preventDefault()
            setError(null)
            issueToken
              .run(tokenName)
              .then((token) => {
                setTokenName('')
                setSecret(token.secret)
                tokens.refresh()
              })
              .catch((caught: unknown) => setError(caught instanceof Error ? caught.message : 'Failed to generate token.'))
          }}
        >
          <Input placeholder="token name (e.g. ci)" value={tokenName} onChange={(e) => setTokenName(e.target.value)} required />
          <Button type="submit" className="w-auto px-3">Generate</Button>
        </form>
      </section>
    </div>
  )
}
