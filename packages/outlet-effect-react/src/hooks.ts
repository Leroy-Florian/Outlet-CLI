import { Cause, Effect, Exit, Fiber, type ManagedRuntime } from 'effect'
import { useCallback, useEffect, useRef, useState } from 'react'

export interface EffectHooksOptions<R> {
  readonly runtime: ManagedRuntime.ManagedRuntime<R, never>
  readonly onError?: (error: unknown) => void
}

export interface EffectQueryState<A, E> {
  readonly data: A | null
  readonly error: E | null
  readonly loading: boolean
}

export interface EffectQueryResult<A, E> extends EffectQueryState<A, E> {
  readonly refresh: () => void
}

export interface EffectFnState<E> {
  readonly running: boolean
  readonly error: E | null
}

export interface EffectFnResult<A, E, Args extends ReadonlyArray<unknown>>
  extends EffectFnState<E> {
  readonly run: (...args: Args) => Promise<A>
}

export interface EffectHooks<R> {
  useEffectQuery<A, E>(
    factory: () => Effect.Effect<A, E, R>,
    deps: ReadonlyArray<unknown>,
  ): EffectQueryResult<A, E>

  useEffectFn<A, E, Args extends ReadonlyArray<unknown>>(
    factory: (...args: Args) => Effect.Effect<A, E, R>,
  ): EffectFnResult<A, E, Args>
}

export function makeEffectHooks<R>(options: EffectHooksOptions<R>): EffectHooks<R> {
  const { runtime, onError } = options

  function useEffectQuery<A, E>(
    factory: () => Effect.Effect<A, E, R>,
    deps: ReadonlyArray<unknown>,
  ): EffectQueryResult<A, E> {
    const [state, setState] = useState<EffectQueryState<A, E>>({
      data: null,
      error: null,
      loading: true,
    })
    const [version, setVersion] = useState(0)
    const factoryRef = useRef(factory)
    factoryRef.current = factory

    useEffect(() => {
      let cancelled = false
      setState((prev) => ({ ...prev, loading: true, error: null }))

      const fiber = runtime.runFork(factoryRef.current())

      fiber.addObserver((exit: Exit.Exit<A, E>) => {
        if (cancelled) return
        Exit.match(exit, {
          onFailure: (cause) => {
            if (Cause.isInterruptedOnly(cause)) {
              setState((prev) => ({ ...prev, loading: false }))
              return
            }
            const failure = Cause.failureOption(cause)
            const error =
              failure._tag === 'Some' ? failure.value : (Cause.squash(cause) as E)
            onError?.(error)
            setState({ data: null, loading: false, error })
          },
          onSuccess: (data) => setState({ data, error: null, loading: false }),
        })
      })

      return () => {
        cancelled = true
        Effect.runFork(Fiber.interruptFork(fiber))
      }
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [...deps, version])

    const refresh = useCallback(() => setVersion((v) => v + 1), [])

    return { ...state, refresh }
  }

  function useEffectFn<A, E, Args extends ReadonlyArray<unknown>>(
    factory: (...args: Args) => Effect.Effect<A, E, R>,
  ): EffectFnResult<A, E, Args> {
    const [state, setState] = useState<EffectFnState<E>>({ running: false, error: null })
    const factoryRef = useRef(factory)
    factoryRef.current = factory

    const run = useCallback(async (...args: Args): Promise<A> => {
      setState({ running: true, error: null })
      try {
        const value = await runtime.runPromise(factoryRef.current(...args))
        setState({ running: false, error: null })
        return value
      } catch (cause) {
        const typed = cause as E
        onError?.(typed)
        setState({ running: false, error: typed })
        throw cause
      }
    }, [])

    return { ...state, run }
  }

  return { useEffectQuery, useEffectFn }
}
