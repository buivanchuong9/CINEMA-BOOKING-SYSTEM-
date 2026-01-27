using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Interfaces.Services;
using BE.Core.Enums;
using BE.Infrastructure.Payment;

namespace BE.Controllers;

public class PaymentController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBookingService _bookingService;
    private readonly VNPayHelper _vnPayHelper;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IUnitOfWork unitOfWork,
        IBookingService bookingService,
        VNPayHelper vnPayHelper,
        ILogger<PaymentController> logger)
    {
        _unitOfWork = unitOfWork;
        _bookingService = bookingService;
        _vnPayHelper = vnPayHelper;
        _logger = logger;
    }

    // GET: /Payment/TestPayment?bookingId=1
    // CHỈ DÙNG CHO DEVELOPMENT - Bypass VNPay để test
    public async Task<IActionResult> TestPayment(int bookingId)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            
            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy đơn đặt vé!";
                return RedirectToAction("Index", "Home");
            }

            if (booking.Status == BookingStatus.Paid)
            {
                TempData["Info"] = "Đơn hàng đã được thanh toán rồi!";
                return RedirectToAction("Details", "Booking", new { id = booking.Id });
            }

            // Simulate successful payment using Service to ensure consistent logic (Loyalty, Seats, Redis)
            var success = await _bookingService.ConfirmPaymentAsync(booking.Id, $"TEST_{DateTime.Now:yyyyMMddHHmmss}");
            
            if (success)
            {
                TempData["Success"] = "Thanh toán test thành công! Vé của bạn đã được xác nhận.";
                return RedirectToAction("Details", "Booking", new { id = booking.Id });
            }
            else
            {
                TempData["Error"] = "Lỗi khi xác nhận thanh toán!";
                return RedirectToAction("Index", "Home");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test payment");
            TempData["Error"] = "Có lỗi xảy ra!";
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: /Payment/VNPayReturn
    public async Task<IActionResult> VNPayReturn()
    {
        try
        {
            // Get all query parameters
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            
            // Process VNPay response
            var response = _vnPayHelper.ProcessReturn(queryParams);
            
            _logger.LogInformation($"VNPay return: OrderId={response.OrderId}, Success={response.Success}, ResponseCode={response.ResponseCode}");

            if (response.Success && response.SecureHashValid)
            {
                // Payment successful - use BookingService to confirm payment
                // This will: 1) Update booking status, 2) Set seats to Booked, 3) Release from Redis
                var confirmed = await _bookingService.ConfirmPaymentAsync(
                    (int)response.OrderId, 
                    response.TransactionId.ToString()
                );
                
                if (confirmed)
                {
                    TempData["Success"] = "Thanh toán thành công! Vé của bạn đã được xác nhận.";
                    return RedirectToAction("Details", "Booking", new { id = (int)response.OrderId });
                }
            }
            else
            {
                _logger.LogWarning($"VNPay payment failed: OrderId={response.OrderId}, ResponseCode={response.ResponseCode}");
                TempData["Error"] = "Thanh toán không thành công. Vui lòng thử lại!";
                
                // Cancel booking if payment failed - use BookingService
                await _bookingService.CancelBookingAsync((int)response.OrderId);
                
                return RedirectToAction("Index", "Home");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay return");
            TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán!";
        }
        
        return RedirectToAction("Index", "Home");
    }

    // POST: /Payment/VNPayIPN (Instant Payment Notification)
    [HttpPost]
    public async Task<IActionResult> VNPayIPN()
    {
        try
        {
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            var response = _vnPayHelper.ProcessReturn(queryParams);
            
            _logger.LogInformation($"VNPay IPN: OrderId={response.OrderId}, Success={response.Success}");

            if (response.Success && response.SecureHashValid)
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync((int)response.OrderId);
                
                if (booking != null && booking.Status == BookingStatus.Pending)
                {
                    // Use BookingService to confirm payment
                    var confirmed = await _bookingService.ConfirmPaymentAsync(
                        (int)response.OrderId,
                        response.TransactionId.ToString()
                    );
                    
                    if (confirmed)
                    {
                        return Json(new { RspCode = "00", Message = "Confirm Success" });
                    }
                }
            }
            
            return Json(new { RspCode = "99", Message = "Unknown error" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay IPN");
            return Json(new { RspCode = "99", Message = ex.Message });
        }
    }
}
