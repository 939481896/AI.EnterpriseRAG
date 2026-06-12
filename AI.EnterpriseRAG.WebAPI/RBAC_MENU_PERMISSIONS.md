# 菜单权限系统实现文档

## 📋 概述

本文档说明了企业RAG系统中的**菜单级权限**和**UI权限适配**功能的实现细节。

## 🎯 功能目标

1. **菜单级权限控制** - 不仅有操作级权限（如 `user.read`），还有菜单级权限（如 `menu.admin`）
2. **前端UI权限适配** - 根据用户权限动态显示/隐藏菜单和按钮
3. **细粒度访问控制** - 同时支持粗粒度（菜单）和细粒度（操作）权限控制

## 🏗️ 架构设计

### 权限层级

```
菜单权限 (粗粒度)
  └── menu.admin (访问管理后台)
      ├── menu.user (访问用户管理菜单)
      ├── menu.role (访问角色管理菜单)
      ├── menu.permission (访问权限管理菜单)
  └── menu.document (访问文档管理菜单)
  └── menu.chat (访问智能问答菜单)
  └── menu.agent (访问Agent工作区菜单)

操作权限 (细粒度)
  └── user.read, user.create, user.update, user.delete
  └── role.read, role.create, role.update, role.delete
  └── permission.read, permission.create, permission.update, permission.delete
  └── doc.read, doc.upload, doc.delete, doc.share
  └── chat.read, chat.ask, chat.history, chat.delete
  └── agent.read, agent.create, agent.update, agent.delete
```

## 📊 数据库权限列表

### 菜单权限 (7个)

| 权限代码 | 权限名称 | 说明 |
|---------|---------|-----|
| `menu.admin` | 访问管理后台 | 控制整个管理后台的访问 |
| `menu.user` | 访问用户管理菜单 | 控制用户管理页面的访问 |
| `menu.role` | 访问角色管理菜单 | 控制角色管理页面的访问 |
| `menu.permission` | 访问权限管理菜单 | 控制权限管理页面的访问 |
| `menu.document` | 访问文档管理菜单 | 控制文档管理页面的访问 |
| `menu.chat` | 访问智能问答菜单 | 控制智能问答页面的访问 |
| `menu.agent` | 访问Agent工作区菜单 | 控制Agent工作区页面的访问 |

### 操作权限 (35个)

详见原有的用户、角色、权限、文档、对话、Agent、系统管理等操作权限。

## 🔧 后端实现

### 1. DatabaseSeeder 更新

**文件位置**: `AI.EnterpriseRAG.WebAPI/Services/DatabaseSeeder.cs`

```csharp
private async Task SeedPermissionsAsync()
{
    var permissionDefinitions = new List<(string Code, string Name)>
    {
        // ==================== 菜单权限 ====================
        ("menu.admin", "访问管理后台"),
        ("menu.user", "访问用户管理菜单"),
        ("menu.role", "访问角色管理菜单"),
        ("menu.permission", "访问权限管理菜单"),
        ("menu.document", "访问文档管理菜单"),
        ("menu.chat", "访问智能问答菜单"),
        ("menu.agent", "访问Agent工作区菜单"),
        
        // ... 其他35个操作权限
    };
    
    // 增量式权限初始化
    // 检查已存在的权限，只添加缺失的权限
}
```

**特点**:
- ✅ 采用增量式初始化，不会重复添加权限
- ✅ 菜单权限放在列表最前面，便于管理
- ✅ 总共42个权限（7个菜单 + 35个操作）
- ✅ Admin角色自动获得所有权限（包括新增的菜单权限）

### 2. 自动权限分配

