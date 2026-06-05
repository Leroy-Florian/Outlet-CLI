import { HttpClient, HttpClientRequest, HttpClientResponse } from '@effect/platform'
import { Effect, Redacted, Schema } from 'effect'
import type { HateoasAction, HateoasMethod } from './types'

export interface FollowEffectInit {
  readonly body?: unknown
  readonly token?: Redacted.Redacted<string> | string | null
}

const buildRequest = (method: HateoasMethod, href: string): HttpClientRequest.HttpClientRequest => {
  switch (method) {
    case 'GET':
      return HttpClientRequest.get(href)
    case 'POST':
      return HttpClientRequest.post(href)
    case 'PUT':
      return HttpClientRequest.put(href)
    case 'PATCH':
      return HttpClientRequest.patch(href)
    case 'DELETE':
      return HttpClientRequest.del(href)
  }
}

const revealToken = (token: FollowEffectInit['token']): string | null => {
  if (!token) return null
  return Redacted.isRedacted(token) ? Redacted.value(token) : token
}

const encodeBody = (
  request: HttpClientRequest.HttpClientRequest,
  body: unknown,
  accepts: string | undefined,
) => {
  const contentType = accepts ?? 'application/json'
  if (contentType.includes('json')) {
    return HttpClientRequest.bodyJson(body)(request)
  }
  return Effect.succeed(
    HttpClientRequest.bodyText(String(body), contentType)(request),
  )
}

/**
 * Effect-flavoured HATEOAS action invocation. Builds the request from the
 * action descriptor (method + href + accepts), injects a bearer token if
 * provided (plain or `Redacted<string>`), executes via `HttpClient` with
 * `filterStatusOk`, and decodes the response body against `responseSchema`.
 *
 * Use `Schema.Unknown` when the caller only cares about success/failure and
 * doesn't need to read the body.
 */
export const followEffect = <A, I>(
  action: HateoasAction,
  responseSchema: Schema.Schema<A, I>,
  init?: FollowEffectInit,
) =>
  Effect.gen(function* () {
    const client = (yield* HttpClient.HttpClient).pipe(HttpClient.filterStatusOk)

    const token = revealToken(init?.token)
    const withAuth = (req: HttpClientRequest.HttpClientRequest) =>
      token ? HttpClientRequest.bearerToken(token)(req) : req

    const baseRequest = withAuth(
      HttpClientRequest.setHeader('Accept', 'application/json')(
        buildRequest(action.method, action.href),
      ),
    )

    const request =
      init?.body === undefined
        ? Effect.succeed(baseRequest)
        : encodeBody(baseRequest, init.body, action.accepts)

    const response = yield* request.pipe(Effect.flatMap(client.execute))
    // 204 No Content / 205 Reset Content have empty bodies — schemaBodyJson would
    // fail trying to parse an empty stream. Decode `undefined` against the schema
    // so callers using `Schema.Unknown` (e.g. a HATEOAS claim where the response
    // body is irrelevant) succeed; structured schemas correctly fail with a
    // ParseError, surfacing the backend contract mismatch.
    if (response.status === 204 || response.status === 205) {
      return yield* Schema.decodeUnknown(responseSchema)(undefined)
    }
    return yield* HttpClientResponse.schemaBodyJson(responseSchema)(response)
  })
