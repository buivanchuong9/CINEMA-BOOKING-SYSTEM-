using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BE.Models;
using BE.Services;
using BE.Data;
using BE.Core.Entities.Business;

namespace BE.Controllers
{
    public class ChatController : Controller
    {
        private readonly GeminiChatService _chatService;
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;

        public ChatController(
            GeminiChatService chatService,
            AppDbContext db,
            UserManager<User> userManager)
        {
            _chatService = chatService;
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Trang chatbot chính
        /// </summary>
        [HttpGet]
        [Route("Chat")]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// API nhận tin nhắn và trả lời từ AI
        /// </summary>
        [HttpPost]
        [Route("api/chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { error = "Tin nhắn không được để trống." });

            // Lấy thông tin user nếu đã đăng nhập
            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
                userId = _userManager.GetUserId(User);

            // Lấy hoặc tạo session ID cho người dùng chưa đăng nhập
            var sessionId = HttpContext.Session.GetString("ChatSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N");
                HttpContext.Session.SetString("ChatSessionId", sessionId);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var reply = await _chatService.AskAsync(
                request.Message,
                userId,
                sessionId,
                ipAddress);

            return Ok(new ChatResponse { Reply = reply });
        }

        /// <summary>
        /// API lấy lịch sử chat của người dùng hiện tại (nếu đã đăng nhập)
        /// hoặc theo session (nếu chưa đăng nhập)
        /// </summary>
        [HttpGet]
        [Route("api/chat/history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            IQueryable<ChatHistory> query;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                query = _db.ChatHistories
                    .Where(ch => ch.UserId == userId);
            }
            else
            {
                var sessionId = HttpContext.Session.GetString("ChatSessionId");
                if (string.IsNullOrEmpty(sessionId))
                    return Ok(new { history = Array.Empty<object>(), total = 0 });

                query = _db.ChatHistories
                    .Where(ch => ch.SessionId == sessionId);
            }

            var total = await query.CountAsync();
            var history = await query
                .OrderByDescending(ch => ch.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ch => new
                {
                    ch.Id,
                    ch.UserMessage,
                    ch.BotReply,
                    CreatedAt = ch.CreatedAt.ToString("HH:mm dd/MM/yyyy")
                })
                .ToListAsync();

            return Ok(new { history, total, page, pageSize });
        }

        /// <summary>
        /// Xóa lịch sử chat của phiên hiện tại
        /// </summary>
        [HttpDelete]
        [Route("api/chat/history")]
        public async Task<IActionResult> ClearHistory()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var records = _db.ChatHistories.Where(ch => ch.UserId == userId);
                _db.ChatHistories.RemoveRange(records);
            }
            else
            {
                var sessionId = HttpContext.Session.GetString("ChatSessionId");
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var records = _db.ChatHistories.Where(ch => ch.SessionId == sessionId);
                    _db.ChatHistories.RemoveRange(records);
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã xóa lịch sử chat." });
        }
    }
}