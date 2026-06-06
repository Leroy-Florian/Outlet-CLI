import type { Catalog, SendRequest, SendResult } from './types'

const base = '/api'

export async function getCatalog(): Promise<Catalog> {
  const response = await fetch(`${base}/catalog`)
  if (!response.ok) {
    throw new Error(`Failed to load catalog (${response.status})`)
  }
  return (await response.json()) as Catalog
}

export async function sendEmail(request: SendRequest): Promise<SendResult> {
  const response = await fetch(`${base}/email/send`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!response.ok) {
    const body = (await response.json().catch(() => ({ error: response.statusText }))) as { error?: string }
    throw new Error(body.error ?? `Request failed (${response.status})`)
  }
  return (await response.json()) as SendResult
}
