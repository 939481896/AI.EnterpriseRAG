# 🔧 RBAC Blank Page Troubleshooting Guide

## Problem Description
- Clicking "角色管理" menu shows blank page
- Seed data initialization incomplete due to existing partial data

---

## ✅ Fixes Applied

### 1. **DatabaseSeeder - Smart Incremental Seeding**

**Problem:** The original seeder skipped initialization entirely if ANY roles/permissions existed, leaving incomplete data.

**Solution:** Updated to intelligently add only missing items:

```csharp
// OLD: Skip if any exist
if (await _context.Permissions.AnyAsync()) return;

// NEW: Add only missing permissions
var existingCodes = await _context.Permissions.Select(p => p.Code).ToListAsync();
var missingPermissions = permissionDefinitions
    .Where(def => !existingCodes.Contains(def.Code))
    .ToList();
```

**Changes Made:**
- ✅ `SeedPermissionsAsync()` - Checks existing codes, adds missing
- ✅ `SeedRolesAsync()` - Checks existing codes, adds missing  
- ✅ `AssignAdminPermissionsAsync()` - Incremental permission assignment

### 2. **Debug Page Created**

Created `/admin/debug-rbac` page to diagnose API and data issues.

**Features:**
- Shows raw API responses from `/api/role` and `/api/systempermission`
- Displays error messages if API calls fail
- Provides debugging tips for browser console

---

## 🛠️ How to Fix Your System

### Step 1: Restart Backend to Trigger Seeder

```bash
# Stop the backend (Ctrl+C)
# Restart it
cd AI.EnterpriseRAG.WebAPI
dotnet run
```

**Expected Console Output:**
```
检查系统权限...
✅ 创建了 X 个缺失的系统权限
检查系统角色...
✅ 创建了 X 个缺失的角色
检查管理员角色权限...
✅ 为管理员角色新增了 X 个权限
✅ 数据库种子数据初始化完成
```

### Step 2: Navigate to Debug Page

```
URL: http://localhost:3000/admin/debug-rbac
```

**What to Check:**
1. **Roles API Section:**
   - Should show `Status: Success`
   - Should display 3 roles: admin, member, guest
   - Each role should have `permissionCount` > 0

2. **Permissions API Section:**
   - Should show `Status: Success`
   - Should display 35 permissions
   - Grouped by modules: user, role, permission, doc, chat, agent, system

3. **If Errors Appear:**
   - Check error message details
   - Verify backend is running
   - Check JWT token is valid

### Step 3: Test Role Management Page

```
URL: http://localhost:3000/admin/roles
```

**Expected Behavior:**
- Table displays with 3 roles
- "新建角色" button visible
- "分配权限", "编辑", "删除" buttons for each role

**If Still Blank:**
1. Open Browser DevTools (F12)
2. Check Console tab for JavaScript errors
3. Check Network tab:
   - Should see: `GET /api/role` → Status 200
   - Should see: `GET /api/systempermission/grouped` → Status 200
4. Click on each failed request to see error details

---

## 🐛 Common Issues & Solutions

### Issue 1: "401 Unauthorized" on API Calls

**Symptoms:**
- Network tab shows 401 errors
- Debug page shows authentication errors

**Solution:**
```javascript
// Check token in browser console
localStorage.getItem('token')

// If null or expired, re-login
// Navigate to /login and enter admin/Admin@123
```

### Issue 2: Backend Not Running

**Symptoms:**
- Network tab shows "Failed to fetch" or "ERR_CONNECTION_REFUSED"
- No API responses

**Solution:**
```bash
# Start backend
cd AI.EnterpriseRAG.WebAPI
dotnet run

# Verify it's running on http://localhost:5243
```

### Issue 3: CORS Errors

**Symptoms:**
- Console shows: "CORS policy: No 'Access-Control-Allow-Origin' header"

**Solution:**
Verify `Program.cs` CORS configuration:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### Issue 4: Partial Data Still Exists

**Symptoms:**
- Some permissions missing
- Admin role doesn't have all permissions

**Solution:**
```bash
# Option 1: Restart backend (seeder runs again)
dotnet run

# Option 2: Manual database cleanup (if needed)
# Connect to MySQL and run:
DELETE FROM RolePermissions WHERE RoleId IN (SELECT Id FROM Roles WHERE RoleCode = 'admin');
DELETE FROM Permissions;
DELETE FROM UserRoles;
DELETE FROM Roles;
DELETE FROM Users WHERE Account = 'admin';

# Then restart backend to re-seed fresh data
```

### Issue 5: React Component Error

**Symptoms:**
- Console shows React error: "Cannot read property 'map' of undefined"

**Possible Causes:**
1. API returns unexpected data structure
2. React Query hook failing silently

**Solution:**
1. Check Network tab for actual API response structure
2. Verify response matches TypeScript interfaces in `api/permission.ts`
3. Add error boundaries:

```typescript
// Temporary fix in RoleManagement.tsx
const { data: roles = [], isLoading, error } = useRoles()

if (error) {
  return <Alert type="error" message="Failed to load roles" description={error.message} />
}
```

---

## 🧪 Verification Checklist

After applying fixes, verify:

