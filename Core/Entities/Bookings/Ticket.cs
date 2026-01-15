using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.Bookings;

/// <summary>
/// Bảng Tickets - Vé điện tử (được tạo sau khi thanh toán thành công)
/// </summary>
[Table("Tickets")]
public class Ticket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int BookingDetailId { get; set; }
    
    /// <summary>
    /// Mã vé duy nhất (dùng để quét QR vào rạp)
    /// Format: CMAX-{BookingId}-{SeatNumber}-{RandomGuid}
    /// Ví dụ: CMAX-123-A5-7F8E9D
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TicketCode { get; set; } = string.Empty;
    
    /// <summary>
    /// URL ảnh QR code (lưu trong wwwroot/qrcodes/)
    /// </summary>
    [MaxLength(500)]
    public string? BarcodeImage { get; set; }
    
    /// <summary>
    /// Đã sử dụng vé chưa (quét QR tại cổng vào)
    /// </summary>
    public bool IsUsed { get; set; } = false;
    
    /// <summary>
    /// Thời gian quét vé
    /// </summary>
    public DateTime? UsedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    [ForeignKey(nameof(BookingDetailId))]
    public virtual BookingDetail BookingDetail { get; set; } = null!;
}
