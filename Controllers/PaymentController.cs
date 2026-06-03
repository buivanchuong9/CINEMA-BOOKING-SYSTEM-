using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Interfaces.Services;
using BE.Core.Enums;
using BE.Infrastructure.Payment;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BE.Controllers;

public class PaymentController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBookingService _bookingService;
    private readonly VietQRHelper _vietQRHelper;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IUnitOfWork unitOfWork,
        IBookingService bookingService,
        VietQRHelper vietQRHelper,
        ILogger<PaymentController> logger)
    {
        _unitOfWork = unitOfWork;
        _bookingService = bookingService;
        _vietQRHelper = vietQRHelper;
        _logger = logger;
    }

    // GET: /Payment/VietQRPayment?bookingId=1
    [Authorize]
    public async Task<IActionResult> VietQRPayment(int bookingId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            
            if (booking == null || booking.UserId != userId)
            {
                TempData["Error"] = "Không tìm thấy đơn đặt vé!";
                return RedirectToAction("Index", "Home");
            }

            if (booking.Status == BookingStatus.Paid)
            {
                TempData["Success"] = "Đơn đặt vé đã được thanh toán thành công!";
                return RedirectToAction("Details", "Booking", new { id = booking.Id });
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["Error"] = "Đơn đặt vé đã bị hủy!";
                return RedirectToAction("Index", "Home");
            }

            // Load showtime and movie
            var showtime = booking.ShowtimeId.HasValue 
                ? await _unitOfWork.Showtimes.GetByIdAsync(booking.ShowtimeId.Value) 
                : null;
            
            var movie = showtime != null 
                ? await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId) 
                : null;

            var room = showtime != null 
                ? await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId) 
                : null;

            if (room != null)
            {
                room.Cinema = (await _unitOfWork.Cinemas.GetByIdAsync(room.CinemaId))!;
            }

            // Generate dynamic VietQR image URL
            string qrImageUrl = _vietQRHelper.GenerateQRUrl(booking.TotalAmount, booking.Id.ToString());

            ViewBag.Booking = booking;
            ViewBag.Showtime = showtime;
            ViewBag.Movie = movie;
            ViewBag.Room = room;
            ViewBag.QRImageUrl = qrImageUrl;
            ViewBag.BankName = "VPBank (Ngân hàng TMCP Thịnh Vượng)";
            ViewBag.AccountNo = _vietQRHelper.Config.AccountNo;
            ViewBag.AccountName = _vietQRHelper.Config.AccountName;
            ViewBag.TransferContent = $"DATVE{booking.Id}";

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in VietQRPayment: bookingId={bookingId}");
            TempData["Error"] = "Có lỗi xảy ra!";
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: /Payment/CheckPaymentStatus?bookingId=1
    [HttpGet]
    public async Task<IActionResult> CheckPaymentStatus(int bookingId)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return Json(new { paid = false, status = "NotFound" });
            }

            return Json(new { 
                paid = booking.Status == BookingStatus.Paid, 
                status = booking.Status.ToString() 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking status for booking {bookingId}");
            return Json(new { paid = false, status = "Error" });
        }
    }

    // API Get Token for VietQR
    // POST: /vqr/api/token_generate
    [HttpPost]
    [Route("vqr/api/token_generate")]
    public IActionResult GenerateToken()
    {
        _logger.LogInformation("VietQR called token_generate API");
        try
        {
            // Read Authorization header
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authHeaderStr = authHeader.ToString();
                if (authHeaderStr.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    var base64Credentials = authHeaderStr.Substring(6).Trim();
                    var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
                    var parts = credentials.Split(':', 2);
                    
                    if (parts.Length == 2)
                    {
                        var username = parts[0];
                        var password = parts[1];
                        
                        if (username == _vietQRHelper.Config.Username && password == _vietQRHelper.Config.Password)
                        {
                            // Authentication successful
                            return Json(new
                            {
                                access_token = "vietqr_access_token_cinema",
                                token_type = "Bearer",
                                expires_in = 300
                            });
                        }
                    }
                }
            }

            _logger.LogWarning("Unauthorized attempt to access token_generate API");
            return Unauthorized(new { code = "401", desc = "Basic Authentication failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateToken API");
            return StatusCode(500, new { code = "500", desc = "Internal server error" });
        }
    }

    // API Callback for VietQR (Handles multiple webhook routes for reliability)
    // POST: /vqr/bank/api/transaction-callback
    // POST: /vqr/bank/api/test/transaction-callback
    // POST: /vqr/api/transaction-callback
    [HttpPost]
    [Route("vqr/bank/api/transaction-callback")]
    [Route("vqr/bank/api/test/transaction-callback")]
    [Route("vqr/api/transaction-callback")]
    public async Task<IActionResult> TransactionCallback([FromBody] JsonElement payload)
    {
        _logger.LogInformation($"VietQR transaction callback triggered: {payload.ToString()}");
        try
        {
            // 1. Verify Bearer token
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader) || 
                !authHeader.ToString().Equals("Bearer vietqr_access_token_cinema", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unauthorized callback attempt");
                return Unauthorized(new { code = "401", desc = "Invalid Bearer Token" });
            }

            // 2. Parse payload
            string? description = null;
            decimal amount = 0;
            string reference = $"VQR_{DateTime.Now:yyyyMMddHHmmss}";

            if (payload.ValueKind == JsonValueKind.Object)
            {
                if (payload.TryGetProperty("description", out var descProp)) 
                    description = descProp.GetString();
                else if (payload.TryGetProperty("content", out var contentProp)) 
                    description = contentProp.GetString();
                else if (payload.TryGetProperty("memo", out var memoProp)) 
                    description = memoProp.GetString();

                if (payload.TryGetProperty("amount", out var amountProp))
                {
                    if (amountProp.ValueKind == JsonValueKind.Number)
                        amount = amountProp.GetDecimal();
                    else if (amountProp.ValueKind == JsonValueKind.String && decimal.TryParse(amountProp.GetString(), out var parsedAmount))
                        amount = parsedAmount;
                }

                if (payload.TryGetProperty("reference", out var refProp))
                    reference = refProp.GetString() ?? reference;
                else if (payload.TryGetProperty("transactionId", out var transProp))
                    reference = transProp.GetString() ?? reference;
            }

            _logger.LogInformation($"Extracted payment: description='{description}', amount={amount}, reference='{reference}'");

            // 3. Extract Booking ID
            int? bookingId = ExtractBookingId(description);
            if (!bookingId.HasValue)
            {
                _logger.LogWarning($"Could not extract booking ID from description: '{description}'");
                return BadRequest(new { code = "400", desc = "Cannot identify booking ID in transfer content" });
            }

            // 4. Load booking and process confirmation
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId.Value);
            if (booking == null)
            {
                _logger.LogWarning($"Booking ID {bookingId} not found");
                return NotFound(new { code = "404", desc = $"Booking with ID {bookingId} not found" });
            }

            if (booking.Status == BookingStatus.Paid)
            {
                _logger.LogInformation($"Booking ID {bookingId} is already paid");
                return Ok(new { code = "200", desc = "Already Paid" });
            }

            // Compare amount (allow slight deviation or check exact amount)
            // VietQR.VN should send the correct amount, but we will print warning if it doesn't match
            if (Math.Abs(booking.TotalAmount - amount) > 500) // difference more than 500 VND
            {
                _logger.LogWarning($"Payment amount mismatch for Booking {bookingId}: Expected {booking.TotalAmount}, Received {amount}");
            }

            // Confirm payment using BookingService
            bool success = await _bookingService.ConfirmPaymentAsync(booking.Id, reference);
            if (success)
            {
                _logger.LogInformation($"Successfully confirmed payment for Booking ID {booking.Id} via VietQR Webhook");
                return Ok(new { code = "200", desc = "Success" });
            }

            _logger.LogError($"Failed to confirm payment in BookingService for Booking ID {booking.Id}");
            return StatusCode(500, new { code = "500", desc = "Error processing payment confirmation" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VietQR transaction callback");
            return StatusCode(500, new { code = "500", desc = "Internal server error" });
        }
    }

    private int? ExtractBookingId(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Matches 'DATVE' followed by spaces/hyphens and then digits
        var match = System.Text.RegularExpressions.Regex.Match(text, @"DATVE\s*-?\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
        {
            return id;
        }

        // Fallback: search for any number sequence
        var matchNumber = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
        if (matchNumber.Success && int.TryParse(matchNumber.Value, out int fallbackId))
        {
            return fallbackId;
        }

        return null;
    }
}
