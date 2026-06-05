import { describe, expect, it } from 'vitest'
import { RemoteData } from './RemoteData'

describe('RemoteData constructors', () => {
  it('builds each variant with the right tag', () => {
    expect(RemoteData.idle()._tag).toBe('Idle')
    expect(RemoteData.loading()._tag).toBe('Loading')
    expect(RemoteData.loaded(42)).toEqual({ _tag: 'Loaded', value: 42 })
    expect(RemoteData.failed('boom')).toEqual({ _tag: 'Failed', error: 'boom' })
  })
})

describe('RemoteData.fromQuery', () => {
  it('maps loading state to Loading', () => {
    const rd = RemoteData.fromQuery({ data: null, error: null, loading: true })

    expect(rd._tag).toBe('Loading')
  })

  it('maps an error to Failed (error wins over data)', () => {
    const rd = RemoteData.fromQuery({ data: 1, error: 'boom', loading: false })

    expect(rd).toEqual({ _tag: 'Failed', error: 'boom' })
  })

  it('maps data to Loaded', () => {
    const rd = RemoteData.fromQuery({ data: 'hello', error: null, loading: false })

    expect(rd).toEqual({ _tag: 'Loaded', value: 'hello' })
  })

  it('maps the empty state to Idle', () => {
    const rd = RemoteData.fromQuery({ data: null, error: null, loading: false })

    expect(rd._tag).toBe('Idle')
  })
})

describe('RemoteData.match', () => {
  const handlers = {
    onIdle: () => 'idle',
    onLoading: () => 'loading',
    onLoaded: (value: number) => `loaded:${value}`,
    onFailed: (error: string) => `failed:${error}`,
  }

  it('dispatches to the handler matching the tag', () => {
    expect(RemoteData.match(RemoteData.idle<string, number>(), handlers)).toBe('idle')
    expect(RemoteData.match(RemoteData.loading<string, number>(), handlers)).toBe('loading')
    expect(RemoteData.match(RemoteData.loaded<string, number>(7), handlers)).toBe('loaded:7')
    expect(RemoteData.match(RemoteData.failed<string, number>('x'), handlers)).toBe('failed:x')
  })
})
