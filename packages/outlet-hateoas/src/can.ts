import type { HateoasAction, HateoasResource } from './types'

export function can<T>(resource: HateoasResource<T> | null | undefined, rel: string): boolean {
  const actions = resource?._actions
  if (!actions) return false
  return Object.hasOwn(actions, rel)
}

export function actionFor<T>(
  resource: HateoasResource<T> | null | undefined,
  rel: string,
): HateoasAction | null {
  const actions = resource?._actions
  if (!actions || !Object.hasOwn(actions, rel)) return null
  return actions[rel] ?? null
}
