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

            // Simulate successful payment
            booking.Status = BookingStatus.Paid;
            booking.PaymentMethod = PaymentMethod.VNPAY;
            booking.TransactionId = $"TEST_{DateTime.Now:yyyyMMddHHmmss}";
            booking.UpdatedAt = DateTime.Now;
            
            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();
            
            TempData["Success"] = "Thanh toán test thành công! Vé của bạn đã được xác nhận.";
            return RedirectToAction("Details", "Booking", new { id = booking.Id });
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
                // Payment successful - update booking status
                var booking = await _unitOfWork.Bookings.GetByIdAsync((int)response.OrderId);
                
                if (booking != null)
                {
                    booking.Status = BookingStatus.Paid;
                    booking.PaymentMethod = PaymentMethod.VNPAY;
                    booking.TransactionId = response.TransactionId.ToString();
                    booking.UpdatedAt = DateTime.Now;
                    
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
                    booking.UpdatedAt = DateTime.Now;
                    _unitOfWork.Bookings.Update(booking);
                    await _unitOfWork.SaveChangesAsync();
                    
                    // Release seats về trạng thái available
                    var bookingDetails = (await _unitOfWork.BookingDetails.GetAllAsync())
                        .Where(bd => bd.BookingId == booking.Id);
                    foreach (var detail in bookingDetails)
                    {
                        var seat = await _unitOfWork.Seats.GetByIdAsync(detail.SeatId);
                        if (seat != null)
                        {
                            seat.Status = SeatStatus.Available;
                            _unitOfWork.Seats.Update(seat);
                        }
                    }
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
                    booking.PaymentMethod = PaymentMethod.VNPAY;
                    booking.TransactionId = response.TransactionId.ToString();
                    booking.UpdatedAt = DateTime.Now;
                    
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
