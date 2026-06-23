import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { roleApi, permissionApi, userRoleApi } from '@/api/permission'
import type { Permission } from '@/api/permission'
import { notification } from '@/services/notification'
import { uiText } from '@/config/uiText'
import { getErrorMessage } from '@/types/error'
import { queryKeys } from '@/config/queryKeys'

/**
 * RBAC hooks module.
 *
 * Design notes:
 * - Read operations use React Query cache with long staleTime.
 * - Mutations use local notifications and silentError to avoid duplicate global toasts.
 * - Keys are centralized through queryKeys to keep invalidation consistent.
 */

// ==================== Role Hooks ====================

/**
 * Get all roles
 */
export function useRoles() {
  return useQuery({
    queryKey: queryKeys.permission.roles,
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
    queryKey: queryKeys.permission.role(roleId),
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
    meta: { silentError: true },
    mutationFn: (data: { roleName: string; roleCode: string; description?: string }) =>
      roleApi.createRole(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.roles })
      notification.success(uiText.feedback.roleCreateSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.createFailed)
    },
  })
}

/**
 * Update role mutation
 */
export function useUpdateRole() {
  const queryClient = useQueryClient()

  return useMutation({
    meta: { silentError: true },
    mutationFn: ({
      roleId,
      data,
    }: {
      roleId: number
      data: { roleName: string; roleCode: string; description?: string }
    }) => roleApi.updateRole(roleId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.roles })
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.role(variables.roleId) })
      notification.success(uiText.feedback.roleUpdateSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.updateFailed)
    },
  })
}

/**
 * Delete role mutation
 */
export function useDeleteRole() {
  const queryClient = useQueryClient()

  return useMutation({
    meta: { silentError: true },
    mutationFn: (roleId: number) => roleApi.deleteRole(roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.roles })
      notification.success(uiText.feedback.roleDeleteSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.deleteFailed)
    },
  })
}

/**
 * Assign permissions to role
 */
export function useAssignRolePermissions() {
  const queryClient = useQueryClient()

  return useMutation({
    meta: { silentError: true },
    mutationFn: ({ roleId, permissionIds }: { roleId: number; permissionIds: number[] }) =>
      roleApi.assignPermissions(roleId, permissionIds),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.roles })
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.role(variables.roleId) })
      notification.success(uiText.feedback.permissionAssignSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.assignFailed)
    },
  })
}

// ==================== Permission Hooks ====================

/**
 * Get all permissions
 */
export function usePermissions() {
  return useQuery({
    queryKey: queryKeys.permission.permissions,
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
    queryKey: queryKeys.permission.groupedPermissions,
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
    meta: { silentError: true },
    mutationFn: (data: { code: string; name: string; description?: string }) =>
      permissionApi.createPermission(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.permissions })
      notification.success(uiText.feedback.permissionCreateSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.createFailed)
    },
  })
}

/**
 * Update permission mutation
 */
export function useUpdatePermission() {
  const queryClient = useQueryClient()

  return useMutation({
    meta: { silentError: true },
    mutationFn: ({
      permissionId,
      data,
    }: {
      permissionId: number
      data: { code: string; name: string; description?: string }
    }) => permissionApi.updatePermission(permissionId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.permissions })
      notification.success(uiText.feedback.permissionUpdateSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.updateFailed)
    },
  })
}

/**
 * Delete permission mutation
 */
export function useDeletePermission() {
  const queryClient = useQueryClient()

  return useMutation({
    meta: { silentError: true },
    mutationFn: (permissionId: number) => permissionApi.deletePermission(permissionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.permissions })
      notification.success(uiText.feedback.permissionDeleteSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.deleteFailed)
    },
  })
}

// ==================== User Role Hooks ====================

/**
 * Get user's roles
 */
export function useUserRoles(userId: number | null) {
  return useQuery({
    queryKey: queryKeys.permission.userRoles(userId),
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
    meta: { silentError: true },
    mutationFn: ({ userId, roleIds }: { userId: number; roleIds: number[] }) =>
      userRoleApi.assignRoles(userId, roleIds),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.permission.userRoles(variables.userId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.user.list }) // Refresh user list if needed
      notification.success(uiText.feedback.userRoleAssignSuccess)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.feedback.assignFailed)
    },
  })
}

/**
 * Get user's effective permissions
 */
export function useUserPermissions(userId: number | null) {
  return useQuery({
    queryKey: queryKeys.permission.userPermissions(userId),
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
  // Permission checks are intentionally lightweight and memoized by React Query cache.
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
