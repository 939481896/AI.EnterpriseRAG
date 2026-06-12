# 🔐 权限管理系统完整实施指南

## 📋 项目概述

实现完整的 RBAC（基于角色的访问控制）系统，包括：
- ✅ 角色管理
- ✅ 权限管理  
- ✅ 用户角色分配
- ✅ 文档权限管理（已存在）
- ✅ 数据库种子数据初始化

---

## 🎯 已完成的后端部分

### 1. 实体层（Domain Layer）✅
- `SysUser` - 用户实体
- `SysRole` - 角色实体
- `Permission` - 权限实体
- `SysUserRole` - 用户角色关联
- `RolePermission` - 角色权限关联
- 文档权限相关实体（已存在）

### 2. 新增Controllers ✅

#### RoleController.cs
```
GET    /api/role              - 获取所有角色
GET    /api/role/{id}         - 获取角色详情
POST   /api/role              - 创建角色
PUT    /api/role/{id}         - 更新角色
DELETE /api/role/{id}         - 删除角色
POST   /api/role/{id}/permissions - 为角色分配权限
```

#### SystemPermissionController.cs
```
GET    /api/systempermission           - 获取所有权限
GET    /api/systempermission/{id}      - 获取权限详情
POST   /api/systempermission           - 创建权限
PUT    /api/systempermission/{id}      - 更新权限
DELETE /api/systempermission/{id}      - 删除权限
GET    /api/systempermission/grouped   - 获取分组权限
```

#### UserController (扩展) ✅
```
GET    /api/user/{id}/roles        - 获取用户角色
POST   /api/user/{id}/roles        - 为用户分配角色
GET    /api/user/{id}/permissions  - 获取用户权限
```

### 3. 种子数据服务 ✅
`DatabaseSeeder.cs` - 自动初始化：
- 35个系统权限
- 3个默认角色（admin, member, guest）
- 管理员用户（admin/Admin@123）
- 权限分配

---

## 🔧 后端集成步骤

### 步骤1：注册DatabaseSeeder服务

在 `Program.cs` 中添加：

```csharp
// 在 builder.Services 部分添加
builder.Services.AddScoped<DatabaseSeeder>();

// 在 app 构建后，app.Run() 之前添加
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}
```

### 步骤2：更新数据库

```bash
# 如果使用EF Migrations
dotnet ef migrations add AddPermissionSystem
dotnet ef database update

# 或者直接运行程序，DatabaseSeeder会自动初始化
dotnet run
```

### 步骤3：验证API

使用Postman/Swagger测试：

#### 获取所有角色
```http
GET http://localhost:5000/api/role
Authorization: Bearer {token}
```

#### 获取所有权限
```http
GET http://localhost:5000/api/systempermission
Authorization: Bearer {token}
```

---

## 🎨 前端实施计划

### 第一阶段：创建前端API客户端

创建 `frontend/src/api/permission.ts`:

```typescript
import request from '@/utils/request'

// ==================== 角色管理 ====================
export const roleApi = {
  // 获取所有角色
  getRoles: () => request.get('/api/role'),
  
  // 获取角色详情
  getRole: (id: number) => request.get(`/api/role/${id}`),
  
  // 创建角色
  createRole: (data: { roleName: string; roleCode: string }) => 
    request.post('/api/role', data),
  
  // 更新角色
  updateRole: (id: number, data: { roleName: string; roleCode: string }) => 
    request.put(`/api/role/${id}`, data),
  
  // 删除角色
  deleteRole: (id: number) => request.delete(`/api/role/${id}`),
  
  // 分配权限
  assignPermissions: (roleId: number, permissionIds: number[]) => 
    request.post(`/api/role/${roleId}/permissions`, { permissionIds }),
}

// ==================== 权限管理 ====================
export const permissionApi = {
  // 获取所有权限
  getPermissions: () => request.get('/api/systempermission'),
  
  // 获取权限详情
  getPermission: (id: number) => request.get(`/api/systempermission/${id}`),
  
  // 获取分组权限
  getGroupedPermissions: () => request.get('/api/systempermission/grouped'),
  
  // 创建权限
  createPermission: (data: { code: string; name: string }) => 
    request.post('/api/systempermission', data),
  
  // 更新权限
  updatePermission: (id: number, data: { code: string; name: string }) => 
    request.put(`/api/systempermission/${id}`, data),
  
  // 删除权限
  deletePermission: (id: number) => request.delete(`/api/systempermission/${id}`),
}

// ==================== 用户角色管理 ====================
export const userRoleApi = {
  // 获取用户角色
  getUserRoles: (userId: number) => request.get(`/api/user/${userId}/roles`),
  
  // 分配用户角色
  assignUserRoles: (userId: number, roleIds: number[]) => 
    request.post(`/api/user/${userId}/roles`, { roleIds }),
  
  // 获取用户权限
  getUserPermissions: (userId: number) => request.get(`/api/user/${userId}/permissions`),
}
```

