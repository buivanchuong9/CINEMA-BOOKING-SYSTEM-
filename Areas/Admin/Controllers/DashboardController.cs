using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Dashboard
    public async Task<IActionResult> Index()
    {
        // Lấy thống kê cơ bản
        var totalMovies = await _unitOfWork.Movies.CountAsync();
        var totalCinemas = await _unitOfWork.Cinemas.CountAsync();
        var totalBookings = await _unitOfWork.Bookings.CountAsync();
        var totalUsers = await _unitOfWork.Users.CountAsync();

        ViewBag.TotalMovies = totalMovies;
        ViewBag.TotalCinemas = totalCinemas;
        ViewBag.TotalBookings = totalBookings;
        ViewBag.TotalUsers = totalUsers;

        // Thống kê doanh thu
        var allBookings = await _unitOfWork.Bookings.GetAllAsync();
        var totalRevenue = allBookings.Where(b => b.Status == Core.Enums.BookingStatus.Paid)
            .Sum(b => b.TotalAmount);
        ViewBag.TotalRevenue = totalRevenue;

        // Top 5 phim có nhiều booking nhất
        var bookingDetails = await _unitOfWork.BookingDetails.GetAllAsync();
        var topMovies = (await _unitOfWork.Movies.GetAllAsync())
            .Select(m => new
            {
                Movie = m,
                Count = bookingDetails.Count(bd => 
                    _unitOfWork.Seats.GetByIdAsync(bd.SeatId).Result?.RoomId == 
                    _unitOfWork.Showtimes.GetAllAsync().Result.FirstOrDefault(st => st.MovieId == m.Id)?.RoomId
                )
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Select(x => new { x.Movie.Title, x.Count })
            .ToList();
        ViewBag.TopMovies = topMovies;

        // Booking status distribution
        var bookingByStatus = allBookings.GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToList();
        ViewBag.BookingByStatus = bookingByStatus;

        // Revenue last 7 days
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-6 + i))
            .Select(date => new
            {
                Date = date.ToString("dd/MM"),
                Revenue = allBookings.Where(b => 
                    b.BookingDate.Date == date && 
                    b.Status == Core.Enums.BookingStatus.Paid
                ).Sum(b => b.TotalAmount)
            })
            .ToList();
        ViewBag.RevenueChart = last7Days;

        return View();
    }
}
