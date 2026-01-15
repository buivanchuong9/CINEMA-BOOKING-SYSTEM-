using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE.Core.Enums;

namespace BE.Core.Entities.Movies;

/// <summary>
/// Bảng Movies - Thông tin phim
/// </summary>
[Table("Movies")]
public class Movie
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }
    
    /// <summary>
    /// URL trailer YouTube
    /// </summary>
    [MaxLength(500)]
    public string? TrailerUrl { get; set; }
    
    /// <summary>
    /// URL poster image
    /// </summary>
    [MaxLength(500)]
    public string? PosterUrl { get; set; }
    
    /// <summary>
    /// Thời lượng phim (phút)
    /// </summary>
    [Required]
    public int Duration { get; set; }
    
    /// <summary>
    /// Ngày khởi chiếu
    /// </summary>
    [Required]
    public DateTime ReleaseDate { get; set; }
    
    /// <summary>
    /// Trạng thái phim (NowShowing, ComingSoon, Ended)
    /// </summary>
    public MovieStatus Status { get; set; } = MovieStatus.ComingSoon;
    
    /// <summary>
    /// Đạo diễn
    /// </summary>
    [MaxLength(200)]
    public string? Director { get; set; }
    
    /// <summary>
    /// Diễn viên chính (cách nhau bởi dấu phẩy)
    /// </summary>
    [MaxLength(500)]
    public string? Cast { get; set; }
    
    /// <summary>
    /// Độ tuổi giới hạn (P, C13, C16, C18)
    /// </summary>
    [MaxLength(5)]
    public string? AgeRating { get; set; }
    
    /// <summary>
    /// Điểm đánh giá trung bình (0-10)
    /// </summary>
    [Column(TypeName = "decimal(3,1)")]
    public decimal? Rating { get; set; }
    
    /// <summary>
    /// Còn hoạt động không
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Alias cho Duration (dùng trong code)
    /// </summary>
    [NotMapped]
    public int DurationMinutes 
    { 
        get => Duration; 
        set => Duration = value; 
    }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Properties
    public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
