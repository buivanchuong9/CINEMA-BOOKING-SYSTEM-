using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Interfaces.Services;
using BE.Application.DTOs;
using System.Security.Claims;
using BE.Core.Enums;
using BE.Data;
using Microsoft.EntityFrameworkCore;
using BE.Core.Entities.Business;
using Microsoft.AspNetCore.Identity;
using BE.Application.Helpers;
using BE.Core.Entities.Bookings;
using BE.Core.Entities.Movies;

namespace BE.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Roles = "Staff,Admin")]
public class BookingController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBookingService _bookingService;
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IUnitOfWork unitOfWork, 
        IBookingService bookingService, 
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<BookingController> logger)
    {
        _unitOfWork = unitOfWork;
        _bookingService = bookingService;
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Staff/Booking
    public async Task<IActionResult> Index(int? cinemaId, int? movieId, string? date, int pageNumber = 1)
    {
        int pageSize = 20;

        // Check if the current user has a cinema assigned
        var userId = _userManager.GetUserId(User);
        var currentUser = await _userManager.FindByIdAsync(userId ?? "");
        var assignedCinemaId = currentUser?.CinemaId;
        
        if (assignedCinemaId.HasValue)
        {
            cinemaId = assignedCinemaId.Value;
            ViewBag.AssignedCinemaId = assignedCinemaId.Value;
        }

        var query = _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
            .Where(s => s.IsActive && s.StartTime >= DateTime.Now.AddHours(-1))
            .AsQueryable();

        // Apply filters
        if (cinemaId.HasValue && cinemaId.Value > 0)
        {
            query = query.Where(s => s.Room != null && s.Room.CinemaId == cinemaId.Value);
        }

        if (movieId.HasValue && movieId.Value > 0)
        {
            query = query.Where(s => s.MovieId == movieId.Value);
        }

        if (!string.IsNullOrEmpty(date))
        {
            if (DateTime.TryParse(date, out var selectedDate))
            {
                var startDate = selectedDate.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(s => s.StartTime >= startDate && s.StartTime < endDate);
            }
        }

        query = query.OrderBy(s => s.StartTime);

        var paginatedShowtimes = await PaginatedList<Showtime>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);

        // Fetch lookup lists for filters
        ViewBag.Cinemas = await _context.Cinemas.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Movies = await _context.Movies.OrderBy(m => m.Title).ToListAsync();
        
        // Active filter values
        ViewBag.CinemaId = cinemaId;
        ViewBag.MovieId = movieId;
        ViewBag.Date = date;

        return View(paginatedShowtimes);
    }

    // GET: /Staff/Booking/SelectSeats?showtimeId=1
    public async Task<IActionResult> SelectSeats(int showtimeId)
    {
        _logger.LogInformation($"Staff SelectSeats called with showtimeId: {showtimeId}");
        
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null) return RedirectToAction(nameof(Index));

        // Check if user has permission for this showtime
        var userId = _userManager.GetUserId(User);
        var currentUser = await _userManager.FindByIdAsync(userId ?? "");
        if (currentUser?.CinemaId != null)
        {
            var roomDetail = await _context.Rooms.FindAsync(showtime.RoomId);
            if (roomDetail == null || roomDetail.CinemaId != currentUser.CinemaId)
            {
                TempData["Error"] = "Bạn không có quyền truy cập suất chiếu của rạp chiếu phim này!";
                return RedirectToAction(nameof(Index));
            }
        }

        var movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
        var room = await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId);
        if (room != null)
        {
            room.Cinema = (await _unitOfWork.Cinemas.GetByIdAsync(room.CinemaId))!;
        }
        
        var seats = (await _unitOfWork.Seats.GetAllAsync())
            .Where(s => s.RoomId == showtime.RoomId)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.Number)
            .ToList();
        
        foreach (var seat in seats)
        {
            seat.SeatType = (await _unitOfWork.SeatTypes.GetByIdAsync(seat.SeatTypeId))!;
        }
        
        var seatStatus = await _bookingService.GetSeatStatusAsync(showtimeId);
        var foods = (await _unitOfWork.Foods.GetAllAsync())
            .Where(f => f.IsAvailable)
            .OrderBy(f => f.DisplayOrder)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingDto dto, string customerEmail)
    {
        _logger.LogInformation($"Staff Create booking for customer: {customerEmail}");

        // Check if user has permission for this showtime
        var userId = _userManager.GetUserId(User);
        var currentUser = await _userManager.FindByIdAsync(userId ?? "");
        if (currentUser?.CinemaId != null)
        {
            var showtime = await _context.Showtimes.Include(s => s.Room).FirstOrDefaultAsync(s => s.Id == dto.ShowtimeId);
            if (showtime == null || showtime.Room?.CinemaId != currentUser.CinemaId)
            {
                TempData["Error"] = "Bạn không có quyền đặt vé cho rạp chiếu phim này!";
                return RedirectToAction(nameof(Index));
            }
        }
        
        string targetUserId;
        if (string.IsNullOrEmpty(customerEmail))
        {
            // Default to a guest user or the staff themselves
            var guest = await _userManager.FindByEmailAsync("guest@cinemax.com");
            if (guest == null)
            {
                guest = new User { UserName = "guest@cinemax.com", Email = "guest@cinemax.com", FullName = "Khách vãng lai" };
                await _userManager.CreateAsync(guest, "Guest@123");
                await _userManager.AddToRoleAsync(guest, "Customer");
            }
            targetUserId = guest.Id;
        }
        else
        {
            var user = await _userManager.FindByEmailAsync(customerEmail);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng với email này!";
                return RedirectToAction(nameof(SelectSeats), new { showtimeId = dto.ShowtimeId });
            }
            targetUserId = user.Id;
        }

        dto.UserId = targetUserId;
        dto.PaymentMethod = PaymentMethod.Cash; // Staff bookings are usually cash
        
        var result = await _bookingService.CreateBookingAsync(dto);
        if (result.Success && result.BookingId.HasValue)
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _bookingService.ConfirmCounterPaymentAsync(result.BookingId.Value, staffId ?? "System");
            
            TempData["Success"] = "Đặt vé và thanh toán thành công!";
            return RedirectToAction(nameof(PrintTicket), new { id = result.BookingId.Value });
        }

        TempData["Error"] = result.Message;
        return RedirectToAction(nameof(SelectSeats), new { showtimeId = dto.ShowtimeId });
    }

    // GET: /Staff/Booking/ManageBookings
    public async Task<IActionResult> ManageBookings(
        int? cinemaId, 
        int? roomId, 
        int? movieId, 
        BookingStatus? status, 
        string? search, 
        int pageNumber = 1)
    {
        int pageSize = 20;

        // Check if the current user has a cinema assigned
        var userId = _userManager.GetUserId(User);
        var currentUser = await _userManager.FindByIdAsync(userId ?? "");
        var assignedCinemaId = currentUser?.CinemaId;
        
        if (assignedCinemaId.HasValue)
        {
            cinemaId = assignedCinemaId.Value;
            ViewBag.AssignedCinemaId = assignedCinemaId.Value;
        }

        var query = _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Room)
                    .ThenInclude(r => r.Cinema)
            .AsQueryable();

        // Apply filters
        if (cinemaId.HasValue && cinemaId.Value > 0)
        {
            query = query.Where(b => b.Showtime != null && b.Showtime.Room != null && b.Showtime.Room.CinemaId == cinemaId.Value);
        }

        if (roomId.HasValue && roomId.Value > 0)
        {
            query = query.Where(b => b.Showtime != null && b.Showtime.RoomId == roomId.Value);
        }

        if (movieId.HasValue && movieId.Value > 0)
        {
            query = query.Where(b => b.Showtime != null && b.Showtime.MovieId == movieId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(b => 
                b.Id.ToString() == search || 
                (b.TransactionId != null && b.TransactionId.ToLower().Contains(search)) ||
                (b.User != null && b.User.FullName != null && b.User.FullName.ToLower().Contains(search)) ||
                (b.User != null && b.User.Email != null && b.User.Email.ToLower().Contains(search)) ||
                (b.User != null && b.User.PhoneNumber != null && b.User.PhoneNumber.Contains(search))
            );
        }

        query = query.OrderByDescending(b => b.BookingDate);

        var paginatedBookings = await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);

        // Load movie titles explicitly (Notes field reuse is kept for backward compatibility just in case)
        foreach (var b in paginatedBookings)
        {
            if (b.Showtime?.Movie != null)
            {
                b.Notes = b.Showtime.Movie.Title;
            }
        }

        // Fetch lookup lists for search filters
        ViewBag.Cinemas = await _context.Cinemas.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
        ViewBag.Movies = await _context.Movies.OrderBy(m => m.Title).ToListAsync();
        ViewBag.Statuses = Enum.GetValues(typeof(BookingStatus)).Cast<BookingStatus>().ToList();

        // Store active filter values
        ViewBag.CinemaId = cinemaId;
        ViewBag.RoomId = roomId;
        ViewBag.MovieId = movieId;
        ViewBag.Status = status;
        ViewBag.Search = search;

        return View(paginatedBookings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCashPayment(int id)
    {
        var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Staff";
        var success = await _bookingService.ConfirmCounterPaymentAsync(id, staffId);
        
        if (success)
        {
            TempData["Success"] = "Đã xác nhận thanh toán tiền mặt thành công!";
        }
        else
        {
            TempData["Error"] = "Xác nhận thanh toán tiền mặt thất bại!";
        }

        return RedirectToAction(nameof(ManageBookings), new {
            cinemaId = Request.Form["cinemaId"],
            roomId = Request.Form["roomId"],
            movieId = Request.Form["movieId"],
            status = Request.Form["status"],
            search = Request.Form["search"],
            pageNumber = Request.Form["pageNumber"]
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        _logger.LogInformation($"Staff cancelling booking: {id}");
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        if (booking.Status == BookingStatus.Paid)
        {
            booking.RefundStatus = "Pending";
            booking.UpdatedAt = DateTime.Now;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã chuyển đơn vé sang trạng thái chờ hoàn tiền!";
        }
        else
        {
            var success = await _bookingService.CancelBookingAsync(id);
            if (success)
            {
                TempData["Success"] = "Đã hủy đơn đặt vé tại quầy và giải phóng ghế thành công!";
            }
            else
            {
                TempData["Error"] = "Hủy đơn đặt vé thất bại!";
            }
        }

        return RedirectToAction(nameof(ManageBookings), new {
            cinemaId = Request.Form["cinemaId"],
            roomId = Request.Form["roomId"],
            movieId = Request.Form["movieId"],
            status = Request.Form["status"],
            search = Request.Form["search"],
            pageNumber = Request.Form["pageNumber"]
        });
    }

    // GET: /Staff/Booking/PrintTicket/5
    public async Task<IActionResult> PrintTicket(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound();

        var showtime = await _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.Id == booking.ShowtimeId);

        var seatDetails = new List<string>();
        foreach (var detail in booking.BookingDetails)
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(detail.SeatId);
            if (seat != null) seatDetails.Add($"{seat.Row}{seat.Number}");
        }

        var foods = (await _unitOfWork.BookingFoods.GetAllAsync())
            .Where(bf => bf.BookingId == id)
            .ToList();
        
        var foodDetails = new List<string>();
        foreach (var f in foods)
        {
            var food = await _unitOfWork.Foods.GetByIdAsync(f.FoodId);
            if (food != null) foodDetails.Add($"{food.Name} x{f.Quantity}");
        }

        ViewBag.Showtime = showtime;
        ViewBag.Seats = string.Join(", ", seatDetails);
        ViewBag.Foods = string.Join(", ", foodDetails);

        return View(booking);
    }

    // GET: /Staff/Booking/SalesDashboard
    public async Task<IActionResult> SalesDashboard(string? startDate, string? endDate)
    {
        var userId = _userManager.GetUserId(User);
        var currentUser = await _userManager.FindByIdAsync(userId ?? "");
        var assignedCinemaId = currentUser?.CinemaId;

        if (!assignedCinemaId.HasValue)
        {
            TempData["Error"] = "Tài khoản của bạn chưa được phân công quản lý rạp nào!";
            return RedirectToAction(nameof(Index));
        }

        var cinema = await _context.Cinemas.FindAsync(assignedCinemaId.Value);
        if (cinema == null)
        {
            TempData["Error"] = "Không tìm thấy rạp được phân công!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.CinemaName = cinema.Name;

        // Parse dates
        DateTime start = DateTime.Today.AddDays(-30);
        DateTime end = DateTime.Today.AddDays(1).AddSeconds(-1);

        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStart))
        {
            start = parsedStart.Date;
        }
        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
        {
            end = parsedEnd.Date.AddDays(1).AddSeconds(-1);
        }

        ViewBag.StartDate = start.ToString("yyyy-MM-dd");
        ViewBag.EndDate = end.ToString("yyyy-MM-dd");

        // Query bookings for this cinema in date range
        var bookings = await _context.Bookings
            .Include(b => b.BookingDetails)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
            .Where(b => b.Status == BookingStatus.Paid
                        && b.Showtime != null 
                        && b.Showtime.Room != null 
                        && b.Showtime.Room.CinemaId == assignedCinemaId.Value
                        && b.BookingDate >= start 
                        && b.BookingDate <= end)
            .ToListAsync();

        // Calculate general statistics
        var totalBookings = bookings.Count;
        var totalTickets = bookings.Sum(b => b.BookingDetails.Count);
        var totalRevenue = bookings.Sum(b => b.TotalAmount); // Combined total amount

        // Concessions / Food revenue
        var bookingIds = bookings.Select(b => b.Id).ToList();
        var bookingFoods = await _context.BookingFoods
            .Where(bf => bookingIds.Contains(bf.BookingId))
            .ToListAsync();
        
        var totalFoodRevenue = bookingFoods.Sum(bf => bf.Quantity * bf.UnitPrice);
        var totalTicketRevenue = totalRevenue - totalFoodRevenue;

        // Movie performance stats
        var movieStats = bookings
            .Where(b => b.Showtime != null && b.Showtime.Movie != null)
            .GroupBy(b => new { b.Showtime.MovieId, b.Showtime.Movie.Title, b.Showtime.Movie.PosterUrl })
            .Select(g => new MovieSalesStatViewModel
            {
                MovieId = g.Key.MovieId,
                Title = g.Key.Title,
                PosterUrl = g.Key.PosterUrl,
                TicketsSold = g.Sum(b => b.BookingDetails.Count),
                Revenue = g.Sum(b => b.TotalAmount)
            })
            .OrderByDescending(m => m.Revenue)
            .ToList();

        // Calculate sales ratios
        if (totalTickets > 0)
        {
            foreach (var stat in movieStats)
            {
                stat.SalesRatio = (double)stat.TicketsSold / totalTickets * 100;
            }
        }

        // Recent sales table
        var recentSales = bookings
            .OrderByDescending(b => b.BookingDate)
            .Take(10)
            .ToList();

        // View models / View Bags
        ViewBag.TotalBookings = totalBookings;
        ViewBag.TotalTickets = totalTickets;
        ViewBag.TotalTicketRevenue = totalTicketRevenue;
        ViewBag.TotalFoodRevenue = totalFoodRevenue;
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.MovieStats = movieStats;
        ViewBag.RecentSales = recentSales;

        return View();
    }
}

public class MovieSalesStatViewModel
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
    public double SalesRatio { get; set; }
}
