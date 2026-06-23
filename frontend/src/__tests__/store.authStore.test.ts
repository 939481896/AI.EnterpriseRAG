import { describe, it, expect, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useAuthStore } from '@/store/authStore'
import { createMockToken, createMockUser } from './utils'

describe('authStore', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear()
    // Reset Zustand store
    useAuthStore.setState({
      user: null,
      token: null,
      isAuthenticated: false,
    })
  })

  describe('setToken', () => {
    it('should set a valid token', () => {
      const { result } = renderHook(() => useAuthStore())
      const token = createMockToken()

      act(() => {
        result.current.setToken(token)
      })

      expect(result.current.token).toBe(token)
      expect(localStorage.setItem).toHaveBeenCalledWith('token', token)
    })

    it('should reject an invalid token format', () => {
      const { result } = renderHook(() => useAuthStore())
      const invalidToken = 'not-a-jwt'

      act(() => {
        result.current.setToken(invalidToken)
      })

      expect(result.current.token).toBeNull()
      expect(result.current.isAuthenticated).toBe(false)
    })

    it('should reject an expired token', () => {
      const { result } = renderHook(() => useAuthStore())
      // Token expired 1 hour ago
      const expiredToken = createMockToken(-1)

      act(() => {
        result.current.setToken(expiredToken)
      })

      expect(result.current.token).toBeNull()
      expect(result.current.isAuthenticated).toBe(false)
    })

    it('should persist valid token to localStorage', () => {
      const { result } = renderHook(() => useAuthStore())
      const token = createMockToken()

      act(() => {
        result.current.setToken(token)
      })

      expect(localStorage.setItem).toHaveBeenCalledWith('token', token)
    })
  })

  describe('setUser', () => {
    it('should set user with existing token', () => {
      const { result } = renderHook(() => useAuthStore())
      const token = createMockToken()
      const user = createMockUser()

      act(() => {
        result.current.setToken(token)
        result.current.setUser(user)
      })

      expect(result.current.user).toEqual(user)
      expect(result.current.isAuthenticated).toBe(true)
    })

    it('should not authenticate without token', () => {
      const { result } = renderHook(() => useAuthStore())
      const user = createMockUser()

      act(() => {
        result.current.setUser(user)
      })

      expect(result.current.user).toEqual(user)
      expect(result.current.isAuthenticated).toBe(false) // No token yet
    })

    it('should set user to null on logout', () => {
      const { result } = renderHook(() => useAuthStore())
      const token = createMockToken()
      const user = createMockUser()

      act(() => {
        result.current.setToken(token)
        result.current.setUser(user)
      })

      expect(result.current.isAuthenticated).toBe(true)

      act(() => {
        result.current.logout()
      })

      expect(result.current.user).toBeNull()
      expect(result.current.token).toBeNull()
      expect(result.current.isAuthenticated).toBe(false)
    })
  })

  describe('validateToken', () => {
    it('should return true for valid token', () => {
      const { result } = renderHook(() => useAuthStore())
      const token = createMockToken()

      act(() => {
        result.current.setToken(token)
      })

      expect(result.current.validateToken()).toBe(true)
    })

    it('should return false for missing token', () => {
      const { result } = renderHook(() => useAuthStore())

      expect(result.current.validateToken()).toBe(false)
    })

    it('should return false for expired token', () => {
      const { result } = renderHook(() => useAuthStore())
      const expiredToken = createMockToken(-1)

      // Manually set expired token (bypass validation)
      act(() => {
        result.current.setToken(createMockToken(24)) // Set valid token first
      })

      // Now test with expired token by state manipulation
      useAuthStore.setState({ token: expiredToken })

      expect(result.current.validateToken()).toBe(false)
    })
  })

  describe('logout', () => {
    it('should clear all auth state', () => {
      const { result } = renderHook(() => useAuthStore())
      const token = createMockToken()
      const user = createMockUser()

      // Setup authenticated state
      act(() => {
        result.current.setToken(token)
        result.current.setUser(user)
      })

      expect(result.current.isAuthenticated).toBe(true)

      // Logout
      act(() => {
        result.current.logout()
      })

      expect(result.current.user).toBeNull()
      expect(result.current.token).toBeNull()
      expect(result.current.isAuthenticated).toBe(false)
      expect(localStorage.removeItem).toHaveBeenCalledWith('token')
      expect(localStorage.removeItem).toHaveBeenCalledWith('user')
    })
  })

  describe('persistence', () => {
    it('should have structured persistence', () => {
      const token = createMockToken()
      const user = createMockUser()

      // Test that store is properly initialized
      const { result } = renderHook(() => useAuthStore())

      // Manually test the structure
      act(() => {
        result.current.setToken(token)
        result.current.setUser(user)
      })

      expect(result.current.token).toBe(token)
      expect(result.current.user).toEqual(user)
    })
  })
})
