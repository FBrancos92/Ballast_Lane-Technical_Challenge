import type { AuthResponse } from '../api/types'

const KEY = 'taskmanager.session'

/**
 * Persists the JWT so a page refresh keeps you logged in.
 * Trade-off: localStorage can be read by injected scripts (XSS). A production app would
 * use an httpOnly cookie; this keeps the demo simple and the API stateless.
 */
export const sessionStore = {
  load(): AuthResponse | null {
    const raw = localStorage.getItem(KEY)
    if (!raw) return null
    try {
      const session = JSON.parse(raw) as AuthResponse
      if (!session.token || new Date(session.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(KEY)
        return null
      }
      return session
    } catch {
      localStorage.removeItem(KEY)
      return null
    }
  },

  save(session: AuthResponse): void {
    localStorage.setItem(KEY, JSON.stringify(session))
  },

  clear(): void {
    localStorage.removeItem(KEY)
  },
}
