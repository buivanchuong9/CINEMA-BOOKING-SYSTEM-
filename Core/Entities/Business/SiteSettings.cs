namespace BE.Core.Entities.Business;

/// <summary>
/// Lưu cấu hình giao diện của website, được chỉnh sửa bởi Admin.
/// </summary>
public class SiteSettings
{
    public int Id { get; set; }

    // ===== BRANDING =====
    /// <summary>Tên thương hiệu hiển thị trên navbar và footer</summary>
    public string SiteName { get; set; } = "CineMax";

    /// <summary>Slogan hiển thị ở footer</summary>
    public string SiteSlogan { get; set; } = "Trải nghiệm sự kỳ diệu của rạp phim với sự thoải mái cao cấp.";

    /// <summary>Icon Bootstrap Icons class cho logo (ví dụ: bi-camera-reels-fill)</summary>
    public string LogoIcon { get; set; } = "bi-camera-reels-fill";

    /// <summary>URL logo tùy chỉnh (nếu muốn dùng ảnh thay icon)</summary>
    public string? LogoUrl { get; set; }

    // ===== CONTACT INFO (footer) =====
    public string ContactAddress { get; set; } = "Đống Đa, Hà Nội";
    public string ContactPhone { get; set; } = "0369062042";
    public string ContactEmail { get; set; } = "support@cinemax.com";

    // ===== COLOR THEME =====
    /// <summary>Màu chủ đạo (accent, nút chính) - hex</summary>
    public string PrimaryColor { get; set; } = "#e11d48";

    /// <summary>Màu phụ (gold) - hex</summary>
    public string SecondaryColor { get; set; } = "#f59e0b";

    /// <summary>Màu nền chính - hex</summary>
    public string BgPrimaryColor { get; set; } = "#0f172a";

    /// <summary>Màu nền phụ - hex</summary>
    public string BgSecondaryColor { get; set; } = "#1e293b";

    // ===== MOVIE DISPLAY SETTINGS =====
    /// <summary>Số phim hiển thị trên trang chủ (phim đang chiếu)</summary>
    public int HomeMoviesCount { get; set; } = 8;

    /// <summary>Chế độ hiển thị phim: Grid | List</summary>
    public string MovieDisplayMode { get; set; } = "Grid";

    /// <summary>Số cột hiển thị phim (2, 3, 4, 6)</summary>
    public int MovieGridColumns { get; set; } = 4;

    /// <summary>Hiển thị phim sắp chiếu trên trang chủ không</summary>
    public bool ShowComingSoon { get; set; } = true;

    /// <summary>Số phim sắp chiếu hiển thị trang chủ</summary>
    public int ComingSoonCount { get; set; } = 4;

    /// <summary>Hiển thị rating phim không</summary>
    public bool ShowMovieRating { get; set; } = true;

    /// <summary>Hiển thị thể loại phim không</summary>
    public bool ShowMovieGenre { get; set; } = true;

    /// <summary>Hiển thị thời lượng phim không</summary>
    public bool ShowMovieDuration { get; set; } = true;

    // ===== HERO SLIDER =====
    /// <summary>Chiều cao hero slider (vh)</summary>
    public int HeroSliderHeight { get; set; } = 85;

    /// <summary>Có bật hero slider trang chủ không</summary>
    public bool EnableHeroSlider { get; set; } = true;

    // ===== FONT =====
    /// <summary>Google Font family (để trống = dùng mặc định hệ thống)</summary>
    public string FontFamily { get; set; } = "Inter";

    // ===== METADATA =====
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
}
