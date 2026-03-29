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
    public async Task<IActionResult> Index() // danh sách phòng
    {
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        
        ViewBag.Cinemas = cinemas.ToDictionary(c => c.Id, c => c.Name); // danh sách rạp
        
        return View(rooms.OrderBy(r => r.CinemaId).ThenBy(r => r.Name).ToList()); // sắp xếp theo rạp và tên phòng
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
            ModelState.Remove("Cinema"); 
            ModelState.Remove("Seats"); 
            ModelState.Remove("SeatMapMatrix"); // xoá sơ đồ ghế
            ModelState.Remove("CreatedAt"); // xoá ngày tạo
            ModelState.Remove("IsActive"); // xoá trạng thái hoạt động

            if (!ModelState.IsValid) // kiểm tra người dùng nhâp dữ liệu có hợp lệ không
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                await PopulateDropdowns();
                return View(room);
            }

            // Tạo ma trận sơ đồ chỗ ngồi mặc định (JSON)
            if (string.IsNullOrEmpty(room.SeatMapMatrix)) // nếu không có sơ đồ ghế thì tạo sơ đồ ghế mặc định
            {
                room.SeatMapMatrix = GenerateDefaultSeatMap(room.TotalRows, room.SeatsPerRow); // tạo sơ đồ ghế mặc định
            }

            room.CreatedAt = DateTime.Now;
            
            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.SaveChangesAsync();

            // Tạo ghế tự động cho phòng mới
            await GenerateSeatsForRoom(room.Id, room.TotalRows, room.SeatsPerRow);

            TempData["Success"] = $"Đã tạo phòng '{room.Name}' với {room.TotalRows * room.SeatsPerRow} ghế thành công!";
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
    public async Task<IActionResult> Edit(int id, Room room) // cập nhật phòng
    {
        if (id != room.Id) // kiểm tra id có khớp không
        {
            return NotFound();
        }

        try
        {
            ModelState.Remove("Cinema"); 
            ModelState.Remove("Seats");
            ModelState.Remove("SeatMapMatrix"); // xoá sơ đồ ghế
            ModelState.Remove("CreatedAt"); // xoá ngày tạo

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                await PopulateDropdowns(); // hiển thị danh sách rạp
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

    // GET: /Admin/Rooms/GetRoomsByCinema?cinemaId=1
    [HttpGet]
    public async Task<IActionResult> GetRoomsByCinema(int cinemaId) // lấy danh sách phòng theo rạp
    {
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        var filteredRooms = rooms // lọc phòng theo rạp và trạng thái hoạt động
            .Where(r => r.CinemaId == cinemaId && r.IsActive) // lọc phòng theo rạp và trạng thái hoạt động
            .OrderBy(r => r.Name) // sắp xếp theo tên phòng
            .Select(r => new // chọn các trường cần thiết
            {
                id = r.Id,
                name = r.Name,
                totalRows = r.TotalRows,
                seatsPerRow = r.SeatsPerRow
            })
            .ToList();

        return Json(filteredRooms); // trả về danh sách phòng dưới dạng JSON
    }

    private async Task PopulateDropdowns() // hiển thị danh sách rạp
    {
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        ViewBag.CinemaSelectList = new SelectList(
            cinemas.Where(c => c.IsActive).OrderBy(c => c.Name), // lọc rạp theo trạng thái hoạt động và sắp xếp theo tên
            "Id", "Name"
        );
    }

    private string GenerateDefaultSeatMap(int rows, int seatsPerRow) // tạo sơ đồ ghế mặc định
    {
        // Tạo ma trận đơn giản: tất cả các ghế đều là loại tiêu chuẩn (1)
        // Định dạng: "1,1,1,1,1;1,1,1,1,1;..." (dấu chấm phẩy phân tách các hàng)
        var seatRows = new List<string>();
        
        for (int r = 0; r < rows; r++)
        {
            var row = string.Join(",", Enumerable.Repeat("1", seatsPerRow)); // tạo hàng ghế 1 là mặc định 
            seatRows.Add(row);
        }
        
        return string.Join(";", seatRows); // trả về sơ đồ ghế
    }

    private async Task GenerateSeatsForRoom(int roomId, int totalRows, int seatsPerRow) // tạo ghế cho phòng
    {
        var seats = new List<Seat>();
        var seatTypes = (await _unitOfWork.SeatTypes.GetAllAsync()).ToList(); // lấy danh sách loại ghế

        // Lấy loại ghế mặc định (Standard)
        var standardSeatType = seatTypes.FirstOrDefault(st => st.Name == "Standard") // lấy loại ghế tiêu chuẩn
                               ?? seatTypes.FirstOrDefault(); // nếu không có loại ghế tiêu chuẩn thì lấy loại ghế đầu tiên
        
        if (standardSeatType == null)
        {
            throw new Exception("Không tìm thấy loại ghế nào trong hệ thống!"); // nếu không có loại ghế thì báo lỗi
        }

        // Tạo ghế cho từng hàng
        for (int row = 0; row < totalRows; row++)
        {
            char rowLabel = (char)('A' + row); // A, B, C, ...
            
            for (int seatNum = 1; seatNum <= seatsPerRow; seatNum++)
            {
                var seat = new Seat
                {
                    RoomId = roomId,
                    Row = rowLabel.ToString(),
                    Number = seatNum,
                    SeatTypeId = standardSeatType.Id,
                    Status = BE.Core.Enums.SeatStatus.Available,
                    CreatedAt = DateTime.Now
                };
                
                seats.Add(seat);
            }
        }

        // Lưu tất cả ghế vào database
        await _unitOfWork.Seats.AddRangeAsync(seats);
        await _unitOfWork.SaveChangesAsync();
    }
}
