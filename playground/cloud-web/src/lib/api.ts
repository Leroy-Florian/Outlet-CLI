export type Role = 'Owner' | 'Admin' | 'Member'

export type OrgSummary = { organizationId: string; slug: string; name: string; role: Role }
export type Member = { userId: string; email: string | null; displayName: string | null; role: Role }
export type OrgDetail = { organizationId: string; slug: string; name: string; role: Role; members: Member[] }
export type TokenSummary = { tokenId: string; name: string; createdAtUtc: string; expiresAtUtc: string | null; revoked: boolean }
export type IssuedToken = { tokenId: string; secret: string; name: string; expiresAtUtc: string | null }

async function errorText(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { error?: string }
    return body.error ?? `Request failed (${response.status}).`
  } catch {
    return `Request failed (${response.status}).`
  }
}

function init(method: string, body?: unknown): RequestInit {
  return {
    method,
    credentials: 'include',
    headers: body === undefined ? undefined : { 'content-type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  }
}

async function send(path: string, options: RequestInit): Promise<void> {
  const response = await fetch(path, options)
  if (!response.ok) throw new Error(await errorText(response))
}

async function read<T>(path: string, options: RequestInit): Promise<T> {
  const response = await fetch(path, options)
  if (!response.ok) throw new Error(await errorText(response))
  return (await response.json()) as T
}

export const api = {
  logout: () => send('/api/auth/logout', init('POST')),
  listOrganizations: () => read<OrgSummary[]>('/api/organizations', init('GET')),
  createOrganization: (slug: string, name: string) => send('/api/organizations', init('POST', { slug, name })),
  getOrganization: (id: string) => read<OrgDetail>(`/api/organizations/${id}`, init('GET')),
  addMember: (id: string, email: string, role: Role) => send(`/api/organizations/${id}/members`, init('POST', { email, role })),
  changeRole: (id: string, userId: string, role: Role) => send(`/api/organizations/${id}/members/${userId}`, init('PUT', { role })),
  removeMember: (id: string, userId: string) => send(`/api/organizations/${id}/members/${userId}`, init('DELETE')),
  listTokens: (id: string) => read<TokenSummary[]>(`/api/organizations/${id}/tokens`, init('GET')),
  createToken: (id: string, name: string) => read<IssuedToken>(`/api/organizations/${id}/tokens`, init('POST', { name })),
  revokeToken: (id: string, tokenId: string) => send(`/api/organizations/${id}/tokens/${tokenId}`, init('DELETE')),
}
