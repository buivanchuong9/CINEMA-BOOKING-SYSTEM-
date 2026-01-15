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
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID của lịch chiếu
    /// </summary>
    public int? ShowtimeId { get; set; }
    
    /// <summary>
    /// Thời gian tạo đơn
    /// </summary>
    [Required]
    public DateTime BookingDate { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Tổng tiền (bao gồm vé + đồ ăn - voucher)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Trạng thái đơn hàng (Pending, Holding, Paid, Cancelled)
    /// </summary>
    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    
    /// <summary>
    /// Phương thức thanh toán
    /// </summary>
    public PaymentMethod? PaymentMethod { get; set; }
    
    /// <summary>
    /// Mã giao dịch từ payment gateway (VNPAY, MoMo...)
    /// </summary>
    [MaxLength(100)]
    public string? TransactionId { get; set; }
    
    /// <summary>
    /// Mã voucher đã sử dụng
    /// </summary>
    public int? VoucherId { get; set; }
    
    /// <summary>
    /// Số tiền giảm giá từ voucher
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;
    
    /// <summary>
    /// Ghi chú của khách hàng
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Optimistic Concurrency Token (RowVersion)
    /// Dùng để phát hiện conflict khi 2 request cùng update 1 booking
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey(nameof(VoucherId))]
    public virtual Voucher? Voucher { get; set; }
    
    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
}
