import { request } from './http'
import type { AuthResponse, User } from './types'

export interface RegisterInput {
  username: string
  email: string
  password: string
}

export const authApi = {
  login: (username: string, password: string) =>
    request<AuthResponse>('/auth/login', { method: 'POST', body: { username, password } }),

  register: (input: RegisterInput) => request<AuthResponse>('/auth/register', { method: 'POST', body: input }),

  me: (signal?: AbortSignal) => request<User>('/users/me', { signal }),
}
