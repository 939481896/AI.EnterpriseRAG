import axios, { AxiosError, AxiosInstance } from 'axios'
import DOMPurify from 'dompurify'
import { getApiBaseUrl, getApiTimeout } from '@/types/env'
import { getErrorMessage, type ApiErrorResponse } from '@/types/error'

// ✅ Helper function to sanitize response data
const sanitizeData = (data: unknown): unknown => {
  if (typeof data === 'string') {
    // Sanitize HTML content to prevent XSS
    return DOMPurify.sanitize(data, { ALLOWED_TAGS: [] })
  }

  if (data && typeof data === 'object') {
    if (Array.isArray(data)) {
      return data.map((item) => sanitizeData(item))
    }

    // Recursively sanitize object properties
    const sanitized: Record<string, unknown> = {}
    for (const [key, value] of Object.entries(data)) {
      sanitized[key] = sanitizeData(value)
    }
    return sanitized
  }

  return data
}

const apiClient = axios.create({
  baseURL: getApiBaseUrl(),
  timeout: getApiTimeout(),
  headers: {
    'Content-Type': 'application/json',
  },
})

// ✅ Track refresh request to avoid infinite loops
let isRefreshing = false
let failedQueue: Array<{
  resolve: (token: string) => void
  reject: (error: Error) => void
}> = []

const processQueue = (token: string | null, error?: Error) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else if (token) {
      prom.resolve(token)
    }
  })

  failedQueue = []
}

// Request interceptor - add JWT token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor - handle errors globally
apiClient.interceptors.response.use(
  (response) => {
    // ✅ Sanitize response data to prevent XSS attacks
    let data = response.data

    try {
      data = sanitizeData(data)
    } catch (sanitizeError) {
      console.error('[API] Failed to sanitize response data', sanitizeError)
      // Still return data even if sanitization fails
    }

    // Return data directly if response structure is { success: true, data: ... }
    if (data?.success !== undefined) {
      return data
    }
    return data
  },
  async (error: AxiosError<ApiErrorResponse>) => {
    const originalRequest = error.config as any

    // ✅ Handle 401 - try to refresh token
    if (error.response?.status === 401) {
      const isLoginRequest = error.config?.url?.includes('/auth/login')
      const isRefreshRequest = error.config?.url?.includes('/Auth/refresh')
      const isOnLoginPage = window.location.pathname === '/login'

      // Don't try to refresh for login/refresh requests
      if (isLoginRequest || isRefreshRequest || isOnLoginPage) {
        localStorage.removeItem('token')
        localStorage.removeItem('refreshToken')
        localStorage.removeItem('user')
        window.location.href = '/login'
        return Promise.reject(error)
      }

      // ✅ If not already refreshing, attempt token refresh
      if (!isRefreshing) {
        isRefreshing = true

        try {
          const refreshToken = localStorage.getItem('refreshToken')

          if (!refreshToken) {
            throw new Error('No refresh token available')
          }

          // Import dynamically to avoid circular dependency
          const { authApi } = await import('./auth')

          console.log('[Auth] Attempting to refresh token...')
          const response = await authApi.refreshToken(refreshToken)

          const newAccessToken = response.data?.accessToken

          if (newAccessToken) {
            console.log('[Auth] Token refreshed successfully')
            localStorage.setItem('token', newAccessToken)

            // Update new refresh token if provided
            if (response.data?.refreshToken) {
              localStorage.setItem('refreshToken', response.data.refreshToken)
            }

            // Update the original request with new token
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`

            // Process queued requests with new token
            processQueue(newAccessToken)

            // Retry original request
            isRefreshing = false
            return apiClient(originalRequest)
          } else {
            throw new Error('No access token in refresh response')
          }
        } catch (refreshError) {
          console.error('[Auth] Token refresh failed:', getErrorMessage(refreshError))

          // Clear auth on refresh failure
          localStorage.removeItem('token')
          localStorage.removeItem('refreshToken')
          localStorage.removeItem('user')

          processQueue(null, new Error('Token refresh failed'))
          isRefreshing = false
          window.location.href = '/login'
          return Promise.reject(refreshError)
        }
      } else {
        // If already refreshing, queue this request
        return new Promise((resolve, reject) => {
          failedQueue.push({
            resolve: (token: string) => {
              originalRequest.headers.Authorization = `Bearer ${token}`
              resolve(apiClient(originalRequest))
            },
            reject: reject,
          })
        })
      }
    }

    // ✅ Sanitize error messages to prevent XSS
    if (error.response?.data?.message) {
      try {
        error.response.data.message = DOMPurify.sanitize(error.response.data.message, {
          ALLOWED_TAGS: [],
        })
      } catch (sanitizeError) {
        console.error('[API] Failed to sanitize error message', getErrorMessage(sanitizeError))
      }
    }

    // ✅ Security: Don't log sensitive information in production
    if (import.meta.env.MODE !== 'production') {
      console.error('[API] Request failed', {
        status: error.response?.status,
        path: error.config?.url,
        // Do not log full error response in production
      })
    }

    // Don't show error messages here to prevent duplicates
    return Promise.reject(error)
  }
)

export default apiClient
