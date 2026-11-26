import { describe, expect, it, vi } from 'vitest'
import { sessionStore } from '../auth/sessionStore'
import { ApiError, onUnauthorized, request } from './http'

function mockFetch(response: Response) {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValue(response)
}

function json(body: unknown, status = 200, contentType = 'application/json') {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function signIn() {
  sessionStore.save({
    token: 'abc.def.ghi',
    expiresAt: new Date(Date.now() + 60_000).toISOString(),
    user: { id: '1', username: 'demo', email: 'demo@example.com', createdAt: '' },
  })
}

describe('request', () => {
  it('prefixes /api and sends the bearer token when logged in', async () => {
    signIn()
    const fetchSpy = mockFetch(json([]))

    await request('/tasks')

    const [url, init] = fetchSpy.mock.calls[0]
    expect(url).toBe('/api/tasks')
    expect((init!.headers as Record<string, string>).Authorization).toBe('Bearer abc.def.ghi')
  })

  it('sends no Authorization header when logged out', async () => {
    const fetchSpy = mockFetch(json([]))

    await request('/tasks')

    expect((fetchSpy.mock.calls[0][1]!.headers as Record<string, string>).Authorization).toBeUndefined()
  })

  it('serializes the body as JSON', async () => {
    const fetchSpy = mockFetch(json({ id: '1' }, 201))

    await request('/tasks', { method: 'POST', body: { title: 'x' } })

    const init = fetchSpy.mock.calls[0][1]!
    expect(init.method).toBe('POST')
    expect(init.body).toBe('{"title":"x"}')
    expect((init.headers as Record<string, string>)['Content-Type']).toBe('application/json')
  })

  it('returns undefined for 204 No Content', async () => {
    mockFetch(new Response(null, { status: 204 }))

    await expect(request('/tasks/1', { method: 'DELETE' })).resolves.toBeUndefined()
  })

  it('turns validation problem details into normalized field errors', async () => {
    mockFetch(
      json(
        { title: 'One or more validation errors occurred.', errors: { Title: ['Title is required.'], '$.dueDate': ['Bad date.'] } },
        400,
        'application/problem+json',
      ),
    )

    const error = await request('/tasks', { method: 'POST', body: {} }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      fieldErrors: { title: ['Title is required.'], dueDate: ['Bad date.'] },
    })
  })

  it('uses the problem detail as the error message', async () => {
    mockFetch(json({ title: 'Conflict.', detail: 'That username is already taken.' }, 409))

    await expect(request('/auth/register', { method: 'POST', body: {} })).rejects.toThrow(
      'That username is already taken.',
    )
  })

  it('calls the unauthorized handler on a 401 for an authenticated request', async () => {
    signIn()
    const handler = vi.fn()
    const unsubscribe = onUnauthorized(handler)
    mockFetch(new Response(null, { status: 401 }))

    await expect(request('/tasks')).rejects.toBeInstanceOf(ApiError)
    expect(handler).toHaveBeenCalledOnce()
    unsubscribe()
  })

  it('does not call the unauthorized handler for a failed login', async () => {
    const handler = vi.fn()
    const unsubscribe = onUnauthorized(handler)
    mockFetch(json({ title: 'Unauthorized.', detail: 'Invalid username or password.' }, 401))

    await expect(request('/auth/login', { method: 'POST', body: {} })).rejects.toThrow('Invalid username or password.')
    expect(handler).not.toHaveBeenCalled()
    unsubscribe()
  })

  it('reports a friendly message when the server is unreachable', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'))

    await expect(request('/tasks')).rejects.toMatchObject({ status: 0, message: expect.stringContaining('reach the server') })
  })
})

describe('sessionStore', () => {
  it('drops an expired session', () => {
    sessionStore.save({
      token: 't',
      expiresAt: new Date(Date.now() - 1000).toISOString(),
      user: { id: '1', username: 'u', email: 'e', createdAt: '' },
    })

    expect(sessionStore.load()).toBeNull()
  })

  it('drops corrupted data', () => {
    localStorage.setItem('taskmanager.session', '{not json')

    expect(sessionStore.load()).toBeNull()
  })
})
