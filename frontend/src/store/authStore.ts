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

// ✅ Helper function to validate JWT format
const isValidJWTFormat = (token: string): boolean => {
  const parts = token.split('.')
  if (parts.length !== 3) {
    console.warn('[Auth] JWT does not have 3 parts')
    return false
  }
  
  try {
    // Validate all three parts can be decoded
    parts.forEach((part) => {
      // Convert base64url to base64 (replace - with + and _ with /)
      const base64 = part
        .replace(/-/g, '+')
        .replace(/_/g, '/')
      
      // Add padding if needed
      const padded = base64 + '='.repeat((4 - base64.length % 4) % 4)
      
      // Try to decode
      atob(padded)
    })
    return true
  } catch (e) {
    console.error('[Auth] JWT format validation failed:', e)
    return false
  }
}

// ✅ Helper function to check if token is expired
// Checks if token will expire within 30 seconds (early expiration check)
const isTokenExpired = (token: string): boolean => {
  try {
    // ✅ First, validate JWT format
    if (!isValidJWTFormat(token)) {
      console.warn('[Auth] Invalid JWT format')
      return true
    }

    const parts = token.split('.')
    
    // Convert base64url to base64 and add padding
    const base64 = parts[1]
      .replace(/-/g, '+')
      .replace(/_/g, '/')
    const padded = base64 + '='.repeat((4 - base64.length % 4) % 4)
    
    const payload = JSON.parse(atob(padded))
    
    // ✅ Check if exp field exists
    if (!payload.exp || typeof payload.exp !== 'number') {
      console.warn('[Auth] Invalid JWT payload: missing or invalid exp field')
      return true
    }

    const expirationTime = payload.exp * 1000 // Convert to milliseconds
    const currentTime = Date.now()
    
    // ✅ Add 30-second buffer for early expiration check
    const expirationBuffer = 30 * 1000 // 30 seconds
    const isExpired = currentTime >= expirationTime - expirationBuffer

    if (isExpired) {
      console.warn('[Auth] Token expired or about to expire', {
        expiresIn: Math.round((expirationTime - currentTime) / 1000),
      })
    }

    return isExpired
  } catch (error) {
    console.error('[Auth] Failed to validate token expiration', error)
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
        {
          const hasToken = !!get().token
          set({
            user,
            // Keep auth state consistent: user and token must both exist.
            isAuthenticated: !!user && hasToken,
          })
        },

      setToken: (token) => {
        // ✅ Validate token format first
        if (token && !isValidJWTFormat(token)) {
          console.error('[Auth] Invalid token format provided:', token?.substring(0, 50))
          set({
            token: null,
            user: null,
            isAuthenticated: false,
          })
          localStorage.removeItem('token')
          localStorage.removeItem('user')
          return
        }

        // Validate token expiration
        if (token && isTokenExpired(token)) {
          // Token expired, clear everything
          console.warn('[Auth] Token expired or invalid, clearing auth state')
          set({
            token: null,
            user: null,
            isAuthenticated: false,
          })
          localStorage.removeItem('token')
          localStorage.removeItem('user')
          return
        }

        console.log('[Auth] Token set successfully')
        set({
          token,
          // Keep auth state consistent: user and token must both exist.
          isAuthenticated: !!token && !!get().user,
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
