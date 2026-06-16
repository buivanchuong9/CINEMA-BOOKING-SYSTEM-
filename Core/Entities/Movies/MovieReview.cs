using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Entities.Business;

namespace BE.Core.Entities.Movies;

[Table("MovieReviews")]
public class MovieReview
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int MovieId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Số sao đánh giá (từ 1 đến 5)
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Nội dung nhận xét
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Phản hồi từ quản trị viên
    /// </summary>
    [MaxLength(1000)]
    public string? Response { get; set; }

    /// <summary>
    /// Thời gian phản hồi của admin
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Tên hoặc tài khoản Admin phản hồi
    /// </summary>
    [MaxLength(100)]
    public string? RespondedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation Properties
    [ForeignKey("MovieId")]
    public virtual Movie? Movie { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
