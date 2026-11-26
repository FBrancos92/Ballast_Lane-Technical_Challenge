import { createContext } from 'react'
import type { RegisterInput } from '../api/authApi'
import type { User } from '../api/types'

export interface AuthContextValue {
  user: User | null
  login: (username: string, password: string) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
