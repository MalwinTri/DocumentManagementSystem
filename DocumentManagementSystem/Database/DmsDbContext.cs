using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Database;

public class DmsDbContext(DbContextOptions<DmsDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        
    }
}
