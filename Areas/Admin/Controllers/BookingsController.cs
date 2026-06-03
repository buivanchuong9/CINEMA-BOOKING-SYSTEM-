using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Data;
using BE.Core.Entities.Bookings;
using BE.Core.Enums;
using BE.Core.Interfaces.Services;
using BE.Application.Helpers;
using System.Security.Claims;
using BE.Core.Interfaces;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BookingsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IBookingService _bookingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingsController> _logger;
    private readonly IWebHostEnvironment _env;

    public BookingsController(
        AppDbContext context, 
        IBookingService bookingService,
        IUnitOfWork unitOfWork,
        ILogger<BookingsController> logger,
        IWebHostEnvironment env)
    {
        _context = context;
        _bookingService = bookingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _env = env;
    }

    // GET: Admin/Bookings
    public async Task<IActionResult> Index(
        int? cinemaId, 
        int? roomId, 
        int? movieId, 
        BookingStatus? status, 
        string? search, 
        int pageNumber = 1)
    {
        _logger.LogInformation("Admin Bookings Index called with filters");
        
        int pageSize = 20;

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

        // Fetch lookup lists for search filters
        ViewBag.Cinemas = await _context.Cinemas.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
        ViewBag.Movies = await _context.Movies.OrderBy(m => m.Title).ToListAsync();
        ViewBag.Statuses = Enum.GetValues(typeof(BookingStatus)).Cast<BookingStatus>().ToList();

        // Store active filter values to repopulate search forms
        ViewBag.CinemaId = cinemaId;
        ViewBag.RoomId = roomId;
        ViewBag.MovieId = movieId;
        ViewBag.Status = status;
        ViewBag.Search = search;

        return View(paginatedBookings);
    }

    // POST: Admin/Bookings/ConfirmPayment/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        _logger.LogInformation($"Admin confirming payment for booking: {id}");
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin";
        var success = await _bookingService.ConfirmCounterPaymentAsync(id, adminId);
        
        if (success)
        {
            TempData["Success"] = "Đã xác nhận thanh toán thành công cho đơn vé!";
        }
        else
        {
            TempData["Error"] = "Xác nhận thanh toán thất bại!";
        }

        return RedirectToAction(nameof(Index), new {
            cinemaId = Request.Form["cinemaId"],
            roomId = Request.Form["roomId"],
            movieId = Request.Form["movieId"],
            status = Request.Form["status"],
            search = Request.Form["search"],
            pageNumber = Request.Form["pageNumber"]
        });
    }

    // POST: Admin/Bookings/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        _logger.LogInformation($"Admin cancelling booking: {id}");
        var success = await _bookingService.CancelBookingAsync(id);
        
        if (success)
        {
            TempData["Success"] = "Đã hủy đơn hàng và giải phóng ghế thành công!";
        }
        else
        {
            TempData["Error"] = "Hủy đơn hàng thất bại!";
        }

        return RedirectToAction(nameof(Index), new {
            cinemaId = Request.Form["cinemaId"],
            roomId = Request.Form["roomId"],
            movieId = Request.Form["movieId"],
            status = Request.Form["status"],
            search = Request.Form["search"],
            pageNumber = Request.Form["pageNumber"]
        });
    }

    // GET: Admin/Bookings/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Voucher)
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

        var foods = await _context.BookingFoods
            .Include(bf => bf.Food)
            .Where(bf => bf.BookingId == id)
            .ToListAsync();

        ViewBag.Showtime = showtime;
        ViewBag.Seats = string.Join(", ", seatDetails);
        ViewBag.FoodsList = foods;

        return View(booking);
    }

    // GET: Admin/Bookings/RefundRequests
    public async Task<IActionResult> RefundRequests(string? refundStatus, string? search, int pageNumber = 1)
    {
        int pageSize = 20;

        var query = _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Room)
                    .ThenInclude(r => r!.Cinema)
            .Where(b => b.Status == BookingStatus.Cancelled && b.RefundStatus != "None")
            .AsQueryable();

        if (!string.IsNullOrEmpty(refundStatus))
            query = query.Where(b => b.RefundStatus == refundStatus);

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(b =>
                b.Id.ToString() == s ||
                (b.User != null && b.User.FullName != null && b.User.FullName.ToLower().Contains(s)) ||
                (b.User != null && b.User.Email != null && b.User.Email.ToLower().Contains(s)) ||
                (b.RefundAccountNumber != null && b.RefundAccountNumber.Contains(s)) ||
                (b.RefundAccountName  != null && b.RefundAccountName.ToLower().Contains(s))
            );
        }

        query = query.OrderByDescending(b => b.UpdatedAt ?? b.BookingDate);

        var paginated = await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);

        ViewBag.RefundStatus = refundStatus;
        ViewBag.Search = search;
        ViewBag.PendingCount  = await _context.Bookings.CountAsync(b => b.RefundStatus == "Pending");
        ViewBag.RefundedCount = await _context.Bookings.CountAsync(b => b.RefundStatus == "Refunded");
        ViewBag.RejectedCount = await _context.Bookings.CountAsync(b => b.RefundStatus == "Rejected");

        return View(paginated);
    }

    // GET: Admin/Bookings/ProcessRefund/5
    public async Task<IActionResult> ProcessRefund(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound();
        return View(booking);
    }

    // POST: Admin/Bookings/ProcessRefund
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessRefund(int id, string action, IFormFile? proofImage)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        if (action == "Refunded")
        {
            if (proofImage == null || proofImage.Length == 0)
            {
                TempData["Error"] = "Vui lòng upload ảnh bằng chứng chuyển khoản!";
                return RedirectToAction(nameof(ProcessRefund), new { id });
            }

            // Save image to wwwroot/uploads/refunds/
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "refunds");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(proofImage.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Chỉ chấp nhận file ảnh (jpg, png, webp, gif)!";
                return RedirectToAction(nameof(ProcessRefund), new { id });
            }

            var fileName = $"refund_{id}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await proofImage.CopyToAsync(stream);
            }

            booking.RefundProofUrl = $"/uploads/refunds/{fileName}";
            booking.RefundStatus = "Refunded";
            booking.UpdatedAt = DateTime.Now;

            TempData["Success"] = $"Đã xác nhận hoàn tiền cho đơn #{id.ToString("D6")}!";
        }
        else if (action == "Rejected")
        {
            booking.RefundStatus = "Rejected";
            booking.UpdatedAt = DateTime.Now;
            TempData["Success"] = $"Đã từ chối yêu cầu hoàn tiền cho đơn #{id.ToString("D6")}!";
        }
        else
        {
            TempData["Error"] = "Hành động không hợp lệ!";
            return RedirectToAction(nameof(ProcessRefund), new { id });
        }

        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(RefundRequests));
    }
}
