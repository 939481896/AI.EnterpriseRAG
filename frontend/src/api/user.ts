import apiClient from './client'
import type { User } from '@/types/auth'
import type { ApiResponse } from '@/types/api'

export const userApi = {
  /**
   * Get user list
   */
  getUsers: async (page = 1, pageSize = 20): Promise<ApiResponse<{
    items: User[]
    total: number
    page: number
    pageSize: number
  }>> => {
    return apiClient.get('/api/user/list', {
      params: { page, pageSize },
    })
  },

  /**
   * Get user by ID
   */
  getUser: async (userId: string): Promise<ApiResponse<User>> => {
    return apiClient.get(`/api/user/${userId}`)
  },

  /**
   * Create new user
   */
  createUser: async (data: {
    account: string
    password: string
    realName: string
    email: string
    phone?: string
    department?: string
  }): Promise<ApiResponse> => {
    return apiClient.post('/api/user', data)
  },

  /**
   * Update user
   */
  updateUser: async (userId: string, data: {
    realName: string
    email: string
    phone?: string
    department?: string
  }): Promise<ApiResponse> => {
    return apiClient.put(`/api/user/${userId}`, data)
  },

  /**
   * Delete user
   */
  deleteUser: async (userId: string): Promise<ApiResponse> => {
    return apiClient.delete(`/api/user/${userId}`)
  },

  /**
   * Toggle user status
   */
  toggleUserStatus: async (userId: string, isActive: boolean): Promise<ApiResponse> => {
    return apiClient.patch(`/api/user/${userId}/status`, { isActive })
  },

  /**
   * Reset user password
   */
  resetPassword: async (userId: string, newPassword: string): Promise<ApiResponse> => {
    return apiClient.post(`/api/user/${userId}/reset-password`, { newPassword })
  },
}
