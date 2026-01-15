using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Enums;
using BE.Infrastructure.Payment;

namespace BE.Controllers;

public class PaymentController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly VNPayHelper _vnPayHelper;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IUnitOfWork unitOfWork, 
        VNPayHelper vnPayHelper,
        ILogger<PaymentController> logger)
    {
        _unitOfWork = unitOfWork;
        _vnPayHelper = vnPayHelper;
        _logger = logger;
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
                // Payment successful - update booking status
                var booking = await _unitOfWork.Bookings.GetByIdAsync((int)response.OrderId);
                
                if (booking != null)
                {
                    booking.Status = BookingStatus.Paid;
                    // PaymentMethod already set during booking creation
                    
                    _unitOfWork.Bookings.Update(booking);
                    await _unitOfWork.SaveChangesAsync();
                    
                    TempData["Success"] = "Thanh toán thành công! Vé của bạn đã được xác nhận.";
                    return RedirectToAction("Details", "Booking", new { id = booking.Id });
                }
            }
            else
            {
                _logger.LogWarning($"VNPay payment failed: OrderId={response.OrderId}, ResponseCode={response.ResponseCode}");
                TempData["Error"] = "Thanh toán không thành công. Vui lòng thử lại!";
                
                // Cancel booking if payment failed
                var booking = await _unitOfWork.Bookings.GetByIdAsync((int)response.OrderId);
                if (booking != null)
                {
                    booking.Status = BookingStatus.Cancelled;
                    _unitOfWork.Bookings.Update(booking);
                    await _unitOfWork.SaveChangesAsync();
                }
                
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
                    booking.Status = BookingStatus.Paid;
                    
                    _unitOfWork.Bookings.Update(booking);
                    await _unitOfWork.SaveChangesAsync();
                    
                    return Json(new { RspCode = "00", Message = "Confirm Success" });
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