### Backend
- [ ] Application starts without errors
- [ ] Console shows: "✅ 数据库种子数据初始化完成"
- [ ] Console shows permission/role counts
- [ ] Database has 35 permissions
- [ ] Database has 3 roles
- [ ] Admin role has all 35 permissions assigned
- [ ] API endpoints return 200 OK:
  - `GET /api/role`
  - `GET /api/systempermission`
  - `GET /api/systempermission/grouped`

### Frontend
- [ ] Debug page (`/admin/debug-rbac`) shows API data
- [ ] Role Management page (`/admin/roles`) displays table
- [ ] Permission Management page (`/admin/permissions`) displays grouped panels
- [ ] No console errors in browser DevTools
- [ ] Network tab shows successful API calls

---

## 📋 Quick Debug Commands

### Check Database Data

```sql
-- Connect to MySQL
mysql -u root -p

-- Use your database
USE enterprise_rag;

-- Check permissions
SELECT COUNT(*) as permission_count FROM Permissions;
SELECT Code, Name FROM Permissions ORDER BY Code;

-- Check roles
SELECT COUNT(*) as role_count FROM Roles;
SELECT RoleName, RoleCode FROM Roles;

-- Check admin role permissions
SELECT COUNT(*) as admin_perm_count 
FROM RolePermissions rp
JOIN Roles r ON r.Id = rp.RoleId
WHERE r.RoleCode = 'admin';

-- Should return 35
```

### Check Backend Logs

```bash
# View latest logs
cat AI.EnterpriseRAG.WebAPI/Logs/app-*.log | tail -100

# Check for errors
cat AI.EnterpriseRAG.WebAPI/Logs/errors-*.log
```

### Check Frontend Network

```javascript
// In browser console
// Check React Query cache
window.__REACT_QUERY_DEVTOOLS__

// Check API responses manually
fetch('http://localhost:5243/api/role', {
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('token')}`
  }
})
.then(r => r.json())
.then(console.log)
```

---

## 🎯 Expected Final State

### Database
```
Permissions: 35 records
  - user.*: 5
  - role.*: 5
  - permission.*: 5
  - doc.*: 5
  - chat.*: 5
  - agent.*: 3
  - system.*: 5

Roles: 3 records
  - admin (超级管理员): 35 permissions
  - member (成员): 8 permissions (added in next phase)
  - guest (访客): 4 permissions (added in next phase)

Users: 1+ records
  - admin user with "admin" role
```

### Frontend Pages

1. **Role Management** (`/admin/roles`):
   - Table with 3 rows
   - Actions: 分配权限, 编辑, 删除
   - Create button works

2. **Permission Management** (`/admin/permissions`):
   - 7 collapsible panels (modules)
   - Each panel shows permissions
   - Create/edit/delete works

3. **Debug Page** (`/admin/debug-rbac`):
   - Shows full API responses
   - No errors
   - Data counts match database

---

## 🆘 Still Not Working?

If the page is still blank after all fixes:

1. **Clear Browser Cache:**
   ```
   Ctrl + Shift + Delete → Clear all cached data
   Or try Incognito mode
   ```

2. **Check React DevTools:**
   - Install React Developer Tools extension
   - Inspect component tree
   - Check if RoleManagement component is mounted

3. **Enable Verbose Logging:**
   ```typescript
   // Add to RoleManagement.tsx
   useEffect(() => {
     console.log('RoleManagement mounted')
     console.log('Roles data:', roles)
     console.log('Is loading:', isLoading)
   }, [roles, isLoading])
   ```

4. **Try Minimal Component:**
   Replace RoleManagement.tsx temporarily:
   ```typescript
   const RoleManagement = () => {
     return <div>Test: Role Management Page Loaded</div>
   }
   export default RoleManagement
   ```
   If this shows, the issue is in the component logic, not routing.

---

## 📞 Support Information

**Files Modified:**
- `AI.EnterpriseRAG.WebAPI\Services\DatabaseSeeder.cs` - Smart incremental seeding
- `frontend\src\pages\Admin\RBACDebug.tsx` - Debug diagnostics page
- `frontend\src\App.tsx` - Added debug route

**Key Logs to Check:**
- Backend: `AI.EnterpriseRAG.WebAPI\Logs\app-YYYYMMDD.log`
- Backend Errors: `AI.EnterpriseRAG.WebAPI\Logs\errors-YYYYMMDD.log`
- Browser Console: F12 → Console tab
- Network Requests: F12 → Network tab

**Related Documentation:**
- `RBAC_IMPLEMENTATION_COMPLETE.md` - Full system documentation
- `RBAC_QUICK_START_TESTING.md` - Testing procedures
- `PERMISSION_SYSTEM_GUIDE.md` - API reference

---

## ✅ Success Indicators

You've successfully fixed the issue when:

1. ✅ Backend starts and logs show all permissions/roles created/verified
2. ✅ `/admin/debug-rbac` displays complete API data
3. ✅ `/admin/roles` shows table with 3 roles
4. ✅ `/admin/permissions` shows 7 module panels with 35 total permissions
5. ✅ No errors in browser console
6. ✅ All API calls return 200 OK
7. ✅ Create/Edit/Delete operations work

**You're now ready to use the RBAC system!** 🎉
