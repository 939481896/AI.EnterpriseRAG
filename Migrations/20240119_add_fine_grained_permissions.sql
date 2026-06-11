-- ============================================
-- 🔐 权限精细化控制数据库设计
-- ============================================

USE `ai_enterprise_rag`;

-- ================================================
-- 1. 用户-文档权限表（核心）
-- ================================================
CREATE TABLE IF NOT EXISTS `user_document_permissions` (
    `Id` CHAR(36) PRIMARY KEY,
    `UserId` BIGINT NOT NULL COMMENT '用户ID（外键）',
    `DocumentId` CHAR(36) NOT NULL COMMENT '文档ID（外键）',
    `PermissionType` INT NOT NULL COMMENT '权限类型（1=Read 2=Write 3=Delete 4=Share）',
    `GrantedBy` VARCHAR(100) NOT NULL COMMENT '授权人账号',
    `GrantedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '授权时间',
    `ExpiresAt` DATETIME NULL COMMENT '过期时间（NULL=永久）',
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否激活',
    `Reason` VARCHAR(500) NULL COMMENT '授权原因',
    
    UNIQUE KEY `UK_User_Document` (`UserId`, `DocumentId`),
    INDEX `IX_UserId` (`UserId`),
    INDEX `IX_DocumentId` (`DocumentId`),
    INDEX `IX_ExpiresAt` (`ExpiresAt`),
    
    FOREIGN KEY (`UserId`) REFERENCES `sys_users`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`DocumentId`) REFERENCES `documents`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户-文档权限表';

-- ================================================
-- 2. 角色-文档权限表（批量授权）
-- ================================================
CREATE TABLE IF NOT EXISTS `role_document_permissions` (
    `Id` CHAR(36) PRIMARY KEY,
    `RoleId` BIGINT NOT NULL COMMENT '角色ID（外键）',
    `DocumentId` CHAR(36) NOT NULL COMMENT '文档ID（外键）',
    `PermissionType` INT NOT NULL COMMENT '权限类型',
    `GrantedBy` VARCHAR(100) NOT NULL,
    `GrantedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ExpiresAt` DATETIME NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    
    UNIQUE KEY `UK_Role_Document` (`RoleId`, `DocumentId`),
    INDEX `IX_RoleId` (`RoleId`),
    INDEX `IX_DocumentId` (`DocumentId`),
    
    FOREIGN KEY (`RoleId`) REFERENCES `sys_roles`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`DocumentId`) REFERENCES `documents`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='角色-文档权限表';

-- ================================================
-- 3. 文档分类表（按类别控制）
-- ================================================
CREATE TABLE IF NOT EXISTS `document_categories` (
    `Id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `CategoryCode` VARCHAR(50) NOT NULL UNIQUE COMMENT '分类代码',
    `CategoryName` VARCHAR(100) NOT NULL COMMENT '分类名称',
    `ParentId` BIGINT NULL COMMENT '父分类ID（支持层级）',
    `Description` VARCHAR(500) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    
    INDEX `IX_ParentId` (`ParentId`),
    FOREIGN KEY (`ParentId`) REFERENCES `document_categories`(`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='文档分类表';

-- 为 documents 表添加分类字段
ALTER TABLE `documents` 
ADD COLUMN `CategoryId` BIGINT NULL COMMENT '文档分类ID' AFTER `TenantId`,
ADD INDEX `IX_CategoryId` (`CategoryId`),
ADD FOREIGN KEY (`CategoryId`) REFERENCES `document_categories`(`Id`) ON DELETE SET NULL;

-- ================================================
-- 4. 文档标签表（灵活标记）
-- ================================================
CREATE TABLE IF NOT EXISTS `document_tags` (
    `Id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `TagName` VARCHAR(50) NOT NULL UNIQUE COMMENT '标签名称',
    `TagColor` VARCHAR(20) NULL COMMENT '标签颜色',
    `CreateTime` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='文档标签表';

-- 文档-标签关联表
CREATE TABLE IF NOT EXISTS `document_tag_relations` (
    `DocumentId` CHAR(36) NOT NULL,
    `TagId` BIGINT NOT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (`DocumentId`, `TagId`),
    INDEX `IX_TagId` (`TagId`),
    
    FOREIGN KEY (`DocumentId`) REFERENCES `documents`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`TagId`) REFERENCES `document_tags`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='文档-标签关联表';

-- ================================================
-- 5. 用户-分类权限表（按分类控制）
-- ================================================
CREATE TABLE IF NOT EXISTS `user_category_permissions` (
    `Id` CHAR(36) PRIMARY KEY,
    `UserId` BIGINT NOT NULL,
    `CategoryId` BIGINT NOT NULL,
    `PermissionType` INT NOT NULL,
    `GrantedBy` VARCHAR(100) NOT NULL,
    `GrantedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE KEY `UK_User_Category` (`UserId`, `CategoryId`),
    INDEX `IX_UserId` (`UserId`),
    INDEX `IX_CategoryId` (`CategoryId`),
    
    FOREIGN KEY (`UserId`) REFERENCES `sys_users`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`CategoryId`) REFERENCES `document_categories`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户-分类权限表';

-- ================================================
-- 6. 权限审计日志表
-- ================================================
CREATE TABLE IF NOT EXISTS `permission_audit_logs` (
    `Id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `UserId` VARCHAR(100) NOT NULL COMMENT '操作用户',
    `TargetUserId` BIGINT NULL COMMENT '目标用户ID',
    `DocumentId` CHAR(36) NULL COMMENT '文档ID',
    `Action` VARCHAR(50) NOT NULL COMMENT '操作类型（Grant/Revoke/Access）',
    `PermissionType` INT NULL COMMENT '权限类型',
    `Reason` VARCHAR(500) NULL COMMENT '操作原因',
    `IP` VARCHAR(50) NULL COMMENT 'IP地址',
    `CreateTime` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    INDEX `IX_UserId` (`UserId`),
    INDEX `IX_DocumentId` (`DocumentId`),
    INDEX `IX_CreateTime` (`CreateTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='权限审计日志';

-- ================================================
-- 7. 插入示例数据
-- ================================================

-- 插入文档分类
INSERT INTO `document_categories` (`Id`, `CategoryCode`, `CategoryName`, `ParentId`, `Description`) VALUES
(1, 'PUBLIC', '公开文档', NULL, '所有人可访问'),
(2, 'INTERNAL', '内部文档', NULL, '公司内部使用'),
(3, 'CONFIDENTIAL', '机密文档', NULL, '高级管理层可访问'),
(4, 'HR', '人力资源', 2, '人事部门专用'),
(5, 'FINANCE', '财务文档', 3, '财务部门专用');

-- 插入文档标签
INSERT INTO `document_tags` (`Id`, `TagName`, `TagColor`) VALUES
(1, '重要', '#FF5733'),
(2, '紧急', '#FF0000'),
(3, '草稿', '#808080'),
(4, '已审核', '#00FF00'),
(5, '归档', '#0000FF');

-- ================================================
-- 8. 验证查询
-- ================================================

-- 查看表结构
SHOW TABLES LIKE '%permission%';

-- 查看分类
SELECT * FROM `document_categories`;

-- 查看标签
SELECT * FROM `document_tags`;
