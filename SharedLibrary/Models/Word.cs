using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedLibrary.Models;

public class Word
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int WordId { get; set; }

    [Required]
    [StringLength(255)] 
    public string WordText { get; set; } = string.Empty;

    // for many-to-many relationship
    public ICollection<Occurrence> Occurrences { get; set; } = new List<Occurrence>();
}