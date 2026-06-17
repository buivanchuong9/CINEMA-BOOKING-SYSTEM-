using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BE.Data;
using BE.Core.Entities.Business;
using BE.Infrastructure.Services;
using System.Net.Http;

namespace BE.Services;

/// <summary>
/// Service xử lý hội thoại chatbot nội bộ (không tốn token API ngoài)
/// </summary>
public class GeminiChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly CinemaDataService _cinemaData;
    private readonly ILogger<GeminiChatService> _logger;

    public GeminiChatService(
        HttpClient httpClient,
        IConfiguration config,
        AppDbContext db,
        CinemaDataService cinemaData,
        ILogger<GeminiChatService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _db = db;
        _cinemaData = cinemaData;
        _logger = logger;
    }

    /// <summary>
    /// Nhận tin nhắn và sinh phản hồi cục bộ từ DB và các tập luật có sẵn
    /// </summary>
    public async Task<string> AskAsync(
        string message,
        string? userId = null,
        string? sessionId = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Xin chào! Bạn cần tôi giúp đỡ gì hôm nay?";

        var msg = message.ToLower().Trim();
        string reply = "";

        try
        {
            // 1. Phim đang chiếu
            if (msg.Contains("đang chiếu") || msg.Contains("now showing") || msg.Contains("xem gì") || msg.Contains("phim hot"))
            {
                var nowShowing = await _cinemaData.GetNowShowingMoviesAsync();
                reply = "📽️ **CÁC PHIM ĐANG CHIẾU TẠI CÁC RẠP CINEMAX:**\n\n" + nowShowing;
            }
            // 2. Phim sắp chiếu
            else if (msg.Contains("sắp chiếu") || msg.Contains("coming soon") || msg.Contains("phim mới") || msg.Contains("sắp ra mắt"))
            {
                var comingSoon = await _cinemaData.GetComingSoonMoviesAsync();
                reply = "🎬 **DANH SÁCH PHIM SẮP CHIẾU TRONG THỜI GIAN TỚI:**\n\n" + comingSoon;
            }
            // 3. Địa chỉ
            else if (msg.Contains("địa chỉ") || msg.Contains("rạp ở đâu") || msg.Contains("chi nhánh") || msg.Contains("vị trí") || msg.Contains("ở đâu"))
            {
                var address = await _cinemaData.GetCinemaAddressAsync();
                reply = "📍 **ĐỊA CHỈ HỆ THỐNG CÁC CỤM RẠP CINEMAX:**\n\n" + address;
            }
            // 4. Giá vé
            else if (msg.Contains("giá vé") || msg.Contains("bảng giá") || msg.Contains("tiền vé") || msg.Contains("vé bao nhiêu"))
            {
                var prices = await _cinemaData.GetTicketPricesAsync();
                reply = "🎫 **BẢNG GIÁ VÉ THAM KHẢO TẠI CINEMAX:**\n\n" + prices;
            }
            // 5. Combo bắp nước
            else if (msg.Contains("bắp") || msg.Contains("nước") || msg.Contains("đồ ăn") || msg.Contains("combo") || msg.Contains("popcorn") || msg.Contains("thức ăn") || msg.Contains("ăn gì"))
            {
                var foods = await _db.Foods.Where(f => f.IsAvailable).ToListAsync();
                if (foods.Any())
                {
                    reply = "🍿 **DANH SÁCH BẮP NƯỚC & COMBO TẠI QUẦY (Giảm 15% khi đặt online):**\n\n" + 
                            string.Join("\n", foods.Select(f => $"• **{f.Name}**: {f.Price:N0}đ\n  *{f.Description}*"));
                }
                else
                {
                    reply = "🍿 Hiện tại chưa có thông tin combo bắp nước trên hệ thống.";
                }
            }
            // 6. Khuyến mãi / Voucher
            else if (msg.Contains("khuyến mãi") || msg.Contains("ưu đãi") || msg.Contains("giảm giá") || msg.Contains("voucher") || msg.Contains("mã"))
            {
                var vouchers = await _db.Vouchers.Where(v => v.IsActive && v.ExpiryDate > DateTime.Now).ToListAsync();
                if (vouchers.Any())
                {
                    reply = "🎁 **CÁC MÃ GIẢM GIÁ & ƯU ĐÃI ĐANG HOẠT ĐỘNG:**\n\n" + 
                            string.Join("\n", vouchers.Select(v => $"• Mã **{v.Code}**: Giảm {v.DiscountPercent:0}% (Giảm tối đa {v.MaxAmount:N0}đ cho đơn hàng từ {v.MinOrderAmount:N0}đ - Hạn dùng: {v.ExpiryDate:dd/MM/yyyy})"));
                }
                else
                {
                    reply = "🎁 Hiện tại rạp chưa có chương trình phát hành voucher mới. Bạn hãy đăng ký thành viên để tích lũy điểm và nhận ưu đãi nhé!";
                }
            }
            // 7. Hoàn tiền / Hủy vé
            else if (msg.Contains("hoàn tiền") || msg.Contains("hủy vé") || msg.Contains("trả vé") || msg.Contains("đổi vé") || msg.Contains("hủy"))
            {
                reply = "📝 **CHÍNH SÁCH HỦY VÉ & HOÀN TIỀN TẠI CINEMAX:**\n\n" +
                        "• Khách hàng có thể thực hiện yêu cầu hủy vé trực tuyến tại trang **Hồ Sơ cá nhân** -> **Lịch Sử Đặt Vé** trước giờ suất chiếu diễn ra tối thiểu 2 giờ.\n" +
                        "• Yêu cầu của bạn sẽ được gửi tới Admin xử lý:\n" +
                        "  - *Nếu được chấp nhận*: Vé sẽ được hủy, tiền/điểm tích lũy sẽ được hoàn trả lại tài khoản của bạn.\n" +
                        "  - *Nếu bị từ chối*: Vé của bạn sẽ được chuyển lại trạng thái 'Đã thanh toán' bình thường và nhật ký yêu cầu sẽ hiển thị từ chối hoàn tiền.\n" +
                        "• Lưu ý: Các vé đã sát giờ chiếu (dưới 2 tiếng) sẽ không thể thực hiện yêu cầu hủy vé.";
            }
            // 8. Đặt vé
            else if (msg.Contains("đặt vé") || msg.Contains("cách đặt") || msg.Contains("mua vé"))
            {
                reply = "📱 **HƯỚNG DẪN ĐẶT VÉ ONLINE NHANH CHÓNG:**\n\n" +
                        "1. Nhấp vào mục **Lịch Chiếu** hoặc chọn phim yêu thích tại **Trang Chủ**.\n" +
                        "2. Chọn suất chiếu và cụm rạp phù hợp.\n" +
                        "3. Chọn số lượng ghế ngồi (Standard/VIP/Couple).\n" +
                        "4. Chọn thêm combo bắp nước để được giảm giá 15%.\n" +
                        "5. Kiểm tra thông tin đặt vé và quét mã VietQR để hoàn tất thanh toán tự động.\n\n" +
                        "Chúc bạn có một buổi xem phim vui vẻ tại CineMax! 🍿🎬";
            }
            // 9. Lịch chiếu
            else if (msg.Contains("lịch chiếu") || msg.Contains("suất chiếu") || msg.Contains("giờ chiếu"))
            {
                var showtimes = await _cinemaData.GetShowtimesAsync();
                reply = "📅 **LỊCH CHIẾU CÁC SUẤT CHIẾU TRONG HÔM NAY VÀ NGÀY MAI:**\n\n" + showtimes;
            }
            // 10. Liên hệ
            else if (msg.Contains("liên hệ") || msg.Contains("hotline") || msg.Contains("hỗ trợ") || msg.Contains("sđt") || msg.Contains("điện thoại"))
            {
                reply = "📞 **THÔNG TIN LIÊN HỆ & HỖ TRỢ:**\n\n" +
                        "• **Hotline hỗ trợ:** 1900 1234\n" +
                        "• **Email chăm sóc khách hàng:** support@cinemax.com.vn\n" +
                        "• Hoặc bạn có thể đến quầy CSKH trực tiếp tại các chi nhánh CineMax Đống Đa/Cầu Giấy để nhân viên hỗ trợ nhanh nhất!";
            }
            // 11. Mặc định
            else
            {
                reply = "🤖 **Xin chào! Tôi là Trợ Lý Tự Động của rạp phim CineMax.**\n\n" +
                        "Tôi hỗ trợ tra cứu thông tin nhanh chóng và hoàn toàn miễn phí. Hãy nhấp chọn một trong các chủ đề định hướng nhanh bên dưới hoặc nhập từ khóa liên quan:\n\n" +
                        "📽️ **Phim đang chiếu** | 🎬 **Phim sắp chiếu**\n" +
                        "📍 **Địa chỉ các rạp** | 🎫 **Bảng giá vé & Loại ghế**\n" +
                        "🍿 **Combo bắp nước** | 📅 **Lịch chiếu hôm nay**\n" +
                        "🎁 **Khuyến mãi/Voucher** | 📝 **Chính sách hủy vé & Hoàn tiền**";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh phản hồi tự động cục bộ.");
            reply = "⚠️ Rất tiếc, tôi đang gặp lỗi hệ thống nhỏ khi truy vấn thông tin. Bạn vui lòng thử lại sau nhé!";
        }

        // Lưu lịch sử vào DB
        try
        {
            var history = new ChatHistory
            {
                UserId = userId,
                SessionId = sessionId,
                UserMessage = message,
                BotReply = reply,
                IpAddress = ipAddress,
                CreatedAt = DateTime.Now
            };
            _db.ChatHistories.Add(history);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lưu lịch sử chat vào DB.");
        }

        return reply;
    }
}