### 第二阶段：创建前端Hooks

创建 `frontend/src/hooks/usePermission.ts`:

```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { roleApi, permissionApi, userRoleApi } from '@/api/permission'

// ==================== 角色Hooks ====================
export function useRoles() {
  return useQuery({
    queryKey: ['roles'],
    queryFn: async () => {
      const response = await roleApi.getRoles()
      return response.data || []
    },
  })
}

export function useRole(id: number) {
  return useQuery({
    queryKey: ['role', id],
    queryFn: async () => {
      const response = await roleApi.getRole(id)
      return response.data
    },
    enabled: !!id,
  })
}

export function useCreateRole() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: roleApi.createRole,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      message.success('角色创建成功')
    },
    onError: () => {
      message.error('角色创建失败')
    },
  })
}

export function useUpdateRole() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) => 
      roleApi.updateRole(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      message.success('角色更新成功')
    },
    onError: () => {
      message.error('角色更新失败')
    },
  })
}

export function useDeleteRole() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: roleApi.deleteRole,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      message.success('角色删除成功')
    },
    onError: () => {
      message.error('角色删除失败')
    },
  })
}

export function useAssignRolePermissions() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ roleId, permissionIds }: { roleId: number; permissionIds: number[] }) => 
      roleApi.assignPermissions(roleId, permissionIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] })
      message.success('权限分配成功')
    },
    onError: () => {
      message.error('权限分配失败')
    },
  })
}

// ==================== 权限Hooks ====================
export function usePermissions() {
  return useQuery({
    queryKey: ['permissions'],
    queryFn: async () => {
      const response = await permissionApi.getPermissions()
      return response.data || []
    },
  })
}

export function useGroupedPermissions() {
  return useQuery({
    queryKey: ['permissions', 'grouped'],
    queryFn: async () => {
      const response = await permissionApi.getGroupedPermissions()
      return response.data || []
    },
  })
}

// ==================== 用户角色Hooks ====================
export function useUserRoles(userId: number) {
  return useQuery({
    queryKey: ['user-roles', userId],
    queryFn: async () => {
      const response = await userRoleApi.getUserRoles(userId)
      return response.data || []
    },
    enabled: !!userId,
  })
}

export function useAssignUserRoles() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ userId, roleIds }: { userId: number; roleIds: number[] }) => 
      userRoleApi.assignUserRoles(userId, roleIds),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['user-roles', variables.userId] })
      message.success('用户角色更新成功')
    },
    onError: () => {
      message.error('用户角色更新失败')
    },
  })
}

export function useUserPermissions(userId: number) {
  return useQuery({
    queryKey: ['user-permissions', userId],
    queryFn: async () => {
      const response = await userRoleApi.getUserPermissions(userId)
      return response.data || []
    },
    enabled: !!userId,
  })
}
```

### 第三阶段：创建前端页面组件

#### 1. 角色管理页面

创建 `frontend/src/pages/Admin/RoleManagement.tsx`:

```typescript
import { useState } from 'react'
import { 
  Table, Button, Modal, Form, Input, Space, Tag, Popconfirm, 
  Card, Drawer, Transfer, message 
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, KeyOutlined } from '@ant-design/icons'
import { 
  useRoles, 
  useCreateRole, 
  useUpdateRole, 
  useDeleteRole,
  useGroupedPermissions,
  useAssignRolePermissions
} from '@/hooks/usePermission'

export default function RoleManagement() {
  const [modalVisible, setModalVisible] = useState(false)
  const [permissionDrawerVisible, setPermissionDrawerVisible] = useState(false)
  const [editingRole, setEditingRole] = useState<any>(null)
  const [selectedRole, setSelectedRole] = useState<any>(null)
  const [form] = Form.useForm()

  const { data: roles, isLoading } = useRoles()
  const { data: groupedPermissions } = useGroupedPermissions()
  const createRole = useCreateRole()
  const updateRole = useUpdateRole()
  const deleteRole = useDeleteRole()
  const assignPermissions = useAssignRolePermissions()

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 80,
    },
    {
      title: '角色名称',
      dataIndex: 'roleName',
      key: 'roleName',
    },
    {
      title: '角色代码',
      dataIndex: 'roleCode',
      key: 'roleCode',
      render: (code: string) => <Tag color="blue">{code}</Tag>,
    },
    {
      title: '权限数量',
      dataIndex: 'permissionCount',
      key: 'permissionCount',
    },
    {
      title: '用户数量',
      dataIndex: 'userCount',
      key: 'userCount',
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: any) => (
        <Space>
          <Button
            icon={<KeyOutlined />}
            onClick={() => handleManagePermissions(record)}
          >
            权限
          </Button>
          <Button
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            编辑
          </Button>
          {record.roleCode !== 'admin' && (
            <Popconfirm
              title="确认删除此角色？"
              onConfirm={() => deleteRole.mutate(record.id)}
            >
              <Button danger icon={<DeleteOutlined />}>
                删除
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  const handleCreate = () => {
    setEditingRole(null)
    form.resetFields()
    setModalVisible(true)
  }

  const handleEdit = (role: any) => {
    setEditingRole(role)
    form.setFieldsValue(role)
    setModalVisible(true)
  }

  const handleSubmit = async () => {
    const values = await form.validateFields()
    
    if (editingRole) {
      await updateRole.mutateAsync({ id: editingRole.id, data: values })
    } else {
      await createRole.mutateAsync(values)
    }
    
    setModalVisible(false)
    form.resetFields()
  }

  const handleManagePermissions = (role: any) => {
    setSelectedRole(role)
    setPermissionDrawerVisible(true)
  }

  return (
    <Card
      title="角色管理"
      extra={
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={handleCreate}
        >
          新建角色
        </Button>
      }
    >
      <Table
        columns={columns}
        dataSource={roles}
        loading={isLoading}
        rowKey="id"
      />

      {/* 创建/编辑角色Modal */}
      <Modal
        title={editingRole ? '编辑角色' : '新建角色'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label="角色名称"
            name="roleName"
            rules={[{ required: true, message: '请输入角色名称' }]}
          >
            <Input placeholder="请输入角色名称" />
          </Form.Item>
          
          <Form.Item
            label="角色代码"
            name="roleCode"
            rules={[{ required: true, message: '请输入角色代码' }]}
          >
            <Input placeholder="请输入角色代码（如：editor）" />
          </Form.Item>
        </Form>
      </Modal>

      {/* 权限管理Drawer */}
      <Drawer
        title={`管理权限 - ${selectedRole?.roleName}`}
        width={720}
        open={permissionDrawerVisible}
        onClose={() => setPermissionDrawerVisible(false)}
      >
        {/* 权限分配界面 - 使用Transfer组件 */}
        {/* 实现省略，见完整代码 */}
      </Drawer>
    </Card>
  )
}
```

#### 2. 用户角色分配组件

创建 `frontend/src/components/Admin/UserRoleAssign.tsx`:

```typescript
import { useState, useEffect } from 'react'
import { Modal, Transfer, message } from 'antd'
import { useRoles, useUserRoles, useAssignUserRoles } from '@/hooks/usePermission'

interface UserRoleAssignProps {
  userId: number
  visible: boolean
  onClose: () => void
}

export default function UserRoleAssign({ 
  userId, 
  visible, 
  onClose 
}: UserRoleAssignProps) {
  const [targetKeys, setTargetKeys] = useState<number[]>([])
  
  const { data: allRoles } = useRoles()
  const { data: userRoles } = useUserRoles(userId)
  const assignRoles = useAssignUserRoles()

  useEffect(() => {
    if (userRoles) {
      setTargetKeys(userRoles.map((r: any) => r.id))
    }
  }, [userRoles])

  const handleSubmit = async () => {
    await assignRoles.mutateAsync({
      userId,
      roleIds: targetKeys
    })
    onClose()
  }

  return (
    <Modal
      title="分配角色"
      open={visible}
      onOk={handleSubmit}
      onCancel={onClose}
      width={600}
    >
      <Transfer
        dataSource={allRoles?.map((role: any) => ({
          key: role.id,
          title: role.roleName,
          description: role.roleCode,
        }))}
        targetKeys={targetKeys}
        onChange={setTargetKeys}
        render={item => item.title}
        listStyle={{
          width: 250,
          height: 400,
        }}
      />
    </Modal>
  )
}
```

