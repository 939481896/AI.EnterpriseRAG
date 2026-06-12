# RBAC System - Quick Start Testing Guide 🚀

## Prerequisites
- Backend running on `http://localhost:5243`
- Frontend running on `http://localhost:3000`
- Database connection configured in `appsettings.json`

---

## Step 1: Start Backend & Initialize Database

```bash
cd AI.EnterpriseRAG.WebAPI
dotnet run
```

**Expected Console Output:**
```
开始初始化种子数据...
初始化系统权限...
✅ 创建了 35 个系统权限
初始化系统角色...
✅ 创建了 3 个角色
初始化管理员用户...
✅ 创建管理员用户 (账号: admin, 密码: Admin@123)
为管理员角色分配所有权限...
✅ 为管理员角色分配了 35 个权限
✅ 种子数据初始化完成
✅ 数据库种子数据初始化完成
```

---

## Step 2: Start Frontend

```bash
cd frontend
npm run dev
```

Navigate to: `http://localhost:3000`

---

## Step 3: Login as Admin

1. Go to: `http://localhost:3000/login`
2. Enter credentials:
   - **Username**: `admin`
   - **Password**: `Admin@123`
3. Click "登录" (Login)

**Expected Result:**
- Redirected to `/chat`
- Sidebar shows "系统管理员" in header
- Sidebar menu includes "管理后台" with 4 sub-items

---

## Step 4: Test Role Management

### Navigate to Role Management
1. Click "管理后台" in sidebar
2. Click "角色管理"
3. URL: `http://localhost:3000/admin/roles`

### Expected Display
- Table with 3 roles:
  - 超级管理员 (admin) - 35 permissions
  - 成员 (member) - 8 permissions
  - 访客 (guest) - 4 permissions

### Create New Role
1. Click "新建角色" button
2. Fill form:
   - **角色名称**: "部门经理"
   - **角色代码**: "dept_manager"
   - **描述**: "部门管理人员"
3. Click "确定"

**Expected Result:**
- Success message: "角色创建成功"
- New role appears in table

### Assign Permissions to Role
1. Find "部门经理" role in table
2. Click "分配权限" button
3. Tree view opens with grouped permissions
4. Check permissions:
   - user → user.read (查看用户)
   - doc → doc.read, doc.upload (查看文档, 上传文档)
   - chat → chat.read, chat.ask, chat.history (查看对话, 发起问答, 查看历史)
5. Click "保存"

**Expected Result:**
- Success message: "权限分配成功"
- Role's permission count updates to 6

### Edit Role
1. Click "编辑" for "部门经理"
2. Change 角色名称 to "经理"
3. Click "确定"

**Expected Result:**
- Success message: "角色更新成功"
- Role name updates in table

### Try to Delete Admin Role (Should Fail Protection)
1. Click "删除" for "超级管理员" role
2. Button should be **disabled**

**Expected Result:**
- Cannot delete admin role (protection)

### Delete Custom Role
1. Click "删除" for "部门经理" role
2. Confirm deletion

**Expected Result:**
- Success message: "角色删除成功"
- Role removed from table

---

## Step 5: Test Permission Management

### Navigate to Permission Management
1. Click "权限管理" in sidebar
2. URL: `http://localhost:3000/admin/permissions`

### Expected Display
- Collapsible panels for each module:
  - user (5 permissions)
  - role (5 permissions)
  - permission (5 permissions)
  - doc (5 permissions)
  - chat (5 permissions)
  - agent (3 permissions)
  - system (5 permissions)

### Create New Permission
1. Click "新建权限" button
2. Fill form:
   - **权限代码**: "doc.export"
   - **权限名称**: "导出文档"
   - **描述**: "允许导出文档为PDF"
3. Click "确定"

**Expected Result:**
- Success message: "权限创建成功"
- New permission appears in "doc" module

### Edit Permission
1. Expand "doc" module
2. Find "导出文档" permission
3. Click "编辑"
4. Change name to "批量导出文档"
5. Click "确定"

**Expected Result:**
- Success message: "权限更新成功"
- Permission name updates

### Delete Permission
1. Click "删除" for "导出文档"
2. Confirm deletion

**Expected Result:**
- Success message: "权限删除成功"
- Permission removed from list

---

## Step 6: Test User Role Assignment

### Navigate to User Management
1. Click "用户管理" in sidebar
2. URL: `http://localhost:3000/admin/users`

### Create New User (if needed)
1. Click "添加用户"
2. Fill form:
   - **账号**: "testuser"
   - **密码**: "Test@123"
   - **真实姓名**: "测试用户"
   - **邮箱**: "test@example.com"
3. Click "确定"

### Assign Roles to User
1. Find "测试用户" in table
2. Click "分配角色" button
3. Drawer opens with role checkboxes
4. Check roles:
   - ☑ member (成员)
   - ☐ guest (访客)
   - ☐ admin (超级管理员)
