using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.Business;

/// <summary>
/// Bảng Vouchers - Mã giảm giá
/// </summary>
[Table("Vouchers")]
public class Voucher
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    /// <summary>
    /// Mã voucher (VD: SUMMER2026, NEWUSER50)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên voucher
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Phần trăm giảm giá (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; } = 0;
    
    /// <summary>
    /// Số tiền giảm tối đa
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxAmount { get; set; } = 0;
    
    /// <summary>
    /// Giá trị đơn hàng tối thiểu để áp dụng
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinOrderAmount { get; set; } = 0;
    
    /// <summary>
    /// Ngày bắt đầu hiệu lực
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Ngày hết hạn
    /// </summary>
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>
    /// Số lượng voucher có thể sử dụng (null = không giới hạn)
    /// </summary>
    public int? UsageLimit { get; set; }
    
    /// <summary>
    /// Số lần đã sử dụng
    /// </summary>
    public int UsedCount { get; set; } = 0;
    
    /// <summary>
    /// Còn hoạt động không
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// ID người sở hữu voucher (nếu là voucher cá nhân)
    /// </summary>
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
