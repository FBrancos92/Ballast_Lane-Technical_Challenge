import { STATUS_LABELS, TASK_STATUSES, type TaskStatus } from '../../api/types'

interface StatusFilterProps {
  value: TaskStatus | null
  onChange: (status: TaskStatus | null) => void
}

const OPTIONS: { value: TaskStatus | null; label: string }[] = [
  { value: null, label: 'All' },
  ...TASK_STATUSES.map((s) => ({ value: s, label: STATUS_LABELS[s] })),
]

export function StatusFilter({ value, onChange }: StatusFilterProps) {
  return (
    <div className="status-filter" role="group" aria-label="Filter by status">
      {OPTIONS.map((option) => (
        <button
          key={option.label}
          type="button"
          className={`chip${value === option.value ? ' active' : ''}`}
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}
