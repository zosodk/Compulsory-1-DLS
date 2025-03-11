using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SharedLibrary.Models;

public class Occurrence
{
    [Key, Column(Order = 1)]
    public int WordId { get; set; }

    [Key, Column(Order = 2)]
    public int FileId { get; set; }

    [Required]
    public int Count { get; set; } //  nr of times the word appears in the file

    [ForeignKey("WordId")]
    public Word Word { get; set; } = null!;

    [ForeignKey("FileId")]
    public FileEntity File { get; set; } = null!;
}