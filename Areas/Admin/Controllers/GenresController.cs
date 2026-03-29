using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class GenresController : Controller // danh sách thể loại
{
    private readonly IUnitOfWork _unitOfWork;

    public GenresController(IUnitOfWork unitOfWork) // DI
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Genres
    public async Task<IActionResult> Index() // danh sách thể loại
    { 
        var genres = await _unitOfWork.Genres.GetAllAsync();
        return View(genres.OrderBy(g => g.Name).ToList()); // sắp xếp theo tên thể loại
    }

    // GET: /Admin/Genres/Create
    public IActionResult Create() // tạo thể loại
    {
        return View();
    }

    // POST: /Admin/Genres/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Genre genre) 
    {
        ModelState.Remove("Slug"); // xóa slug
        ModelState.Remove("CreatedAt"); // xóa ngày tạo

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
            return View(genre);
        }

        // tạo slug từ tên
        if (string.IsNullOrEmpty(genre.Slug)) // nếu slug rỗng thì tạo slug từ tên
        {
            genre.Slug = GenerateSlug(genre.Name);
        }

        genre.CreatedAt = DateTime.Now; // gán ngày tạo
        await _unitOfWork.Genres.AddAsync(genre);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm thể loại '{genre.Name}' thành công!";
        return RedirectToAction(nameof(Index)); // chuyển sang trang danh sách thể loại
    }

    // GET: /Admin/Genres/Edit/5
    public async Task<IActionResult> Edit(int id) // chỉnh sửa thể loại
    {
        var genre = await _unitOfWork.Genres.GetByIdAsync(id);
        if (genre == null)
        {
            return NotFound();
        }
        return View(genre); // trả về view chỉnh sửa thể loại
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
            genre.Slug = GenerateSlug(genre.Name); // tạo slug từ tên
        }

        _unitOfWork.Genres.Update(genre); // cập nhật thể loại
        await _unitOfWork.SaveChangesAsync(); // lưu thay đổi

        TempData["Success"] = $"Đã cập nhật thể loại '{genre.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Genres/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) // xóa thể loại
    {
        var genre = await _unitOfWork.Genres.GetByIdAsync(id);
        if (genre == null)
        {
            return NotFound();
        }

        _unitOfWork.Genres.Delete(genre); // xóa thể loại
        await _unitOfWork.SaveChangesAsync(); // lưu thay đổi

        TempData["Success"] = $"Đã xóa thể loại '{genre.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    private string GenerateSlug(string name)
    {
        // Simple slug generation (Vietnamese to Latin)
        var slug = name.ToLower() // chuyển sang chữ thường
            .Replace(" ", "-") // thay khoảng trắng bằng dấu gạch ngang
            .Replace("đ", "d") // thay đ bằng d
            .Replace("ă", "a") // thay ă bằng a
            .Replace("â", "a") // thay â bằng a
            .Replace("ê", "e") // thay ê bằng e
            .Replace("ô", "o")
            .Replace("ơ", "o")
            .Replace("ư", "u");
        
        // xóa dấu và ký tự đặc biệt
        return System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
    }
}
