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
            // In a real RAG system, consider if VectorJson should be a specific DB type (like vector in pgvector)
            entity.Property(c => c.VectorJson).IsRequired();
            entity.HasOne(c => c.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
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