5. Click "保存"

**Expected Result:**
- Success message: "角色分配成功"
- Drawer closes
- User now has "member" role

### Verify Role Assignment
1. Logout from admin account
2. Login as "testuser" / "Test@123"
3. Navigate sidebar - should see limited menu items
4. Try accessing `/admin/roles` - should be restricted (if permission checks implemented)

---

## Step 7: API Testing with Postman/Thunder Client

### Get All Roles
```http
GET http://localhost:5243/api/role
Authorization: Bearer {YOUR_TOKEN}
```

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "roleName": "超级管理员",
      "roleCode": "admin",
      "userCount": 1,
      "permissionCount": 35
    },
    ...
  ]
}
```

### Get Role Details
```http
GET http://localhost:5243/api/role/1
Authorization: Bearer {YOUR_TOKEN}
```

### Create Role
```http
POST http://localhost:5243/api/role
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "roleName": "测试角色",
  "roleCode": "test_role",
  "description": "用于测试"
}
```

### Assign Permissions to Role
```http
POST http://localhost:5243/api/role/4/permissions
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "permissionIds": [1, 2, 16, 17, 21, 22]
}
```

### Get User's Roles
```http
GET http://localhost:5243/api/user/2/roles
Authorization: Bearer {YOUR_TOKEN}
```

### Assign Roles to User
```http
POST http://localhost:5243/api/user/2/roles
Authorization: Bearer {YOUR_TOKEN}
Content-Type: application/json

{
  "roleIds": [2, 3]
}
```

### Get User's Effective Permissions
```http
GET http://localhost:5243/api/user/2/permissions
Authorization: Bearer {YOUR_TOKEN}
```

---

## Common Issues & Solutions

### Issue 1: "种子数据初始化失败"
**Cause**: Database connection failure or permissions table already seeded
**Solution**: 
- Check database connection string
- Verify database exists
- If permissions already exist, seeder skips initialization (expected behavior)

### Issue 2: Frontend shows "401 Unauthorized"
**Cause**: Token expired or invalid
**Solution**:
- Logout and login again
- Check token in localStorage: `localStorage.getItem('token')`
- Verify backend JWT configuration matches

### Issue 3: "角色删除失败 - 该角色已分配给用户"
**Cause**: Cannot delete role with associated users (by design)
**Solution**:
- Remove all users from the role first
- Then delete the role

### Issue 4: Menu items not showing after adding routes
**Cause**: Page component not loaded
**Solution**:
- Check import path in `App.tsx`
- Verify file exists in `pages/Admin/`
- Clear browser cache (Ctrl+Shift+R)

### Issue 5: React Query cache not updating
**Cause**: Missing `invalidateQueries` after mutation
**Solution**:
- All mutations include `invalidateQueries`
- If issues persist, check queryKey matches

---

## Verification Checklist

### Backend
- [ ] DatabaseSeeder runs on startup
- [ ] Admin user created successfully
- [ ] All 35 permissions seeded
- [ ] All 3 roles created
- [ ] Admin role has all permissions
- [ ] All API endpoints return 200 OK
- [ ] MessageResources return Chinese messages

### Frontend
- [ ] Role Management page loads
- [ ] Permission Management page loads
- [ ] User Management shows role assignment
- [ ] Create role works
- [ ] Edit role works
- [ ] Delete role works (except admin)
- [ ] Assign permissions works
- [ ] Create permission works
- [ ] Assign roles to user works
- [ ] Success/Error messages display
- [ ] Real-time updates work

### Security
- [ ] Admin role cannot be deleted
- [ ] Admin role code cannot be changed
- [ ] JWT authentication required for all endpoints
- [ ] Permission checks work (if implemented)
- [ ] Token expiration handled correctly

---

## Performance Metrics

### Expected Load Times
- Role list page: < 200ms
- Permission list page: < 200ms
- User list page: < 300ms
- Create/Edit modal: < 50ms
- Permission tree load: < 100ms

### Cache Hit Rates
- Roles: 80%+ (5min stale time)
- Permissions: 90%+ (10min stale time)
- User roles: 70%+ (5min stale time)

---

## Success Criteria ✅

If all of the following work, the implementation is complete:

1. ✅ Backend starts without errors
2. ✅ Seed data logs show success
3. ✅ Admin login works
4. ✅ Role CRUD operations work
5. ✅ Permission CRUD operations work
6. ✅ User role assignment works
7. ✅ UI updates in real-time
8. ✅ No console errors in browser
9. ✅ All API calls return valid responses
10. ✅ Protection logic works (admin role/user)

---

## 🎉 You're Done!

The RBAC system is fully functional. You can now:
- Manage roles and permissions
- Assign roles to users
- Control access to resources
- Extend with document permissions
- Implement permission checks in other pages

Happy testing! 🚀
