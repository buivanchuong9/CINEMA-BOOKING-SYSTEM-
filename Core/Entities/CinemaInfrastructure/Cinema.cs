using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.CinemaInfrastructure;

/// <summary>
/// Bảng Cinemas - Thông tin rạp chiếu phim
/// </summary>
[Table("Cinemas")]
public class Cinema
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    /// <summary>
    /// URL nhúng Google Maps (iframe)
    /// </summary>
    [MaxLength(1000)]
    public string? MapEmbedUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
