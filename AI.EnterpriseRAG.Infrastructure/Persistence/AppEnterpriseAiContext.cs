
using Microsoft.EntityFrameworkCore;
using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Infrastructure.Persistence;

/// <summary>
/// 数据库上下文（企业级EF Core配置）
/// </summary>
public class AppEnterpriseAiContext:DbContext
{
    public AppEnterpriseAiContext(DbContextOptions<AppEnterpriseAiContext> options):base(options)
    {

    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

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

}
