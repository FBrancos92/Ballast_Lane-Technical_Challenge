import { useState } from 'react'
import { STATUS_LABELS, TASK_STATUSES, TaskStatus } from '../../api/types'

interface BulkStatusBarProps {
  selectedCount: number
  totalCount: number
  applying: boolean
  onApply: (status: TaskStatus) => void
  onSelectAll: () => void
  onClear: () => void
  onCancel: () => void
}

/** Toolbar shown in selection mode: pick a status and apply it to every selected task. */
export function BulkStatusBar({
  selectedCount,
  totalCount,
  applying,
  onApply,
  onSelectAll,
  onClear,
  onCancel,
}: BulkStatusBarProps) {
  const [status, setStatus] = useState<TaskStatus>(TaskStatus.Done)
  const allSelected = totalCount > 0 && selectedCount === totalCount
  const noun = selectedCount === 1 ? 'task' : 'tasks'

  return (
    <div className="bulk-bar" role="toolbar" aria-label="Change status of selected tasks">
      <span className="bulk-count" aria-live="polite">
        {selectedCount} selected
      </span>

      <button type="button" className="button ghost small" onClick={allSelected ? onClear : onSelectAll}>
        {allSelected ? 'Clear selection' : 'Select all'}
      </button>

      <div className="bulk-apply">
        <label htmlFor="bulk-status" className="visually-hidden">
          New status
        </label>
        <select id="bulk-status" value={status} onChange={(e) => setStatus(e.target.value as TaskStatus)}>
          {TASK_STATUSES.map((s) => (
            <option key={s} value={s}>
              {STATUS_LABELS[s]}
            </option>
          ))}
        </select>
        <button
          type="button"
          className="button primary small"
          disabled={selectedCount === 0 || applying}
          onClick={() => onApply(status)}
        >
          {applying ? 'Applying…' : `Apply to ${selectedCount} ${noun}`}
        </button>
        <button type="button" className="button secondary small" onClick={onCancel} disabled={applying}>
          Cancel
        </button>
      </div>
    </div>
  )
}
