import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { tasksApi } from '../../api/tasksApi'
import type { Task } from '../../api/types'
import { useMoveTask } from './useTasks'

vi.mock('../../api/tasksApi', () => ({ tasksApi: { updateStatus: vi.fn() } }))

const task: Task = {
  id: '1',
  title: 'Write README',
  description: null,
  status: 'Todo',
  priority: 'Medium',
  dueDate: null,
  createdAt: '',
  updatedAt: '',
}
const listKey = ['tasks', { status: null }]

function setup() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: Infinity } } })
  client.setQueryData(listKey, [task])
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  )
  const { result } = renderHook(() => useMoveTask(), { wrapper })
  const cached = () => client.getQueryData<Task[]>(listKey)![0].status
  return { result, cached }
}

/** A promise the test resolves or rejects by hand, to look at the cache while the request is in flight. */
function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (error: Error) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

describe('useMoveTask', () => {
  beforeEach(() => {
    vi.mocked(tasksApi.updateStatus).mockReset()
  })

  it('moves the card in the cache before the server answers', async () => {
    const request = deferred<Task[]>()
    vi.mocked(tasksApi.updateStatus).mockReturnValue(request.promise)
    const { result, cached } = setup()

    act(() => result.current.mutate({ task, status: 'Done' }))

    await waitFor(() => expect(cached()).toBe('Done'))
    expect(tasksApi.updateStatus).toHaveBeenCalledWith(['1'], 'Done')
    await act(async () => request.resolve([{ ...task, status: 'Done' }]))
  })

  it('puts the card back if the server refuses', async () => {
    vi.mocked(tasksApi.updateStatus).mockRejectedValue(new Error('Task not found'))
    const { result, cached } = setup()

    act(() => result.current.mutate({ task, status: 'Done' }))

    await waitFor(() => expect(result.current.isError).toBe(true))
    expect(cached()).toBe('Todo')
  })
})
