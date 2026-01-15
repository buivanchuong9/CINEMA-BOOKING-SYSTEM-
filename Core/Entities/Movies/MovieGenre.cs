using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.Movies;

/// <summary>
/// Bảng MovieGenres - Bảng trung gian Many-to-Many giữa Movies và Genres
/// </summary>
[Table("MovieGenres")]
public class MovieGenre
{
    [Required]
    public int MovieId { get; set; }
    
    [Required]
    public int GenreId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    [ForeignKey(nameof(MovieId))]
    public virtual Movie Movie { get; set; } = null!;
    
    [ForeignKey(nameof(GenreId))]
    public virtual Genre Genre { get; set; } = null!;
}
