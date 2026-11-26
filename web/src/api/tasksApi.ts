import { request } from './http'
import type { Task, TaskInput, TaskStatus } from './types'

export const tasksApi = {
  list: (status: TaskStatus | null, signal?: AbortSignal) =>
    request<Task[]>(status ? `/tasks?status=${encodeURIComponent(status)}` : '/tasks', { signal }),

  get: (id: string, signal?: AbortSignal) => request<Task>(`/tasks/${encodeURIComponent(id)}`, { signal }),

  create: (input: TaskInput) => request<Task>('/tasks', { method: 'POST', body: input }),

  update: (id: string, input: TaskInput) =>
    request<Task>(`/tasks/${encodeURIComponent(id)}`, { method: 'PUT', body: input }),

  remove: (id: string) => request<void>(`/tasks/${encodeURIComponent(id)}`, { method: 'DELETE' }),
}
