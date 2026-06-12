import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { roleApi, permissionApi, userRoleApi } from '@/api/permission'
import type { Permission } from '@/api/permission'

// ==================== Role Hooks ====================

/**
 * Get all roles
 */
export function useRoles() {
  return useQuery({
    queryKey: ['roles'],
    queryFn: async () => {
      const response = await roleApi.getRoles()
      return response.data || []
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 30 * 60 * 1000, // 30 minutes (previously cacheTime)
  })
}

/**
 * Get role by ID
 */
export function useRole(roleId: number | null) {
  return useQuery({
    queryKey: ['role', roleId],
    queryFn: async () => {
      if (!roleId) return null
      const response = await roleApi.getRole(roleId)
      return response.data
    },
    enabled: !!roleId,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Create role mutation
 */
export function useCreateRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: { roleName: string; roleCode: string; description?: string }) =>
      roleApi.createRole(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      message.success('角色创建成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '创建失败')
    },
  })
}

/**
 * Update role mutation
 */
export function useUpdateRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      roleId,
      data,
    }: {
      roleId: number
      data: { roleName: string; roleCode: string; description?: string }
    }) => roleApi.updateRole(roleId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      queryClient.invalidateQueries({ queryKey: ['role', variables.roleId] })
      message.success('角色更新成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '更新失败')
    },
  })
}

/**
 * Delete role mutation
 */
export function useDeleteRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (roleId: number) => roleApi.deleteRole(roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      message.success('角色删除成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '删除失败')
    },
  })
}

/**
 * Assign permissions to role
 */
export function useAssignRolePermissions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ roleId, permissionIds }: { roleId: number; permissionIds: number[] }) =>
      roleApi.assignPermissions(roleId, permissionIds),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      queryClient.invalidateQueries({ queryKey: ['role', variables.roleId] })
      message.success('权限分配成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '分配失败')
    },
  })
}

// ==================== Permission Hooks ====================

/**
 * Get all permissions
 */
export function usePermissions() {
  return useQuery({
    queryKey: ['permissions'],
    queryFn: async () => {
      const response = await permissionApi.getPermissions()
      return response.data || []
    },
    staleTime: 10 * 60 * 1000, // 10 minutes (permissions rarely change)
    gcTime: 60 * 60 * 1000, // 1 hour (previously cacheTime)
  })
}

/**
 * Get grouped permissions by module
 */
export function useGroupedPermissions() {
  return useQuery({
    queryKey: ['permissions', 'grouped'],
    queryFn: async () => {
      const response = await permissionApi.getGroupedPermissions()
      return response.data || {}
    },
    staleTime: 10 * 60 * 1000,
    gcTime: 60 * 60 * 1000, // (previously cacheTime)
  })
}

/**
 * Create permission mutation
 */
export function useCreatePermission() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: { code: string; name: string; description?: string }) =>
      permissionApi.createPermission(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['permissions'] })
      message.success('权限创建成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '创建失败')
    },
  })
}

/**
 * Update permission mutation
 */
export function useUpdatePermission() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      permissionId,
      data,
    }: {
      permissionId: number
      data: { code: string; name: string; description?: string }
    }) => permissionApi.updatePermission(permissionId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['permissions'] })
      message.success('权限更新成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '更新失败')
    },
  })
}

/**
 * Delete permission mutation
 */
export function useDeletePermission() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (permissionId: number) => permissionApi.deletePermission(permissionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['permissions'] })
      message.success('权限删除成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '删除失败')
    },
  })
}

// ==================== User Role Hooks ====================

/**
 * Get user's roles
 */
export function useUserRoles(userId: number | null) {
  return useQuery({
    queryKey: ['user-roles', userId],
    queryFn: async () => {
      if (!userId) return []
      const response = await userRoleApi.getUserRoles(userId)
      return response.data || []
    },
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Assign roles to user
 */
export function useAssignUserRoles() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, roleIds }: { userId: number; roleIds: number[] }) =>
      userRoleApi.assignRoles(userId, roleIds),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['user-roles', variables.userId] })
      queryClient.invalidateQueries({ queryKey: ['users'] }) // Refresh user list if needed
      message.success('角色分配成功')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '分配失败')
    },
  })
}

/**
 * Get user's effective permissions
 */
export function useUserPermissions(userId: number | null) {
  return useQuery({
    queryKey: ['user-permissions', userId],
    queryFn: async () => {
      if (!userId) return []
      const response = await userRoleApi.getUserPermissions(userId)
      return response.data || []
    },
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Check if current user has specific permission
 */
export function useHasPermission(permissionCode: string) {
  const userId = JSON.parse(localStorage.getItem('user') || '{}')?.id
  const { data: permissions = [] } = useUserPermissions(userId)

  return permissions.some((p: Permission) => p.code === permissionCode)
}

/**
 * Check if current user has any of the specified permissions
 */
export function useHasAnyPermission(permissionCodes: string[]) {
  const userId = JSON.parse(localStorage.getItem('user') || '{}')?.id
  const { data: permissions = [] } = useUserPermissions(userId)

  return permissionCodes.some((code) =>
    permissions.some((p: Permission) => p.code === code)
  )
}

/**
 * Check if current user has all of the specified permissions
 */
export function useHasAllPermissions(permissionCodes: string[]) {
  const userId = JSON.parse(localStorage.getItem('user') || '{}')?.id
  const { data: permissions = [] } = useUserPermissions(userId)

  return permissionCodes.every((code) =>
    permissions.some((p: Permission) => p.code === code)
  )
}
