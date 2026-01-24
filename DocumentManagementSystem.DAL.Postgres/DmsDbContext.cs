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

        // Enable Postgres extension
        mb.HasPostgresExtension("citext");

        // Tags.Name: case-insensitive unique via citext
        mb.Entity<Tag>(e =>
        {
            e.Property(t => t.Name)
             .HasMaxLength(64)
             .HasColumnType("citext")
             .IsRequired();

            e.HasIndex(t => t.Name)
             .IsUnique();
        });

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

        // DocumentDailyAccess 1:n (Document -> DailyAccess)
        mb.Entity<DocumentDailyAccess>(e =>
        {
            e.ToTable("DocumentDailyAccesses");

            // pro Document + Day genau ein Eintrag
            e.HasKey(x => new { x.DocumentId, x.Day });

            e.Property(x => x.Day).HasColumnType("date");
            e.Property(x => x.Count).IsRequired();

            e.HasOne(x => x.Document)
             .WithMany(d => d.DailyAccess)
             .HasForeignKey(x => x.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);

            // optional, wenn du oft nach Datum über alle Docs filterst:
            e.HasIndex(x => x.Day);
        });
    }
}