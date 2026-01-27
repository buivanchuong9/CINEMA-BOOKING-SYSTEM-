using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BE.Core.Entities.Business;

/// <summary>
/// Bảng Users - Kế thừa từ IdentityUser để sử dụng ASP.NET Core Identity
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// Họ và tên đầy đủ
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Điểm tích lũy (1 điểm = 10,000 VND)
    /// </summary>
    public int Points { get; set; } = 0;

    /// <summary>
    /// Tổng số vé đã mua (dùng để xét hạng thành viên)
    /// </summary>
    public int TotalTicketsPurchased { get; set; } = 0;
    
    /// <summary>
    /// Cấp độ thành viên (Bronze, Silver, Gold, Platinum)
    /// </summary>
    [MaxLength(20)]
    public string MembershipLevel { get; set; } = "Bronze";
    
    /// <summary>
    /// Ngày sinh
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>
    /// Giới tính (Male, Female, Other)
    /// </summary>
    [MaxLength(10)]
    public string? Gender { get; set; }
    
    /// <summary>
    /// Avatar URL
    /// </summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }
}
