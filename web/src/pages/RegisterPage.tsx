import { useState, type ChangeEvent, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { ApiError, type FieldErrors } from '../api/http'
import { useAuth } from '../auth/useAuth'
import { FormField } from '../components/FormField'

export function RegisterPage() {
  const { user, register } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ username: '', email: '', password: '' })
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to="/" replace />

  const update = (field: keyof typeof form) => (e: ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [field]: e.target.value }))

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    setFieldErrors({})
    try {
      await register({ username: form.username.trim(), email: form.email.trim(), password: form.password })
      navigate('/', { replace: true })
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) setFieldErrors(err.fieldErrors)
      else setError(err instanceof Error ? err.message : 'Registration failed.')
      setSubmitting(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit} noValidate aria-labelledby="register-heading">
        <h1 id="register-heading">Create an account</h1>

        {error && (
          <div className="alert" role="alert">
            {error}
          </div>
        )}

        <FormField label="Username" error={fieldErrors.username?.[0]} hint="3–50 letters, digits, '.', '_' or '-'">
          {({ id, describedBy, invalid }) => (
            <input
              id={id}
              value={form.username}
              onChange={update('username')}
              autoComplete="username"
              aria-describedby={describedBy}
              aria-invalid={invalid}
            />
          )}
        </FormField>
        <FormField label="Email" error={fieldErrors.email?.[0]}>
          {({ id, describedBy, invalid }) => (
            <input
              id={id}
              type="email"
              value={form.email}
              onChange={update('email')}
              autoComplete="email"
              aria-describedby={describedBy}
              aria-invalid={invalid}
            />
          )}
        </FormField>
        <FormField
          label="Password"
          error={fieldErrors.password?.[0]}
          hint="At least 8 characters, with a letter and a digit"
        >
          {({ id, describedBy, invalid }) => (
            <input
              id={id}
              type="password"
              value={form.password}
              onChange={update('password')}
              autoComplete="new-password"
              aria-describedby={describedBy}
              aria-invalid={invalid}
            />
          )}
        </FormField>

        <button type="submit" className="button primary block" disabled={submitting}>
          {submitting ? 'Creating account…' : 'Create account'}
        </button>

        <p className="auth-switch">
          Already registered? <Link to="/login">Log in</Link>
        </p>
      </form>
    </div>
  )
}
