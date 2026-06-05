import axios, { AxiosError } from 'axios'
import { message } from 'antd'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 60000, // 60 seconds for LLM responses
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
    // Handle common errors
    if (error.response) {
      const { status, data } = error.response

      switch (status) {
        case 401:
          // Unauthorized - token expired or invalid
          message.error('登录已过期，请重新登录')
          localStorage.removeItem('token')
          localStorage.removeItem('user')
          window.location.href = '/login'
          break
        
        case 403:
          // Forbidden - no permission
          message.error('无权限访问该资源')
          break
        
        case 404:
          message.error('请求的资源不存在')
          break
        
        case 500:
          message.error(data?.message || '服务器内部错误')
          break
        
        default:
          message.error(data?.message || '请求失败，请稍后重试')
      }
    } else if (error.request) {
      // Request sent but no response
      message.error('网络连接失败，请检查网络')
    } else {
      // Error in request setup
      message.error('请求配置错误')
    }

    return Promise.reject(error)
  }
)

export default apiClient
