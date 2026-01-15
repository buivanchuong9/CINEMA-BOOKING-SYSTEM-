using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.CinemaInfrastructure;

/// <summary>
/// Bảng SeatTypes - Loại ghế (Standard, VIP, Couple)
/// </summary>
[Table("SeatTypes")]
public class SeatType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Hệ số nhân với giá vé cơ bản
    /// VD: VIP = 1.5, Couple = 2.0, Standard = 1.0
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal SurchargeRatio { get; set; } = 1.0m;
    
    /// <summary>
    /// Mã màu hiển thị trên UI (Hex format)
    /// VD: #FFD700 (vàng cho VIP), #4169E1 (xanh cho Standard)
    /// </summary>
    [MaxLength(7)]
    public string ColorHex { get; set; } = "#4169E1";
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
