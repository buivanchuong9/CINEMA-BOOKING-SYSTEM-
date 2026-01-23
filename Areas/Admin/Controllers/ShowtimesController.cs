using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ShowtimesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ShowtimesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Showtimes
    public async Task<IActionResult> Index()
    {
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync();
        var movies = await _unitOfWork.Movies.GetAllAsync();
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        
        ViewBag.Movies = movies.ToDictionary(m => m.Id, m => m.Title);
        ViewBag.Rooms = rooms.ToDictionary(r => r.Id, r => r.Name);
        
        // Tạo ViewModel để truyền dữ liệu tính toán
        var showtimeViewModels = new List<dynamic>();
        
        foreach (var showtime in showtimes)
        {
            var room = rooms.FirstOrDefault(r => r.Id == showtime.RoomId);
            var totalSeats = room != null ? (room.TotalRows * room.SeatsPerRow) : 0;
            var bookedSeats = await GetBookedSeatsCountAsync(showtime.Id);
            
            showtimeViewModels.Add(new
            {
                Showtime = showtime,
                Room = room,
                TotalSeats = totalSeats,
                BookedSeats = bookedSeats,
                AvailableSeats = totalSeats - bookedSeats
            });
        }
        
        ViewBag.ShowtimeData = showtimeViewModels;
        
        return View(showtimes.OrderByDescending(s => s.StartTime).ToList());
    }

    // GET: /Admin/Showtimes/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View();
    }

    // POST: /Admin/Showtimes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Showtime showtime)
    {
        try
        {
            ModelState.Remove("Movie");
            ModelState.Remove("Room");
            ModelState.Remove("EndTime");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("IsActive");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                await PopulateDropdowns();
                return View(showtime);
            }

            // Calculate EndTime based on Movie duration
            var movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
            if (movie != null)
            {
                showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + 15); // +15 phút dọn dẹp
            }
            
            // Check if room is available at this time
            var conflictingShowtime = await CheckRoomConflictAsync(showtime.RoomId, showtime.StartTime, showtime.EndTime);
            if (conflictingShowtime != null)
            {
                TempData["Error"] = "Phòng chiếu đã có lịch chiếu trong khung giờ này!";
                await PopulateDropdowns();
                return View(showtime);
            }

            showtime.CreatedAt = DateTime.Now;
            showtime.IsActive = true;
            
            await _unitOfWork.Showtimes.AddAsync(showtime);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Đã tạo lịch chiếu thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            await PopulateDropdowns();
            return View(showtime);
        }
    }

    // GET: /Admin/Showtimes/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(id);
        if (showtime == null)
        {
            return NotFound();
        }
        
        await PopulateDropdowns();
        return View(showtime);
    }

    // POST: /Admin/Showtimes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Showtime showtime)
    {
        if (id != showtime.Id)
        {
            return NotFound();
        }

        try
        {
            ModelState.Remove("Movie");
            ModelState.Remove("Room");
            ModelState.Remove("EndTime");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                await PopulateDropdowns();
                return View(showtime);
            }

            // Recalculate EndTime
            var movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
            if (movie != null)
            {
                showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + 15);
            }

            _unitOfWork.Showtimes.Update(showtime);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật lịch chiếu thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            await PopulateDropdowns();
            return View(showtime);
        }
    }

    // POST: /Admin/Showtimes/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(id);
        if (showtime == null)
        {
            return NotFound();
        }

        // Check if there are any bookings
        var hasBookings = await HasBookingsAsync(showtime.Id);
        if (hasBookings)
        {
            TempData["Error"] = "Không thể xóa lịch chiếu đã có người đặt vé!";
            return RedirectToAction(nameof(Index));
        }

        _unitOfWork.Showtimes.Delete(showtime);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Đã xóa lịch chiếu thành công!";
        return RedirectToAction(nameof(Index));
    }

    #region Helper Methods

    private async Task PopulateDropdowns()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync();
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        
        ViewBag.MovieSelectList = new SelectList(
            movies.Where(m => m.IsActive).OrderBy(m => m.Title), 
            "Id", "Title"
        );
        
        ViewBag.RoomSelectList = new SelectList(
            rooms.Where(r => r.IsActive).OrderBy(r => r.Name), 
            "Id", "Name"
        );
        
        ViewBag.CinemaSelectList = new SelectList(
            cinemas.Where(c => c.IsActive).OrderBy(c => c.Name),
            "Id", "Name"
        );
    }

    private async Task<int> GetBookedSeatsCountAsync(int showtimeId)
    {
        // Lấy tất cả seats của showtime này
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null) return 0;
        
        var seats = (await _unitOfWork.Seats.GetAllAsync())
            .Where(s => s.RoomId == showtime.RoomId && s.Status == Core.Enums.SeatStatus.Booked);
        
        return seats.Count();
    }

    private async Task<Showtime?> CheckRoomConflictAsync(int roomId, DateTime startTime, DateTime endTime)
    {
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync();
        
        return showtimes.FirstOrDefault(s => 
            s.RoomId == roomId &&
            s.IsActive &&
            ((s.StartTime >= startTime && s.StartTime < endTime) ||
             (s.EndTime > startTime && s.EndTime <= endTime) ||
             (s.StartTime <= startTime && s.EndTime >= endTime))
        );
    }

    private async Task<bool> HasBookingsAsync(int showtimeId)
    {
        var bookedCount = await GetBookedSeatsCountAsync(showtimeId);
        return bookedCount > 0;
    }

    #endregion
}
