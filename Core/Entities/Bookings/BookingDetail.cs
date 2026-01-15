using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Entities.CinemaInfrastructure;

namespace BE.Core.Entities.Bookings;

/// <summary>
/// Bảng BookingDetails - Chi tiết ghế trong đơn đặt vé
/// </summary>
[Table("BookingDetails")]
public class BookingDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    public int SeatId { get; set; }
    
    /// <summary>
    /// Giá vé tại thời điểm đặt (lưu lại để tránh thay đổi giá sau này ảnh hưởng doanh thu)
    /// Công thức: Showtime.BasePrice * SeatType.SurchargeRatio * (1 + WeekendSurcharge)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceAtBooking { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    [ForeignKey(nameof(BookingId))]
    public virtual Booking Booking { get; set; } = null!;
    
    [ForeignKey(nameof(SeatId))]
    public virtual Seat Seat { get; set; } = null!;
    
    public virtual Ticket? Ticket { get; set; }
}
