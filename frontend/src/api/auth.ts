import apiClient from './client'
import type { LoginRequest, RegisterRequest, LoginResponse, RefreshTokenResponse } from '@/types/auth'
import type { ApiResponse } from '@/types/api'

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

  /**
   * Refresh access token using refresh token
   */
  refreshToken: async (refreshToken: string): Promise<ApiResponse<RefreshTokenResponse>> => {
    return apiClient.post('/api/Auth/refresh', { refreshToken })
  },
}

export type { RefreshTokenResponse } from '@/types/auth'