```csharp
private async Task AssignAdminPermissionsAsync()
{
    var adminRole = await _context.Roles
        .Include(r => r.RolePermissions)
        .FirstOrDefaultAsync(r => r.RoleCode == "admin");
        
    var allPermissions = await _context.Permissions.ToListAsync();
    
    var assignedPermissionIds = adminRole.RolePermissions
        .Select(rp => rp.PermissionId)
        .ToHashSet();
    
    // 自动分配所有缺失的权限给 admin 角色
    var missingPermissions = allPermissions
        .Where(p => !assignedPermissionIds.Contains(p.Id))
        .ToList();
        
    // ... 添加缺失的权限
}
```

## 🎨 前端实现

### 1. PermissionContext (权限上下文)

**文件位置**: `frontend/src/contexts/PermissionContext.tsx`

提供全局权限检查功能：

```typescript
// 权限提供者 - 包裹整个应用
<PermissionProvider>
  {children}
</PermissionProvider>

// 使用权限检查 Hook
const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermissionContext()

// 检查单个权限
if (hasPermission('menu.admin')) {
  // 显示管理后台菜单
}

// 检查多个权限（任一）
if (hasAnyPermission(['user.create', 'user.update'])) {
  // 显示用户操作按钮
}

// 检查多个权限（全部）
if (hasAllPermissions(['doc.read', 'doc.upload'])) {
  // 显示文档管理功能
}
```

### 2. PermissionGuard (权限守卫组件)

用于条件性渲染UI元素：

```typescript
// 单个权限守卫
<PermissionGuard permission="user.create">
  <Button type="primary" icon={<PlusOutlined />}>添加用户</Button>
</PermissionGuard>

// 任一权限守卫
<PermissionGuard anyPermissions={['role.update', 'role.delete']}>
  <Button>管理角色</Button>
</PermissionGuard>

// 全部权限守卫
<PermissionGuard allPermissions={['doc.read', 'doc.upload']}>
  <UploadButton />
</PermissionGuard>

// 带回退内容
<PermissionGuard permission="admin.access" fallback={<div>无权限</div>}>
  <AdminPanel />
</PermissionGuard>
```

### 3. App.tsx 集成

**文件位置**: `frontend/src/App.tsx`

```typescript
<ProtectedRoute>
  <PermissionProvider>  {/* 权限提供者包裹所有页面 */}
    <AppLayout>
      <Routes>
        {/* 所有路由 */}
      </Routes>
    </AppLayout>
  </PermissionProvider>
</ProtectedRoute>
```

### 4. AppLayout 菜单过滤

**文件位置**: `frontend/src/components/Layout/AppLayout.tsx`

根据用户权限动态生成菜单：

```typescript
const { hasPermission } = usePermissionContext()

const menuItems = useMemo(() => {
  const allMenuItems = [
    {
      key: '/chat',
      icon: <MessageOutlined />,
      label: '智能问答',
      permission: 'menu.chat',  // 菜单权限控制
    },
    {
      key: '/documents',
      icon: <FileTextOutlined />,
      label: '文档管理',
      permission: 'menu.document',
    },
    {
      key: 'admin',
      icon: <DashboardOutlined />,
      label: '管理后台',
      permission: 'menu.admin',
      children: [
        {
          key: '/admin/users',
          label: '用户管理',
          permission: 'menu.user',
        },
        // ... 其他子菜单
      ],
    },
  ]

  // 根据权限过滤菜单
  return allMenuItems
    .filter(item => !item.permission || hasPermission(item.permission))
    .map(item => {
      if (item.children) {
        const filteredChildren = item.children.filter(
          child => !child.permission || hasPermission(child.permission)
        )
        // 如果所有子菜单都被过滤，父菜单也不显示
        if (filteredChildren.length === 0) return null
        return { ...item, children: filteredChildren }
      }
      return item
    })
    .filter(Boolean)
}, [hasPermission])
```

### 5. 管理页面按钮权限控制

#### 用户管理页面

**文件位置**: `frontend/src/pages/Admin/UserManagement.tsx`

