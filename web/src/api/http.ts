import { sessionStore } from '../auth/sessionStore'

export type FieldErrors = Record<string, string[]>

/** Any non-2xx response, already parsed from the API's RFC 9457 problem details. */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors

  constructor(status: number, message: string, fieldErrors: FieldErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

let unauthorizedHandler: () => void = () => {}

/** Called when an authenticated request gets a 401, for example because the token expired. */
export function onUnauthorized(handler: () => void): () => void {
  unauthorizedHandler = handler
  return () => {
    unauthorizedHandler = () => {}
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
}

export async function request<T>(path: string, { method = 'GET', body, signal }: RequestOptions = {}): Promise<T> {
  const token = sessionStore.load()?.token
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (token) headers.Authorization = `Bearer ${token}`
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    })
  } catch (error) {
    if (signal?.aborted) throw error
    throw new ApiError(0, 'Could not reach the server. Is the API running?')
  }

  if (response.status === 401 && token) unauthorizedHandler()
  if (!response.ok) throw await toApiError(response)
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as { title?: string; detail?: string; errors?: FieldErrors }
    return new ApiError(
      response.status,
      problem.detail ?? problem.title ?? 'Request failed.',
      normalizeFieldErrors(problem.errors),
    )
  } catch {
    return new ApiError(response.status, response.statusText || 'Request failed.')
  }
}

/** "$.dueDate", "DueDate" and "dueDate" all become "dueDate", so forms can look errors up by field name. */
function normalizeFieldErrors(errors: FieldErrors | undefined): FieldErrors {
  const result: FieldErrors = {}
  for (const [key, messages] of Object.entries(errors ?? {})) {
    const clean = key.replace(/^\$\./, '')
    const field = clean.charAt(0).toLowerCase() + clean.slice(1)
    result[field] = [...(result[field] ?? []), ...messages]
  }
  return result
}
