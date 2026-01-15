using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MoviesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public MoviesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Movies
    public async Task<IActionResult> Index()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync();
        return View(movies.OrderByDescending(m => m.CreatedAt).ToList());
    }

    // GET: /Admin/Movies/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Admin/Movies/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Movie movie)
    {
        try
        {
            // Remove optional fields from validation
            ModelState.Remove("PosterUrl");
            ModelState.Remove("TrailerUrl");
            ModelState.Remove("Description");
            ModelState.Remove("AgeRating");
            ModelState.Remove("Rating");
            ModelState.Remove("Director");
            ModelState.Remove("Cast");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("IsActive");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                return View(movie);
            }

            movie.CreatedAt = DateTime.Now;
            await _unitOfWork.Movies.AddAsync(movie);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm phim '{movie.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return View(movie);
        }
    }

    // GET: /Admin/Movies/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        if (movie == null)
        {
            return NotFound();
        }
        return View(movie);
    }

    // POST: /Admin/Movies/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Movie movie)
    {
        if (id != movie.Id)
        {
            TempData["Error"] = "ID không khớp!";
            return NotFound();
        }

        try
        {
            // Remove optional fields
            ModelState.Remove("PosterUrl");
            ModelState.Remove("TrailerUrl");
            ModelState.Remove("Description");
            ModelState.Remove("AgeRating");
            ModelState.Remove("Rating");
            ModelState.Remove("Director");
            ModelState.Remove("Cast");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                return View(movie);
            }

            // Update movie
            _unitOfWork.Movies.Update(movie);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật phim '{movie.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi cập nhật: {ex.Message}";
            return View(movie);
        }
    }

    // POST: /Admin/Movies/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        _unitOfWork.Movies.Delete(movie);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa phim '{movie.Title}' thành công!";
        return RedirectToAction(nameof(Index));
    }
}
