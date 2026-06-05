# Quick Start: Apply Permission & Audit Updates

## ⚡ **One Command to Apply All Changes**

### **Prerequisites:**
- ✅ MySQL database running
- ✅ Connection string configured in `appsettings.json`

---

## 🚀 **Step 1: Start MySQL (if not running)**

### **Option A: Windows Service**
```powershell
net start mysql
```

### **Option B: Docker**
```powershell
docker start mysql-container
```

### **Option C: XAMPP**
- Open XAMPP Control Panel
- Click "Start" for MySQL

---

## 🎯 **Step 2: Apply Migration**

```powershell
cd C:\Users\H381850\Documents\WorkSpace\Learn-Study\AI.EnterpriseRAG
dotnet ef database update -p AI.EnterpriseRAG.Infrastructure -s AI.EnterpriseRAG.WebAPI
```

**Expected Output:**
```
Build started...
Build succeeded.
Applying migration '20260605023536_AddCategoryAndAuditSupport'.
Done.
```

---

## ✅ **Step 3: Verify Migration Applied**

### **Check Database:**
```sql
-- Verify new tables exist
SHOW TABLES LIKE 'PermissionAuditLog';
SHOW TABLES LIKE 'UserCategoryPermission';

-- Verify CategoryId column added
DESCRIBE documents;

-- Should see: CategoryId BIGINT NULL
```

### **Check Application:**
```powershell
dotnet run --project AI.EnterpriseRAG.WebAPI
```

**Success Indicators:**
```
✅ 应用程序启动成功
✅ Qdrant Collection 初始化成功
```

---

## 🧪 **Step 4: Test Category Permission**

### **1. Create a Category (via SQL)**
```sql
INSERT INTO DocumentCategory (CategoryCode, CategoryName, Description, IsActive)
VALUES ('TECH', 'Technical Documents', 'Technical documentation', 1);

SELECT * FROM DocumentCategory; -- Note the ID
```

### **2. Assign Document to Category**
```sql
UPDATE documents 
SET CategoryId = 1  -- Use the ID from step 1
WHERE Id = 'your-document-guid';
```

### **3. Grant Category Permission (via API)**
```http
POST /api/documentpermission/grant-category
Content-Type: application/json
Authorization: Bearer {admin-token}

{
  "userId": 1,
  "categoryId": 1,
  "permissionType": 1
}
```

### **4. Verify Permission Works**
```http
GET /api/documentpermission/check?documentId={document-guid}&requiredPermission=1
Authorization: Bearer {user-token}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "hasPermission": true,
    "currentPermission": "Read",
    "documentId": "..."
  }
}
```

---

## 📋 **Step 5: Test Audit Logging**

### **1. Check Audit Logs (via API)**
```http
GET /api/documentpermission/audit-logs?startTime=2026-06-05T00:00:00
Authorization: Bearer {admin-token}
```

### **2. Check Database Directly**
```sql
SELECT * FROM PermissionAuditLog 
ORDER BY CreateTime DESC 
LIMIT 10;
```

**Should show:**
- Grant operations
- User IDs
- Document IDs
- Timestamps
- IP addresses (if captured)

---

## ⚠️ **Troubleshooting**

### **Issue: "Unable to connect to MySQL"**
**Solution:**
```powershell
# Check MySQL status
Get-Service mysql*

# Or check connection
mysql -u root -p -e "SELECT 1"
```

### **Issue: "Migration already applied"**
**Solution:**
```sql
-- Check migration history
SELECT * FROM __EFMigrationsHistory;

-- Should see: 20260605023536_AddCategoryAndAuditSupport
```

### **Issue: "Foreign key constraint fails"**
**Solution:**
- Ensure `DocumentCategory` table exists
- Ensure `sys_users` table has data
- Run migrations in order

---

## 🎉 **Success Checklist**

- [ ] MySQL running
- [ ] Migration applied successfully
- [ ] `PermissionAuditLog` table exists
- [ ] `UserCategoryPermission` table exists
- [ ] `documents.CategoryId` column exists
- [ ] Application starts without errors
- [ ] Category permission grants work
- [ ] Audit logs are recorded
- [ ] Permission checks include category logic

---

## 🔄 **Rollback (if needed)**

### **Remove Last Migration:**
```powershell
dotnet ef migrations remove -p AI.EnterpriseRAG.Infrastructure -s AI.EnterpriseRAG.WebAPI
```

### **Revert Database:**
```powershell
# Revert to previous migration
dotnet ef database update AddDocumentPermissions -p AI.EnterpriseRAG.Infrastructure -s AI.EnterpriseRAG.WebAPI
```

---

## 📊 **What Gets Created**

### **Tables:**
1. **PermissionAuditLog** - Tracks all permission changes
2. **UserCategoryPermission** - Category-level permissions

### **Columns:**
1. **documents.CategoryId** - Links documents to categories

### **Indexes:**
- Performance indexes on UserCategoryPermission
- Audit log indexes for fast queries
- Category foreign key index

### **Relationships:**
- Document → Category (SetNull on delete)
- UserCategoryPermission → User (Cascade)
- UserCategoryPermission → Category (Cascade)

---

## 🚀 **Quick Test Script**

```powershell
# Full test sequence
cd C:\Users\H381850\Documents\WorkSpace\Learn-Study\AI.EnterpriseRAG

# 1. Apply migration
dotnet ef database update -p AI.EnterpriseRAG.Infrastructure -s AI.EnterpriseRAG.WebAPI

# 2. Start application
dotnet run --project AI.EnterpriseRAG.WebAPI

# 3. Test in another terminal:
# Use Postman or curl to test category permission endpoints
```

---

**Estimated Time**: 5 minutes  
**Complexity**: Low  
**Risk**: Very Low (migration is reversible)
