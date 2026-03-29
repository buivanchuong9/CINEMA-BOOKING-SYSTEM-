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
        
        // Xóa change tracker để đảm bảo load fresh data từ DB
        _context.ChangeTracker.Clear();
        
        if (showtimeId <= 0) // kiểm tra showtimeId có hợp lệ không
        {
            TempData["Error"] = "Lịch chiếu không hợp lệ!";
            _logger.LogWarning($"Invalid showtimeId: {showtimeId}");
            return RedirectToAction("Index", "Movies"); // chuyển hướng đến trang danh sách phim
        }

        // Kiểm tra showtime tồn tại và đang hoạt động
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null) // nếu không tìm thấy showtime
        {
            TempData["Error"] = "Không tìm thấy lịch chiếu!";
            _logger.LogWarning($"Showtime not found: {showtimeId}");
            return RedirectToAction("Index", "Movies"); // chuyển hướng đến trang danh sách phim
        }
        
        if (!showtime.IsActive) // kiểm tra showtime còn hoạt động không
        {
            TempData["Error"] = "Lịch chiếu đã ngừng bán vé!";
            _logger.LogWarning($"Showtime inactive: {showtimeId}");
            return RedirectToAction("Index", "Movies"); // chuyển hướng đến trang danh sách phim
        }
        
        if (showtime.StartTime < DateTime.Now) // kiểm tra showtime đã hết hạn chưa
        {
            TempData["Error"] = "Lịch chiếu đã hết hạn!";
            _logger.LogWarning($"Showtime expired: {showtimeId}, StartTime: {showtime.StartTime}");
            return RedirectToAction("Index", "Movies");
        }

        // Tải thuộc tính điều hướng
        var movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId); // lấy thông tin phim
        var room = await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId); 
        if (room != null) // nếu tìm thấy phòng
        {
            var cinema = await _unitOfWork.Cinemas.GetByIdAsync(room.CinemaId); // lấy thông tin rạp
            if (cinema != null) // nếu tìm thấy rạp
            {
                room.Cinema = cinema; // gán rạp cho phòng
            }
        }
        
        // Lấy tất cả ghế trong phòng với loại ghế của chúng
        var seats = (await _unitOfWork.Seats.GetAllAsync())
            .Where(s => s.RoomId == showtime.RoomId)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.Number)
            .ToList();
        
        foreach (var seat in seats) // duyệt qua từng ghế
        {
            var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(seat.SeatTypeId); // lấy loại ghế
            if (seatType != null) // nếu tìm thấy loại ghế
            {
                seat.SeatType = seatType; // gán loại ghế cho ghế
            }
        }
        
        // Lấy trạng thái ghế cho showtime này
        var seatStatus = await _bookingService.GetSeatStatusAsync(showtimeId);
        
        // Lấy thực phẩm có sẵn
        var foods = (await _unitOfWork.Foods.GetAllAsync())
            .Where(f => f.IsAvailable)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Name)
            .ToList();
        
        ViewBag.ShowtimeId = showtimeId; // gán showtimeId cho ViewBag
        ViewBag.Showtime = showtime;
        ViewBag.Movie = movie;
        ViewBag.Room = room;
        ViewBag.Seats = seats;
        ViewBag.SeatStatus = seatStatus;
        ViewBag.Foods = foods;
        
        return View();
    }

    // POST: /Booking/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingDto dto, bool useTestPayment = false)
    {
        _logger.LogInformation($"Create booking called with ShowtimeId: {dto.ShowtimeId}, SeatIds count: {dto.SeatIds?.Count ?? 0}, UseTestPayment: {useTestPayment}");
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // lấy Id của người dùng đang đăng nhập.
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Vui lòng đăng nhập!";
            return RedirectToAction("Login", "Account");
        }

        dto.UserId = userId;
        var result = await _bookingService.CreateBookingAsync(dto); 

        if (result.Success) // nếu tạo booking thành công
        {
            // Tạo booking thành công -> Redirect đến VNPay
            try
            {
                if (!result.BookingId.HasValue) // nếu không có bookingId thì trả về lỗi
                {
                    TempData["Error"] = "Không tạo được đơn đặt vé!";
                    return RedirectToAction("SelectSeats", new { showtimeId = dto.ShowtimeId });
                }

                var booking = await _unitOfWork.Bookings.GetByIdAsync(result.BookingId.Value); // lấy booking
                if (booking == null) // nếu không tìm thấy booking
                {
                    TempData["Error"] = "Không tìm thấy đơn đặt vé!";
                    return RedirectToAction("SelectSeats", new { showtimeId = dto.ShowtimeId });
                }

                // dev Vượt qua VNPay nếu useTestPayment = true
                if (useTestPayment)
                {
                    _logger.LogInformation($"Using TEST PAYMENT for Booking ID={booking.Id}");
                    return RedirectToAction("TestPayment", "Payment", new { bookingId = booking.Id });
                }

                // Lấy IP của client
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                
                // Tạo URL thanh toán VNPay
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
    public async Task<IActionResult> Details(int id) // hiển thị chi tiết đơn đặt vé
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // lấy Id của người dùng đang đăng nhập.
        if (string.IsNullOrEmpty(userId)) // nếu không có Id
        {
            return RedirectToAction("Login", "Account"); // chuyển hướng đến trang đăng nhập
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
