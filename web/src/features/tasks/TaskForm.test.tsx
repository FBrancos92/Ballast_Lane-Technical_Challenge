import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Task } from '../../api/types'
import { todayIso } from './dates'
import { TaskForm } from './TaskForm'

function renderForm(props: Partial<Parameters<typeof TaskForm>[0]> = {}) {
  const onSubmit = vi.fn()
  const onCancel = vi.fn()
  render(<TaskForm submitLabel="Create task" submitting={false} onSubmit={onSubmit} onCancel={onCancel} {...props} />)
  return { onSubmit, onCancel, user: userEvent.setup() }
}

const existing: Task = {
  id: '42',
  title: 'Existing',
  description: 'Old notes',
  status: 'InProgress',
  dueDate: '2020-01-01',
  createdAt: '',
  updatedAt: '',
}

describe('TaskForm', () => {
  it('submits trimmed values, with empty optional fields as null', async () => {
    const { onSubmit, user } = renderForm()

    await user.type(screen.getByLabelText('Title'), '  Buy milk  ')
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(onSubmit).toHaveBeenCalledWith({ title: 'Buy milk', description: null, status: 'Todo', dueDate: null })
  })

  it('requires a title', async () => {
    const { onSubmit, user } = renderForm()

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Title is required.')
    expect(screen.getByLabelText('Title')).toHaveAttribute('aria-invalid', 'true')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('rejects a new due date in the past', async () => {
    const { onSubmit, user } = renderForm()

    await user.type(screen.getByLabelText('Title'), 'Task')
    await user.type(screen.getByLabelText('Due date'), '2000-01-01')
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Due date cannot be in the past.')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('accepts today as a due date', async () => {
    const { onSubmit, user } = renderForm()

    await user.type(screen.getByLabelText('Title'), 'Task')
    await user.type(screen.getByLabelText('Due date'), todayIso())
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ dueDate: todayIso() }))
  })

  it('pre-fills when editing and keeps an existing overdue date', async () => {
    const { onSubmit, user } = renderForm({ initial: existing, submitLabel: 'Save changes' })

    expect(screen.getByLabelText('Title')).toHaveValue('Existing')
    expect(screen.getByLabelText('Status')).toHaveValue('InProgress')

    await user.selectOptions(screen.getByLabelText('Status'), 'Done')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(onSubmit).toHaveBeenCalledWith({
      title: 'Existing',
      description: 'Old notes',
      status: 'Done',
      dueDate: '2020-01-01',
    })
  })

  it('shows field errors returned by the API', () => {
    renderForm({ serverErrors: { title: ['Title must be at most 200 characters.'] } })

    expect(screen.getByRole('alert')).toHaveTextContent('Title must be at most 200 characters.')
  })

  it('disables buttons while saving', () => {
    renderForm({ submitting: true })

    expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('calls onCancel', async () => {
    const { onCancel, user } = renderForm()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
