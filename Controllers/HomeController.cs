using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BE.Models;
using BE.Core.Interfaces;
using BE.Core.Enums;
using BE.Application.Services;

namespace BE.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISiteSettingsService _siteSettingsService;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, ISiteSettingsService siteSettingsService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _siteSettingsService = siteSettingsService;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _siteSettingsService.GetSettingsAsync();
        var allMovies = await _unitOfWork.Movies.GetAllAsync();

        var nowShowing = allMovies
            .Where(m => m.Status == MovieStatus.NowShowing && m.IsActive)
            .OrderByDescending(m => m.Rating)
            .Take(settings.HomeMoviesCount)
            .ToList();

        var comingSoon = allMovies
            .Where(m => m.Status == MovieStatus.ComingSoon && m.IsActive)
            .OrderBy(m => m.ReleaseDate)
            .Take(settings.ComingSoonCount)
            .ToList();

        ViewBag.NowShowing = nowShowing;
        ViewBag.ComingSoon = settings.ShowComingSoon ? comingSoon : new List<BE.Core.Entities.Movies.Movie>();
        ViewBag.FeaturedMovie = nowShowing.FirstOrDefault();
        ViewBag.Settings = settings;

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