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
    public async Task<IActionResult> Index()
    {
        var showtimes = (await _unitOfWork.Showtimes.GetAllAsync())
            .Where(s => s.IsActive && s.StartTime >= DateTime.Now.AddHours(-1)) // Show showtimes that started recently too
            .OrderBy(s => s.StartTime)
            .ToList();

        foreach (var st in showtimes)
        {
            st.Movie = (await _unitOfWork.Movies.GetByIdAsync(st.MovieId))!;
            st.Room = (await _unitOfWork.Rooms.GetByIdAsync(st.RoomId))!;
            if (st.Room != null)
            {
                st.Room.Cinema = (await _unitOfWork.Cinemas.GetByIdAsync(st.Room.CinemaId))!;
            }
        }

        return View(showtimes);
    }

    // GET: /Staff/Booking/SelectSeats?showtimeId=1
    public async Task<IActionResult> SelectSeats(int showtimeId)
    {
        _logger.LogInformation($"Staff SelectSeats called with showtimeId: {showtimeId}");
        
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null) return RedirectToAction(nameof(Index));

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
        
        string userId;
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
            userId = guest.Id;
        }
        else
        {
            var user = await _userManager.FindByEmailAsync(customerEmail);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng với email này!";
                return RedirectToAction(nameof(SelectSeats), new { showtimeId = dto.ShowtimeId });
            }
            userId = user.Id;
        }

        dto.UserId = userId;
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
    public async Task<IActionResult> ManageBookings(int pageNumber = 1)
    {
        int pageSize = 20;
        var bookingsQuery = _context.Bookings
            .Include(b => b.User)
            .OrderByDescending(b => b.BookingDate);
            
        var paginatedBookings = await PaginatedList<Booking>.CreateAsync(bookingsQuery.AsNoTracking(), pageNumber, pageSize);
            
        // Load showtime and movie for each booking manually to avoid complex includes if needed
        foreach (var b in paginatedBookings)
        {
            if (b.ShowtimeId.HasValue)
            {
                var st = await _context.Showtimes.Include(s => s.Movie).FirstOrDefaultAsync(s => s.Id == b.ShowtimeId);
                b.Notes = st?.Movie?.Title ?? "Unknown Movie"; // Temporary reuse Notes for display if model doesn't have it
            }
        }
            
        return View(paginatedBookings);
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
}
