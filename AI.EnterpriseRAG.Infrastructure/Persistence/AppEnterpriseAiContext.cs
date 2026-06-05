using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Infrastructure.Persistence;

public class AppEnterpriseAiContext : DbContext
{
    public AppEnterpriseAiContext(DbContextOptions<AppEnterpriseAiContext> options)
        : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<SysUser> Users => Set<SysUser>();
    public DbSet<SysRole> Roles => Set<SysRole>();
    public DbSet<SysUserRole> UserRoles => Set<SysUserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Agent智能体相关实体
    public DbSet<AgentSession> AgentSessions => Set<AgentSession>();
    public DbSet<AgentStep> AgentSteps => Set<AgentStep>();

    // 🆕 细粒度权限控制实体
    public DbSet<UserDocumentPermission> UserDocumentPermissions => Set<UserDocumentPermission>();
    public DbSet<RoleDocumentPermission> RoleDocumentPermissions => Set<RoleDocumentPermission>();
    public DbSet<UserCategoryPermission> UserCategoryPermissions => Set<UserCategoryPermission>();
    public DbSet<PermissionAuditLog> PermissionAuditLogs => Set<PermissionAuditLog>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();
    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
    public DbSet<DocumentTagRelation> DocumentTagRelations => Set<DocumentTagRelation>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DocumentStatus>().HaveConversion<int>();
        configurationBuilder.Properties<long>().HaveColumnType("BIGINT");
        configurationBuilder.Properties<decimal>().HaveColumnType("DECIMAL(18,6)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureRAGEntities(modelBuilder);
        ConfigureIdentityEntities(modelBuilder);
        ConfigureFineGrainedPermissions(modelBuilder); // 🆕 配置细粒度权限表
        ApplySeedData(modelBuilder);
    }

    private static void ConfigureRAGEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(500);
            entity.Property(d => d.FileType).IsRequired().HasMaxLength(50);
            entity.Property(d => d.StoragePath).IsRequired().HasMaxLength(1000);
            entity.Property(d => d.Status).HasConversion<int>();

            // 🆕 权限控制字段配置
            entity.Property(d => d.UploadedBy).IsRequired().HasMaxLength(100);
            entity.Property(d => d.TenantId).HasMaxLength(50);
            entity.Property(d => d.IsPublic).IsRequired().HasDefaultValue(false);

            // 🆕 重复上传检测字段配置
            entity.Property(d => d.FileHash).HasMaxLength(64);
            entity.Property(d => d.UpdateTime);

            // 🆕 权限控制索引（提升查询性能）
            entity.HasIndex(d => d.UploadedBy);
            entity.HasIndex(d => d.TenantId);
            entity.HasIndex(d => new { d.TenantId, d.Status }); // 组合索引
            entity.HasIndex(d => d.FileHash); // 哈希索引（用于重复检测）
            entity.HasIndex(d => d.CategoryId); // 分类索引

            // 🆕 分类关系配置
            entity.HasOne(d => d.Category)
                  .WithMany(c => c.Documents)
                  .HasForeignKey(d => d.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(d => d.Chunks)
                  .WithOne(c => c.Document)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.ChunkId).HasMaxLength(200);
            entity.Property(c => c.SectionTitle).HasMaxLength(500);

            entity.HasOne(c => c.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.ChunkId);
            entity.HasIndex(c => c.DocumentId);

