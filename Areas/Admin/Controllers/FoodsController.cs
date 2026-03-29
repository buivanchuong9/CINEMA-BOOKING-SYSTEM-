using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Concessions;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FoodsController : Controller // danh sách món ăn
{
    private readonly IUnitOfWork _unitOfWork;

    public FoodsController(IUnitOfWork unitOfWork) // DI
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Foods
    public async Task<IActionResult> Index() // danh sách món ăn
    {
        var foods = await _unitOfWork.Foods.GetAllAsync();
        return View(foods.OrderBy(f => f.Name).ToList()); // sắp xếp theo tên món ăn
    }

    // GET: /Admin/Foods/Create
    public IActionResult Create() // tạo món ăn
    {
        return View();
    }

    // POST: /Admin/Foods/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Food food)
    {
        try
        {
            ModelState.Remove("Description");
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Validation errors: " + string.Join(", ", errors);
                return View(food);
            }

            food.CreatedAt = DateTime.Now;
            food.IsAvailable = true;
            await _unitOfWork.Foods.AddAsync(food);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm món '{food.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return View(food);
        }
    }

    // GET: /Admin/Foods/Edit/5
    public async Task<IActionResult> Edit(int id) // chỉnh sửa món ăn
    {
        var food = await _unitOfWork.Foods.GetByIdAsync(id);
        if (food == null)
        {
            return NotFound(); // không tìm thấy món ăn
        }
        return View(food);
    }

    // POST: /Admin/Foods/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken] // chống hack CSRF
    public async Task<IActionResult> Edit(int id, Food food)
    {
        if (id != food.Id)
        {
            return NotFound();
        }

        try
        {
            ModelState.Remove("Description"); // xóa mô tả
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid) // check dữ liệu nhập vào hợp lệ không
            {
                return View(food);
            }

            _unitOfWork.Foods.Update(food);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật món '{food.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return View(food);
        }
    }

    // POST: /Admin/Foods/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var food = await _unitOfWork.Foods.GetByIdAsync(id);
        if (food == null)
        {
            return NotFound();
        }

        _unitOfWork.Foods.Delete(food);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa món '{food.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }
}
