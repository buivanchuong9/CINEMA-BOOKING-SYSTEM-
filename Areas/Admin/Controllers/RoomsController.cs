using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BE.Core.Interfaces;
using BE.Core.Entities.CinemaInfrastructure;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RoomsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Rooms
    public async Task<IActionResult> Index()
    {
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        
        ViewBag.Cinemas = cinemas.ToDictionary(c => c.Id, c => c.Name);
        
        return View(rooms.OrderBy(r => r.CinemaId).ThenBy(r => r.Name).ToList());
    }

    // GET: /Admin/Rooms/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View();
    }

    // POST: /Admin/Rooms/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Room room)
    {
        try
        {
            ModelState.Remove("SeatMapMatrix");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("IsActive");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                await PopulateDropdowns();
                return View(room);
            }

            // Generate default seat map matrix (JSON)
            if (string.IsNullOrEmpty(room.SeatMapMatrix))
            {
                room.SeatMapMatrix = GenerateDefaultSeatMap(room.TotalRows, room.SeatsPerRow);
            }

            room.CreatedAt = DateTime.Now;
            
            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo phòng '{room.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            await PopulateDropdowns();
            return View(room);
        }
    }

    // GET: /Admin/Rooms/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }
        
        await PopulateDropdowns();
        return View(room);
    }

    // POST: /Admin/Rooms/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Room room)
    {
        if (id != room.Id)
        {
            return NotFound();
        }

        try
        {
            ModelState.Remove("SeatMapMatrix");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                await PopulateDropdowns();
                return View(room);
            }

            _unitOfWork.Rooms.Update(room);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật phòng '{room.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            await PopulateDropdowns();
            return View(room);
        }
    }

    // POST: /Admin/Rooms/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        _unitOfWork.Rooms.Delete(room);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa phòng '{room.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        ViewBag.CinemaSelectList = new SelectList(
            cinemas.Where(c => c.IsActive).OrderBy(c => c.Name), 
            "Id", "Name"
        );
    }

    private string GenerateDefaultSeatMap(int rows, int seatsPerRow)
    {
        // Generate simple matrix: all seats are standard type (1)
        // Format: "1,1,1,1,1;1,1,1,1,1;..." (semicolon separates rows)
        var seatRows = new List<string>();
        
        for (int r = 0; r < rows; r++)
        {
            var row = string.Join(",", Enumerable.Repeat("1", seatsPerRow));
            seatRows.Add(row);
        }
        
        return string.Join(";", seatRows);
    }
}
