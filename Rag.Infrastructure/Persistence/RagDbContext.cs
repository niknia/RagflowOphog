using Microsoft.EntityFrameworkCore;
using Rag.Domain.Entities;

namespace Rag.Infrastructure.Persistence;
public class RagDbContext : DbContext
{
    public RagDbContext(DbContextOptions<RagDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<ProcessingProgress> ProcessingProgresses => Set<ProcessingProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.FileName).HasMaxLength(500).IsRequired();
            e.Property(d => d.ContentType).HasMaxLength(200);
            e.Property(d => d.CollectionId).HasMaxLength(100);
            e.Property(d => d.CollectionName).HasMaxLength(200);
            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(d => d.DetectedLanguage).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(d => d.Status);
            e.HasIndex(d => d.CollectionId);
            e.HasQueryFilter(d => !d.IsDeleted);
        });

        modelBuilder.Entity<Chunk>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CollectionId).HasMaxLength(100);
            e.Property(c => c.Content).HasColumnType("TEXT");
            e.Property(c => c.Language).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(c => c.DocumentId);
            e.HasIndex(c => c.CollectionId);
            e.HasIndex(c => c.IsIndexed);
        });

        modelBuilder.Entity<Chat>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Title).HasMaxLength(500);
            e.Property(c => c.UserId).HasMaxLength(200);
            e.Property(c => c.CollectionId).HasMaxLength(100);
            e.Property(c => c.SessionId).HasMaxLength(200);
            e.HasMany(c => c.Messages).WithOne().HasForeignKey(m => m.ChatId);
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Role).HasMaxLength(50).IsRequired();
            e.Property(m => m.Content).HasColumnType("TEXT");
            e.Property(m => m.RetrievalMode).HasConversion<string>().HasMaxLength(50);
            e.Property(m => m.Citations).HasColumnType("TEXT");
            e.HasIndex(m => m.ChatId);
            e.HasIndex(m => m.CreatedAt);
        });

        modelBuilder.Entity<Collection>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Description).HasMaxLength(1000);
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<ProcessingProgress>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.DocumentId).IsRequired();
            e.Property(p => p.FileName).HasMaxLength(500);
            e.Property(p => p.Stage).HasMaxLength(100);
            e.Property(p => p.Message).HasMaxLength(1000);
            e.Property(p => p.Status).HasMaxLength(50);
            e.HasIndex(p => p.DocumentId).IsUnique();
            e.HasIndex(p => p.Status);
        });
    }
}
