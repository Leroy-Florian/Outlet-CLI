import { Redacted } from 'effect'
import type { HateoasAction } from './types'

export interface FollowInit {
  body?: unknown
  token?: string | Redacted.Redacted<string> | null
  headers?: Record<string, string>
  signal?: AbortSignal
}

export async function follow(action: HateoasAction, init?: FollowInit): Promise<Response> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
    ...(init?.headers ?? {}),
  }

  if (init?.token) {
    const revealed = Redacted.isRedacted(init.token) ? Redacted.value(init.token) : init.token
    if (revealed) headers.Authorization = `Bearer ${revealed}`
  }

  let body: BodyInit | undefined
  if (init?.body !== undefined) {
    const contentType = action.accepts ?? 'application/json'
    headers['Content-Type'] = contentType
    body = contentType.includes('json')
      ? JSON.stringify(init.body)
      : (init.body as BodyInit)
  }

  return fetch(action.href, {
    method: action.method,
    headers,
    body,
    signal: init?.signal,
  })
}
