-- ============================================
-- 🔐 添加文档权限控制字段
-- ============================================

USE `ai_enterprise_rag`;

-- 1. 添加权限控制字段
ALTER TABLE `documents` 
ADD COLUMN `UploadedBy` VARCHAR(100) NOT NULL DEFAULT '' COMMENT '上传者账号' AFTER `CompleteTime`,
ADD COLUMN `TenantId` VARCHAR(50) NULL COMMENT '租户ID（多租户隔离）' AFTER `UploadedBy`,
ADD COLUMN `IsPublic` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否公开（0=私有 1=公开）' AFTER `TenantId`;

-- 2. 创建索引（提升查询性能）
CREATE INDEX `IX_Documents_UploadedBy` ON `documents`(`UploadedBy`);
CREATE INDEX `IX_Documents_TenantId` ON `documents`(`TenantId`);
CREATE INDEX `IX_Documents_TenantId_Status` ON `documents`(`TenantId`, `Status`);

-- 3. 更新历史数据（设置默认上传者为admin）
UPDATE `documents` 
SET `UploadedBy` = 'admin' 
WHERE `UploadedBy` = '' OR `UploadedBy` IS NULL;

-- 4. 验证迁移结果
SELECT 
    COUNT(*) AS TotalDocuments,
    COUNT(DISTINCT UploadedBy) AS UniqueUploaders,
    COUNT(DISTINCT TenantId) AS UniqueTenants,
    SUM(CASE WHEN IsPublic = 1 THEN 1 ELSE 0 END) AS PublicDocuments,
    SUM(CASE WHEN IsPublic = 0 THEN 1 ELSE 0 END) AS PrivateDocuments
FROM `documents`;

-- 5. 查看迁移后的表结构
DESCRIBE `documents`;
