using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Enums;
using BE.Core.Entities.Business;

namespace BE.Core.Entities.Bookings;

/// <summary>
/// Bảng Bookings - Đơn đặt vé
/// QUAN TRỌNG: Bảng này có Optimistic Locking (RowVersion) để xử lý concurrency
/// </summary>
[Table("Bookings")]
public class Booking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // tự động tăng
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty; // id của người dùng
    
    /// <summary>
    /// ID của lịch chiếu
    /// </summary>
    public int? ShowtimeId { get; set; } // id của lịch chiếu
    
    /// <summary>
    /// Thời gian tạo đơn
    /// </summary>
    [Required]
    public DateTime BookingDate { get; set; } = DateTime.Now; // thời gian tạo đơn
    
    /// <summary>
    /// Tổng tiền (bao gồm vé + đồ ăn - voucher)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")] // định nghĩa kiểu dữ liệu là decimal với 18 số tổng và 2 số thập phân
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Trạng thái đơn hàng (Pending, Holding, Paid, Cancelled)
    /// </summary>
    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Pending; // trạng thái đơn hàng
    
    /// <summary>
    /// Phương thức thanh toán
    /// </summary>
    public PaymentMethod? PaymentMethod { get; set; } // phương thức thanh toán
    
    /// <summary>
    /// Mã giao dịch từ payment gateway (VNPAY, MoMo...)
    /// </summary>
    [MaxLength(100)] // tối đa 100 ký tự
    public string? TransactionId { get; set; } // mã giao dịch
    
    /// <summary>
    /// Mã voucher đã sử dụng
    /// </summary>
    public int? VoucherId { get; set; }
    
    /// <summary>
    /// Số tiền giảm giá từ voucher
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] // định nghĩa kiểu dữ liệu là decimal với 18 số tổng và 2 số thập phân
    public decimal DiscountAmount { get; set; } = 0; // số tiền giảm giá
    
    /// <summary>
    /// Ghi chú của khách hàng
    /// </summary>
    [MaxLength(500)] // tối đa 500 ký tự
    public string? Notes { get; set; } // ghi chú của khách hàng
    
    /// <summary>
    /// Optimistic Concurrency Token (RowVersion)
    /// Dùng để phát hiện conflict khi 2 request cùng update 1 booking
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; } // token để phát hiện conflict
    
    public DateTime CreatedAt { get; set; } = DateTime.Now; // thời gian tạo đơn
    public DateTime? UpdatedAt { get; set; } // thời gian cập nhật
    
    // Navigation Properties
    [ForeignKey(nameof(UserId))] // khóa ngoại của UserId
    public virtual User User { get; set; } = null!;
    
    [ForeignKey(nameof(VoucherId))] // khóa ngoại của VoucherId
    public virtual Voucher? Voucher { get; set; }
    
    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>(); // danh sách chi tiết đơn hàng
}
