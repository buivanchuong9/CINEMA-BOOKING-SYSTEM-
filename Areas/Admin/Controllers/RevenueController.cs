using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BE.Data;
using BE.Core.Entities.Business;
using BE.Core.Entities.Bookings;
using BE.Core.Enums;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RevenueController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public RevenueController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Admin/Revenue
    public async Task<IActionResult> Index(int? cinemaId, string? startDate, string? endDate)
    {
        // 1. Fetch all cinemas for dropdown
        var cinemas = await _context.Cinemas.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Cinemas = cinemas;

        if (!cinemas.Any())
        {
            ViewBag.Message = "Chưa có rạp chiếu phim nào trong hệ thống.";
            return View();
        }

        // 2. Determine selected cinema
        int selectedCinemaId = cinemaId ?? 0;
        ViewBag.SelectedCinemaId = selectedCinemaId;
        ViewBag.CinemaName = selectedCinemaId == 0 ? "Tất cả các rạp" : cinemas.FirstOrDefault(c => c.Id == selectedCinemaId)?.Name;

        // 3. Parse date range (default to last 30 days)
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

        // 4. Fetch bookings in date range for this cinema
        var bookingsQuery = _context.Bookings
            .Include(b => b.User)
            .Include(b => b.BookingDetails)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s.Room)
                    .ThenInclude(r => r.Cinema)
            .Where(b => b.Status == BookingStatus.Paid
                        && b.Showtime != null
                        && b.Showtime.Room != null
                        && b.BookingDate >= start
                        && b.BookingDate <= end);

        if (selectedCinemaId > 0)
        {
            bookingsQuery = bookingsQuery.Where(b => b.Showtime.Room.CinemaId == selectedCinemaId);
        }

        var bookings = await bookingsQuery.ToListAsync();

        // 5. Get Booking Foods (Concessions) for food revenue
        var bookingIds = bookings.Select(b => b.Id).ToList();
        var bookingFoods = await _context.BookingFoods
            .Where(bf => bookingIds.Contains(bf.BookingId))
            .ToListAsync();

        // Map booking ID to its total food cost
        var foodRevenueByBooking = bookingFoods
            .GroupBy(bf => bf.BookingId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(bf => bf.Quantity * bf.UnitPrice)
            );

        // 6. Get all staff users
        var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
        var staffMap = staffUsers.ToDictionary(u => u.Id, u => u);

        // 7. Process sales per seller
        var staffSales = new Dictionary<string, StaffSalesViewModel>();

        // Pre-initialize staff assigned to this cinema so they appear even with 0 sales
        var cinemaStaff = selectedCinemaId == 0 
            ? staffUsers.ToList() 
            : staffUsers.Where(u => u.CinemaId == selectedCinemaId).ToList();

        foreach (var staff in cinemaStaff)
        {
            var cName = cinemas.FirstOrDefault(c => c.Id == staff.CinemaId)?.Name ?? "Chưa phân rạp";
            staffSales[staff.Id] = new StaffSalesViewModel
            {
                StaffId = staff.Id,
                FullName = staff.FullName ?? staff.UserName ?? "Chưa đặt tên",
                Email = staff.Email ?? "N/A",
                CinemaName = cName,
                TotalBookings = 0,
                TotalTickets = 0,
                TicketRevenue = 0,
                FoodRevenue = 0,
                TotalRevenue = 0
            };
        }

        // Attribute bookings to sellers
        decimal onlineRevenue = 0;
        int onlineTickets = 0;
        int onlineBookingsCount = 0;

        decimal totalCinemaRevenue = 0;
        int totalTicketsSold = 0;
        decimal totalFoodRevenue = 0;

        foreach (var b in bookings)
        {
            var foodCost = foodRevenueByBooking.ContainsKey(b.Id) ? foodRevenueByBooking[b.Id] : 0;
            var ticketCost = b.TotalAmount - foodCost;

            totalCinemaRevenue += b.TotalAmount;
            totalTicketsSold += b.BookingDetails.Count;
            totalFoodRevenue += foodCost;

            // Check if counter sale
            bool isCounterSale = b.PaymentMethod == PaymentMethod.Cash && b.TransactionId != null && b.TransactionId.StartsWith("CASH-");
            if (isCounterSale)
            {
                // Format: CASH-{staffId}-{timestamp}
                var parts = b.TransactionId!.Split('-');
                string? sellerId = parts.Length >= 2 ? parts[1] : null;

                if (!string.IsNullOrEmpty(sellerId))
                {
                    if (!staffSales.ContainsKey(sellerId))
                    {
                        // Staff member who is not currently assigned to this cinema, or system account
                        staffMap.TryGetValue(sellerId, out var externalStaff);
                        staffSales[sellerId] = new StaffSalesViewModel
                        {
                            StaffId = sellerId,
                            FullName = externalStaff?.FullName ?? externalStaff?.UserName ?? $"Nhân viên (ID: {sellerId.Take(6)}...)",
                            Email = externalStaff?.Email ?? "N/A",
                            CinemaName = cinemas.FirstOrDefault(c => c.Id == externalStaff?.CinemaId)?.Name ?? "Chưa phân rạp",
                            TotalBookings = 0,
                            TotalTickets = 0,
                            TicketRevenue = 0,
                            FoodRevenue = 0,
                            TotalRevenue = 0
                        };
                    }

                    var stats = staffSales[sellerId];
                    stats.TotalBookings++;
                    stats.TotalTickets += b.BookingDetails.Count;
                    stats.TicketRevenue += ticketCost;
                    stats.FoodRevenue += foodCost;
                    stats.TotalRevenue += b.TotalAmount;
                }
                else
                {
                    // Cash sale but no staff ID found
                    const string fallbackKey = "Unknown_Staff";
                    if (!staffSales.ContainsKey(fallbackKey))
                    {
                        staffSales[fallbackKey] = new StaffSalesViewModel
                        {
                            StaffId = fallbackKey,
                            FullName = "Nhân viên khác / Quầy",
                            Email = "N/A",
                            CinemaName = "N/A",
                            TotalBookings = 0,
                            TotalTickets = 0,
                            TicketRevenue = 0,
                            FoodRevenue = 0,
                            TotalRevenue = 0
                        };
                    }
                    var stats = staffSales[fallbackKey];
                    stats.TotalBookings++;
                    stats.TotalTickets += b.BookingDetails.Count;
                    stats.TicketRevenue += ticketCost;
                    stats.FoodRevenue += foodCost;
                    stats.TotalRevenue += b.TotalAmount;
                }
            }
            else
            {
                // Online sale
                onlineBookingsCount++;
                onlineTickets += b.BookingDetails.Count;
                onlineRevenue += b.TotalAmount;
            }
        }

        ViewBag.StaffSalesList = staffSales.Values.OrderByDescending(s => s.TotalRevenue).ToList();
        
        // General stats
        ViewBag.TotalCinemaRevenue = totalCinemaRevenue;
        ViewBag.TotalTicketsSold = totalTicketsSold;
        ViewBag.TotalFoodRevenue = totalFoodRevenue;
        ViewBag.TotalTicketRevenue = totalCinemaRevenue - totalFoodRevenue;
        
        ViewBag.OnlineBookingsCount = onlineBookingsCount;
        ViewBag.OnlineTickets = onlineTickets;
        ViewBag.OnlineRevenue = onlineRevenue;

        // 8. Prepare daily sales chart data for last 30 days
        var dailySales = new List<object>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var dayBookings = bookings.Where(b => b.BookingDate.Date == date).ToList();
            var dayRevenue = dayBookings.Sum(b => b.TotalAmount);
            dailySales.Add(new
            {
                DateStr = date.ToString("dd/MM"),
                Revenue = dayRevenue
            });
        }
        ViewBag.DailySalesData = dailySales;

        // 9. Prepare detailed list of transactions
        var detailedTransactions = bookings
            .OrderByDescending(b => b.BookingDate)
            .Take(50) // limit to recent 50
            .Select(b => {
                string sellerName = "Khách đặt Online";
                if (b.PaymentMethod == PaymentMethod.Cash && b.TransactionId != null && b.TransactionId.StartsWith("CASH-"))
                {
                    var parts = b.TransactionId.Split('-');
                    var sellerId = parts.Length >= 2 ? parts[1] : null;
                    if (sellerId != null && staffMap.TryGetValue(sellerId, out var sUser))
                    {
                        sellerName = sUser.FullName ?? sUser.UserName ?? "Nhân viên";
                    }
                    else
                    {
                        sellerName = "Nhân viên bán hàng";
                    }
                }
                return new TransactionDetailViewModel
                {
                    BookingId = b.Id,
                    BookingDate = b.BookingDate,
                    CustomerName = b.User?.FullName ?? "Khách vãng lai",
                    CustomerEmail = b.User?.Email ?? "guest@cinemax.com",
                    MovieTitle = b.Showtime?.Movie?.Title ?? "N/A",
                    RoomName = b.Showtime?.Room?.Name ?? "N/A",
                    CinemaName = b.Showtime?.Room?.Cinema?.Name ?? "N/A",
                    TicketsCount = b.BookingDetails.Count,
                    PaymentMethod = b.PaymentMethod?.ToString() ?? "N/A",
                    SellerName = sellerName,
                    TotalAmount = b.TotalAmount
                };
            })
            .ToList();

        ViewBag.DetailedTransactions = detailedTransactions;

        // 10. Pass staff list to view for management
        ViewBag.CinemaStaff = cinemaStaff;
        ViewBag.AllStaff = staffUsers.ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(string fullName, string email, string password, string phoneNumber, int cinemaId)
    {
        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            TempData["Error"] = "Vui lòng điền đầy đủ các thông tin bắt buộc (Họ tên, Email, Mật khẩu).";
            return RedirectToAction(nameof(Index), new { cinemaId });
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            TempData["Error"] = "Email này đã được sử dụng bởi một tài khoản khác.";
            return RedirectToAction(nameof(Index), new { cinemaId });
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            MembershipLevel = "Bronze",
            Points = 0,
            CinemaId = cinemaId,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Staff");
            TempData["Success"] = $"Đã cấp tài khoản Staff thành công cho {fullName}!";
        }
        else
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            TempData["Error"] = $"Lỗi khi tạo tài khoản: {errors}";
        }

        return RedirectToAction(nameof(Index), new { cinemaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReassignStaff(string staffId, int? cinemaId)
    {
        var user = await _userManager.FindByIdAsync(staffId);
        if (user == null)
        {
            TempData["Error"] = "Không tìm thấy tài khoản nhân viên.";
            return RedirectToAction(nameof(Index), new { cinemaId });
        }

        user.CinemaId = cinemaId;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            TempData["Success"] = $"Đã cập nhật phân rạp thành công cho nhân viên {user.FullName}!";
        }
        else
        {
            TempData["Error"] = "Lỗi khi cập nhật rạp phân công.";
        }

        return RedirectToAction(nameof(Index), new { cinemaId = cinemaId ?? user.CinemaId });
    }
}

public class StaffSalesViewModel
{
    public string StaffId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int TotalTickets { get; set; }
    public decimal TicketRevenue { get; set; }
    public decimal FoodRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TransactionDetailViewModel
{
    public int BookingId { get; set; }
    public DateTime BookingDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public int TicketsCount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
