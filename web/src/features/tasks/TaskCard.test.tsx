import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Task } from '../../api/types'
import { TaskCard } from './TaskCard'

const task: Task = {
  id: '1',
  title: 'Write README',
  description: 'Setup steps',
  status: 'Todo',
  priority: 'High',
  dueDate: '2000-01-01',
  createdAt: '',
  updatedAt: '',
}

function renderCard(overrides: Partial<Task> = {}) {
  const handlers = { onEdit: vi.fn(), onDelete: vi.fn() }
  render(
    <div>
      <TaskCard task={{ ...task, ...overrides }} busy={false} {...handlers} />
    </div>,
  )
  return { ...handlers, user: userEvent.setup() }
}

describe('TaskCard', () => {
  it('shows the task details', () => {
    renderCard()

    expect(screen.getByRole('heading', { name: 'Write README' })).toBeInTheDocument()
    expect(screen.getByText('Setup steps')).toBeInTheDocument()
    expect(screen.getByText('To do')).toBeInTheDocument()
  })

  it('flags an overdue open task', () => {
    renderCard()

    expect(screen.getByText(/Overdue/)).toBeInTheDocument()
  })

  it('does not flag a finished task as overdue', () => {
    renderCard({ status: 'Done' })

    expect(screen.queryByText(/Overdue/)).not.toBeInTheDocument()
  })

  it('has no checkbox outside selection mode', () => {
    renderCard()

    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
  })

  it('asks for confirmation before deleting', async () => {
    const { onDelete, user } = renderCard()

    await user.click(screen.getByRole('button', { name: 'Delete Write README' }))
    expect(onDelete).not.toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Yes, delete' }))
    expect(onDelete).toHaveBeenCalledWith(expect.objectContaining({ id: '1' }))
  })

  it('can cancel a delete', async () => {
    const { onDelete, user } = renderCard()

    await user.click(screen.getByRole('button', { name: 'Delete Write README' }))
    await user.click(screen.getByRole('button', { name: 'Keep' }))

    expect(onDelete).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Edit Write README' })).toBeInTheDocument()
  })

  it('opens the editor', async () => {
    const { onEdit, user } = renderCard()

    await user.click(screen.getByRole('button', { name: 'Edit Write README' }))

    expect(onEdit).toHaveBeenCalled()
  })

  it('shows the priority', () => {
    renderCard()

    expect(screen.getByText('High priority')).toBeInTheDocument()
  })

  it('has no status menu: moving between columns is done by dragging', () => {
    renderCard()

    expect(screen.queryByRole('combobox')).not.toBeInTheDocument()
  })

  describe('in selection mode', () => {
    function renderSelectable(selected = false) {
      const onSelectChange = vi.fn()
      render(
        <div>
          <TaskCard
            task={task}
            busy={false}
            onEdit={vi.fn()}
            onDelete={vi.fn()}
            selection={{ selected, onChange: onSelectChange }}
          />
        </div>,
      )
      return { onSelectChange, user: userEvent.setup() }
    }

    it('shows a select checkbox instead of done, and hides edit and delete', () => {
      renderSelectable()

      expect(screen.getByRole('checkbox', { name: 'Select "Write README"' })).not.toBeChecked()
      expect(screen.queryByRole('checkbox', { name: /as done/ })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Edit Write README' })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Delete Write README' })).not.toBeInTheDocument()
    })

    it('reports selection changes', async () => {
      const { onSelectChange, user } = renderSelectable()

      await user.click(screen.getByRole('checkbox', { name: 'Select "Write README"' }))

      expect(onSelectChange).toHaveBeenCalledWith(expect.objectContaining({ id: '1' }), true)
    })

    it('shows when it is selected', () => {
      renderSelectable(true)

      expect(screen.getByRole('checkbox', { name: 'Select "Write README"' })).toBeChecked()
    })
  })
})
