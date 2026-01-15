using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Interfaces.Services;
using BE.Application.DTOs;
using System.Security.Claims;
using BE.Infrastructure.Payment;

namespace BE.Controllers;

public class BookingController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;
    private readonly VNPayHelper _vnPayHelper;

    public BookingController(
        IUnitOfWork unitOfWork, 
        IBookingService bookingService, 
        ILogger<BookingController> logger,
        VNPayHelper vnPayHelper)
    {
        _unitOfWork = unitOfWork;
        _bookingService = bookingService;
        _logger = logger;
        _vnPayHelper = vnPayHelper;
    }

    // GET: /Booking/SelectSeats?showtimeId=1
    public async Task<IActionResult> SelectSeats(int showtimeId)
    {
        _logger.LogInformation($"SelectSeats called with showtimeId: {showtimeId}");
        
        if (showtimeId <= 0)
        {
            TempData["Error"] = "Lịch chiếu không hợp lệ!";
            _logger.LogWarning($"Invalid showtimeId: {showtimeId}");
            return RedirectToAction("Index", "Movies");
        }

        // Validate showtime exists và đang active
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null)
        {
            TempData["Error"] = "Không tìm thấy lịch chiếu!";
            _logger.LogWarning($"Showtime not found: {showtimeId}");
            return RedirectToAction("Index", "Movies");
        }
        
        if (!showtime.IsActive)
        {
            TempData["Error"] = "Lịch chiếu đã ngừng bán vé!";
            _logger.LogWarning($"Showtime inactive: {showtimeId}");
            return RedirectToAction("Index", "Movies");
        }
        
        if (showtime.StartTime < DateTime.Now)
        {
            TempData["Error"] = "Lịch chiếu đã hết hạn!";
            _logger.LogWarning($"Showtime expired: {showtimeId}, StartTime: {showtime.StartTime}");
            return RedirectToAction("Index", "Movies");
        }

        // Load navigation properties
        var movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
        var room = await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId);
        if (room != null)
        {
            var cinema = await _unitOfWork.Cinemas.GetByIdAsync(room.CinemaId);
            room.Cinema = cinema;
        }
        
        // Get all seats in room with their types
        var seats = (await _unitOfWork.Seats.GetAllAsync())
            .Where(s => s.RoomId == showtime.RoomId)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.Number)
            .ToList();
        
        foreach (var seat in seats)
        {
            var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(seat.SeatTypeId);
            seat.SeatType = seatType;
        }
        
        // Get seat status for this showtime
        var seatStatus = await _bookingService.GetSeatStatusAsync(showtimeId);
        
        // Get available foods
        var foods = (await _unitOfWork.Foods.GetAllAsync())
            .Where(f => f.IsAvailable)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Name)
            .ToList();
        
        ViewBag.ShowtimeId = showtimeId;
        ViewBag.Showtime = showtime;
        ViewBag.Movie = movie;
        ViewBag.Room = room;
        ViewBag.Seats = seats;
        ViewBag.SeatStatus = seatStatus;
        ViewBag.Foods = foods;
        
        return View();
    }

    // POST: /Booking/HoldSeats
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> HoldSeats(int showtimeId, List<int> seatIds)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập!" });
        }

        var result = await _bookingService.SelectSeatsAsync(showtimeId, seatIds, userId);
        return Json(result);
    }

    // POST: /Booking/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingDto dto)
    {
        _logger.LogInformation($"Create booking called with ShowtimeId: {dto.ShowtimeId}, SeatIds count: {dto.SeatIds?.Count ?? 0}");
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Vui lòng đăng nhập!";
            return RedirectToAction("Login", "Account");
        }

        dto.UserId = userId;
        var result = await _bookingService.CreateBookingAsync(dto);

        if (result.Success)
        {
            // Tạo booking thành công → Redirect đến VNPay
            try
            {
                if (!result.BookingId.HasValue)
                {
                    TempData["Error"] = "Không tạo được đơn đặt vé!";
                    return RedirectToAction("SelectSeats", new { showtimeId = dto.ShowtimeId });
                }

                var booking = await _unitOfWork.Bookings.GetByIdAsync(result.BookingId.Value);
                if (booking == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn đặt vé!";
                    return RedirectToAction("SelectSeats", new { showtimeId = dto.ShowtimeId });
                }

                // Get client IP
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                // Create VNPay payment URL
                var paymentUrl = _vnPayHelper.CreatePaymentUrl(
                    orderId: booking.Id.ToString(),
                    amount: booking.TotalAmount,
                    orderInfo: $"Thanh toan ve xem phim - Ma don: {booking.Id}",
                    ipAddress: ipAddress
                );

                _logger.LogInformation($"Redirecting to VNPay: {paymentUrl}");
                
                // Redirect to VNPay
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment URL");
                TempData["Error"] = "Có lỗi khi chuyển đến trang thanh toán!";
                return RedirectToAction("SelectSeats", new { showtimeId = dto.ShowtimeId });
            }
        }

        TempData["Error"] = result.Message;
        return RedirectToAction("SelectSeats", new { showtimeId = dto.ShowtimeId });
    }

    [Authorize]
    // GET: /Booking/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var booking = await _bookingService.GetBookingByIdAsync(id, userId);
        if (booking == null)
        {
            TempData["Error"] = "Không tìm thấy đơn đặt vé!";
            return RedirectToAction("MyBookings");
        }

        return View(booking);
    }

    [Authorize]
    // GET: /Booking/MyBookings
    public async Task<IActionResult> MyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var bookings = await _bookingService.GetUserBookingsAsync(userId);
        return View(bookings);
    }

    // POST: /Booking/Cancel/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var success = await _bookingService.CancelBookingAsync(id);
        
        if (success)
        {
            TempData["Success"] = "Đã hủy đơn đặt vé thành công!";
        }
        else
        {
            TempData["Error"] = "Không thể hủy đơn đặt vé!";
        }

        return RedirectToAction("MyBookings");
    }

    // Legacy view support
    public IActionResult SeatSelection()
    {
        return View();
    }
}
