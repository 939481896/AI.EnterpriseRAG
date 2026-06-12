# 🎯 菜单权限系统快速使用指南

## 问题解决

### ✅ 已解决的问题

1. **缺少菜单权限** ❌ → ✅ 已添加7个菜单权限（menu.admin, menu.user, menu.role, menu.permission, menu.document, menu.chat, menu.agent）
2. **前端UI未适配权限** ❌ → ✅ 已实现基于权限的动态菜单和按钮显示/隐藏

## 🚀 快速开始

### 1. 启动后端（自动初始化菜单权限）

```bash
cd AI.EnterpriseRAG.WebAPI
dotnet run
```

后端启动时会自动：
- ✅ 添加7个新的菜单权限到数据库
- ✅ 将所有菜单权限分配给 admin 角色
- ✅ 保持现有数据不变（增量式更新）

### 2. 启动前端

```bash
cd frontend
npm run dev
```

### 3. 测试权限系统

#### 测试1: Admin用户（拥有所有权限）

```
账号: admin
密码: Admin@123
```

**预期结果**:
- ✅ 看到所有4个主菜单（智能问答、文档管理、Agent工作区、管理后台）
- ✅ 管理后台下有4个子菜单（数据面板、用户管理、角色管理、权限管理）
- ✅ 所有页面的所有按钮都可见（添加、编辑、删除、分配权限）

#### 测试2: 创建受限用户

1. 以 admin 身份登录
2. 进入 **角色管理** 页面
3. 创建新角色，例如 "文档管理员"
4. 分配权限时**只选择**:
   - ✅ menu.document (访问文档管理菜单)
   - ✅ doc.read (查看文档)
   - ✅ doc.upload (上传文档)
   - ❌ 不选择 menu.admin, menu.user, menu.role 等其他菜单权限
5. 进入 **用户管理** 页面
6. 创建新用户并分配 "文档管理员" 角色
7. 退出登录，用新用户登录

**预期结果**:
- ✅ 只能看到 **文档管理** 菜单
- ❌ 看不到 **管理后台** 菜单
- ❌ 看不到 **智能问答** 和 **Agent工作区** 菜单

## 📊 权限类型说明

### 菜单权限（控制页面访问）

| 权限代码 | 说明 | 效果 |
|---------|------|------|
| `menu.admin` | 访问管理后台 | 控制整个管理后台的显示 |
| `menu.user` | 访问用户管理 | 控制用户管理子菜单 |
| `menu.role` | 访问角色管理 | 控制角色管理子菜单 |
| `menu.permission` | 访问权限管理 | 控制权限管理子菜单 |
| `menu.document` | 访问文档管理 | 控制文档管理主菜单 |
| `menu.chat` | 访问智能问答 | 控制智能问答主菜单 |
| `menu.agent` | 访问Agent工作区 | 控制Agent工作区主菜单 |

### 操作权限（控制按钮/功能）

| 模块 | 权限代码 | 说明 |
|-----|---------|------|
| 用户 | `user.create` | 控制"添加用户"按钮 |
| 用户 | `user.update` | 控制"编辑"和"分配角色"按钮 |
| 用户 | `user.delete` | 控制"删除"按钮 |
| 角色 | `role.create` | 控制"新建角色"按钮 |
| 角色 | `role.update` | 控制"编辑"和"分配权限"按钮 |
| 角色 | `role.delete` | 控制"删除"按钮 |
| 权限 | `permission.create` | 控制"新建权限"按钮 |
| 权限 | `permission.update` | 控制"编辑"按钮 |
| 权限 | `permission.delete` | 控制"删除"按钮 |

## 🎨 UI适配示例

### 场景1: 普通用户（只有查看权限）

配置权限:
```
✅ menu.user (可以进入用户管理页面)
✅ user.read (可以查看用户列表)
❌ user.create (没有)
❌ user.update (没有)
❌ user.delete (没有)
```

界面效果:
```
用户管理页面
┌─────────────────────────────┐
│ 用户管理        [按钮不显示] │ ← "添加用户"按钮被隐藏
├─────────────────────────────┤
│ ID │ 账号 │ 姓名 │ 操作      │
├────┼─────┼──────┼──────────┤
│ 1  │ xxx │ xxx  │ [空白]    │ ← 操作列按钮都被隐藏
│ 2  │ xxx │ xxx  │ [空白]    │
└─────────────────────────────┘
```

### 场景2: 部分管理员（有编辑但无删除权限）

配置权限:
```
✅ menu.user
✅ user.read
✅ user.update
❌ user.create
❌ user.delete
```

