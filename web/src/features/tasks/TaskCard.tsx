import { useState } from 'react'
import { STATUS_LABELS, TaskStatus, type Task } from '../../api/types'
import { formatDueDate, isOverdue } from './dates'

interface TaskCardProps {
  task: Task
  busy: boolean
  onToggleDone: (task: Task) => void
  onEdit: (task: Task) => void
  onDelete: (task: Task) => void
}

export function TaskCard({ task, busy, onToggleDone, onEdit, onDelete }: TaskCardProps) {
  const [confirming, setConfirming] = useState(false)
  const done = task.status === TaskStatus.Done
  const overdue = !done && isOverdue(task.dueDate)

  return (
    <li className={`task-card${done ? ' is-done' : ''}`}>
      <input
        type="checkbox"
        className="task-check"
        checked={done}
        disabled={busy}
        onChange={() => onToggleDone(task)}
        aria-label={done ? `Mark "${task.title}" as not done` : `Mark "${task.title}" as done`}
      />

      <div className="task-body">
        <h3 className="task-title">{task.title}</h3>
        {task.description && <p className="task-description">{task.description}</p>}
        <div className="task-meta">
          <span className={`badge status-${task.status}`}>{STATUS_LABELS[task.status]}</span>
          <span className={`badge priority-${task.priority}`}>{task.priority} priority</span>
          {task.dueDate && (
            <span className={`due${overdue ? ' overdue' : ''}`}>
              {overdue ? 'Overdue · ' : 'Due '}
              <time dateTime={task.dueDate}>{formatDueDate(task.dueDate)}</time>
            </span>
          )}
        </div>
      </div>

      <div className="task-actions">
        {confirming ? (
          <>
            <span className="confirm-text">Delete?</span>
            <button type="button" className="button danger small" disabled={busy} onClick={() => onDelete(task)}>
              Yes, delete
            </button>
            <button type="button" className="button secondary small" onClick={() => setConfirming(false)}>
              Keep
            </button>
          </>
        ) : (
          <>
            <button
              type="button"
              className="button secondary small"
              disabled={busy}
              onClick={() => onEdit(task)}
              aria-label={`Edit ${task.title}`}
            >
              Edit
            </button>
            <button
              type="button"
              className="button ghost small"
              disabled={busy}
              onClick={() => setConfirming(true)}
              aria-label={`Delete ${task.title}`}
            >
              Delete
            </button>
          </>
        )}
      </div>
    </li>
  )
}
