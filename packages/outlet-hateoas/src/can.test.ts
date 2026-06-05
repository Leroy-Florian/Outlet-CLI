import { describe, expect, it } from 'vitest'
import { actionFor, can } from './can'
import type { HateoasResource } from './types'

interface Player {
  name: string
}

const playerWithActions: HateoasResource<Player> = {
  name: 'Florian',
  _self: { href: '/api/players/florian', method: 'GET' },
  _actions: {
    claim: { href: '/api/players/florian/claim', method: 'POST' },
    'run-stats': { href: '/api/players/florian/run-stats', method: 'GET' },
  },
}

describe('can', () => {
  it('returns true when the rel is present in _actions', () => {
    expect(can(playerWithActions, 'claim')).toBe(true)
    expect(can(playerWithActions, 'run-stats')).toBe(true)
  })

  it('returns false when the rel is absent', () => {
    expect(can(playerWithActions, 'delete')).toBe(false)
  })

  it('returns false when the resource is null or undefined', () => {
    expect(can<Player>(null, 'claim')).toBe(false)
    expect(can<Player>(undefined, 'claim')).toBe(false)
  })

  it('does not match inherited object properties', () => {
    expect(can(playerWithActions, 'toString')).toBe(false)
    expect(can(playerWithActions, 'hasOwnProperty')).toBe(false)
  })
})

describe('actionFor', () => {
  it('returns the action descriptor when the rel is present', () => {
    expect(actionFor(playerWithActions, 'claim')).toEqual({
      href: '/api/players/florian/claim',
      method: 'POST',
    })
  })

  it('returns null when the rel is absent', () => {
    expect(actionFor(playerWithActions, 'delete')).toBeNull()
  })

  it('returns null when the resource is null or undefined', () => {
    expect(actionFor<Player>(null, 'claim')).toBeNull()
    expect(actionFor<Player>(undefined, 'claim')).toBeNull()
  })
})
