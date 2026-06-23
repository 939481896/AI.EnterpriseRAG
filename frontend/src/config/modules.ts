/**
 * Module Registry - 模块元信息中心
 *
 * 所有业务模块在此注册，包括路由、菜单、权限。
 * 系统会自动从该配置生成路由与菜单，避免重复维护。
 *
 * 使用场景：
 * 1. 新增业务模块：在此补充配置，路由与菜单自动生成
 * 2. 权限管理：权限码规范化，便于权限初始化脚本
 * 3. 菜单治理：集中管理菜单层级、顺序、权限
 */

import React from 'react'
import {
  MessageOutlined,
  FileTextOutlined,
  RobotOutlined,
  DashboardOutlined,
} from '@ant-design/icons'

/**
 * 模块配置接口
 */
export interface ModuleConfig {
  /** 模块唯一ID */
  id: string
  /** 路由路径（仅顶级和二级需要） */
  path?: string
  /** 菜单标签（引用 uiText 键） */
  label: string
  /** 菜单图标（Ant Icon 组件） */
  iconComponent?: typeof MessageOutlined
  /** 访问权限码 */
  permission?: string
  /** 页面组件（lazy）*/
  component?: React.LazyExoticComponent<React.ComponentType<any>>
  /** 是否在菜单中显示 */
  hideInMenu?: boolean
  /** 子模块（用于管理后台等有多个功能的模块） */
  children?: ModuleConfig[]
  /** 权限码列表（此模块涉及的所有权限，用于权限初始化） */
  permissions?: string[]
  /** 菜单顺序 */
  order?: number
}

/**
 * 业务模块注册表
 *
 * 新增模块步骤：
 * 1. 在此补充 ModuleConfig
 * 2. 导入相应的 lazy component
 * 3. 在后端权限表中补充对应的权限码
 * 4. 完成！路由与菜单会自动生成
 */
export const moduleRegistry: ModuleConfig[] = [
  // ============ 核心业务模块 ============

  {
    id: 'chat',
    path: '/chat',
    label: 'layout.menuChat',
    iconComponent: MessageOutlined,
    permission: 'menu.chat',
    component: React.lazy(() => import('@/pages/Chat/ChatPage')),
    permissions: ['menu.chat', 'chat.send', 'chat.delete_session', 'chat.update_session_title'],
    order: 10,
  },

  {
    id: 'documents',
    path: '/documents',
    label: 'layout.menuDocument',
    iconComponent: FileTextOutlined,
    permission: 'menu.document',
    component: React.lazy(() => import('@/pages/Document/DocumentPage')),
    permissions: ['menu.document', 'document.upload', 'document.delete', 'document.view'],
    order: 20,
  },

  {
    id: 'agent',
    path: '/agent',
    label: 'layout.menuAgent',
    iconComponent: RobotOutlined,
    permission: 'menu.agent',
    component: React.lazy(() => import('@/pages/Agent/AgentWorkspace')),
    permissions: ['menu.agent', 'agent.execute', 'agent.view_history'],
    order: 30,
  },

  // ============ 管理后台 ============

  {
    id: 'admin',
    label: 'layout.menuAdmin',
    iconComponent: DashboardOutlined,
    permission: 'menu.admin',
    hideInMenu: false,
    order: 1000, // 菜单靠后
    children: [
      {
        id: 'admin.dashboard',
        path: '/admin/dashboard',
        label: 'layout.menuDashboard',
        permission: 'menu.admin',
        component: React.lazy(() => import('@/pages/Admin/Dashboard')),
        permissions: ['menu.admin', 'admin.dashboard.view'],
      },

      {
        id: 'admin.users',
        path: '/admin/users',
        label: 'layout.menuUsers',
        permission: 'menu.user',
        component: React.lazy(() => import('@/pages/Admin/UserManagement')),
        permissions: [
          'menu.user',
          'user.create',
          'user.update',
          'user.delete',
          'user.view',
          'user.assign_role',
        ],
      },

      {
        id: 'admin.roles',
        path: '/admin/roles',
        label: 'layout.menuRoles',
        permission: 'menu.role',
        component: React.lazy(() => import('@/pages/Admin/RoleManagement')),
        permissions: [
          'menu.role',
          'role.create',
          'role.update',
          'role.delete',
          'role.view',
          'role.assign_permission',
        ],
      },

      {
        id: 'admin.permissions',
        path: '/admin/permissions',
        label: 'layout.menuPermissions',
        permission: 'menu.permission',
        component: React.lazy(() => import('@/pages/Admin/PermissionManagement')),
        permissions: [
          'menu.permission',
          'permission.create',
          'permission.update',
          'permission.delete',
          'permission.view',
        ],
      },

      {
        id: 'admin.debug-rbac',
        path: '/admin/debug-rbac',
        label: 'RBAC Debug',
        permission: 'menu.admin', // Only admin users
        component: React.lazy(() => import('@/pages/Admin/RBACDebug')),
        hideInMenu: true, // Hidden in production
        permissions: [],
      },
    ],
  },
]

