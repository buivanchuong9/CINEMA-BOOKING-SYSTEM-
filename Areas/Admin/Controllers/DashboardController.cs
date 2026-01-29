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
        try
        {
            // Lấy thống kê cơ bản cho cards
            var totalMovies = await _unitOfWork.Movies.CountAsync();
            var totalCinemas = await _unitOfWork.Cinemas.CountAsync();
            var totalRooms = await _unitOfWork.Rooms.CountAsync();
            
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            var totalRevenue = allBookings
                .Where(b => b.Status == Core.Enums.BookingStatus.Paid)
                .Sum(b => b.TotalAmount);
            
            ViewBag.TotalMovies = totalMovies;
            ViewBag.TotalCinemas = totalCinemas;
            ViewBag.TotalRooms = totalRooms;
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dashboard Error: {ex.Message}");
            // Trả về view với dữ liệu mặc định
            ViewBag.TotalMovies = 0;
            ViewBag.TotalCinemas = 0;
            ViewBag.TotalRooms = 0;
            ViewBag.TotalRevenue = 0;
            return View();
        }
    }
}
