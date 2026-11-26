import { useState } from 'react'
import { STATUS_LABELS, TaskStatus, type Task } from '../../api/types'
import { formatDueDate, isOverdue } from './dates'

interface TaskCardProps {
  task: Task
  busy: boolean
  onToggleDone: (task: Task) => void
  onEdit: (task: Task) => void
  onDelete: (task: Task) => void
  /** When set, the card is in selection mode: a select checkbox replaces done, edit and delete. */
  selection?: { selected: boolean; onChange: (task: Task, selected: boolean) => void }
}

export function TaskCard({ task, busy, onToggleDone, onEdit, onDelete, selection }: TaskCardProps) {
  const [confirming, setConfirming] = useState(false)
  const done = task.status === TaskStatus.Done
  const overdue = !done && isOverdue(task.dueDate)
  const classes = ['task-card', done && 'is-done', selection?.selected && 'is-selected'].filter(Boolean).join(' ')

  return (
    <article className={classes}>
      {selection ? (
        <input
          type="checkbox"
          className="task-check select-check"
          checked={selection.selected}
          onChange={(e) => selection.onChange(task, e.target.checked)}
          aria-label={`Select "${task.title}"`}
        />
      ) : (
        <input
          type="checkbox"
          className="task-check"
          checked={done}
          disabled={busy}
          onChange={() => onToggleDone(task)}
          aria-label={done ? `Mark "${task.title}" as not done` : `Mark "${task.title}" as done`}
        />
      )}

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
        {selection ? null : confirming ? (
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
    </article>
  )
}
