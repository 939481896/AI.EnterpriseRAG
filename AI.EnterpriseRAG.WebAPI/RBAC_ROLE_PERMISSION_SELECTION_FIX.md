# 🐛 角色权限配置 - 已分配权限未勾选问题修复

## 问题描述
在角色管理页面，点击"分配权限"按钮打开权限配置抽屉时，该角色已经分配的权限没有被自动勾选，导致用户不知道哪些权限已经配置过。

## 🔍 问题根源

### 原始代码问题
```typescript
const handleAssignPermissions = (role: Role) => {
  setCurrentRole(role)
  // ❌ 问题：role.permissions 是 undefined
  const currentPermissionIds = role.permissions?.map((p) => p.id) || []
  setSelectedPermissions(currentPermissionIds) // 总是设置为空数组
  setIsPermissionDrawerOpen(true)
}
```

### 为什么 `role.permissions` 是 undefined？

检查后端 API：

#### 1. 角色列表 API (`GET /api/role`)
```csharp
public async Task<IActionResult> GetRoles()
{
    var roles = await _context.Roles
        .Select(r => new
        {
            r.Id,
            r.RoleName,
            r.RoleCode,
            PermissionCount = r.RolePermissions.Count,  // ✅ 只返回数量
            UserCount = r.UserRoles.Count
        })
        .ToListAsync();
    
    return Ok(Result<object>.SuccessResult(roles));
}
```

**返回数据**：
```json
{
  "id": 1,
  "roleName": "管理员",
  "roleCode": "admin",
  "permissionCount": 42,  // ← 只有数量，没有详细列表
  "userCount": 1
}
```

#### 2. 角色详情 API (`GET /api/role/{id}`)
```csharp
public async Task<IActionResult> GetRole(long id)
{
    var role = await _context.Roles
        .Where(r => r.Id == id)
        .Select(r => new
        {
            r.Id,
            r.RoleName,
            r.RoleCode,
            Permissions = r.RolePermissions.Select(rp => new  // ✅ 返回完整权限列表
            {
                rp.Permission.Id,
                rp.Permission.Code,
                rp.Permission.Name
            }).ToList()
        })
        .FirstOrDefaultAsync();
    
    return Ok(Result<object>.SuccessResult(role));
}
```

**返回数据**：
```json
{
  "id": 1,
  "roleName": "管理员",
  "roleCode": "admin",
  "permissions": [  // ← 完整的权限列表
    { "id": 1, "code": "menu.admin", "name": "访问管理后台" },
    { "id": 2, "code": "menu.user", "name": "访问用户管理菜单" },
    ...
  ]
}
```

### 结论
列表 API 不返回 `permissions` 数组（出于性能考虑），只有详情 API 才返回。所以从列表页打开抽屉时，`role.permissions` 是 `undefined`。

## ✅ 修复方案

### 策略：按需加载角色详情

当用户点击"分配权限"时，才调用详情 API 获取该角色的完整权限信息。

### 1. 添加 `useRole` Hook（已存在）

```typescript
export function useRole(roleId: number | null) {
  return useQuery({
    queryKey: ['role', roleId],
    queryFn: async () => {
      if (!roleId) return null
      const response = await roleApi.getRole(roleId)
      return response.data
    },
    enabled: !!roleId,  // 只有 roleId 存在时才调用 API
    staleTime: 5 * 60 * 1000,
  })
}
```

### 2. 修改组件状态

```typescript
const RoleManagement: React.FC = () => {
  const [currentRole, setCurrentRole] = useState<Role | null>(null)
  const [currentRoleId, setCurrentRoleId] = useState<number | null>(null)  // ✅ 新增
  const [selectedPermissions, setSelectedPermissions] = useState<number[]>([])

  const { data: roles = [] } = useRoles()
  const { data: roleDetail, isLoading: isLoadingRoleDetail } = useRole(currentRoleId)  // ✅ 新增
  const { data: groupedPermissions = {} } = useGroupedPermissions()

  // ✅ 新增：当角色详情加载完成后，自动设置已选权限
  useEffect(() => {
    if (roleDetail && roleDetail.permissions) {
      const permissionIds = roleDetail.permissions.map((p: any) => p.id)
      setSelectedPermissions(permissionIds)
    }
  }, [roleDetail])
```

### 3. 修改打开抽屉的处理

```typescript
const handleAssignPermissions = (role: Role) => {
  setCurrentRole(role)
  setCurrentRoleId(role.id)  // ✅ 触发 useRole 加载详情
  setIsPermissionDrawerOpen(true)
}
```

### 4. 修改关闭抽屉的处理

```typescript
const handlePermissionDrawerClose = React.useCallback(() => {
  setIsPermissionDrawerOpen(false)
  setCurrentRole(null)
  setCurrentRoleId(null)  // ✅ 清除 roleId，停止加载
  setSelectedPermissions([])
}, [])
```

### 5. 添加加载状态提示

```typescript
<Drawer ...>
  {isLoadingPermissions || isLoadingRoleDetail ? (
    <div style={{ textAlign: 'center', padding: '40px 0' }}>
      <Spin />
      <div style={{ marginTop: 16 }}>
        {isLoadingRoleDetail ? '加载角色权限中...' : '加载权限数据中...'}
      </div>
    </div>
  ) : (
    <>
      <div style={{ marginBottom: 16, color: '#666' }}>
        已选择 {selectedPermissions.length} 个权限
      </div>
      <Tree
        checkable
        defaultExpandAll
        treeData={permissionTreeData}
        checkedKeys={selectedPermissions}  // ✅ 自动勾选已有权限
        onCheck={(checkedKeys) => {
          const leafKeys = (checkedKeys as number[]).filter((key) => typeof key === 'number')
          setSelectedPermissions(leafKeys)
        }}
      />
    </>
  )}
</Drawer>
```

