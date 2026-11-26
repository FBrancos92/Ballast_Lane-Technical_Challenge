import { useState, type FormEvent } from 'react'
import type { FieldErrors } from '../../api/http'
import { STATUS_LABELS, TASK_STATUSES, TaskStatus, type Task, type TaskInput } from '../../api/types'
import { FormField } from '../../components/FormField'
import { todayIso } from './dates'

export const TITLE_MAX = 200
export const DESCRIPTION_MAX = 2000

interface TaskFormProps {
  initial?: Task
  submitLabel: string
  submitting: boolean
  /** Field errors returned by the API, shown next to the matching inputs. */
  serverErrors?: FieldErrors
  onSubmit: (input: TaskInput) => void
  onCancel: () => void
}

/**
 * Create/edit form. Checks the same rules as the backend so users get instant feedback,
 * but the API stays the source of truth: its errors are shown here as well.
 */
export function TaskForm({ initial, submitLabel, submitting, serverErrors = {}, onSubmit, onCancel }: TaskFormProps) {
  const [title, setTitle] = useState(initial?.title ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [status, setStatus] = useState<TaskStatus>(initial?.status ?? TaskStatus.Todo)
  const [dueDate, setDueDate] = useState(initial?.dueDate ?? '')
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})

  const today = todayIso()
  const dueDateChanged = dueDate !== (initial?.dueDate ?? '')

  function validate(): Record<string, string> {
    const errors: Record<string, string> = {}
    if (!title.trim()) errors.title = 'Title is required.'
    else if (title.trim().length > TITLE_MAX) errors.title = `Title must be at most ${TITLE_MAX} characters.`
    if (description.trim().length > DESCRIPTION_MAX)
      errors.description = `Description must be at most ${DESCRIPTION_MAX} characters.`
    if (dueDate && dueDateChanged && dueDate < today) errors.dueDate = 'Due date cannot be in the past.'
    return errors
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const errors = validate()
    setClientErrors(errors)
    if (Object.keys(errors).length > 0) return

    onSubmit({
      title: title.trim(),
      description: description.trim() || null,
      status,
      dueDate: dueDate || null,
    })
  }

  const errorFor = (field: string) => clientErrors[field] ?? serverErrors[field]?.[0]

  return (
    <form className="task-form" onSubmit={handleSubmit} noValidate>
      <FormField label="Title" error={errorFor('title')}>
        {({ id, describedBy, invalid }) => (
          <input
            id={id}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            maxLength={TITLE_MAX}
            aria-describedby={describedBy}
            aria-invalid={invalid}
            autoComplete="off"
            required
          />
        )}
      </FormField>

      <FormField label="Description" error={errorFor('description')} hint="Optional">
        {({ id, describedBy, invalid }) => (
          <textarea
            id={id}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={4}
            maxLength={DESCRIPTION_MAX}
            aria-describedby={describedBy}
            aria-invalid={invalid}
          />
        )}
      </FormField>

      <div className="field-row">
        <FormField label="Status" error={errorFor('status')}>
          {({ id, describedBy, invalid }) => (
            <select
              id={id}
              value={status}
              onChange={(e) => setStatus(e.target.value as TaskStatus)}
              aria-describedby={describedBy}
              aria-invalid={invalid}
            >
              {TASK_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {STATUS_LABELS[s]}
                </option>
              ))}
            </select>
          )}
        </FormField>

        <FormField label="Due date" error={errorFor('dueDate')} hint="Optional">
          {({ id, describedBy, invalid }) => (
            <input
              id={id}
              type="date"
              value={dueDate}
              min={dueDateChanged || !initial ? today : undefined}
              onChange={(e) => setDueDate(e.target.value)}
              aria-describedby={describedBy}
              aria-invalid={invalid}
            />
          )}
        </FormField>
      </div>

      <div className="form-actions">
        <button type="button" className="button secondary" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
        <button type="submit" className="button primary" disabled={submitting}>
          {submitting ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
