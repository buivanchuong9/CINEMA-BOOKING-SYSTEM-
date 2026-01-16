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
        // Load movies for homepage - CHỈ HIỂN THỊ ĐANG CHIẾU VÀ SẮP CHIẾU
        var allMovies = await _unitOfWork.Movies.GetAllAsync();
        
        var nowShowing = allMovies.Where(m => m.Status == MovieStatus.NowShowing)
                                  .OrderByDescending(m => m.Rating)
                                  .Take(8)
                                  .ToList();
        
        var comingSoon = allMovies.Where(m => m.Status == MovieStatus.ComingSoon)
                                  .OrderBy(m => m.ReleaseDate)
                                  .Take(6)
                                  .ToList();

        ViewBag.NowShowing = nowShowing;
        ViewBag.ComingSoon = comingSoon;
        ViewBag.FeaturedMovie = nowShowing.FirstOrDefault();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}