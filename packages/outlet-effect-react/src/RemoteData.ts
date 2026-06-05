import type { EffectQueryState } from './hooks'

export type RemoteData<E, A> =
  | { readonly _tag: 'Idle' }
  | { readonly _tag: 'Loading' }
  | { readonly _tag: 'Loaded'; readonly value: A }
  | { readonly _tag: 'Failed'; readonly error: E }

export const RemoteData = {
  idle: <E, A>(): RemoteData<E, A> => ({ _tag: 'Idle' }),
  loading: <E, A>(): RemoteData<E, A> => ({ _tag: 'Loading' }),
  loaded: <E, A>(value: A): RemoteData<E, A> => ({ _tag: 'Loaded', value }),
  failed: <E, A>(error: E): RemoteData<E, A> => ({ _tag: 'Failed', error }),

  fromQuery: <A, E>(state: EffectQueryState<A, E>): RemoteData<E, A> => {
    if (state.loading) return { _tag: 'Loading' }
    if (state.error !== null) return { _tag: 'Failed', error: state.error }
    if (state.data !== null) return { _tag: 'Loaded', value: state.data }
    return { _tag: 'Idle' }
  },

  match: <E, A, R>(
    rd: RemoteData<E, A>,
    handlers: {
      readonly onIdle: () => R
      readonly onLoading: () => R
      readonly onLoaded: (value: A) => R
      readonly onFailed: (error: E) => R
    },
  ): R => {
    switch (rd._tag) {
      case 'Idle':
        return handlers.onIdle()
      case 'Loading':
        return handlers.onLoading()
      case 'Loaded':
        return handlers.onLoaded(rd.value)
      case 'Failed':
        return handlers.onFailed(rd.error)
    }
  },
} as const
