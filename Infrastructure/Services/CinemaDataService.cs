using BE.Data;
using BE.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace BE.Infrastructure.Services;

/// <summary>
/// Service lấy dữ liệu rạp phim thực tế từ database để cung cấp context cho chatbot
/// </summary>
public class CinemaDataService
{
    private readonly AppDbContext _db;

    public CinemaDataService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách phim đang chiếu (NowShowing)
    /// </summary>
    public async Task<string> GetNowShowingMoviesAsync()
    {
        var movies = await _db.Movies
            .Where(m => m.Status == MovieStatus.NowShowing && m.IsActive)
            .OrderByDescending(m => m.Rating)
            .ToListAsync();

        if (!movies.Any())
            return "Hiện tại chưa có phim đang chiếu.";

        var lines = movies.Select(m =>
            $"• {m.Title} ({m.Duration} phút)" +
            (m.Rating.HasValue ? $" - ⭐ {m.Rating:0.0}/10" : "") +
            (m.AgeRating != null ? $" [{m.AgeRating}]" : ""));
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Lấy danh sách phim sắp chiếu (ComingSoon)
    /// </summary>
    public async Task<string> GetComingSoonMoviesAsync()
    {
        var movies = await _db.Movies
            .Where(m => m.Status == MovieStatus.ComingSoon && m.IsActive)
            .OrderBy(m => m.ReleaseDate)
            .ToListAsync();

        if (!movies.Any())
            return "Hiện tại chưa có thông tin phim sắp chiếu.";

        var lines = movies.Select(m =>
            $"• {m.Title} - Khởi chiếu: {m.ReleaseDate:dd/MM/yyyy}");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Lấy thông tin địa chỉ các rạp
    /// </summary>
    public async Task<string> GetCinemaAddressAsync()
    {
        var cinemas = await _db.Cinemas
            .Where(c => c.IsActive)
            .ToListAsync();

        if (!cinemas.Any())
            return "CineMax - Rạp chiếu phim hiện đại tại trung tâm thành phố.";

        var lines = cinemas.Select(c =>
            $"• {c.Name}: {c.Address}" + (c.Phone != null ? $" | Hotline: {c.Phone}" : ""));
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Lấy thông tin loại ghế và hệ số giá
    /// </summary>
    public async Task<string> GetTicketPricesAsync()
    {
        var seatTypes = await _db.SeatTypes.ToListAsync();
        // Lấy giá cơ bản từ showtime gần nhất
        var basePrice = await _db.Showtimes
            .Where(s => s.IsActive && s.StartTime > DateTime.Now)
            .OrderBy(s => s.StartTime)
            .Select(s => s.BasePrice)
            .FirstOrDefaultAsync();

        if (!seatTypes.Any())
            return "Giá vé từ 50.000 VND - 200.000 VND tuỳ loại ghế và suất chiếu.";

        if (basePrice == 0)
            return "Giá vé tuỳ loại ghế: Thường (1x), VIP (1.5x), Couple (2x) nhân với giá cơ bản của suất chiếu.";

        var lines = seatTypes.Select(s =>
            $"• Ghế {s.Name}: {(basePrice * s.SurchargeRatio):N0} VND");
        return "Giá vé tham khảo (suất chiếu tiêu chuẩn):\n" + string.Join("\n", lines);
    }

    /// <summary>
    /// Lấy lịch chiếu hôm nay và ngày mai
    /// </summary>
    public async Task<string> GetShowtimesAsync(string? movieTitle = null)
    {
        var now = DateTime.Now;
        var tomorrow = now.AddDays(2);

        var query = _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
            .Where(s => s.StartTime >= now && s.StartTime <= tomorrow && s.IsActive);

        if (!string.IsNullOrWhiteSpace(movieTitle))
            query = query.Where(s => s.Movie.Title.Contains(movieTitle));

        var showtimes = await query
            .OrderBy(s => s.StartTime)
            .Take(15)
            .ToListAsync();

        if (!showtimes.Any())
            return movieTitle != null
                ? $"Không tìm thấy lịch chiếu phim '{movieTitle}' trong 2 ngày tới."
                : "Hiện không có lịch chiếu trong 2 ngày tới.";

        var lines = showtimes.Select(s =>
            $"• {s.Movie.Title} | {s.StartTime:HH:mm dd/MM} | {s.Room.Cinema.Name} - Phòng {s.Room.Name}");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Xây dựng context tổng hợp cho chatbot từ dữ liệu thực tế
    /// </summary>
    public async Task<string> BuildContextAsync()
    {
        var nowShowing = await GetNowShowingMoviesAsync();
        var comingSoon = await GetComingSoonMoviesAsync();
        var cinemas = await GetCinemaAddressAsync();
        var prices = await GetTicketPricesAsync();

        return $@"=== DỮ LIỆU THỜI GIAN THỰC CỦA RẠP CINEMAX ===

📽️ PHIM ĐANG CHIẾU:
{nowShowing}

🎬 PHIM SẮP CHIẾU:
{comingSoon}

📍 ĐỊA CHỈ CÁC RẠP:
{cinemas}

🎫 BẢNG GIÁ VÉ:
{prices}

📋 CHÍNH SÁCH:
• Đặt vé online tại website hoặc ứng dụng CineMax
• Hủy vé miễn phí trước 2 giờ so với giờ chiếu
• Tích điểm thành viên: 1 điểm / 10.000 VND
• Thành viên Silver/Gold/Platinum được giảm giá đặc biệt
• Thanh toán qua VietQR / chuyển khoản ngân hàng VPBank
• Combo bắp nước giảm 15% khi đặt online
=== HẾT DỮ LIỆU ===";
    }
}