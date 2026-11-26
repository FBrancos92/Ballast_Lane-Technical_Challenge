import { useRef, useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { DEMO_CREDENTIALS } from '../auth/demoCredentials'
import { useAuth } from '../auth/useAuth'
import { FormField } from '../components/FormField'

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const submitRef = useRef<HTMLButtonElement>(null)

  const from = (location.state as { from?: string } | null)?.from ?? '/'
  if (user) return <Navigate to={from} replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!username.trim() || !password) {
      setError('Enter your username and password.')
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await login(username.trim(), password)
      navigate(from, { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed.')
      setSubmitting(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit} noValidate aria-labelledby="login-heading">
        <h1 id="login-heading">Log in</h1>

        {error && (
          <div className="alert" role="alert">
            {error}
          </div>
        )}

        <FormField label="Username">
          {({ id }) => (
            <input id={id} value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" />
          )}
        </FormField>
        <FormField label="Password">
          {({ id }) => (
            <input
              id={id}
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
            />
          )}
        </FormField>

        <button ref={submitRef} type="submit" className="button primary block" disabled={submitting}>
          {submitting ? 'Logging in…' : 'Log in'}
        </button>

        <div className="demo-hint">
          <span>
            Demo account: <code>{DEMO_CREDENTIALS.username}</code> / <code>{DEMO_CREDENTIALS.password}</code>
          </span>
          <button
            type="button"
            className="button ghost small"
            onClick={() => {
              setUsername(DEMO_CREDENTIALS.username)
              setPassword(DEMO_CREDENTIALS.password)
              // Move focus to "Log in" so pressing Enter next submits instead of re-clicking "Fill in".
              submitRef.current?.focus()
            }}
          >
            Fill in
          </button>
        </div>

        <p className="auth-switch">
          No account? <Link to="/register">Create one</Link>
        </p>
      </form>
    </div>
  )
}
