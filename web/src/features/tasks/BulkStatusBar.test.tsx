import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { BulkStatusBar } from './BulkStatusBar'

function renderBar(props: Partial<Parameters<typeof BulkStatusBar>[0]> = {}) {
  const handlers = { onApply: vi.fn(), onSelectAll: vi.fn(), onClear: vi.fn(), onCancel: vi.fn() }
  render(<BulkStatusBar selectedCount={2} totalCount={5} applying={false} {...handlers} {...props} />)
  return { ...handlers, user: userEvent.setup() }
}

describe('BulkStatusBar', () => {
  it('shows how many tasks are selected', () => {
    renderBar()

    expect(screen.getByText('2 selected')).toBeInTheDocument()
  })

  it('applies the chosen status', async () => {
    const { onApply, user } = renderBar()

    await user.selectOptions(screen.getByLabelText('New status'), 'Done')
    await user.click(screen.getByRole('button', { name: 'Apply to 2 tasks' }))

    expect(onApply).toHaveBeenCalledWith('Done')
  })

  it('cannot apply with nothing selected', () => {
    renderBar({ selectedCount: 0 })

    expect(screen.getByRole('button', { name: /^Apply/ })).toBeDisabled()
  })

  it('disables apply while saving', () => {
    renderBar({ applying: true })

    expect(screen.getByRole('button', { name: /^Apply|Applying/ })).toBeDisabled()
  })

  it('selects all', async () => {
    const { onSelectAll, user } = renderBar()

    await user.click(screen.getByRole('button', { name: 'Select all' }))

    expect(onSelectAll).toHaveBeenCalled()
  })

  it('offers to clear once everything is selected', async () => {
    const { onClear, user } = renderBar({ selectedCount: 5 })

    await user.click(screen.getByRole('button', { name: 'Clear selection' }))

    expect(onClear).toHaveBeenCalled()
  })

  it('can be cancelled', async () => {
    const { onCancel, user } = renderBar()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
