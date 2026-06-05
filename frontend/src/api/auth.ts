import apiClient from './client'
import type { LoginRequest, RegisterRequest, LoginResponse, ApiResponse } from '@/types/auth'

export const authApi = {
  /**
   * User login
   */
  login: async (data: LoginRequest): Promise<ApiResponse<LoginResponse>> => {
    return apiClient.post('/api/auth/login', data)
  },

  /**
   * User registration
   */
  register: async (data: RegisterRequest): Promise<ApiResponse> => {
    return apiClient.post('/api/auth/register', data)
  },

  /**
   * Logout
   */
  logout: async (): Promise<void> => {
    // Clear local storage
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  },

  /**
   * Get current user info
   */
  getCurrentUser: async (): Promise<ApiResponse> => {
    return apiClient.get('/api/auth/me')
  },
}
