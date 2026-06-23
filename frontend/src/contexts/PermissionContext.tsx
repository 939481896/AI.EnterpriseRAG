import React, { createContext, useContext, useMemo } from 'react'
import { useAuthStore } from '@/store/authStore'

interface PermissionContextValue {
  permissions: string[]
  hasPermission: (permission: string) => boolean
  hasAnyPermission: (permissions: string[]) => boolean
  hasAllPermissions: (permissions: string[]) => boolean
  isLoading: boolean
}

const PermissionContext = createContext<PermissionContextValue>({
  permissions: [],
  hasPermission: () => false,
  hasAnyPermission: () => false,
  hasAllPermissions: () => false,
  isLoading: false,
})

export const usePermissionContext = () => useContext(PermissionContext)

/**
 * Permission Provider - 提供权限上下文给整个应用
 * ✅ 使用登录时返回的权限列表，不需要额外 API 调用
 */
export const PermissionProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user } = useAuthStore()

  // ✅ 直接使用登录时返回的权限列表
  const permissions = useMemo(() => {
    return user?.permissions || []
  }, [user?.permissions])

  // 检查单个权限
  const hasPermission = useMemo(
    () => (permission: string) => permissions.includes(permission),
    [permissions]
  )

  // 检查是否拥有任一权限
  const hasAnyPermission = useMemo(
    () => (perms: string[]) => perms.some((p) => permissions.includes(p)),
    [permissions]
  )

  // 检查是否拥有所有权限
  const hasAllPermissions = useMemo(
    () => (perms: string[]) => perms.every((p) => permissions.includes(p)),
    [permissions]
  )

  const value = useMemo(
    () => ({
      permissions,
      hasPermission,
      hasAnyPermission,
      hasAllPermissions,
      isLoading: false,
    }),
    [permissions, hasPermission, hasAnyPermission, hasAllPermissions]
  )

  return <PermissionContext.Provider value={value}>{children}</PermissionContext.Provider>
}

/**
 * PermissionGuard - 权限守卫组件
 * 根据权限显示/隐藏子组件
 */
interface PermissionGuardProps {
  permission?: string
  anyPermissions?: string[]
  allPermissions?: string[]
  fallback?: React.ReactNode
  children: React.ReactNode
}

export const PermissionGuard: React.FC<PermissionGuardProps> = ({
  permission,
  anyPermissions,
  allPermissions,
  fallback = null,
  children,
}) => {
  const { hasPermission, hasAnyPermission, hasAllPermissions, isLoading } = usePermissionContext()

  // 加载中时不渲染
  if (isLoading) {
    return <>{fallback}</>
  }

  // 检查权限
  let hasAccess = true

  if (permission) {
    hasAccess = hasPermission(permission)
  } else if (anyPermissions) {
    hasAccess = hasAnyPermission(anyPermissions)
  } else if (allPermissions) {
    hasAccess = hasAllPermissions(allPermissions)
  }

  return hasAccess ? <>{children}</> : <>{fallback}</>
}
