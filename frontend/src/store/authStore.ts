import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { User } from '@/types/auth'

interface AuthState {
  user: User | null
  token: string | null
  isAuthenticated: boolean
  setUser: (user: User | null) => void
  setToken: (token: string | null) => void
  logout: () => void
  validateToken: () => boolean
}

// Helper function to check if token is expired
const isTokenExpired = (token: string): boolean => {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const exp = payload.exp * 1000 // Convert to milliseconds
    return Date.now() >= exp
  } catch {
    return true
  }
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      token: null,
      isAuthenticated: false,

      setUser: (user) =>
        set({
          user,
          isAuthenticated: !!user,
        }),

      setToken: (token) => {
        // Validate token on set
        if (token && isTokenExpired(token)) {
          // Token expired, clear everything
          set({
            token: null,
            user: null,
            isAuthenticated: false,
          })
          localStorage.removeItem('token')
          localStorage.removeItem('user')
          return
        }

        set({
          token,
          isAuthenticated: !!token,
        })

        if (token) {
          localStorage.setItem('token', token)
        } else {
          localStorage.removeItem('token')
        }
      },

      logout: () => {
        set({
          user: null,
          token: null,
          isAuthenticated: false,
        })
        localStorage.removeItem('token')
        localStorage.removeItem('user')
      },

      validateToken: () => {
        const { token } = get()
        if (!token) {
          get().logout()
          return false
        }

        if (isTokenExpired(token)) {
          get().logout()
          return false
        }

        return true
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
