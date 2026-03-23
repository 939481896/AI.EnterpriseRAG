
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.EnterpriseRAG.Infrastructure.Persistence;

/// <summary>
/// 数据库上下文（企业级EF Core配置,AOT兼容）
/// </summary>
public class AppEnterpriseAiContext:DbContext
{
    public AppEnterpriseAiContext(DbContextOptions<AppEnterpriseAiContext> options):base(options)
    {

    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    // AOT兼容：显式配置约定（避免反射推断）
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // 字符串默认配置
        configurationBuilder.Properties<string>()
            .HaveMaxLength(1000)
            .AreUnicode(true);

        // 枚举转换配置
        configurationBuilder.Properties<DocumentStatus>()
            .HaveConversion<int>();

        // 数字类型配置
        configurationBuilder.Properties<long>()
            .HaveColumnType("BIGINT");

        configurationBuilder.Properties<decimal>()
            .HaveColumnType("DECIMAL(18,6)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 文档实体配置
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(500);
            entity.Property(d => d.FileType).IsRequired().HasMaxLength(50);
            entity.Property(d => d.StoragePath).IsRequired().HasMaxLength(1000);
            entity.Property(d => d.Status).HasConversion<int>();
        });

        // 文档分块配置
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.VectorJson).IsRequired();
            entity.HasOne(c => c.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 对话记录配置
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

    // AOT兼容：拆分配置方法（避免复杂表达式）
    private static void ConfigureDocument(EntityTypeBuilder<Document> builder)
    {


        builder.ToTable("documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(500);
        builder.Property(d => d.FileType).IsRequired().HasMaxLength(50);
        builder.Property(d => d.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.Status).HasConversion<int>();
        builder.HasMany(d => d.Chunks)
               .WithOne(c => c.Document)
               .HasForeignKey(c => c.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDocumentChunk(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.VectorJson).IsRequired();
        builder.HasOne(c => c.Document)
              .WithMany(d => d.Chunks)
              .HasForeignKey(c => c.DocumentId)
              .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureChatConversation(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("chat_conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.UserId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Question).IsRequired();
        builder.Property(c => c.Answer).IsRequired();
        builder.Property(c => c.ReferenceContexts).IsRequired();
    }



}
