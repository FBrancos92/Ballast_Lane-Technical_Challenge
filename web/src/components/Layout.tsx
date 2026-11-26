import type { ReactNode } from 'react'
import { useAuth } from '../auth/useAuth'

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="app-header">
        <span className="brand">✓ Task Manager</span>
        {user && (
          <div className="user-menu">
            <span className="user-name" title={user.email}>
              {user.username}
            </span>
            <button type="button" className="button ghost small" onClick={logout}>
              Log out
            </button>
          </div>
        )}
      </header>
      <main className="app-main">{children}</main>
    </div>
  )
}
