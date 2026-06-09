import { Data } from 'effect'

/** The single failure type crossing the OutletApi port. */
export class OutletError extends Data.TaggedError('OutletError')<{
  readonly status: number
  readonly message: string
}> {}
