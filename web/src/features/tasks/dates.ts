/** Today as yyyy-MM-dd in the user's local time zone (what <input type="date"> uses). */
export function todayIso(now: Date = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/** Formats yyyy-MM-dd without the UTC shift that `new Date('2026-06-15')` would cause. */
export function formatDueDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
}

export function isOverdue(dueDate: string | null, today: string = todayIso()): boolean {
  // ISO dates compare correctly as strings.
  return dueDate !== null && dueDate < today
}
