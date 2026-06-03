using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE.Core.Entities.Business;

/// <summary>
/// Bảng ChatHistories - Lưu lịch sử hội thoại chatbot
/// </summary>
[Table("ChatHistories")]
public class ChatHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// UserId (null nếu khách vãng lai)
    /// </summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    /// <summary>
    /// Session ID cho khách vãng lai (dùng cookie/guid)
    /// </summary>
    [MaxLength(100)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Tin nhắn của người dùng
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// Phản hồi của bot
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string BotReply { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian gửi tin nhắn
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Địa chỉ IP của người dùng
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