### 第四阶段：更新路由

在 `frontend/src/router/index.tsx` 中添加：

```typescript
{
  path: '/admin',
  element: <AdminLayout />,
  children: [
    {
      path: 'users',
      element: <UserManagement />,
    },
    {
      path: 'roles',
      element: <RoleManagement />,
    },
    {
      path: 'permissions',
      element: <PermissionManagement />,
    },
  ],
}
```

---

## 🔐 权限验证集成

### 后端权限验证（已存在）

使用 `[Permission("permission.code")]` attribute：

```csharp
[HttpPost("upload")]
[Permission("doc.upload")]
public async Task<IActionResult> UploadDocument(...)
{
    // 只有具有 doc.upload 权限的用户才能访问
}
```

### 前端权限验证

创建权限Hook `frontend/src/hooks/usePermissionCheck.ts`:

```typescript
import { useAuthStore } from '@/store/authStore'

export function useHasPermission() {
  const { user } = useAuthStore()
  
  return (permissionCode: string) => {
    // 从用户信息中检查权限
    // 需要在登录时获取用户权限列表
    return user?.permissions?.includes(permissionCode) || false
  }
}

// 使用示例
function DocumentUpload() {
  const hasPermission = useHasPermission()
  
  if (!hasPermission('doc.upload')) {
    return <div>您没有上传文档的权限</div>
  }
  
  return <UploadForm />
}
```

---

## 📝 测试清单

### 后端API测试

- [ ] GET /api/role - 获取角色列表
- [ ] POST /api/role - 创建角色
- [ ] PUT /api/role/{id} - 更新角色
- [ ] DELETE /api/role/{id} - 删除角色
- [ ] POST /api/role/{id}/permissions - 分配权限
- [ ] GET /api/systempermission - 获取权限列表
- [ ] GET /api/user/{id}/roles - 获取用户角色
- [ ] POST /api/user/{id}/roles - 分配用户角色

### 前端功能测试

- [ ] 角色列表显示
- [ ] 创建新角色
- [ ] 编辑角色信息
- [ ] 删除角色（非admin）
- [ ] 为角色分配权限
- [ ] 为用户分配角色
- [ ] 权限验证生效

### 数据库测试

- [ ] 种子数据正确初始化
- [ ] 管理员账号可登录
- [ ] 权限关联正确

---

## 🚀 快速开始

### 1. 启动后端
```bash
cd AI.EnterpriseRAG.WebAPI
dotnet run
```

### 2. 验证种子数据
查看控制台输出：
```
✅ 创建了 35 个系统权限
✅ 创建了 3 个角色
✅ 创建管理员用户 (账号: admin, 密码: Admin@123)
✅ 为管理员角色分配了 35 个权限
✅ 种子数据初始化完成
```

### 3. 登录管理员账号
```
账号：admin
密码：Admin@123
```

### 4. 访问管理页面
```
http://localhost:3000/admin/roles
http://localhost:3000/admin/permissions
```

---

## 📚 参考资料

- RBAC权限模型：https://en.wikipedia.org/wiki/Role-based_access_control
- ASP.NET Core Authorization：https://docs.microsoft.com/en-us/aspnet/core/security/authorization/
- Ant Design权限管理示例：https://pro.ant.design/zh-CN/docs/authority-management

---

## 🎯 下一步计划

1. ✅ 完成后端API（已完成）
2. ✅ 数据库种子数据（已完成）
3. ⏳ 完成前端页面（进行中）
4. ⏳ 集成权限验证
5. ⏳ 添加审计日志
6. ⏳ 添加权限缓存优化
