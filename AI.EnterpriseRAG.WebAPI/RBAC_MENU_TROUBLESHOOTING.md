# 🐛 菜单不显示问题排查指南

## 问题描述
登录成功后，侧边栏菜单没有显示，但可以通过直接输入路由访问页面。

## 🔍 诊断步骤

### 步骤1: 检查浏览器控制台日志

启动前端应用后，登录系统，打开浏览器开发者工具（F12），查看 Console 标签页：

#### 应该看到的日志：

```javascript
🔐 [PermissionProvider] User: { id: "1", account: "admin", realName: "管理员", ... }
🔐 [PermissionProvider] UserId: 1
🔐 [PermissionProvider] PermissionsData: [{ code: "menu.admin", name: "访问管理后台" }, ...]
🔐 [PermissionProvider] IsLoading: false
🔐 [PermissionProvider] Permission codes: ["menu.admin", "menu.user", "menu.role", ...]

🔍 [AppLayout] Permissions: ["menu.admin", "menu.user", ...]
🔍 [AppLayout] IsLoading: false
📋 [AppLayout] Menu "智能问答" (menu.chat): true
📋 [AppLayout] Menu "文档管理" (menu.document): true
📋 [AppLayout] Menu "Agent 工作区" (menu.agent): true
📋 [AppLayout] Menu "管理后台" (menu.admin): true
  └─ Submenu "数据面板" (menu.admin): true
  └─ Submenu "用户管理" (menu.user): true
  └─ Submenu "角色管理" (menu.role): true
  └─ Submenu "权限管理" (menu.permission): true
✅ [AppLayout] Final filtered menu items: 4
```

### 步骤2: 检查可能的问题

#### 问题 A: 用户没有任何菜单权限

**症状**:
```javascript
🔐 [PermissionProvider] Permission codes: []  // ← 空数组
📋 [AppLayout] Menu "智能问答" (menu.chat): false
📋 [AppLayout] Menu "文档管理" (menu.document): false
✅ [AppLayout] Final filtered menu items: 0  // ← 没有菜单
```

**原因**: 用户没有被分配任何角色，或角色没有菜单权限

**解决方案**:
1. 以 admin 用户登录（admin/Admin@123）
2. 进入"用户管理"页面
3. 找到当前用户，点击"分配角色"
4. 至少分配一个有菜单权限的角色（如 admin 角色）
5. 退出并重新登录

#### 问题 B: 用户信息不存在

**症状**:
```javascript
🔐 [PermissionProvider] User: null  // ← 用户为 null
🔐 [PermissionProvider] UserId: null
🔐 [PermissionProvider] PermissionsData: []
```

**原因**: localStorage 中的用户信息丢失或损坏

**解决方案**:
1. 打开浏览器控制台
2. 运行: `localStorage.getItem('user')`
3. 如果返回 `null` 或格式错误，清空并重新登录:
   ```javascript
   localStorage.clear()
   location.reload()
   ```

#### 问题 C: 权限API请求失败

**症状**:
```javascript
🔐 [PermissionProvider] IsLoading: true  // ← 一直是 true
// 或者看到网络错误
```

**原因**: 后端API未启动或权限API返回错误

**解决方案**:
1. 检查 Network 标签页，查找 `/api/user/{userId}/permissions` 请求
2. 检查请求是否返回 200 状态码
3. 检查返回数据格式是否正确:
   ```json
   [
     { "id": 1, "code": "menu.admin", "name": "访问管理后台" },
     { "id": 2, "code": "menu.user", "name": "访问用户管理菜单" },
     ...
   ]
   ```
4. 如果请求失败，检查后端是否正常运行

#### 问题 D: 数据库未初始化菜单权限

**症状**:
```javascript
🔐 [PermissionProvider] Permission codes: ["user.read", "user.create", ...]
// 只有操作权限，没有 menu.* 开头的权限
```

**原因**: 数据库中没有菜单权限数据

**解决方案**:
1. 停止后端应用
2. 删除数据库中的旧数据（或备份后重新初始化）
3. 重新启动后端，DatabaseSeeder 会自动添加7个菜单权限
4. 或者手动在数据库中添加菜单权限

### 步骤3: 手动验证权限数据

#### 在浏览器控制台执行：

```javascript
// 1. 检查当前用户
const user = JSON.parse(localStorage.getItem('user'))
console.log('User:', user)

// 2. 检查 token
const token = localStorage.getItem('token')
console.log('Token:', token)

// 3. 手动调用权限 API
fetch(`http://localhost:5173/api/user/${user.id}/permissions`, {
  headers: {
    'Authorization': `Bearer ${token}`
  }
})
  .then(res => res.json())
  .then(data => console.log('Permissions:', data))
  .catch(err => console.error('Error:', err))
```

#### 预期结果：
```json
{
  "data": [
    { "id": 1, "code": "menu.admin", "name": "访问管理后台" },
    { "id": 2, "code": "menu.user", "name": "访问用户管理菜单" },
    { "id": 3, "code": "menu.role", "name": "访问角色管理菜单" },
    ...
  ]
}
```

### 步骤4: 检查数据库

连接到 MySQL 数据库，执行以下查询：

```sql
-- 1. 检查菜单权限是否存在
SELECT * FROM permission WHERE code LIKE 'menu.%';

