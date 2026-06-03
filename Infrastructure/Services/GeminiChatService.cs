using System.Text;
using System.Text.Json;
using BE.Data;
using BE.Core.Entities.Business;
using BE.Infrastructure.Services;

namespace BE.Services;

/// <summary>
/// Service giao tiếp với Gemini AI API, hỗ trợ lưu lịch sử hội thoại
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
    /// Gửi tin nhắn đến Gemini và nhận phản hồi, đồng thời lưu lịch sử vào DB
    /// </summary>
    public async Task<string> AskAsync(
        string message,
        string? userId = null,
        string? sessionId = null,
        string? ipAddress = null)
    {
        var apiKey = _config["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API key chưa được cấu hình.");
            return "⚠️ Chatbot chưa được cấu hình. Vui lòng liên hệ quản trị viên.";
        }

        // Lấy dữ liệu rạp từ DB để đưa vào prompt
        string cinemaContext;
        try
        {
            cinemaContext = await _cinemaData.BuildContextAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy dữ liệu rạp phim từ DB.");
            cinemaContext = "=== DỮ LIỆU RẠP CINEMAX ===\nVui lòng truy cập website để xem thông tin mới nhất.";
        }

        var systemPrompt = $@"Bạn là trợ lý AI của rạp phim CineMax - một trong những chuỗi rạp phim hiện đại và sang trọng nhất Việt Nam.

NGUYÊN TẮC TRẢ LỜI:
1. Chỉ trả lời các câu hỏi liên quan đến rạp phim, phim ảnh, đặt vé, lịch chiếu, giá vé, ưu đãi
2. Nếu câu hỏi không liên quan đến rạp phim, trả lời: ""Xin lỗi, tôi chỉ hỗ trợ thông tin về rạp phim CineMax. Bạn có câu hỏi gì về phim hoặc dịch vụ của chúng tôi không?""
3. Trả lời ngắn gọn, thân thiện, chuyên nghiệp bằng tiếng Việt
4. Sử dụng emoji phù hợp để câu trả lời sinh động hơn
5. Luôn đề xuất người dùng đặt vé online qua website khi phù hợp
6. KHÔNG bịa đặt thông tin không có trong dữ liệu bên dưới

DỮ LIỆU THỰC TẾ TỪ HỆ THỐNG:
{cinemaContext}

Hãy dùng dữ liệu trên để trả lời câu hỏi của khách hàng.";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = systemPrompt + "\n\nCÂU HỎI KHÁCH HÀNG: " + message }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 1024,
                topP = 0.9
            }
        };

        var json = JsonSerializer.Serialize(body);
        string reply;

        try
        {
            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var resultJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, resultJson);
                return "⚠️ Xin lỗi, tôi đang gặp sự cố kết nối. Vui lòng thử lại sau hoặc liên hệ hotline.";
            }

            using var doc = JsonDocument.Parse(resultJson);
            reply = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Xin lỗi, tôi không thể xử lý câu hỏi này. Bạn có thể hỏi khác không?";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Gemini API");
            reply = "⚠️ Dịch vụ chatbot tạm thời không khả dụng. Vui lòng thử lại sau.";
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