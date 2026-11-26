import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi, type RegisterInput } from '../api/authApi'
import { onUnauthorized } from '../api/http'
import type { AuthResponse } from '../api/types'
import { AuthContext, type AuthContextValue } from './authContext'
import { sessionStore } from './sessionStore'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<AuthResponse | null>(() => sessionStore.load())

  const start = useCallback((next: AuthResponse) => {
    sessionStore.save(next)
    setSession(next)
  }, [])

  const logout = useCallback(() => {
    sessionStore.clear()
    setSession(null)
    // Never show one user's cached tasks to the next user on this browser.
    queryClient.clear()
  }, [queryClient])

  // A 401 on any request means the token is no longer accepted.
  useEffect(() => onUnauthorized(logout), [logout])

  // Log out as soon as the token expires, not on the next failed request.
  useEffect(() => {
    if (!session) return
    const msLeft = new Date(session.expiresAt).getTime() - Date.now()
    const timer = window.setTimeout(logout, Math.max(msLeft, 0))
    return () => window.clearTimeout(timer)
  }, [session, logout])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      login: async (username, password) => start(await authApi.login(username, password)),
      register: async (input: RegisterInput) => start(await authApi.register(input)),
      logout,
    }),
    [session, start, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
