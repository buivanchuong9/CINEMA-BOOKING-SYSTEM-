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
            // Only validate required fields
            if (string.IsNullOrWhiteSpace(movie.Title) || movie.Duration <= 0)
            {
                TempData["Error"] = "Tên phim và thời lượng là bắt buộc!";
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
            // Manual validation for required fields only
            if (string.IsNullOrWhiteSpace(movie.Title))
            {
                TempData["Error"] = "Tên phim là bắt buộc!";
                return View(movie);
            }

            if (movie.Duration <= 0)
            {
                TempData["Error"] = "Thời lượng phải lớn hơn 0!";
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
