using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Infrastructure.Persistence;

public class AppEnterpriseAiContext : DbContext
{
    public AppEnterpriseAiContext(DbContextOptions<AppEnterpriseAiContext> options) : base(options) { }

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
        // Global string defaults
        configurationBuilder.Properties<string>()
            .HaveMaxLength(1000)
            .AreUnicode(true);

        // Enum and Numeric defaults
        configurationBuilder.Properties<DocumentStatus>().HaveConversion<int>();
        configurationBuilder.Properties<long>().HaveColumnType("BIGINT");
        configurationBuilder.Properties<decimal>().HaveColumnType("DECIMAL(18,6)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Core RAG Entities
        ConfigureRAGEntities(modelBuilder);

        // 2. Identity & RBAC Entities
        ConfigureIdentityEntities(modelBuilder);

        // 3. Seed Data
        ApplySeedData(modelBuilder);
    }

    private static void ConfigureRAGEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(500);
            entity.Property(d => d.FileType).HasMaxLength(50);

            entity.HasMany(d => d.Chunks)
                  .WithOne(c => c.Document)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.Property(c => c.Content).IsRequired();
            // In a real RAG system, consider if VectorJson should be a specific DB type (like vector in pgvector)
            entity.Property(c => c.VectorJson).IsRequired();
        });

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.ToTable("chat_conversations");
            entity.Property(c => c.UserId).IsRequired().HasMaxLength(100);
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

            entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
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
            entity.HasOne(t => t.User)
          .WithMany()
          .HasForeignKey(t => t.UserId);
        });
    }

    private static void ApplySeedData(ModelBuilder modelBuilder)
    {
        const long adminRoleId = 1L;
        const long adminUserId = 1L;

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
                PasswordHash = "$2a$11$qR7jXmE.7sR.4Y4zX.6FGuU9z8z.7z8z.7z8z.7z8z.7z8z." // Ensure this matches your hasher
            }
        );

        modelBuilder.Entity<SysUserRole>().HasData(
            new SysUserRole { UserId = adminUserId, RoleId = adminRoleId }
        );
    }
}