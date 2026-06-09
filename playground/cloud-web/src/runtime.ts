import { makeEffectHooks } from '@outlet/effect-react'
import { ManagedRuntime } from 'effect'
import { OutletApiHttpLive } from './adapters/http-outlet-api'
import { OutletApi } from './ports/OutletApi'

/** The single shared runtime: provides the OutletApi port (HTTP adapter) to the UI. */
export const runtime = ManagedRuntime.make(OutletApiHttpLive)

const hooks = makeEffectHooks({ runtime })

export const useEffectQuery = hooks.useEffectQuery
export const useEffectFn = hooks.useEffectFn
export { OutletApi }
