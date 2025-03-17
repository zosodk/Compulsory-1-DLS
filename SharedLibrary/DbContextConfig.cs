using SharedLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace SharedLibrary;


public class DbContextConfig : DbContext
{
    public DbSet<Word> Words { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<Occurrence> Occurrences { get; set; }
    

    public DbContextConfig(DbContextOptions<DbContextConfig> options)
        : base(options)
    {
        
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
    public void InsertInitialData()
    {
        /* if (!Files.Any())
         {
             Files.AddRange(
                 new FileEntity { FileId = 1, FileName = "file1.txt", Content = Convert.FromBase64String("SGVsbG8gd29ybGQ=") },
                 new FileEntity { FileId = 2, FileName = "file2.txt", Content = Convert.FromBase64String("VGVzdCBmaWxlIGNvbnRlbnQ=") }
             );
         }

         if (!Words.Any())
         {
             Words.AddRange(
                 new Word { WordId = 1, WordText = "Hello" },
                 new Word { WordId = 2, WordText = "World" },
                 new Word { WordId = 3, WordText = "Test" }
             );
         }

         if (!Occurrences.Any())
         {
             Occurrences.AddRange(
                 new Occurrence { WordId = 1, FileId = 1, Count = 3 },
                 new Occurrence { WordId = 2, FileId = 1, Count = 2 },
                 new Occurrence { WordId = 3, FileId = 2, Count = 5 }
             );
         }

         SaveChanges();*/
    }
}