            // 忽略非持久化字段
            entity.Ignore(c => c.Similarity);
        });

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.ToTable("chat_conversations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.UserId).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Question).IsRequired();
            entity.Property(c => c.Answer).IsRequired();
            entity.Property(c => c.ReferenceContexts).IsRequired();
        });

        // Agent智能体实体配置
        modelBuilder.Entity<AgentSession>(entity =>
        {
            entity.ToTable("agent_sessions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.UserId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.TenantId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.UserIntent).IsRequired();
            entity.Property(s => s.IntentType).HasMaxLength(50);
            entity.Property(s => s.Status).HasConversion<int>();

            entity.HasMany(s => s.Steps)
                  .WithOne(st => st.Session)
                  .HasForeignKey(st => st.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.TenantId);
            entity.HasIndex(s => s.StartTime);
        });

        modelBuilder.Entity<AgentStep>(entity =>
        {
            entity.ToTable("agent_steps");
            entity.HasKey(st => st.Id);
            entity.Property(st => st.StepType).IsRequired().HasMaxLength(50);
            entity.Property(st => st.ToolName).HasMaxLength(100);

            entity.HasOne(st => st.Session)
                  .WithMany(s => s.Steps)
                  .HasForeignKey(st => st.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(st => st.SessionId);
            entity.HasIndex(st => st.StepIndex);
        });
    }

    private static void ConfigureFineGrainedPermissions(ModelBuilder modelBuilder)
    {
        // UserDocumentPermission 配置
        modelBuilder.Entity<UserDocumentPermission>(entity =>
        {
            entity.ToTable("UserDocumentPermission");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.GrantedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.UserId, e.DocumentId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Document)
                  .WithMany()
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RoleDocumentPermission 配置
        modelBuilder.Entity<RoleDocumentPermission>(entity =>
        {
            entity.ToTable("RoleDocumentPermission");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.GrantedBy).IsRequired().HasMaxLength(100);

            entity.HasIndex(e => e.RoleId);
            entity.HasIndex(e => e.DocumentId);

            entity.HasOne(e => e.Role)
                  .WithMany()
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Document)
                  .WithMany()
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentCategory 配置
        modelBuilder.Entity<DocumentCategory>(entity =>
        {
            entity.ToTable("DocumentCategory");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CategoryCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.CategoryCode).IsUnique();

            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // DocumentTag 配置
        modelBuilder.Entity<DocumentTag>(entity =>
        {
            entity.ToTable("DocumentTag");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TagName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TagColor).HasMaxLength(20);

            entity.HasIndex(e => e.TagName).IsUnique();
        });

        // DocumentTagRelation 配置
        modelBuilder.Entity<DocumentTagRelation>(entity =>
        {
            entity.ToTable("DocumentTagRelation");
            entity.HasKey(e => new { e.DocumentId, e.TagId });

            // 修复：使用导航属性 Document
            entity.HasOne(e => e.Document)
                  .WithMany()
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                  .WithMany(t => t.DocumentRelations)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserCategoryPermission 配置
        modelBuilder.Entity<UserCategoryPermission>(entity =>
        {
            entity.ToTable("UserCategoryPermission");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.GrantedBy).IsRequired().HasMaxLength(100);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => new { e.UserId, e.CategoryId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PermissionAuditLog 配置
        modelBuilder.Entity<PermissionAuditLog>(entity =>
        {
            entity.ToTable("PermissionAuditLog");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.IP).HasMaxLength(50);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.CreateTime);
            entity.HasIndex(e => new { e.UserId, e.CreateTime }); // 组合索引用于用户历史查询
        });
    }

    private static void ConfigureIdentityEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SysUser>(entity =>
        {
            entity.ToTable("sys_users");
            entity.HasIndex(u => u.Account).IsUnique();
            entity.Property(u => u.Account).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<SysRole>(entity =>
        {
            entity.ToTable("sys_roles");
            entity.HasIndex(r => r.RoleCode).IsUnique();
        });

        modelBuilder.Entity<SysUserRole>(entity =>
        {
            entity.ToTable("sys_user_roles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("sys_role_permissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("sys_refresh_tokens");
            entity.HasIndex(t => t.Token).IsUnique();
        });
    }

    private static void ApplySeedData(ModelBuilder modelBuilder)
    {
        const long adminRoleId = 1L;
        const long adminUserId = 1L;


        // 动态创建密码哈希（自动生成，不需要手动写死）
        var passwordHasher = new PasswordHasher<SysUser>();
        var adminPassword = "123456"; // 你想设置的密码
        var adminPwdHash = passwordHasher.HashPassword(null!, adminPassword);

        modelBuilder.Entity<SysRole>().HasData(
            new SysRole { Id = adminRoleId, RoleName = "超级管理员", RoleCode = "admin" },
            new SysRole { Id = 2L, RoleName = "普通用户", RoleCode = "user" }
        );

        modelBuilder.Entity<SysUser>().HasData(
            new SysUser
            {
                Id = adminUserId,
                Account = "admin",
                UserName = "Admin",
                PasswordHash = adminPwdHash,
                IsEnabled = true,
                TenantId = "default",
                CreateTime = DateTime.UtcNow
            }
        );

        // 先添加权限
        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1L, Code = "chat.ask", Name = "智能问答" },
            new Permission { Id = 2L, Code = "doc.read", Name = "文档查看" },
            new Permission { Id = 3L, Code = "doc.upload", Name = "文档上传" }
        );

        // 给 admin 角色分配权限
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = 1L, PermissionId = 1L }, // admin → 问答
            new RolePermission { RoleId = 1L, PermissionId = 2L },
            new RolePermission { RoleId = 1L, PermissionId = 3L }
        );

        modelBuilder.Entity<SysUserRole>().HasData(
            new SysUserRole { UserId = adminUserId, RoleId = adminRoleId }
        );
    }
}