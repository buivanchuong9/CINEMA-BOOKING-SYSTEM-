using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BE.Models;
using BE.Core.Interfaces;
using BE.Core.Enums;

namespace BE.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        // Load phim cho trang chủ - CHỈ HIỂN THỊ ĐANG CHIẾU VÀ SẮP CHIẾU
        var allMovies = await _unitOfWork.Movies.GetAllAsync(); 
        
        var nowShowing = allMovies.Where(m => m.Status == MovieStatus.NowShowing) // lấy phim đang chiếu
                                  .OrderByDescending(m => m.Rating) // sắp xếp theo điểm giảm dần
                                  .Take(8) // lấy 8 phim
                                  .ToList();
        
        var comingSoon = allMovies.Where(m => m.Status == MovieStatus.ComingSoon) // lấy phim sắp chiếu
                                  .OrderBy(m => m.ReleaseDate) // sắp xếp theo ngày phát hành
                                  .Take(6)
                                  .ToList();

        ViewBag.NowShowing = nowShowing; // gán phim đang chiếu cho ViewBag
        ViewBag.ComingSoon = comingSoon; // gán phim sắp chiếu cho ViewBag
        ViewBag.FeaturedMovie = nowShowing.FirstOrDefault(); // gán phim nổi bật cho ViewBag

        return View();
    }

    public IActionResult Privacy() // trang quyền riêng tư
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] // tắt hoàn toàn caching cho action 
    public IActionResult Error() // trang lỗi
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); // trả về ErrorViewModel
    }
}