using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Data;
using BE.Core.Entities.Business;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BE.Application.Services;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AppearanceController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AppearanceController> _logger;
    private readonly ISiteSettingsService _siteSettingsService;

    public AppearanceController(AppDbContext context, IWebHostEnvironment env, ILogger<AppearanceController> logger, ISiteSettingsService siteSettingsService)
    {
        _context = context;
        _env = env;
        _logger = logger;
        _siteSettingsService = siteSettingsService;
    }

    // GET: Admin/Appearance
    public async Task<IActionResult> Index()
    {
        var settings = await GetOrCreateSettingsAsync();
        return View(settings);
    }

    // POST: Admin/Appearance/Save
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SiteSettings model, IFormFile? logoFile)
    {
        var settings = await GetOrCreateSettingsAsync();

        // Branding
        settings.SiteName = model.SiteName?.Trim() ?? "CineMax";
        settings.SiteSlogan = model.SiteSlogan?.Trim() ?? "";
        settings.LogoIcon = model.LogoIcon?.Trim() ?? "bi-camera-reels-fill";
        settings.ContactAddress = model.ContactAddress?.Trim() ?? "";
        settings.ContactPhone = model.ContactPhone?.Trim() ?? "";
        settings.ContactEmail = model.ContactEmail?.Trim() ?? "";

        // Colors
        settings.PrimaryColor = model.PrimaryColor ?? "#e11d48";
        settings.SecondaryColor = model.SecondaryColor ?? "#f59e0b";
        settings.BgPrimaryColor = model.BgPrimaryColor ?? "#0f172a";
        settings.BgSecondaryColor = model.BgSecondaryColor ?? "#1e293b";

        // Movie Display
        settings.HomeMoviesCount = Math.Clamp(model.HomeMoviesCount, 4, 24);
        settings.MovieDisplayMode = model.MovieDisplayMode ?? "Grid";
        settings.MovieGridColumns = model.MovieGridColumns is 2 or 3 or 4 or 6 ? model.MovieGridColumns : 4;
        settings.ShowComingSoon = model.ShowComingSoon;
        settings.ComingSoonCount = Math.Clamp(model.ComingSoonCount, 2, 12);
        settings.ShowMovieRating = model.ShowMovieRating;
        settings.ShowMovieGenre = model.ShowMovieGenre;
        settings.ShowMovieDuration = model.ShowMovieDuration;

        // Hero
        settings.HeroSliderHeight = Math.Clamp(model.HeroSliderHeight, 40, 100);
        settings.EnableHeroSlider = model.EnableHeroSlider;

        // Font
        settings.FontFamily = model.FontFamily?.Trim() ?? "Inter";

        // Logo upload
        if (logoFile != null && logoFile.Length > 0)
        {
            var ext = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
            if (allowed.Contains(ext))
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "logo");
                Directory.CreateDirectory(uploadsDir);
                var fileName = $"logo_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                settings.LogoUrl = $"/uploads/logo/{fileName}";
            }
        }

        settings.UpdatedAt = DateTime.Now;
        settings.UpdatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";

        _context.SiteSettings.Update(settings);
        await _context.SaveChangesAsync();
        _siteSettingsService.InvalidateCache();

        TempData["Success"] = "Đã lưu cài đặt giao diện thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Appearance/Reset
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset()
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            _context.SiteSettings.Remove(settings);
            await _context.SaveChangesAsync();
        }
        TempData["Success"] = "Đã khôi phục giao diện về mặc định!";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SiteSettings> GetOrCreateSettingsAsync()
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SiteSettings();
            _context.SiteSettings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return settings;
    }
}
