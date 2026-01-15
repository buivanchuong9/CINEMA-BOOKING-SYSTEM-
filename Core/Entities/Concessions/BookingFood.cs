using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Entities.Bookings;

namespace BE.Core.Entities.Concessions;

/// <summary>
/// Bảng BookingFoods - Đồ ăn trong đơn đặt vé
/// </summary>
[Table("BookingFoods")]
public class BookingFood
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    public int FoodId { get; set; }
    
    [Required]
    public int Quantity { get; set; } = 1;
    
    /// <summary>
    /// Giá tại thời điểm đặt (lưu lại để tránh thay đổi giá sau này)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    [ForeignKey(nameof(BookingId))]
    public virtual Booking Booking { get; set; } = null!;
    
    [ForeignKey(nameof(FoodId))]
    public virtual Food Food { get; set; } = null!;
}
