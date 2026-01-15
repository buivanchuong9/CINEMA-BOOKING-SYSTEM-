using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Entities.CinemaInfrastructure;

namespace BE.Core.Entities.Movies;

/// <summary>
/// Bảng Showtimes - Lịch chiếu phim
/// </summary>
[Table("Showtimes")]
public class Showtime
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int MovieId { get; set; }
    
    [Required]
    public int RoomId { get; set; }
    
    /// <summary>
    /// Thời gian bắt đầu chiếu
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Thời gian kết thúc chiếu (tự động tính = StartTime + Movie.Duration)
    /// </summary>
    [Required]
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Property tiện ích: Alias cho StartTime (dùng trong Views)
    /// </summary>
    [NotMapped]
    public DateTime ShowDateTime 
    { 
        get => StartTime; 
        set => StartTime = value; 
    }
    
    /// <summary>
    /// Property tiện ích: Số ghế đã đặt (calculated from BookingDetails)
    /// </summary>
    [NotMapped]
    public int BookedSeats { get; set; }
    
    /// <summary>
    /// Property tiện ích: Số ghế còn trống (calculated)
    /// </summary>
    [NotMapped]
    public int AvailableSeats { get; set; }
    
    /// <summary>
    /// Giá vé cơ bản (chưa tính hệ số ghế)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }
    
    /// <summary>
    /// Có áp dụng giá cuối tuần không
    /// </summary>
    public bool IsWeekendPricing { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    [ForeignKey(nameof(MovieId))]
    public virtual Movie Movie { get; set; } = null!;
    
    [ForeignKey(nameof(RoomId))]
    public virtual Room Room { get; set; } = null!;
}
