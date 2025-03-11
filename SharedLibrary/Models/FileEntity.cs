using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models;

public class FileEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FileId { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = new byte[0];
    public ICollection<Occurrence> Occurrences { get; set; } = new List<Occurrence>();
}