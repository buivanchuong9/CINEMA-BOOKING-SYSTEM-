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

    public CinemasController(IUnitOfWork unitOfWork) // DI
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Cinemas
    public async Task<IActionResult> Index() // danh sách rạp
    {
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        return View(cinemas.OrderBy(c => c.Name).ToList());
    }

    // GET: /Admin/Cinemas/Create
    public IActionResult Create() // tạo rạp
    {
        return View();
    }

    // POST: /Admin/Cinemas/Create
    [HttpPost]
    [ValidateAntiForgeryToken] // Chống hack CSRF (giả mạo người dùng gửi form)
    public async Task<IActionResult> Create(Cinema cinema) // tạo rạp
    {
        try
        {
            // Xóa các trường tùy chọn khỏi ModelState
            ModelState.Remove("MapEmbedUrl"); // gg map
            ModelState.Remove("Phone"); // số điện thoại
            ModelState.Remove("CreatedAt"); // thời gian tạo
            ModelState.Remove("IsActive"); // trạng thái hoạt động

            if (!ModelState.IsValid) // check dữ liệu nhập vào hợp lệ không
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(); 
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin: " + string.Join(", ", errors);
                return View(cinema);
            }

            cinema.CreatedAt = DateTime.Now; 
            cinema.IsActive = true; // mặc định rạp hoạt động
            
            await _unitOfWork.Cinemas.AddAsync(cinema); // thêm rạp vào database
            await _unitOfWork.SaveChangesAsync(); // lưu thay đổi

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
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
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

    // POST: /Admin/Cinemas/Delete/5 (Route parameter /Delete/5)
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
