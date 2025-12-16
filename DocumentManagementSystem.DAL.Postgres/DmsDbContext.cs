using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Database;

public class DmsDbContext(DbContextOptions<DmsDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<DocumentMetadata> DocumentMetadatas => Set<DocumentMetadata>();
    public DbSet<ExtractedEntity> ExtractedEntities => Set<ExtractedEntity>();
    public DbSet<DocumentEmbedding> DocumentEmbeddings => Set<DocumentEmbedding>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Tag unique
        mb.Entity<Tag>()
          .HasIndex(t => t.Name)
          .IsUnique();

        // DocumentMetadata 1:1
        mb.Entity<DocumentMetadata>()
          .HasIndex(m => m.DocumentId)
          .IsUnique();

        mb.Entity<DocumentMetadata>()
          .HasOne(m => m.Document)
          .WithOne(d => d.Metadata)
          .HasForeignKey<DocumentMetadata>(m => m.DocumentId)
          .OnDelete(DeleteBehavior.Cascade);

        // DocumentEmbedding 1:1 (PK = DocumentId!)
        mb.Entity<DocumentEmbedding>()
          .HasKey(e => e.DocumentId);

        mb.Entity<DocumentEmbedding>()
          .HasOne(e => e.Document)
          .WithOne(d => d.Embedding)
          .HasForeignKey<DocumentEmbedding>(e => e.DocumentId)
          .OnDelete(DeleteBehavior.Cascade);

        // ExtractedEntity 1:n
        mb.Entity<ExtractedEntity>()
          .HasIndex(e => e.DocumentId);

        mb.Entity<ExtractedEntity>()
          .HasOne(e => e.Document)
          .WithMany(d => d.ExtractedEntities)
          .HasForeignKey(e => e.DocumentId)
          .OnDelete(DeleteBehavior.Cascade);
    }
}
