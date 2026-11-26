import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/http'
import { AuthContext, type AuthContextValue } from '../auth/authContext'
import { DEMO_CREDENTIALS } from '../auth/demoCredentials'
import { LoginPage } from './LoginPage'

function renderLogin(auth: Partial<AuthContextValue> = {}) {
  const value: AuthContextValue = {
    user: null,
    login: vi.fn().mockResolvedValue(undefined),
    register: vi.fn(),
    logout: vi.fn(),
    ...auth,
  }
  render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<p>Tasks home</p>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
  return { ...value, user: userEvent.setup() }
}

describe('LoginPage', () => {
  it('logs in with the demo credentials and goes to the task list', async () => {
    const { login, user } = renderLogin()

    await user.click(screen.getByRole('button', { name: 'Fill in' }))
    await user.click(screen.getByRole('button', { name: 'Log in' }))

    expect(login).toHaveBeenCalledWith(DEMO_CREDENTIALS.username, DEMO_CREDENTIALS.password)
    expect(await screen.findByText('Tasks home')).toBeInTheDocument()
  })

  it('moves focus to the submit button after filling in the demo account', async () => {
    const { user } = renderLogin()

    await user.click(screen.getByRole('button', { name: 'Fill in' }))

    expect(screen.getByRole('button', { name: 'Log in' })).toHaveFocus()
  })

  it('validates empty fields without calling the API', async () => {
    const { login, user } = renderLogin()

    await user.click(screen.getByRole('button', { name: 'Log in' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Enter your username and password.')
    expect(login).not.toHaveBeenCalled()
  })

  it('shows the API error when login fails', async () => {
    const { user } = renderLogin({
      login: vi.fn().mockRejectedValue(new ApiError(401, 'Invalid username or password.')),
    })

    await user.type(screen.getByLabelText('Username'), 'demo')
    await user.type(screen.getByLabelText('Password'), 'wrong')
    await user.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid username or password.')
    expect(screen.getByRole('button', { name: 'Log in' })).toBeEnabled()
  })

  it('redirects away when already logged in', () => {
    renderLogin({ user: { id: '1', username: 'demo', email: 'demo@example.com', createdAt: '' } })

    expect(screen.getByText('Tasks home')).toBeInTheDocument()
  })
})
