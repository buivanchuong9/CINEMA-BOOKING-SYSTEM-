using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.CinemaInfrastructure;

/// <summary>
/// Bảng Rooms - Phòng chiếu phim trong rạp
/// </summary>
[Table("Rooms")]
public class Room
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int CinemaId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tổng số hàng ghế (A, B, C...)
    /// </summary>
    [Required]
    public int TotalRows { get; set; } = 10;
    
    /// <summary>
    /// Số ghế mỗi hàng
    /// </summary>
    [Required]
    public int SeatsPerRow { get; set; } = 12;
    
    /// <summary>
    /// Ma trận sơ đồ ghế (JSON format)
    /// Ví dụ: {"rows": 10, "cols": 15, "aisles": [5, 10]}
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? SeatMapMatrix { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    [ForeignKey(nameof(CinemaId))]
    public virtual Cinema Cinema { get; set; } = null!;
    
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