## 📊 执行流程

### 修复前（问题）
```
用户点击"分配权限"
  ↓
从列表中的 role 对象获取 permissions
  ↓
role.permissions = undefined  // ❌ 列表 API 不返回
  ↓
setSelectedPermissions([])  // 设置为空数组
  ↓
Tree 组件没有任何勾选项  // ❌ 用户看不到已配置的权限
```

### 修复后（正确）
```
用户点击"分配权限"
  ↓
setCurrentRoleId(role.id)  // 触发 useRole hook
  ↓
调用 GET /api/role/{id}  // 获取角色详情
  ↓
返回包含 permissions 数组的完整数据
  ↓
useEffect 监听到 roleDetail 变化
  ↓
提取 permissionIds 并 setSelectedPermissions
  ↓
Tree 组件自动勾选对应的权限  // ✅ 显示已配置的权限
```

## 🧪 测试步骤

### 1. 准备测试数据
确保数据库中有：
- 至少一个角色（如 admin 角色）
- 该角色已分配部分权限

### 2. 打开角色管理页面
```
http://localhost:5173/admin/roles
```

### 3. 点击"分配权限"
找到任一角色，点击"分配权限"按钮

### 4. 验证加载状态
应该先看到：
```
[Spin 加载动画]
加载角色权限中...
```

### 5. 验证权限勾选
加载完成后，应该看到：
- ✅ 已选择 X 个权限（顶部提示）
- ✅ 树形结构中，已分配的权限被勾选
- ✅ 未分配的权限没有勾选

### 6. 测试修改权限
- 取消勾选一些权限
- 勾选一些新权限
- 点击"保存"
- 关闭抽屉
- 重新打开，验证修改是否保存

## 📝 API 响应示例

### 角色详情 API 响应
```json
{
  "success": true,
  "data": {
    "id": 1,
    "roleName": "管理员",
    "roleCode": "admin",
    "permissions": [
      { "id": 1, "code": "menu.admin", "name": "访问管理后台" },
      { "id": 2, "code": "menu.user", "name": "访问用户管理菜单" },
      { "id": 3, "code": "menu.role", "name": "访问角色管理菜单" },
      { "id": 8, "code": "user.read", "name": "查看用户" },
      { "id": 9, "code": "user.create", "name": "创建用户" },
      ...
    ]
  }
}
```

### Tree 组件勾选效果
```
☑ 菜单权限 (menu)
  ☑ 访问管理后台 (menu.admin)
  ☑ 访问用户管理菜单 (menu.user)
  ☑ 访问角色管理菜单 (menu.role)
  ☐ 访问权限管理菜单 (menu.permission)
  ☐ 访问文档管理菜单 (menu.document)

☑ 用户管理权限 (user)
  ☑ 查看用户 (user.read)
  ☑ 创建用户 (user.create)
  ☐ 更新用户 (user.update)
  ☐ 删除用户 (user.delete)
```

## 🎯 性能优化

### 1. React Query 缓存
```typescript
staleTime: 5 * 60 * 1000  // 5分钟内不会重复请求
```

如果5分钟内多次打开同一个角色的权限配置，会直接使用缓存数据，不会重复调用 API。

### 2. 条件请求
```typescript
enabled: !!roleId  // 只有 roleId 存在时才发起请求
```

抽屉关闭后，`roleId` 被设置为 `null`，查询会自动停止。

### 3. 列表 API 保持轻量
列表 API 只返回必要字段（名称、代码、统计数），不加载完整的权限列表，保持列表页的加载速度。

## ⚠️ 注意事项

### 1. 网络延迟
在慢速网络下，用户可能会看到短暂的加载状态。这是正常的，说明系统正在获取最新的权限数据。

### 2. 权限数据一致性
每次打开抽屉都会获取最新的权限数据，确保显示的是数据库中的最新状态。

### 3. 保存后刷新
保存权限后，会自动调用 `queryClient.invalidateQueries` 刷新角色列表和详情缓存，确保数据同步。

## 📚 相关代码文件

- ✅ `frontend/src/pages/Admin/RoleManagement.tsx` - 角色管理页面
- ✅ `frontend/src/hooks/usePermission.ts` - 权限相关 React Query hooks
- ✅ `frontend/src/api/permission.ts` - 权限 API 客户端
- ✅ `AI.EnterpriseRAG.WebAPI/Controllers/RoleController.cs` - 后端角色控制器

## ✅ 修复验证清单

- [x] 打开权限抽屉时显示加载状态
- [x] 加载完成后自动勾选已分配的权限
- [x] 可以修改权限勾选
- [x] 保存后权限更新成功
- [x] 再次打开抽屉，显示更新后的权限
- [x] 关闭抽屉后停止加载数据
- [x] 构建成功无错误

---

**修复完成时间**: 2024  
**问题类型**: 前端数据加载逻辑  
**严重程度**: 中等（影响用户体验，但不影响功能）  
**影响范围**: 角色管理页面的权限配置功能