```typescript
// 页面标题按钮
<PermissionGuard permission="user.create">
  <Button type="primary" icon={<PlusOutlined />}>添加用户</Button>
</PermissionGuard>

// 表格操作列按钮
<PermissionGuard permission="user.update">
  <Button icon={<EditOutlined />}>编辑</Button>
</PermissionGuard>

<PermissionGuard permission="user.delete">
  <Popconfirm onConfirm={handleDelete}>
    <Button danger>删除</Button>
  </Popconfirm>
</PermissionGuard>
```

#### 角色管理页面

**文件位置**: `frontend/src/pages/Admin/RoleManagement.tsx`

```typescript
<PermissionGuard permission="role.create">
  <Button type="primary">新建角色</Button>
</PermissionGuard>

<PermissionGuard permission="role.update">
  <Button icon={<KeyOutlined />}>分配权限</Button>
</PermissionGuard>
```

#### 权限管理页面

**文件位置**: `frontend/src/pages/Admin/PermissionManagement.tsx`

```typescript
<PermissionGuard permission="permission.create">
  <Button type="primary">新建权限</Button>
</PermissionGuard>

<PermissionGuard permission="permission.update">
  <Button icon={<EditOutlined />}>编辑</Button>
</PermissionGuard>
```

## 🧪 测试场景

### 场景1: 管理员用户 (admin角色)

**预期行为**:
- ✅ 可以看到所有菜单（智能问答、文档管理、Agent工作区、管理后台）
- ✅ 管理后台下可以看到所有子菜单（数据面板、用户管理、角色管理、权限管理）
- ✅ 可以看到所有页面的所有操作按钮（添加、编辑、删除、分配）

### 场景2: 普通成员 (member角色)

**假设配置**: 
- 拥有权限: `menu.chat`, `menu.document`, `menu.agent`
- 没有权限: `menu.admin`, `menu.user`, `menu.role`, `menu.permission`

**预期行为**:
- ✅ 可以看到：智能问答、文档管理、Agent工作区菜单
- ❌ **不能看到**：管理后台菜单
- ✅ 侧边栏干净，只显示有权限的菜单

### 场景3: 访客用户 (guest角色)

**假设配置**:
- 拥有权限: `menu.chat`
- 没有权限: 其他所有菜单权限

**预期行为**:
- ✅ 只能看到：智能问答菜单
- ❌ **不能看到**：文档管理、Agent工作区、管理后台

### 场景4: 部分管理权限用户

**假设配置**:
- 拥有权限: `menu.admin`, `menu.user`, `user.read`, `user.update`
- 没有权限: `menu.role`, `menu.permission`, `user.create`, `user.delete`

**预期行为**:
- ✅ 可以看到：管理后台菜单
- ✅ 管理后台下可以看到：用户管理子菜单
- ❌ 管理后台下**不能看到**：角色管理、权限管理子菜单
- ✅ 用户管理页面可以看到：编辑按钮
- ❌ 用户管理页面**不能看到**：添加用户按钮、删除按钮

## 🔄 权限检查流程

```
用户登录
  ↓
JWT Token 包含 userId
  ↓
PermissionProvider 启动
  ↓
调用 useUserPermissions(userId)
  ↓
GET /api/user/{userId}/permissions
  ↓
返回用户的所有权限代码数组
  ↓
存储到 React Context
  ↓
组件使用 usePermissionContext
  ↓
hasPermission('menu.admin') 检查
  ↓
返回 true/false
  ↓
PermissionGuard 条件渲染
  ↓
有权限: 显示组件
无权限: 不显示或显示 fallback
```

## 📝 最佳实践

### 1. 菜单权限命名规范

```
menu.{模块名称}

例如:
✅ menu.admin
✅ menu.user
✅ menu.document
❌ admin.menu (不推荐)
❌ menuAdmin (不推荐)
```

### 2. 操作权限命名规范

```
{模块名称}.{操作类型}

例如:
✅ user.create
✅ doc.upload
✅ role.update
❌ createUser (不推荐)
❌ user_create (不推荐)
```

### 3. 权限粒度设计

