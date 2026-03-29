using BE.Core.Entities.Bookings;
using BE.Application.DTOs;

namespace BE.Core.Interfaces.Services;

/// <summary>
/// Booking Service Interface
/// Core business logic cho đặt vé, tính giá, xử lý payment
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Bước 1: Chọn ghế và hold trong Redis (10 phút)
    /// </summary>
    Task<BookingSeatResult> SelectSeatsAsync(int showtimeId, List<int> seatIds, string userId); // chọn ghế và hold trong Redis (10 phút)
    
    /// <summary>
    /// Bước 2: Tạo Booking (Status = Pending)
    /// </summary>
    Task<CreateBookingResult> CreateBookingAsync(CreateBookingDto dto); // tạo booking (Status = Pending)
    
    /// <summary>
    /// Bước 3: Xác nhận thanh toán và chuyển Status = Paid
    /// </summary>
    Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId); // xác nhận thanh toán và chuyển Status = Paid
    
    /// <summary>
    /// Hủy booking và release seats
    /// </summary>
    Task<bool> CancelBookingAsync(int bookingId);
    
    /// <summary>
    /// Tính tổng giá vé (Dynamic Pricing)
    /// BasePrice * SeatType.Ratio * (Weekend Multiplier) - Voucher + Food
    /// </summary>
    Task<decimal> CalculateTotalPriceAsync(int showtimeId, List<int> seatIds, int? voucherId = null, List<FoodItemDto>? foods = null); // tính tổng giá vé (Dynamic Pricing)
    
    /// <summary>
    /// Get seat status cho showtime (Available, Held, Sold)
    /// </summary>
    Task<List<SeatStatusDto>> GetSeatStatusAsync(int showtimeId); // lấy trạng thái ghế cho showtime (Available, Held, Sold)
    
    /// <summary>
    /// Get booking details cho user
    /// </summary>
    Task<Booking?> GetBookingByIdAsync(int bookingId, string userId); // lấy chi tiết booking cho user
    
    /// <summary>
    /// Get user's booking history
    /// </summary>
    Task<List<Booking>> GetUserBookingsAsync(string userId); // lấy lịch sử đặt vé của user
}
