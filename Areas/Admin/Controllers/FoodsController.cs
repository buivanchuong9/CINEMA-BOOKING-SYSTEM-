using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Concessions;
using BE.Application.Helpers;

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
    public async Task<IActionResult> Index(int pageNumber = 1, string? search = null) // danh sách món ăn
    {
        int pageSize = 20;
        var foods = await _unitOfWork.Foods.GetAllAsync();
        var query = foods.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(f => 
                (f.Name != null && f.Name.ToLower().Contains(s)) ||
                (f.Description != null && f.Description.ToLower().Contains(s))
            );
        }

        ViewBag.Search = search;
        var sortedFoods = query.OrderBy(f => f.Name);
        return View(PaginatedList<Food>.Create(sortedFoods, pageNumber, pageSize)); // sắp xếp theo tên món ăn và phân trang
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

            // 1. Kiểm tra trùng tên món ăn/combo
            var duplicateFood = await _unitOfWork.Foods.FirstOrDefaultAsync(f => 
                f.Name.Trim().ToLower() == food.Name.Trim().ToLower()
            );
            if (duplicateFood != null)
            {
                TempData["Error"] = $"Sản phẩm '{food.Name}' đã tồn tại trong hệ thống!";
                return View(food);
            }

            // 2. Ràng buộc giá cả không âm
            if (food.Price < 0)
            {
                TempData["Error"] = "Giá tiền sản phẩm không được nhỏ hơn 0 VNĐ!";
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

            // 1. Kiểm tra sản phẩm tồn tại
            var existingFood = await _unitOfWork.Foods.GetByIdAsync(id);
            if (existingFood == null)
            {
                return NotFound();
            }

            // 2. Kiểm tra trùng tên món ăn/combo (trừ chính nó)
            var duplicateFood = await _unitOfWork.Foods.FirstOrDefaultAsync(f => 
                f.Id != id &&
                f.Name.Trim().ToLower() == food.Name.Trim().ToLower()
            );
            if (duplicateFood != null)
            {
                TempData["Error"] = $"Sản phẩm '{food.Name}' đã tồn tại trong hệ thống!";
                return View(food);
            }

            // 3. Ràng buộc giá cả không âm
            if (food.Price < 0)
            {
                TempData["Error"] = "Giá tiền sản phẩm không được nhỏ hơn 0 VNĐ!";
                return View(food);
            }

            existingFood.Name = food.Name;
            existingFood.Description = food.Description;
            existingFood.Price = food.Price;
            existingFood.ImageUrl = food.ImageUrl;
            existingFood.IsCombo = food.IsCombo;
            existingFood.IsAvailable = food.IsAvailable;
            existingFood.DisplayOrder = food.DisplayOrder;

            _unitOfWork.Foods.Update(existingFood);
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

        // Kiểm tra xem món ăn này có liên kết với đơn đặt vé nào không
        var hasOrders = await _unitOfWork.BookingFoods.ExistsAsync(bf => bf.FoodId == id);
        if (hasOrders)
        {
            TempData["Error"] = "Không thể xóa sản phẩm này vì đã có khách hàng đặt trong đơn hàng!";
            return RedirectToAction(nameof(Index));
        }

        _unitOfWork.Foods.Delete(food);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa món '{food.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }
}
