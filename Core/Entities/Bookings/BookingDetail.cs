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
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // tự động tăng
    public int Id { get; set; }
    
    [Required] 
    public int BookingId { get; set; } // id của đơn hàng
    
    [Required]
    public int SeatId { get; set; } // id của ghế
    
    /// <summary>
    /// Giá vé tại thời điểm đặt (lưu lại để tránh thay đổi giá sau này ảnh hưởng doanh thu)
    /// Công thức: Showtime.BasePrice * SeatType.SurchargeRatio * (1 + WeekendSurcharge)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")] // định nghĩa kiểu dữ liệu là decimal với 18 số tổng và 2 số thập phân
    public decimal PriceAtBooking { get; set; } // giá vé tại thời điểm đặt
    
    public DateTime CreatedAt { get; set; } = DateTime.Now; // thời gian tạo chi tiết đơn hàng
    
    // Navigation Properties
    [ForeignKey(nameof(BookingId))] // khóa ngoại của BookingId
    public virtual Booking Booking { get; set; } = null!;
    
    [ForeignKey(nameof(SeatId))] // khóa ngoại của SeatId
    public virtual Seat Seat { get; set; } = null!; 
    
    public virtual Ticket? Ticket { get; set; } // vé tương ứng
}
