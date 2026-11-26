// Mirrors the API contracts (TaskManager.Application.*).

export const TaskStatus = {
  Todo: 'Todo',
  InProgress: 'InProgress',
  Done: 'Done',
} as const

export type TaskStatus = (typeof TaskStatus)[keyof typeof TaskStatus]

export const TASK_STATUSES: readonly TaskStatus[] = [TaskStatus.Todo, TaskStatus.InProgress, TaskStatus.Done]

export const STATUS_LABELS: Record<TaskStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  Done: 'Done',
}

export interface Task {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  /** ISO date, yyyy-MM-dd */
  dueDate: string | null
  createdAt: string
  updatedAt: string
}

export interface TaskInput {
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string | null
}

export interface User {
  id: string
  username: string
  email: string
  createdAt: string
}

export interface AuthResponse {
  token: string
  expiresAt: string
  user: User
}