界面效果:
```
用户管理页面
┌─────────────────────────────────────┐
│ 用户管理        [按钮不显示]        │ ← "添加用户"按钮被隐藏
├─────────────────────────────────────┤
│ ID │ 账号 │ 姓名 │ 操作              │
├────┼─────┼──────┼──────────────────┤
│ 1  │ xxx │ xxx  │ [编辑] [分配角色] │ ← 只显示编辑相关按钮
│ 2  │ xxx │ xxx  │ [编辑] [分配角色] │   删除按钮被隐藏
└─────────────────────────────────────┘
```

## 🔧 自定义权限配置

### 创建自定义角色

1. 进入 **角色管理** 页面
2. 点击 **新建角色** 按钮
3. 填写角色信息:
   - 角色代码: `document_manager` (小写英文，用下划线)
   - 角色名称: `文档管理员`
   - 角色描述: `负责文档的上传、查看和管理`
4. 点击 **确定** 创建角色
5. 在角色列表中找到新角色，点击 **分配权限**
6. 选择需要的权限:
   ```
   ✅ 菜单权限
      ✅ menu.document (访问文档管理菜单)
   
   ✅ 文档管理权限
      ✅ doc.read (查看文档)
      ✅ doc.upload (上传文档)
      ✅ doc.delete (删除文档)
      ✅ doc.share (分享文档)
   ```
7. 点击 **保存** 完成权限分配

### 分配角色给用户

1. 进入 **用户管理** 页面
2. 找到目标用户，点击 **分配角色**
3. 在弹出的抽屉中勾选 `文档管理员` 角色
4. 点击 **保存**
5. 用户重新登录后即可看到权限变化

## 🐛 常见问题

### Q1: 修改了用户权限，但前端界面没有变化？

**解决方案**: 
- 退出登录
- 重新登录
- 原因: React Query 有5分钟的权限缓存

### Q2: 为什么没有菜单权限也能看到某些菜单？

**答**: 检查菜单配置中是否设置了 `permission` 属性。只有设置了 `permission` 的菜单才会进行权限检查。

### Q3: 管理后台菜单消失了？

**检查**:
1. 用户是否有 `menu.admin` 权限
2. 至少需要有一个子菜单权限（`menu.user`, `menu.role`, 或 `menu.permission`）
3. 如果子菜单全被过滤，父菜单也会隐藏

### Q4: 按钮还在但点击没反应？

**说明**: 
- `PermissionGuard` 只控制显示/隐藏
- 后端API有独立的权限验证
- 如果按钮显示但操作失败，检查后端权限配置

## 📚 技术细节

### 前端权限检查流程

```typescript
// 1. 在组件中导入 Hook
import { usePermissionContext } from '@/contexts/PermissionContext'

// 2. 获取权限检查函数
const { hasPermission, hasAnyPermission } = usePermissionContext()

// 3. 检查单个权限
if (hasPermission('menu.admin')) {
  // 显示管理后台
}

// 4. 检查多个权限（任一满足）
if (hasAnyPermission(['user.create', 'user.update'])) {
  // 显示用户操作按钮
}

// 5. 使用 PermissionGuard 组件（推荐）
<PermissionGuard permission="user.create">
  <Button>添加用户</Button>
</PermissionGuard>
```

### 后端权限检查（未来可扩展）

```csharp
// 在 Controller 中使用特性保护 API
[Authorize(Policy = "RequireMenuAdmin")]
public class AdminController : BaseApiController
{
    // ...
}

// 或在方法级别
[Authorize(Policy = "RequireUserCreate")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
{
    // ...
}
```

## ✅ 功能验证清单

完成以下测试确保系统正常工作:

- [ ] Admin用户可以看到所有菜单和按钮
- [ ] 创建只有文档权限的角色，分配给用户
- [ ] 新用户登录后只能看到文档管理菜单
- [ ] 创建只有查看权限的角色，分配给用户
- [ ] 新用户登录后看到页面但没有操作按钮
- [ ] 创建完全无菜单权限的角色，分配给用户
- [ ] 新用户登录后侧边栏为空或只显示默认菜单

## 🎉 总结

菜单权限系统现已完全实现！

**核心功能**:
✅ 7个菜单级权限控制页面访问
✅ 35个操作级权限控制功能按钮
✅ 前端UI自动适配用户权限
✅ 动态菜单显示/隐藏
✅ 动态按钮显示/隐藏
✅ React高性能实现（useMemo + useCallback）
✅ 增量式数据库初始化

**用户体验**:
- 🎯 只看到有权限的内容
- 🔒 无权限的功能完全隐藏
- 🚀 流畅的页面加载体验
- 📱 响应式权限检查

---

详细技术文档请查看: [RBAC_MENU_PERMISSIONS.md](./RBAC_MENU_PERMISSIONS.md)
