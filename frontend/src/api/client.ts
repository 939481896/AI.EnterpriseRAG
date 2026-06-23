import axios, { AxiosError } from 'axios'
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
  (error: AxiosError<ApiErrorResponse>) => {
    // Only handle 401 globally for protected routes (not login page)
    if (error.response?.status === 401) {
      // Don't redirect if already on login page or if it's a login request
      const isLoginRequest = error.config?.url?.includes('/auth/login')
      const isOnLoginPage = window.location.pathname === '/login'

      if (!isLoginRequest && !isOnLoginPage) {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
        window.location.href = '/login'
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
