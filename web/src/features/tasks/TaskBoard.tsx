import { useState, type DragEvent, type ReactNode } from 'react'
import { STATUS_LABELS, TASK_STATUSES, type Task, type TaskStatus } from '../../api/types'

const DRAG_TYPE = 'application/x-task-id'

interface TaskBoardProps {
  tasks: Task[]
  /** Called when a card is dropped on a different column. */
  onMove: (task: Task, status: TaskStatus) => void
  renderCard: (task: Task) => ReactNode
}

/**
 * One column per status. Cards are moved with the browser's native drag and drop, which needs a
 * mouse; on touch screens the Select mode's bulk status bar changes a card's column instead.
 */
export function TaskBoard({ tasks, onMove, renderCard }: TaskBoardProps) {
  // Which card is being dragged, kept in state because browsers hide drag data until the drop.
  const [draggingId, setDraggingId] = useState<string | null>(null)
  const [overStatus, setOverStatus] = useState<TaskStatus | null>(null)

  function handleDragStart(event: DragEvent, task: Task) {
    event.dataTransfer.setData(DRAG_TYPE, task.id)
    event.dataTransfer.setData('text/plain', task.title) // Firefox only starts a drag with data set.
    event.dataTransfer.effectAllowed = 'move'
    setDraggingId(task.id)
  }

  function endDrag() {
    setDraggingId(null)
    setOverStatus(null)
  }

  function handleDragOver(event: DragEvent, status: TaskStatus) {
    if (!draggingId) return
    event.preventDefault() // Marks the column as a valid drop target.
    event.dataTransfer.dropEffect = 'move'
    setOverStatus(status)
  }

  function handleDragLeave(event: DragEvent<HTMLElement>, status: TaskStatus) {
    // Moving onto a child element also fires dragleave; only clear when the pointer really left.
    if (event.currentTarget.contains(event.relatedTarget as Node | null)) return
    setOverStatus((current) => (current === status ? null : current))
  }

  function handleDrop(event: DragEvent, status: TaskStatus) {
    event.preventDefault()
    const id = event.dataTransfer.getData(DRAG_TYPE) || draggingId
    endDrag()
    const task = tasks.find((t) => t.id === id)
    if (task && task.status !== status) onMove(task, status)
  }

  return (
    <div className="board">
      {TASK_STATUSES.map((status) => {
        const columnTasks = tasks.filter((t) => t.status === status)
        const label = STATUS_LABELS[status]
        const noun = columnTasks.length === 1 ? 'task' : 'tasks'
        return (
          <section
            key={status}
            className={`board-column status-col-${status}${overStatus === status ? ' drop-target' : ''}`}
            aria-label={`${label}, ${columnTasks.length} ${noun}`}
            onDragOver={(e) => handleDragOver(e, status)}
            onDragLeave={(e) => handleDragLeave(e, status)}
            onDrop={(e) => handleDrop(e, status)}
          >
            <h2 className="board-column-title" aria-hidden="true">
              {label} <span className="board-count">{columnTasks.length}</span>
            </h2>
            {columnTasks.length === 0 ? (
              <p className="board-empty">Drop tasks here</p>
            ) : (
              <ul className="board-list">
                {columnTasks.map((task) => (
                  <li
                    key={task.id}
                    className={`board-item${draggingId === task.id ? ' dragging' : ''}`}
                    draggable
                    onDragStart={(e) => handleDragStart(e, task)}
                    onDragEnd={endDrag}
                  >
                    {renderCard(task)}
                  </li>
                ))}
              </ul>
            )}
          </section>
        )
      })}
    </div>
  )
}
