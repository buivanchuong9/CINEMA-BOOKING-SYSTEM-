using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class GenresController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public GenresController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Genres
    public async Task<IActionResult> Index()
    {
        var genres = await _unitOfWork.Genres.GetAllAsync();
        return View(genres.OrderBy(g => g.Name).ToList());
    }

    // GET: /Admin/Genres/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Admin/Genres/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Genre genre)
    {
        ModelState.Remove("Slug");
        ModelState.Remove("CreatedAt");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
            return View(genre);
        }

        // Auto-generate slug from name
        if (string.IsNullOrEmpty(genre.Slug))
        {
            genre.Slug = GenerateSlug(genre.Name);
        }

        genre.CreatedAt = DateTime.Now;
        await _unitOfWork.Genres.AddAsync(genre);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm thể loại '{genre.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Genres/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var genre = await _unitOfWork.Genres.GetByIdAsync(id);
        if (genre == null)
        {
            return NotFound();
        }
        return View(genre);
    }

    // POST: /Admin/Genres/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Genre genre)
    {
        if (id != genre.Id)
        {
            return NotFound();
        }

        ModelState.Remove("Slug");
        ModelState.Remove("CreatedAt");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
            return View(genre);
        }

        if (string.IsNullOrEmpty(genre.Slug))
        {
            genre.Slug = GenerateSlug(genre.Name);
        }

        _unitOfWork.Genres.Update(genre);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật thể loại '{genre.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Genres/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await _unitOfWork.Genres.GetByIdAsync(id);
        if (genre == null)
        {
            return NotFound();
        }

        _unitOfWork.Genres.Delete(genre);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa thể loại '{genre.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    private string GenerateSlug(string name)
    {
        // Simple slug generation (Vietnamese to Latin)
        var slug = name.ToLower()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Replace("ă", "a")
            .Replace("â", "a")
            .Replace("ê", "e")
            .Replace("ô", "o")
            .Replace("ơ", "o")
            .Replace("ư", "u");
        
        // Remove accents and special characters
        return System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
    }
}