- **菜单权限**: 控制整个模块/页面的访问（粗粒度）
- **操作权限**: 控制具体功能按钮的显示（细粒度）
- **推荐做法**: 同时配置菜单权限和操作权限，实现多层次的权限控制

### 4. React性能优化

- ✅ **使用 useMemo**: `hasPermission` 等函数都已 memoized
- ✅ **避免内联权限检查**: 使用 `PermissionGuard` 组件而非内联 `if` 判断
- ✅ **批量权限检查**: 使用 `hasAnyPermission` 或 `hasAllPermissions` 而非多个 `hasPermission`

## 🚀 部署步骤

### 1. 后端部署

```bash
cd AI.EnterpriseRAG.WebAPI
dotnet build
dotnet run
```

后端启动时会自动执行 `DatabaseSeeder`，增量添加菜单权限到数据库。

### 2. 前端部署

```bash
cd frontend
npm install
npm run dev
```

## 🔍 调试技巧

### 1. 查看用户权限

访问 `/admin/debug-rbac` 页面，可以看到：
- 当前用户信息
- 用户的所有角色
- 用户的所有权限（包括菜单权限和操作权限）

### 2. 浏览器控制台检查

```javascript
// 在浏览器控制台执行
localStorage.getItem('user')  // 查看当前用户信息
```

### 3. 检查权限API响应

```bash
# 查看用户权限列表
curl -H "Authorization: Bearer {token}" \
  http://localhost:5000/api/user/{userId}/permissions

# 应该看到类似的响应
[
  { "code": "menu.admin", "name": "访问管理后台" },
  { "code": "menu.user", "name": "访问用户管理菜单" },
  { "code": "user.read", "name": "查看用户" },
  ...
]
```

## ⚠️ 注意事项

1. **菜单权限 ≠ 路由权限**: 
   - 菜单权限控制UI显示
   - 后端API还需要独立的权限验证（通过 `[Authorize]` 特性）

2. **子菜单权限逻辑**:
   - 如果用户有子菜单权限但没有父菜单权限，父菜单不会显示
   - 如果用户有父菜单权限但所有子菜单都没有权限，父菜单也不会显示

3. **权限缓存**:
   - React Query 缓存时间: 5分钟 staleTime, 30分钟 gcTime
   - 如果修改了用户权限，需要退出重新登录或等待缓存过期

4. **Admin角色保护**:
   - Admin角色不能被删除
   - Admin角色的 `roleCode` 不能被修改
   - 建议至少保留一个 admin 用户

## 📚 相关文档

- [RBAC_IMPLEMENTATION_COMPLETE.md](./RBAC_IMPLEMENTATION_COMPLETE.md) - RBAC系统完整实现指南
- [RBAC_QUICK_START_TESTING.md](./RBAC_QUICK_START_TESTING.md) - 快速开始和测试指南
- [RBAC_ALL_ISSUES_RESOLVED.md](./RBAC_ALL_ISSUES_RESOLVED.md) - 所有已解决问题的总结
- [RBAC_INFINITE_RENDERS_FIXED.md](./RBAC_INFINITE_RENDERS_FIXED.md) - 无限渲染问题修复指南

## 🎉 总结

菜单权限系统现已完全实现，提供了：

✅ **两级权限控制** - 菜单级 + 操作级
✅ **动态UI适配** - 根据权限自动显示/隐藏
✅ **React高性能** - 使用 Context + useMemo + useCallback
✅ **增量式初始化** - 数据库权限自动同步
✅ **易于维护** - 统一的 PermissionGuard 组件
✅ **类型安全** - 完整的 TypeScript 类型支持

用户现在可以：
1. 配置菜单级权限控制整个模块的访问
2. 配置操作级权限控制具体功能的使用
3. 前端UI会自动根据权限隐藏无权访问的菜单和按钮
4. 提供更细粒度和更灵活的权限管理体验

---

**作者**: AI Enterprise RAG Team  
**日期**: 2024  
**版本**: 1.0.0
