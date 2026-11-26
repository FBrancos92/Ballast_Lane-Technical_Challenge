import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { tasksApi } from '../../api/tasksApi'
import type { Task, TaskInput, TaskStatus } from '../../api/types'

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

/**
 * Moves one card to another column. Optimistic: the cache changes at once so the card lands where
 * it was dropped, and the previous lists are put back if the server refuses.
 */
export function useMoveTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ task, status }: { task: Task; status: TaskStatus }) => tasksApi.updateStatus([task.id], status),
    onMutate: async ({ task, status }) => {
      // Stop in-flight refetches from overwriting the optimistic change.
      await queryClient.cancelQueries({ queryKey: tasksKey })
      const previous = queryClient.getQueriesData<Task[]>({ queryKey: tasksKey })
      queryClient.setQueriesData<Task[]>({ queryKey: tasksKey }, (list) =>
        list?.map((t) => (t.id === task.id ? { ...t, status } : t)),
      )
      return { previous }
    },
    onError: (_error, _variables, context) => {
      context?.previous.forEach(([key, list]) => queryClient.setQueryData(key, list))
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: tasksKey }),
  })
}

export function useDeleteTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => tasksApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tasksKey }),
  })
}
