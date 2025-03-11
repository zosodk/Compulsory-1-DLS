using SharedLibrary.Models;

namespace SharedLibrary;

using Microsoft.EntityFrameworkCore;

public class DbContextConfig : DbContext
{
    public DbSet<Word> Words { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<Occurrence> Occurrences { get; set; }

    public DbContextConfig(DbContextOptions<DbContextConfig> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Occurrence>()
            .HasKey(o => new { o.WordId, o.FileId });

        modelBuilder.Entity<Occurrence>()
            .HasOne(o => o.Word)
            .WithMany(w => w.Occurrences)
            .HasForeignKey(o => o.WordId);

        modelBuilder.Entity<Occurrence>()
            .HasOne(o => o.File)
            .WithMany(f => f.Occurrences)
            .HasForeignKey(o => o.FileId);
    }
}
