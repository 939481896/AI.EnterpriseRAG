-- ================================================================
-- 文档重复上传优化 - 数据库变更
-- 用途：添加FileHash字段和相关索引
-- ================================================================

USE EnterpriseRAG;

-- 1. 添加FileHash字段
ALTER TABLE documents 
ADD COLUMN FileHash VARCHAR(64) NULL COMMENT '文件MD5哈希（用于重复检测）'
AFTER FileSize;

-- 2. 创建FileHash索引（加速查询）
CREATE INDEX IX_documents_FileHash 
ON documents(FileHash);

-- 3. 创建复合索引（防止同一用户/租户重复上传相同文件）
-- 注意：这个索引只对非NULL的FileHash生效
CREATE UNIQUE INDEX IX_documents_FileHash_UploadedBy_TenantId 
ON documents(FileHash, UploadedBy, TenantId) 
WHERE FileHash IS NOT NULL;

-- 4. 添加UpdateTime字段（记录文档更新时间）
ALTER TABLE documents 
ADD COLUMN UpdateTime DATETIME(6) NULL COMMENT '最后更新时间'
AFTER CompleteTime;

-- 5. 验证变更
DESCRIBE documents;

-- 6. 查看索引
SHOW INDEX FROM documents;

-- ================================================================
-- 可选：为已有文档计算哈希（后台任务）
-- ================================================================

-- 查询没有哈希的文档
SELECT Id, Name, FileSize, StoragePath
FROM documents
WHERE FileHash IS NULL
ORDER BY CreateTime DESC
LIMIT 10;

-- 注意：需要应用程序代码计算哈希并更新
-- UPDATE documents SET FileHash = ? WHERE Id = ?;

-- ================================================================
-- 测试查询
-- ================================================================

-- 查询重复文档（相同哈希）
SELECT FileHash, COUNT(*) as DuplicateCount
FROM documents
WHERE FileHash IS NOT NULL
GROUP BY FileHash
HAVING COUNT(*) > 1;

-- 查询特定用户的文档（按哈希去重）
SELECT DISTINCT FileHash, Name, FileSize, UploadedBy
FROM documents
WHERE UploadedBy = 'admin'
  AND FileHash IS NOT NULL
ORDER BY CreateTime DESC;

-- ================================================================
-- 回滚脚本（如需撤销变更）
-- ================================================================

-- DROP INDEX IF EXISTS IX_documents_FileHash_UploadedBy_TenantId ON documents;
-- DROP INDEX IF EXISTS IX_documents_FileHash ON documents;
-- ALTER TABLE documents DROP COLUMN UpdateTime;
-- ALTER TABLE documents DROP COLUMN FileHash;
