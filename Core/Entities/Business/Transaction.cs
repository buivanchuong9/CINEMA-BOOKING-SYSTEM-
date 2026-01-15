using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Entities.Bookings;

namespace BE.Core.Entities.Business;

/// <summary>
/// Bảng Transactions - Lịch sử giao dịch thanh toán (QUAN TRỌNG cho đối soát với VNPAY)
/// </summary>
[Table("Transactions")]
public class Transaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    /// <summary>
    /// Mã tham chiếu từ VNPAY (vnp_TxnRef)
    /// </summary>
    [MaxLength(100)]
    public string? VnpayRef { get; set; }
    
    /// <summary>
    /// Mã giao dịch từ VNPAY (vnp_TransactionNo)
    /// </summary>
    [MaxLength(100)]
    public string? VnpayTransactionNo { get; set; }
    
    /// <summary>
    /// Số tiền thanh toán
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Mã phản hồi từ VNPAY (00 = thành công)
    /// </summary>
    [MaxLength(10)]
    public string? ResponseCode { get; set; }
    
    /// <summary>
    /// Nội dung phản hồi từ VNPAY
    /// </summary>
    [MaxLength(500)]
    public string? ResponseMessage { get; set; }
    
    /// <summary>
    /// Mã ngân hàng (nếu thanh toán qua ATM)
    /// </summary>
    [MaxLength(50)]
    public string? BankCode { get; set; }
    
    /// <summary>
    /// Loại thẻ (ATM, VISA, MASTERCARD...)
    /// </summary>
    [MaxLength(50)]
    public string? CardType { get; set; }
    
    /// <summary>
    /// Thời gian thanh toán
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Toàn bộ query string trả về từ VNPAY (để debug)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? RawResponse { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(BookingId))]
    public virtual Booking Booking { get; set; } = null!;
}
