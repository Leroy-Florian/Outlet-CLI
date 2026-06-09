import { Context, type Effect } from 'effect'
import type { OutletError } from '../domain/errors'
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

/**
 * DRIVEN PORT — everything the UI needs from the Outlet Cloud backend, as Effects.
 * The HTTP adapter implements it; the UI depends only on this interface (hexagon).
 */
export interface OutletApiService {
  readonly me: () => Effect.Effect<SessionUser | null, OutletError>
  readonly register: (email: string, password: string, displayName: string) => Effect.Effect<void, OutletError>
  readonly login: (email: string, password: string) => Effect.Effect<void, OutletError>
  readonly logout: () => Effect.Effect<void, OutletError>

  readonly subscribe: () => Effect.Effect<void, OutletError>
  readonly cancelSubscription: () => Effect.Effect<void, OutletError>
  readonly forgotPassword: (email: string) => Effect.Effect<string | null, OutletError>
  readonly resetPassword: (email: string, token: string, newPassword: string) => Effect.Effect<void, OutletError>

  readonly listOrganizations: () => Effect.Effect<ReadonlyArray<OrgSummary>, OutletError>
  readonly createOrganization: (slug: string, name: string) => Effect.Effect<void, OutletError>
  readonly getOrganization: (id: string) => Effect.Effect<OrgDetail, OutletError>

  readonly addMember: (id: string, email: string, role: Role) => Effect.Effect<void, OutletError>
  readonly changeRole: (id: string, userId: string, role: Role) => Effect.Effect<void, OutletError>
  readonly removeMember: (id: string, userId: string) => Effect.Effect<void, OutletError>

  readonly listTokens: (id: string) => Effect.Effect<ReadonlyArray<TokenSummary>, OutletError>
  readonly issueToken: (id: string, name: string) => Effect.Effect<IssuedToken, OutletError>
  readonly revokeToken: (id: string, tokenId: string) => Effect.Effect<void, OutletError>

  readonly listPublished: (id: string) => Effect.Effect<ReadonlyArray<PublishedSummary>, OutletError>
  readonly publish: (id: string, input: PublishInput) => Effect.Effect<void, OutletError>
}

export class OutletApi extends Context.Tag('@outlet/OutletApi')<OutletApi, OutletApiService>() {}
