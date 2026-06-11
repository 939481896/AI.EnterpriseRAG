import axios, { AxiosError } from 'axios'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5243',
  timeout: 300000, // 🆕 5 minutes (300s) for LLM responses - matching backend timeout
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
    // Return data directly if response structure is { success: true, data: ... }
    if (response.data?.success !== undefined) {
      return response.data
    }
    return response.data
  },
  (error: AxiosError<any>) => {
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

    // Don't show error messages here to prevent duplicates
    return Promise.reject(error)
  }
)

export default apiClient
