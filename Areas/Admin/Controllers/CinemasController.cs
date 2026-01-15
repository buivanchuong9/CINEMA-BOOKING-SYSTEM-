using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.CinemaInfrastructure;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CinemasController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CinemasController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Cinemas
    public async Task<IActionResult> Index()
    {
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        return View(cinemas.OrderBy(c => c.Name).ToList());
    }

    // GET: /Admin/Cinemas/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Admin/Cinemas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cinema cinema)
    {
        try
        {
            // Only validate Name and Address (required fields)
            if (string.IsNullOrWhiteSpace(cinema.Name) || string.IsNullOrWhiteSpace(cinema.Address))
            {
                TempData["Error"] = "Tên rạp và địa chỉ là bắt buộc!";
                return View(cinema);
            }

            cinema.CreatedAt = DateTime.Now;
            cinema.IsActive = true;
            
            await _unitOfWork.Cinemas.AddAsync(cinema);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm rạp '{cinema.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return View(cinema);
        }
    }

    // GET: /Admin/Cinemas/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var cinema = await _unitOfWork.Cinemas.GetByIdAsync(id);
        if (cinema == null)
        {
            return NotFound();
        }
        return View(cinema);
    }

    // POST: /Admin/Cinemas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Cinema cinema)
    {
        if (id != cinema.Id)
        {
            return NotFound();
        }

        try
        {
            ModelState.Remove("MapEmbedUrl");
            ModelState.Remove("Phone");

            if (!ModelState.IsValid)
            {
                return View(cinema);
            }

            _unitOfWork.Cinemas.Update(cinema);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật rạp '{cinema.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return View(cinema);
        }
    }

    // POST: /Admin/Cinemas/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cinema = await _unitOfWork.Cinemas.GetByIdAsync(id);
        if (cinema == null)
        {
            return NotFound();
        }

        _unitOfWork.Cinemas.Delete(cinema);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa rạp '{cinema.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }
}
