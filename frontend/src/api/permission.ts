import apiClient from './client'
import type { ApiResponse } from '@/types/api'

// ==================== Types ====================
export interface Permission {
  id: number
  code: string
  name: string
  description?: string
  module?: string
  roleCount?: number
}

export interface Role {
  id: number
  roleName: string
  roleCode: string
  description?: string
  userCount?: number
  permissionCount?: number
  permissions?: Permission[]
}

export interface UserRole {
  userId: number
  userName: string
  roles: Role[]
}

export interface GroupedPermissions {
  [module: string]: Permission[]
}

// ==================== Role API ====================
export const roleApi = {
  /**
   * Get all roles
   */
  getRoles: async (): Promise<ApiResponse<Role[]>> => {
    return apiClient.get('/api/role')
  },

  /**
   * Get role by ID
   */
  getRole: async (roleId: number): Promise<ApiResponse<Role>> => {
    return apiClient.get(`/api/role/${roleId}`)
  },

  /**
   * Create new role
   */
  createRole: async (data: {
    roleName: string
    roleCode: string
    description?: string
  }): Promise<ApiResponse<Role>> => {
    return apiClient.post('/api/role', data)
  },

  /**
   * Update role
   */
  updateRole: async (roleId: number, data: {
    roleName: string
    roleCode: string
    description?: string
  }): Promise<ApiResponse> => {
    return apiClient.put(`/api/role/${roleId}`, data)
  },

  /**
   * Delete role
   */
  deleteRole: async (roleId: number): Promise<ApiResponse> => {
    return apiClient.delete(`/api/role/${roleId}`)
  },

  /**
   * Assign permissions to role
   */
  assignPermissions: async (roleId: number, permissionIds: number[]): Promise<ApiResponse> => {
    return apiClient.post(`/api/role/${roleId}/permissions`, { permissionIds })
  },
}

// ==================== Permission API ====================
export const permissionApi = {
  /**
   * Get all permissions
   */
  getPermissions: async (): Promise<ApiResponse<Permission[]>> => {
    return apiClient.get('/api/systempermission')
  },

  /**
   * Get grouped permissions by module
   */
  getGroupedPermissions: async (): Promise<ApiResponse<GroupedPermissions>> => {
    return apiClient.get('/api/systempermission/grouped')
  },

  /**
   * Get permission by ID
   */
  getPermission: async (permissionId: number): Promise<ApiResponse<Permission>> => {
    return apiClient.get(`/api/systempermission/${permissionId}`)
  },

  /**
   * Create new permission
   */
  createPermission: async (data: {
    code: string
    name: string
    description?: string
  }): Promise<ApiResponse<Permission>> => {
    return apiClient.post('/api/systempermission', data)
  },

  /**
   * Update permission
   */
  updatePermission: async (permissionId: number, data: {
    code: string
    name: string
    description?: string
  }): Promise<ApiResponse> => {
    return apiClient.put(`/api/systempermission/${permissionId}`, data)
  },

  /**
   * Delete permission
   */
  deletePermission: async (permissionId: number): Promise<ApiResponse> => {
    return apiClient.delete(`/api/systempermission/${permissionId}`)
  },
}

// ==================== User Role API ====================
export const userRoleApi = {
  /**
   * Get user's roles
   */
  getUserRoles: async (userId: number): Promise<ApiResponse<Role[]>> => {
    return apiClient.get(`/api/user/${userId}/roles`)
  },

  /**
   * Assign roles to user
   */
  assignRoles: async (userId: number, roleIds: number[]): Promise<ApiResponse> => {
    return apiClient.post(`/api/user/${userId}/roles`, { roleIds })
  },

  /**
   * Get user's effective permissions
   */
  getUserPermissions: async (userId: number): Promise<ApiResponse<Permission[]>> => {
    return apiClient.get(`/api/user/${userId}/permissions`)
  },
}
