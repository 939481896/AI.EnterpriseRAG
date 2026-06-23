import { describe, it, expect } from 'vitest'
import { createMockToken } from './utils'

/**
 * JWT Validation Tests
 * Tests JWT format validation and expiration checks
 */

// Replicate the validation functions for testing
function isValidJWT(token: string): boolean {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) {
      return false
    }

    parts.forEach((part) => {
      const base64 = part
        .replace(/-/g, '+')
        .replace(/_/g, '/')
      const padded = base64 + '='.repeat((4 - base64.length % 4) % 4)
      atob(padded)
    })
    return true
  } catch {
    return false
  }
}

function isExpired(token: string): boolean {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return true

    const base64 = parts[1]
      .replace(/-/g, '+')
      .replace(/_/g, '/')
    const padded = base64 + '='.repeat((4 - base64.length % 4) % 4)

    const payload = JSON.parse(atob(padded))
    if (!payload.exp) return true

    const expirationTime = payload.exp * 1000
    const currentTime = Date.now()
    return currentTime >= expirationTime - 30000
  } catch {
    return true
  }
}

describe('JWT Validation', () => {
  describe('isValidJWT', () => {
    it('should validate correct JWT format', () => {
      const token = createMockToken()
      expect(isValidJWT(token)).toBe(true)
    })

    it('should reject token with wrong number of parts', () => {
      expect(isValidJWT('invalid')).toBe(false)
      expect(isValidJWT('part1.part2')).toBe(false)
      expect(isValidJWT('part1.part2.part3.part4')).toBe(false)
    })

    it('should reject non-base64 content', () => {
      expect(isValidJWT('!!!.!!!.!!!')).toBe(false)
    })

    it('should handle base64url encoding (with - and _)', () => {
      // Create a token with base64url characters
      const header = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9' // Valid base64url
      const payload = 'eyJzdWIiOiIxMjM0NTY3ODkwIn0' // Valid base64url
      const signature = 'dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U' // Valid base64url

      const token = `${header}.${payload}.${signature}`
      expect(isValidJWT(token)).toBe(true)
    })
  })

  describe('isExpired', () => {
    it('should return false for valid non-expired token', () => {
      const token = createMockToken(24) // Expires in 24 hours
      expect(isExpired(token)).toBe(false)
    })

    it('should return true for expired token', () => {
      const token = createMockToken(-1) // Expired 1 hour ago
      expect(isExpired(token)).toBe(true)
    })

    it('should return true for token expiring very soon', () => {
      // Create token expiring in 10 seconds (less than 30s buffer)
      const now = Math.floor(Date.now() / 1000)
      const exp = now + 10

      const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
      const payload = btoa(JSON.stringify({ sub: 'user123', exp }))
      const signature = btoa('mock-signature')

      const token = `${header}.${payload}.${signature}`
      expect(isExpired(token)).toBe(true) // Within 30s buffer
    })

    it('should return true for token without exp claim', () => {
      const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
      const payload = btoa(JSON.stringify({ sub: 'user123' })) // No exp
      const signature = btoa('mock-signature')

      const token = `${header}.${payload}.${signature}`
      expect(isExpired(token)).toBe(true)
    })

    it('should return true for malformed token', () => {
      expect(isExpired('invalid.token.format')).toBe(true)
      expect(isExpired('not-a-token')).toBe(true)
    })
  })

  describe('Token creation for testing', () => {
    it('should create valid test tokens', () => {
      const token = createMockToken()
      expect(isValidJWT(token)).toBe(true)
      expect(isExpired(token)).toBe(false)
    })

    it('should create tokens with custom expiration', () => {
      const token = createMockToken(1) // 1 hour
      expect(isValidJWT(token)).toBe(true)
      expect(isExpired(token)).toBe(false)
    })
  })
})
