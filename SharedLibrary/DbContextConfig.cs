using SharedLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace SharedLibrary;


public class DbContextConfig : DbContext
{
    public DbSet<Word> Words { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<Occurrence> Occurrences { get; set; }

    private readonly IConfiguration _configuration;

    public DbContextConfig(DbContextOptions<DbContextConfig> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }
    

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
