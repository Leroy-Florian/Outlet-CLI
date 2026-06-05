import { Redacted } from 'effect'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { follow } from './follow'
import type { HateoasAction } from './types'

const getAction: HateoasAction = { href: '/api/items', method: 'GET' }
const postAction: HateoasAction = { href: '/api/items/claim', method: 'POST' }

function stubFetch() {
  const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 200 }))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('follow', () => {
  it('uses the method and href from the action descriptor', async () => {
    const fetchMock = stubFetch()

    await follow(getAction)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/items',
      expect.objectContaining({ method: 'GET' }),
    )
  })

  it('always sends an Accept: application/json header', async () => {
    const fetchMock = stubFetch()

    await follow(getAction)

    const init = fetchMock.mock.calls[0]![1] as RequestInit
    expect(init.headers).toMatchObject({ Accept: 'application/json' })
  })

  it('adds a bearer token from a plain string', async () => {
    const fetchMock = stubFetch()

    await follow(getAction, { token: 'secret-token' })

    const init = fetchMock.mock.calls[0]![1] as RequestInit
    expect(init.headers).toMatchObject({ Authorization: 'Bearer secret-token' })
  })

  it('adds a bearer token from a Redacted<string>', async () => {
    const fetchMock = stubFetch()

    await follow(getAction, { token: Redacted.make('hidden-token') })

    const init = fetchMock.mock.calls[0]![1] as RequestInit
    expect(init.headers).toMatchObject({ Authorization: 'Bearer hidden-token' })
  })

  it('sends no Authorization header when token is null', async () => {
    const fetchMock = stubFetch()

    await follow(getAction, { token: null })

    const init = fetchMock.mock.calls[0]![1] as RequestInit
    expect(init.headers).not.toHaveProperty('Authorization')
  })

  it('serializes the body as JSON with Content-Type by default', async () => {
    const fetchMock = stubFetch()

    await follow(postAction, { body: { realm: 'area-52' } })

    const init = fetchMock.mock.calls[0]![1] as RequestInit
    expect(init.method).toBe('POST')
    expect(init.headers).toMatchObject({ 'Content-Type': 'application/json' })
    expect(init.body).toBe('{"realm":"area-52"}')
  })

  it('passes the body through untouched for non-JSON accepts', async () => {
    const fetchMock = stubFetch()
    const action: HateoasAction = { ...postAction, accepts: 'text/plain' }

    await follow(action, { body: 'raw payload' })

    const init = fetchMock.mock.calls[0]![1] as RequestInit
    expect(init.headers).toMatchObject({ 'Content-Type': 'text/plain' })
    expect(init.body).toBe('raw payload')
  })

  it('forwards the abort signal', async () => {
    const fetchMock = stubFetch()
    const controller = new AbortController()

    await follow(getAction, { signal: controller.signal })

    const init = fetchMock.mock.calls[0]![1] as RequestInit
    expect(init.signal).toBe(controller.signal)
  })
})