/**
 * 获取所有权限码集合（用于权限初始化）
 */
export function getAllPermissionCodes(): string[] {
  const codes = new Set<string>()

  const collect = (modules: ModuleConfig[]) => {
    modules.forEach((mod) => {
      // 添加菜单权限码
      if (mod.permission) {
        codes.add(mod.permission)
      }
      // 添加操作权限码
      if (mod.permissions) {
        mod.permissions.forEach((code) => codes.add(code))
      }
      // 递归子模块
      if (mod.children) {
        collect(mod.children)
      }
    })
  }

  collect(moduleRegistry)
  return Array.from(codes)
}

/**
 * 获取所有路由配置（用于 App.tsx）
 */
export function getRouteConfigs(): Array<{
  path: string
  id: string
  component: React.LazyExoticComponent<React.ComponentType<any>>
}> {
  const routes: Array<{
    path: string
    id: string
    component: React.LazyExoticComponent<React.ComponentType<any>>
  }> = []

  const collect = (modules: ModuleConfig[]) => {
    modules.forEach((mod) => {
      if (mod.path && mod.component) {
        routes.push({
          path: mod.path,
          id: mod.id,
          component: mod.component,
        })
      }
      if (mod.children) {
        collect(mod.children)
      }
    })
  }

  collect(moduleRegistry)
  return routes
}

/**
 * 获取菜单配置（用于 AppLayout.tsx）
 *
 * @param hasPermission - 权限检查函数
 */
export function getMenuConfigs(
  hasPermission: (permission: string) => boolean
): Array<{
  key: string
  iconComponent?: typeof MessageOutlined
  label: string
  children?: Array<{
    key: string
    label: string
  }>
}> {
  const menuItems: Array<{
    key: string
    iconComponent?: typeof MessageOutlined
    label: string
    children?: Array<{
      key: string
      label: string
    }>
  }> = []

  // Helper to translate label key to actual text
  const getLabel = (key: string) => {
    // In real usage, this would be uiText[key]
    // For now, return the key itself
    return key
  }

  const collect = (modules: ModuleConfig[]) => {
    modules
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .forEach((mod) => {
        // Skip if hidden or no permission
        if (mod.hideInMenu) {
          return
        }

        // Check permission
        if (mod.permission && !hasPermission(mod.permission)) {
          return
        }

        // Handle parent menu items with children
        if (mod.children) {
          const childMenus = mod.children
            .filter((child) => {
              if (child.hideInMenu) return false
              if (child.permission && !hasPermission(child.permission)) return false
              return true
            })
            .map((child) => ({
              key: child.path || `/admin/${child.id}`,
              label: getLabel(child.label),
            }))

          if (childMenus.length > 0) {
            menuItems.push({
              key: mod.id,
              iconComponent: mod.iconComponent as any,
              label: getLabel(mod.label),
              children: childMenus,
            })
          }
          return
        }

        // Handle leaf menu items
        menuItems.push({
          key: mod.path || `/${mod.id}`,
          iconComponent: mod.iconComponent as any,
          label: getLabel(mod.label),
        })
      })
  }

  collect(moduleRegistry)
  return menuItems
}