-- 预期结果：应该有7条记录
-- menu.admin, menu.user, menu.role, menu.permission, menu.document, menu.chat, menu.agent

-- 2. 检查 admin 角色的权限
SELECT p.code, p.name
FROM permission p
JOIN role_permission rp ON p.id = rp.permission_id
JOIN role r ON rp.role_id = r.id
WHERE r.role_code = 'admin'
ORDER BY p.code;

-- 预期结果：应该包含所有42个权限（7个菜单 + 35个操作）

-- 3. 检查当前用户的角色
SELECT u.account, r.role_name, r.role_code
FROM sys_user u
JOIN sys_user_role sur ON u.id = sur.user_id
JOIN role r ON sur.role_id = r.id
WHERE u.account = 'admin';  -- 替换为您的用户名

-- 预期结果：应该至少有一个角色

-- 4. 检查当前用户的有效权限
SELECT DISTINCT p.code, p.name
FROM sys_user u
JOIN sys_user_role sur ON u.id = sur.user_id
JOIN role r ON sur.role_id = r.id
JOIN role_permission rp ON r.id = rp.role_id
JOIN permission p ON rp.permission_id = p.id
WHERE u.account = 'admin'  -- 替换为您的用户名
ORDER BY p.code;

-- 预期结果：应该有多个权限记录
```

## 🛠️ 快速修复方案

### 方案1: 重置 admin 用户权限

如果您是 admin 用户但菜单不显示：

```sql
-- 1. 确保 admin 角色有所有权限
DELETE FROM role_permission WHERE role_id = (SELECT id FROM role WHERE role_code = 'admin');

INSERT INTO role_permission (role_id, permission_id)
SELECT 
  (SELECT id FROM role WHERE role_code = 'admin'),
  p.id
FROM permission p;

-- 2. 确保 admin 用户有 admin 角色
INSERT IGNORE INTO sys_user_role (user_id, role_id)
VALUES (
  (SELECT id FROM sys_user WHERE account = 'admin'),
  (SELECT id FROM role WHERE role_code = 'admin')
);
```

### 方案2: 重新初始化数据库

如果数据库数据混乱：

1. 备份重要数据
2. 停止后端应用
3. 删除数据库：`DROP DATABASE your_database_name;`
4. 重新创建数据库：`CREATE DATABASE your_database_name;`
5. 启动后端应用，DatabaseSeeder 会自动初始化所有数据
6. 使用 admin/Admin@123 登录

### 方案3: 临时禁用权限检查（调试用）

如果您需要临时绕过权限检查来测试：

在 `AppLayout.tsx` 中，临时修改菜单过滤逻辑：

```typescript
// 临时方案：显示所有菜单（不检查权限）
const menuItems = useMemo(() => {
  const allMenuItems = [ ... ]
  
  // ⚠️ 临时禁用权限检查 - 仅用于调试
  return allMenuItems.map(item => {
    if (item.children) {
      return {
        ...item,
        children: item.children.map(({ permission, ...child }) => child),
      }
    }
    const { permission, ...menuItem } = item
    return menuItem
  })
  
  // 正常的权限检查逻辑...
}, [])
```

**警告**: 这只是临时调试方案，生产环境必须使用权限检查！

## 📊 正常工作的完整日志示例

```
🔐 [PermissionProvider] User: {id: "1", account: "admin", realName: "系统管理员"}
🔐 [PermissionProvider] UserId: 1
🔐 [PermissionProvider] PermissionsData: Array(42) [...]
🔐 [PermissionProvider] IsLoading: false
🔐 [PermissionProvider] Permission codes: Array(42) ["menu.admin", "menu.user", "menu.role", "menu.permission", "menu.document", "menu.chat", "menu.agent", "user.read", "user.create", ...]

🔍 [AppLayout] Permissions: Array(42) ["menu.admin", "menu.user", ...]
🔍 [AppLayout] IsLoading: false
📋 [AppLayout] Menu "智能问答" (menu.chat): true
📋 [AppLayout] Menu "文档管理" (menu.document): true
📋 [AppLayout] Menu "Agent 工作区" (menu.agent): true
📋 [AppLayout] Menu "管理后台" (menu.admin): true
  └─ Submenu "数据面板" (menu.admin): true
  └─ Submenu "用户管理" (menu.user): true
  └─ Submenu "角色管理" (menu.role): true
  └─ Submenu "权限管理" (menu.permission): true
✅ [AppLayout] Final filtered menu items: 4
```

## 🎯 总结

最常见的3个原因：

1. **用户没有被分配角色** → 分配角色给用户
2. **数据库未初始化菜单权限** → 重启后端让 DatabaseSeeder 运行
3. **权限API请求失败** → 检查后端是否运行，检查网络请求

按照上述步骤逐一排查，应该能快速定位并解决问题！

---

**需要帮助？**
- 查看浏览器控制台的完整日志
- 检查后端日志是否有错误
- 验证数据库中的权限数据是否完整
- 确认用户已分配正确的角色
