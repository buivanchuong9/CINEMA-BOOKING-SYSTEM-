using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Interfaces.Services;
using BE.Application.DTOs;
using System.Security.Claims;
using BE.Infrastructure.Payment;
using BE.Data;
using Microsoft.EntityFrameworkCore;

namespace BE.Controllers;

public class BookingController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;
    private readonly VNPayHelper _vnPayHelper;
    private readonly AppDbContext _context;

    public BookingController(
        IUnitOfWork unitOfWork, 
        IBookingService bookingService, 
        ILogger<BookingController> logger,
        VNPayHelper vnPayHelper,
        AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _bookingService = bookingService;
        _logger = logger;
        _vnPayHelper = vnPayHelper;
        _context = context;
    }

    // GET: /Booking/SelectSeats?showtimeId=1
    public async Task<IActionResult> SelectSeats(int showtimeId)
    {
        _logger.LogInformation($"SelectSeats called with showtimeId: {showtimeId}");
        
        // IMPORTANT: Clear change tracker để đảm bảo load fresh data từ DB
        _context.ChangeTracker.Clear();
        
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
            if (cinema != null)
            {
                room.Cinema = cinema;
            }
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
            if (seatType != null)
            {
                seat.SeatType = seatType;
            }
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
    // MVC PATTERN: Không cần HoldSeats API nữa - submit form trực tiếp
    // Giữ lại để backward compatibility nếu cần
    /*
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
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
    */

    // POST: /Booking/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingDto dto, bool useTestPayment = false)
    {
        _logger.LogInformation($"Create booking called with ShowtimeId: {dto.ShowtimeId}, SeatIds count: {dto.SeatIds?.Count ?? 0}, UseTestPayment: {useTestPayment}");
        
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

                // DEVELOPMENT: Bypass VNPay nếu useTestPayment = true
                if (useTestPayment)
                {
                    _logger.LogInformation($"Using TEST PAYMENT for Booking ID={booking.Id}");
                    return RedirectToAction("TestPayment", "Payment", new { bookingId = booking.Id });
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

        var booking = await _unitOfWork.Bookings.GetByIdAsync(id);
        if (booking == null || booking.UserId != userId)
        {
            TempData["Error"] = "Không tìm thấy đơn đặt vé!";
            return RedirectToAction("MyBookings");
        }

        // Load booking details (seats)
        var bookingDetails = (await _unitOfWork.BookingDetails.GetAllAsync())
            .Where(bd => bd.BookingId == id)
            .ToList();

        // Load showtime, movie, room, cinema
        BE.Core.Entities.Movies.Showtime? showtime = null;
        BE.Core.Entities.Movies.Movie? movie = null;
        BE.Core.Entities.CinemaInfrastructure.Room? room = null;
        BE.Core.Entities.CinemaInfrastructure.Cinema? cinema = null;

        if (booking.ShowtimeId.HasValue)
        {
            showtime = await _unitOfWork.Showtimes.GetByIdAsync(booking.ShowtimeId.Value);
            if (showtime != null)
            {
                movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
                room = await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId);
                if (room != null)
                {
                    cinema = await _unitOfWork.Cinemas.GetByIdAsync(room.CinemaId);
                }
            }
        }

        // Load seats with their info
        var seats = new List<BE.Core.Entities.CinemaInfrastructure.Seat>();
        foreach (var detail in bookingDetails)
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(detail.SeatId);
            if (seat != null)
            {
                seats.Add(seat);
            }
        }

        // Load foods
        var bookingFoods = (await _unitOfWork.BookingFoods.GetAllAsync())
            .Where(bf => bf.BookingId == id)
            .ToList();

        var foods = new List<(BE.Core.Entities.Concessions.Food food, int quantity)>();
        foreach (var bf in bookingFoods)
        {
            var food = await _unitOfWork.Foods.GetByIdAsync(bf.FoodId);
            if (food != null)
            {
                foods.Add((food, bf.Quantity));
            }
        }

        ViewBag.Booking = booking;
        ViewBag.Seats = seats;
        ViewBag.Showtime = showtime;
        ViewBag.Movie = movie;
        ViewBag.Room = room;
        ViewBag.Cinema = cinema;
        ViewBag.Foods = foods;
        ViewBag.BookingDetails = bookingDetails;

        return View();
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

    // ADMIN/DEV HELPER: Generate seats if missing
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FixMissingSeats(int showtimeId)
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null) return NotFound();

        var room = await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId);
        if (room == null) return NotFound();

        // Check if seats exist
        var existingSeats = (await _unitOfWork.Seats.GetAllAsync())
            .Where(s => s.RoomId == room.Id)
            .Any();

        if (!existingSeats)
        {
            var seatTypes = (await _unitOfWork.SeatTypes.GetAllAsync()).ToList();
            var standardType = seatTypes.FirstOrDefault(st => st.Name == "Standard") ?? seatTypes.First();

            var seats = new List<BE.Core.Entities.CinemaInfrastructure.Seat>();
            // Default 10x10 if not specified (or use room props if available)
            int rows = room.TotalRows > 0 ? room.TotalRows : 10;
            int cols = room.SeatsPerRow > 0 ? room.SeatsPerRow : 10;

            for (int r = 0; r < rows; r++)
            {
                char rowLabel = (char)('A' + r);
                for (int c = 1; c <= cols; c++)
                {
                    seats.Add(new BE.Core.Entities.CinemaInfrastructure.Seat
                    {
                        RoomId = room.Id,
                        Row = rowLabel.ToString(),
                        Number = c,
                        SeatTypeId = standardType.Id,
                        Status = BE.Core.Enums.SeatStatus.Available,
                        CreatedAt = DateTime.Now
                    });
                }
            }
            await _unitOfWork.Seats.AddRangeAsync(seats);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo {seats.Count} ghế cho phòng {room.Name}!";
        }
        else
        {
            TempData["Error"] = "Phòng đã có ghế, không thể tạo lại!";
        }

        return RedirectToAction("SelectSeats", new { showtimeId = showtimeId });
    }

    // Legacy view support
    public IActionResult SeatSelection()
    {
        return View();
    }
}
