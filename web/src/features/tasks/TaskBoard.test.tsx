import { createEvent, fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { Task } from '../../api/types'
import { TaskBoard } from './TaskBoard'

function makeTask(id: string, title: string, status: Task['status']): Task {
  return { id, title, description: null, status, priority: 'Medium', dueDate: null, createdAt: '', updatedAt: '' }
}

const tasks = [
  makeTask('1', 'Write README', 'Todo'),
  makeTask('2', 'Review PR', 'Todo'),
  makeTask('3', 'Prepare demo', 'InProgress'),
]

function renderBoard() {
  const onMove = vi.fn()
  render(<TaskBoard tasks={tasks} onMove={onMove} renderCard={(task) => <span>{task.title}</span>} />)
  return { onMove }
}

/** jsdom has no DataTransfer, so drag events get a minimal stand-in. */
function dataTransfer() {
  const data = new Map<string, string>()
  return {
    setData: (type: string, value: string) => data.set(type, value),
    getData: (type: string) => data.get(type) ?? '',
    effectAllowed: '',
    dropEffect: '',
  }
}

function drag(card: HTMLElement, target: HTMLElement) {
  const transfer = dataTransfer()
  fireEvent.dragStart(card, { dataTransfer: transfer })
  const over = createEvent.dragOver(target, { dataTransfer: transfer })
  fireEvent(target, over)
  fireEvent.drop(target, { dataTransfer: transfer })
  fireEvent.dragEnd(card, { dataTransfer: transfer })
  return over
}

const column = (name: RegExp) => screen.getByRole('region', { name })
const card = (title: string) => screen.getByText(title).closest('li')!

describe('TaskBoard', () => {
  it('shows one column per status, each with its tasks and a count', () => {
    renderBoard()

    expect(within(column(/^To do/)).getByText('Write README')).toBeInTheDocument()
    expect(within(column(/^To do/)).getByText('Review PR')).toBeInTheDocument()
    expect(within(column(/^In progress/)).getByText('Prepare demo')).toBeInTheDocument()
    expect(column(/^To do/)).toHaveAccessibleName('To do, 2 tasks')
    expect(column(/^Done/)).toHaveAccessibleName('Done, 0 tasks')
  })

  it('shows a hint in an empty column', () => {
    renderBoard()

    expect(within(column(/^Done/)).getByText('Drop tasks here')).toBeInTheDocument()
  })

  it('moves a card when it is dropped on another column', () => {
    const { onMove } = renderBoard()

    const over = drag(card('Write README'), column(/^Done/))

    expect(over.defaultPrevented).toBe(true) // lets the browser allow the drop
    expect(onMove).toHaveBeenCalledWith(expect.objectContaining({ id: '1' }), 'Done')
  })

  it('does nothing when a card is dropped back on its own column', () => {
    const { onMove } = renderBoard()

    drag(card('Write README'), column(/^To do/))

    expect(onMove).not.toHaveBeenCalled()
  })

  it('ignores drops that are not task cards', () => {
    const { onMove } = renderBoard()

    fireEvent.drop(column(/^Done/), { dataTransfer: dataTransfer() })

    expect(onMove).not.toHaveBeenCalled()
  })

  it('highlights the column a card is dragged over', () => {
    renderBoard()
    const transfer = dataTransfer()

    fireEvent.dragStart(card('Write README'), { dataTransfer: transfer })
    fireEvent.dragOver(column(/^Done/), { dataTransfer: transfer })

    expect(column(/^Done/)).toHaveClass('drop-target')
  })
})
