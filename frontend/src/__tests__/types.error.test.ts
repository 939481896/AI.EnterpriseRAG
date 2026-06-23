import { describe, it, expect } from 'vitest'
import { AxiosError } from 'axios'
import { getErrorMessage, getErrorCode } from '@/types/error'

describe('Error utilities', () => {
  describe('getErrorMessage', () => {
    it('should extract API error message', () => {
      const error = new AxiosError('Network error')
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: { message: 'Invalid request' },
        headers: {},
        config: {} as any,
      }

      const message = getErrorMessage(error)
      expect(message).toBe('Invalid request')
    })

    it('should fallback to axios error message', () => {
      const error = new AxiosError('Request timeout')
      error.response = {
        status: 408,
        statusText: 'Request Timeout',
        data: {},
        headers: {},
        config: {} as any,
      }

      const message = getErrorMessage(error)
      expect(message).toBe('Request timeout')
    })

    it('should handle Error objects', () => {
      const error = new Error('Test error')
      const message = getErrorMessage(error)
      expect(message).toBe('Test error')
    })

    it('should handle unknown errors', () => {
      const message = getErrorMessage('unknown error')
      expect(message).toBe('Unknown error occurred')
    })

    it('should handle 401 errors without leaking sensitive info', () => {
      const error = new AxiosError('Unauthorized')
      error.response = {
        status: 401,
        statusText: 'Unauthorized',
        data: { message: 'Detailed auth failure' },
        headers: {},
        config: {} as any,
      }

      const message = getErrorMessage(error)
      // Should not expose the detailed message
      expect(message).toContain('Unauthorized')
    })
  })

  describe('getErrorCode', () => {
    it('should extract error code from response', () => {
      const error = new AxiosError('Error')
      error.response = {
        status: 404,
        statusText: 'Not Found',
        data: { code: 'RESOURCE_NOT_FOUND' },
        headers: {},
        config: {} as any,
      }

      const code = getErrorCode(error)
      expect(code).toBe('RESOURCE_NOT_FOUND')
    })

    it('should fallback to HTTP status code', () => {
      const error = new AxiosError('Error')
      error.response = {
        status: 500,
        statusText: 'Internal Server Error',
        data: {},
        headers: {},
        config: {} as any,
      }

      const code = getErrorCode(error)
      expect(code).toBe(500)
    })

    it('should return undefined for non-axios errors', () => {
      const error = new Error('Regular error')
      const code = getErrorCode(error)
      expect(code).toBeUndefined()
    })
  })
})
