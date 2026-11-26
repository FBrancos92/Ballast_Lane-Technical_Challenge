import { describe, expect, it } from 'vitest'
import { isOverdue, todayIso } from './dates'

describe('dates', () => {
  it('formats today in local time as yyyy-MM-dd', () => {
    expect(todayIso(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05')
  })

  it('treats only dates before today as overdue', () => {
    expect(isOverdue('2026-01-04', '2026-01-05')).toBe(true)
    expect(isOverdue('2026-01-05', '2026-01-05')).toBe(false)
    expect(isOverdue(null, '2026-01-05')).toBe(false)
  })
})
