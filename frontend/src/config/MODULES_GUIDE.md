# 模块注册与扩展指南

## 概述

系统使用 **中央模块注册表** (`modules.ts`) 来管理所有业务模块的：
- 路由定义
- 菜单生成
- 权限映射

新增模块只需在注册表中补充配置，路由和菜单会自动生成，**无需修改** `App.tsx` 或 `AppLayout.tsx`。

## 快速入门：添加新模块

### 第 1 步：在 `src/config/modules.ts` 中补充模块配置

```typescript
{
  id: 'department',                                    // 唯一标识
  path: '/department',                                 // 路由路径
  label: 'layout.menuDepartment',                      // 菜单标签（uiText 键）
  iconComponent: TeamOutlined,                         // Ant Icon 组件
  permission: 'menu.department',                       // 访问权限码
  component: React.lazy(() => import('@/pages/Admin/DepartmentManagement')),
  permissions: [                                       // 此模块涉及的所有权限码
    'menu.department',
    'department.create',
    'department.update',
    'department.delete',
    'department.view',
  ],
  order: 40,                                          // 菜单顺序（越小越前）
}
```

### 第 2 步：在 `src/config/uiText.ts` 中添加文案

```typescript
// 中文
const zhText = {
  layout: {
    menuDepartment: '部门管理',
    // ...
  },
};

// 英文
const enText = {
  layout: {
    menuDepartment: 'Department',
    // ...
  },
};
```

### 第 3 步：创建页面组件

在 `src/pages/Admin/DepartmentManagement.tsx` 中实现业务逻辑。

### 第 4 步：在后端初始化权限

调用权限初始化脚本，补充上述权限码到权限表：

```typescript
import { getAllPermissionCodes } from '@/config/modules'

const codes = getAllPermissionCodes()
// 返回: ['menu.chat', 'chat.send', ..., 'menu.department', 'department.create', ...]
```

**完成！** 路由、菜单、权限会自动生成和管理。

## 高级用法

### 添加子模块（管理后台风格）

某些模块可能有多个子功能（如管理后台包含 Dashboard、用户管理、角色管理等）：

```typescript
{
  id: 'system',
  label: 'layout.menuSystem',
  iconComponent: SettingOutlined,
  permission: 'menu.system',
  order: 2000,
  children: [
    {
      id: 'system.config',
      path: '/system/config',
      label: 'layout.menuConfig',
      permission: 'menu.config',
      component: React.lazy(() => import('@/pages/System/ConfigPage')),
      permissions: ['menu.config', 'config.view', 'config.edit'],
    },
    {
      id: 'system.audit',
      path: '/system/audit',
      label: 'layout.menuAudit',
      permission: 'menu.audit',
      component: React.lazy(() => import('@/pages/System/AuditPage')),
      permissions: ['menu.audit', 'audit.view'],
    },
  ],
}
```

菜单会自动渲染为：
```
┌─ System Settings (icon)
├─ Config
└─ Audit
```

### 隐藏菜单项

某些路由不需要在菜单中显示（如 RBAC Debug）：

```typescript
{
  id: 'admin.debug-rbac',
  path: '/admin/debug-rbac',
  label: 'RBAC Debug',
  permission: 'menu.admin',
  component: React.lazy(() => import('@/pages/Admin/RBACDebug')),
  hideInMenu: true,  // ✅ 隐藏菜单
  permissions: [],
}
```

### 自定义菜单顺序

使用 `order` 属性控制菜单显示顺序：

```typescript
// 核心模块（order: 10-50）
{ id: 'chat', order: 10, ... }
{ id: 'documents', order: 20, ... }
{ id: 'agent', order: 30, ... }

// 管理后台（order: 1000+）
{ id: 'admin', order: 1000, ... }
```

## API 参考

### `ModuleConfig` 接口

```typescript
interface ModuleConfig {
  id: string                                    // 模块唯一 ID
  path?: string                                 // 路由路径
  label: string                                 // 菜单标签（uiText 键）
  iconComponent?: typeof MessageOutlined        // Ant Icon 组件
  permission?: string                           // 访问权限码
  component?: React.LazyExoticComponent<...>   // Lazy 页面组件
  hideInMenu?: boolean                          // 隐藏菜单（默认 false）
  children?: ModuleConfig[]                     // 子模块
  permissions?: string[]                        // 权限码列表
  order?: number                                // 菜单顺序
}
```

### `getRouteConfigs()` 函数

返回所有路由配置，用于 `App.tsx`：

```typescript
const routes = getRouteConfigs()
// 返回:
// [
//   { path: '/chat', id: 'chat', component: ChatPage },
//   { path: '/documents', id: 'documents', component: DocumentPage },
//   ...
// ]
```

### `getMenuConfigs(hasPermission)` 函数

返回菜单配置，用于 `AppLayout.tsx`：

```typescript
const menus = getMenuConfigs(hasPermission)
// 返回:
// [
//   {
//     key: '/chat',
//     iconComponent: MessageOutlined,
//     label: 'layout.menuChat',
//   },
//   {
//     key: 'admin',
//     iconComponent: DashboardOutlined,
//     label: 'layout.menuAdmin',
//     children: [...]
//   },
// ]
```

### `getAllPermissionCodes()` 函数

返回所有权限码集合，用于权限初始化：

```typescript
const codes = getAllPermissionCodes()
// 返回: ['menu.chat', 'chat.send', ..., 'menu.department', ...]
```

## 最佳实践

### ✅ 权限码命名规范

- 菜单权限：`menu.{module}` (e.g., `menu.chat`, `menu.user`)
- 操作权限：`{module}.{action}` (e.g., `chat.send`, `user.create`)

### ✅ 模块 ID 命名规范

- 顶级模块：kebab-case (e.g., `chat`, `documents`, `agent`)
- 子模块：`{parentId}.{childId}` (e.g., `admin.users`, `admin.roles`)

### ✅ 菜单标签键命名规范

- 统一使用 `layout.menu{ModuleName}` 格式
- 例如：`layout.menuChat`, `layout.menuDepartment`

### ✅ 路由路径规范

- 顶级模块：`/{module}` (e.g., `/chat`, `/documents`)
- 子模块：`/{parent}/{child}` (e.g., `/admin/users`, `/admin/roles`)

## 故障排除

### Q: 添加了模块但菜单没有显示

**A:** 检查以下几点：

1. **权限检查** - 确保当前用户有 `permission` 中指定的权限
2. **文案缺失** - 在 `uiText.ts` 中补充对应的 `label` 键
3. **hideInMenu** - 检查是否设置了 `hideInMenu: true`
4. **子模块过滤** - 子模块全部被权限过滤时，父菜单也会隐藏

### Q: 路由无法访问

**A:** 确保：

1. 模块配置中包含 `component` 属性
2. `path` 与实际路由路径一致
3. Lazy component 导入路径正确

### Q: 图标显示错误

**A:** 确保：

1. `iconComponent` 指向有效的 Ant Icon
2. Icon 组件已导入到 `modules.ts` 顶部
3. 未设置 `hideInMenu: true`（隐藏菜单的模块不显示图标）

## 延伸阅读

- [Ant Design Icons](https://ant.design/components/icon/)
- [React.lazy 文档](https://react.dev/reference/react/lazy)
- [权限管理架构](../ENTERPRISE_MATURITY.md#权限模型增强)
