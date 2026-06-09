import { Effect, Layer } from 'effect'
import { OutletError } from '../domain/errors'
import type {
  IssuedToken,
  OrgDetail,
  OrgSummary,
  PublishedSummary,
  PublishInput,
  Role,
  SessionUser,
  TokenSummary,
} from '../domain/models'
import { OutletApi, type OutletApiService } from '../ports/OutletApi'

const networkError = (cause: unknown): OutletError =>
  new OutletError({ status: 0, message: cause instanceof Error ? cause.message : 'Network error.' })

const fetchEffect = (path: string, init?: RequestInit): Effect.Effect<Response, OutletError> =>
  Effect.tryPromise({
    try: (signal) => fetch(`/api${path}`, { credentials: 'include', signal, ...init }),
    catch: networkError,
  })

const toError = (response: Response): Effect.Effect<OutletError> =>
  Effect.promise(async () => {
    let message = `Request failed (${response.status}).`
    try {
      const body = (await response.json()) as { error?: string }
      if (body.error) message = body.error
    } catch {
      // keep the default message
    }
    return new OutletError({ status: response.status, message })
  })

const ok = (path: string, init?: RequestInit): Effect.Effect<Response, OutletError> =>
  Effect.gen(function* () {
    const response = yield* fetchEffect(path, init)
    if (!response.ok) return yield* Effect.fail(yield* toError(response))
    return response
  })

const readJson = <A>(path: string, init?: RequestInit): Effect.Effect<A, OutletError> =>
  Effect.gen(function* () {
    const response = yield* ok(path, init)
    return (yield* Effect.promise(() => response.json() as Promise<A>))
  })

const jsonInit = (method: string, payload?: unknown): RequestInit => ({
  method,
  headers: payload === undefined ? undefined : { 'content-type': 'application/json' },
  body: payload === undefined ? undefined : JSON.stringify(payload),
})

const mutate = (path: string, method: string, payload?: unknown): Effect.Effect<void, OutletError> =>
  Effect.asVoid(ok(path, jsonInit(method, payload)))

const service: OutletApiService = {
  me: () =>
    Effect.gen(function* () {
      const response = yield* fetchEffect('/auth/me')
      if (response.status === 401) return null
      if (!response.ok) return yield* Effect.fail(yield* toError(response))
      return (yield* Effect.promise(() => response.json() as Promise<SessionUser>))
    }),
  register: (email, password, displayName) => mutate('/auth/register', 'POST', { email, password, displayName }),
  login: (email, password) => mutate('/auth/login', 'POST', { email, password }),
  logout: () => mutate('/auth/logout', 'POST'),

  subscribe: () => mutate('/billing/subscribe', 'POST'),
  cancelSubscription: () => mutate('/billing/cancel', 'POST'),
  forgotPassword: (email) =>
    Effect.map(readJson<{ token: string | null }>('/auth/forgot-password', jsonInit('POST', { email })), (r) => r.token),
  resetPassword: (email, token, newPassword) =>
    mutate('/auth/reset-password', 'POST', { email, token, newPassword }),

  listOrganizations: () => readJson<ReadonlyArray<OrgSummary>>('/organizations'),
  createOrganization: (slug, name) => mutate('/organizations', 'POST', { slug, name }),
  getOrganization: (id) => readJson<OrgDetail>(`/organizations/${id}`),

  addMember: (id, email, role: Role) => mutate(`/organizations/${id}/members`, 'POST', { email, role }),
  changeRole: (id, userId, role: Role) => mutate(`/organizations/${id}/members/${userId}`, 'PUT', { role }),
  removeMember: (id, userId) => mutate(`/organizations/${id}/members/${userId}`, 'DELETE'),

  listTokens: (id) => readJson<ReadonlyArray<TokenSummary>>(`/organizations/${id}/tokens`),
  issueToken: (id, name) => readJson<IssuedToken>(`/organizations/${id}/tokens`, jsonInit('POST', { name })),
  revokeToken: (id, tokenId) => mutate(`/organizations/${id}/tokens/${tokenId}`, 'DELETE'),

  listPublished: (id) => readJson<ReadonlyArray<PublishedSummary>>(`/organizations/${id}/registry/items`),
  publish: (id, input: PublishInput) =>
    mutate(`/organizations/${id}/registry/items`, 'POST', {
      name: input.name,
      manifest: input.manifest,
      files: input.files,
    }),
}

/** SECONDARY ADAPTER — the OutletApi port backed by the Cloud HTTP API (cookie session). */
export const OutletApiHttpLive = Layer.succeed(OutletApi, service)
