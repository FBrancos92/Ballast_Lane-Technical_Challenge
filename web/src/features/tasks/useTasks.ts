import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { tasksApi } from '../../api/tasksApi'
import type { TaskInput, TaskStatus } from '../../api/types'

// Server state lives in React Query's cache, keyed by filter. Every mutation
// invalidates all task lists, so every filter view refetches fresh data.
const tasksKey = ['tasks'] as const

export function useTasks(status: TaskStatus | null) {
  return useQuery({
    queryKey: [...tasksKey, { status }],
    queryFn: ({ signal }) => tasksApi.list(status, signal),
  })
}

export function useCreateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: TaskInput) => tasksApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksKey }),
  })
}

export function useUpdateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: TaskInput }) => tasksApi.update(id, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksKey }),
  })
}

export function useUpdateTasksStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ ids, status }: { ids: string[]; status: TaskStatus }) => tasksApi.updateStatus(ids, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksKey }),
  })
}

export function useDeleteTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => tasksApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksKey }),
  })
}
