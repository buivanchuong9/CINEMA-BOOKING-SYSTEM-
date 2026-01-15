using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.Concessions;

/// <summary>
/// Bảng Foods - Đồ ăn/nước uống
/// </summary>
[Table("Foods")]
public class Food
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    /// <summary>
    /// URL hình ảnh sản phẩm
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Có phải combo không (VD: Combo Bắp + Nước)
    /// </summary>
    public bool IsCombo { get; set; } = false;
    
    /// <summary>
    /// Còn hàng không
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    
    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
