import { useId, type ReactElement } from 'react'

interface FormFieldProps {
  label: string
  error?: string
  hint?: string
  /** Receives the ids to wire up for accessibility. */
  children: (ids: { id: string; describedBy: string | undefined; invalid: boolean }) => ReactElement
}

/** Label + input + error message, linked with aria attributes so screen readers announce errors. */
export function FormField({ label, error, hint, children }: FormFieldProps) {
  const id = useId()
  const errorId = `${id}-error`
  const hintId = `${id}-hint`
  const describedBy = [error ? errorId : null, hint ? hintId : null].filter(Boolean).join(' ') || undefined

  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      {children({ id, describedBy, invalid: Boolean(error) })}
      {hint && !error && (
        <p id={hintId} className="field-hint">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className="field-error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
