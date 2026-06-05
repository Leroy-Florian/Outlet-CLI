export type HateoasMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

export interface HateoasAction {
  href: string
  method: HateoasMethod
  accepts?: string
}

export type HateoasResource<T> = T & {
  _self?: HateoasAction
  _actions: Record<string, HateoasAction>
}
