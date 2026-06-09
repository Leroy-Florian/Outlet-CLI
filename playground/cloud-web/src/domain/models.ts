// Domain model — pure, framework-free types shared across the hexagon.

export type Role = 'Owner' | 'Admin' | 'Member'

export type Plan = 'Free' | 'Pro'

export interface SessionUser {
  readonly userId: string
  readonly email: string
  readonly displayName: string
  readonly plan: Plan
}

export interface OrgSummary {
  readonly organizationId: string
  readonly slug: string
  readonly name: string
  readonly role: Role
}

export interface Member {
  readonly userId: string
  readonly email: string | null
  readonly displayName: string | null
  readonly role: Role
}

export interface OrgDetail {
  readonly organizationId: string
  readonly slug: string
  readonly name: string
  readonly role: Role
  readonly members: ReadonlyArray<Member>
}

export interface TokenSummary {
  readonly tokenId: string
  readonly name: string
  readonly createdAtUtc: string
  readonly expiresAtUtc: string | null
  readonly revoked: boolean
}

export interface IssuedToken {
  readonly tokenId: string
  readonly secret: string
  readonly name: string
  readonly expiresAtUtc: string | null
}

export interface PublishedSummary {
  readonly name: string
  readonly fileCount: number
}

export interface PublishInput {
  readonly name: string
  readonly manifest: Record<string, unknown>
  readonly files: ReadonlyArray<{ readonly path: string; readonly content: string }>
}
