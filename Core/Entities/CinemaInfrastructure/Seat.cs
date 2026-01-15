using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Enums;

namespace BE.Core.Entities.CinemaInfrastructure;

/// <summary>
/// Bảng Seats - Ghế ngồi trong phòng chiếu
/// </summary>
[Table("Seats")]
public class Seat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int RoomId { get; set; }
    
    /// <summary>
    /// Hàng ghế (A, B, C, D...)
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string Row { get; set; } = string.Empty;
    
    /// <summary>
    /// Số ghế trong hàng (1, 2, 3...)
    /// </summary>
    [Required]
    public int Number { get; set; }
    
    [Required]
    public int SeatTypeId { get; set; }
    
    /// <summary>
    /// Trạng thái ghế (Available, Holding, Booked, Unavailable)
    /// Note: Trạng thái Holding được quản lý bởi Redis, không lưu vào DB
    /// </summary>
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    [ForeignKey(nameof(RoomId))]
    public virtual Room Room { get; set; } = null!;
    
    [ForeignKey(nameof(SeatTypeId))]
    public virtual SeatType SeatType { get; set; } = null!;
}
