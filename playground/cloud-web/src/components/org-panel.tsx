import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api, type IssuedToken, type OrgDetail, type Role, type TokenSummary } from '../lib/api'
import { Button } from './ui/button'
import { Input } from './ui/input'

const ROLES: Role[] = ['Owner', 'Admin', 'Member']
const selectClass =
  'h-9 rounded-md border border-input bg-transparent px-2 text-sm outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50'

export function OrgPanel({ organizationId, currentUserId }: { organizationId: string; currentUserId: string }) {
  const [detail, setDetail] = useState<OrgDetail | null>(null)
  const [tokens, setTokens] = useState<TokenSummary[]>([])
  const [issued, setIssued] = useState<IssuedToken | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [memberEmail, setMemberEmail] = useState('')
  const [memberRole, setMemberRole] = useState<Role>('Member')
  const [tokenName, setTokenName] = useState('')

  const reload = useCallback(async () => {
    setDetail(await api.getOrganization(organizationId))
    setTokens(await api.listTokens(organizationId))
  }, [organizationId])

  useEffect(() => {
    setIssued(null)
    setError(null)
    void reload().catch((caught: unknown) =>
      setError(caught instanceof Error ? caught.message : 'Failed to load organization.'),
    )
  }, [reload])

  async function run(action: () => Promise<void>) {
    setError(null)
    try {
      await action()
      await reload()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Action failed.')
    }
  }

  async function onGenerateToken(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    try {
      const token = await api.createToken(organizationId, tokenName)
      setTokenName('')
      setIssued(token)
      setTokens(await api.listTokens(organizationId))
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Failed to generate token.')
    }
  }

  if (!detail) {
    return <p className="text-muted-foreground text-sm">{error ?? 'Loading…'}</p>
  }

  const canManage = detail.role === 'Owner' || detail.role === 'Admin'

  return (
    <div className="flex flex-col gap-8">
      <div>
        <h1 className="text-2xl font-bold">{detail.name}</h1>
        <p className="text-muted-foreground text-sm">
          /{detail.slug} · you are <span className="font-medium">{detail.role}</span>
        </p>
      </div>

      {error && <p className="text-destructive text-sm">{error}</p>}

      <section className="flex flex-col gap-3">
        <h2 className="font-semibold">Members</h2>
        <ul className="flex flex-col gap-2">
          {detail.members.map((member) => (
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
                      onChange={(e) => void run(() => api.changeRole(organizationId, member.userId, e.target.value as Role))}
                    >
                      {ROLES.map((role) => (
                        <option key={role} value={role}>{role}</option>
                      ))}
                    </select>
                    <Button variant="outline" className="w-auto px-2" onClick={() => void run(() => api.removeMember(organizationId, member.userId))}>
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
              void run(() => api.addMember(organizationId, memberEmail, memberRole)).then(() => setMemberEmail(''))
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

        {issued && (
          <div className="border-primary bg-muted rounded-md border p-3 text-sm">
            <p className="font-medium">Copy your token now — it won&apos;t be shown again:</p>
            <code className="mt-1 block break-all">{issued.secret}</code>
          </div>
        )}

        <ul className="flex flex-col gap-2">
          {tokens.map((token) => (
            <li key={token.tokenId} className="flex items-center justify-between rounded-md border px-3 py-2">
              <div className="text-sm">
                <span className="font-medium">{token.name}</span>{' '}
                <span className="text-muted-foreground">{token.revoked ? 'revoked' : 'active'}</span>
              </div>
              {!token.revoked && (
                <Button variant="outline" className="w-auto px-2" onClick={() => void run(() => api.revokeToken(organizationId, token.tokenId))}>
                  Revoke
                </Button>
              )}
            </li>
          ))}
          {tokens.length === 0 && <p className="text-muted-foreground text-sm">No tokens yet.</p>}
        </ul>

        <form className="flex gap-2" onSubmit={onGenerateToken}>
          <Input placeholder="token name (e.g. ci)" value={tokenName} onChange={(e) => setTokenName(e.target.value)} required />
          <Button type="submit" className="w-auto px-3">Generate</Button>
        </form>
      </section>
    </div>
  )
}
