using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;
using BE.Application.Helpers;

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
    public async Task<IActionResult> Index(int pageNumber = 1, string? search = null)
    {
        int pageSize = 20;
        var allShowtimes = await _unitOfWork.Showtimes.GetAllAsync();
        var movies = await _unitOfWork.Movies.GetAllAsync();
        var rooms = await _unitOfWork.Rooms.GetAllAsync();

        var query = allShowtimes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(sh => 
                (movies.FirstOrDefault(m => m.Id == sh.MovieId)?.Title != null && movies.FirstOrDefault(m => m.Id == sh.MovieId).Title.ToLower().Contains(s)) ||
                (rooms.FirstOrDefault(r => r.Id == sh.RoomId)?.Name != null && rooms.FirstOrDefault(r => r.Id == sh.RoomId).Name.ToLower().Contains(s))
            );
        }

        var sortedShowtimes = query.OrderByDescending(s => s.StartTime);
        
        var paginatedShowtimes = PaginatedList<Showtime>.Create(sortedShowtimes, pageNumber, pageSize);
        
        ViewBag.Movies = movies.ToDictionary(m => m.Id, m => m.Title); // danh sách phim theo id và tên
        ViewBag.Rooms = rooms.ToDictionary(r => r.Id, r => r.Name); // danh sách phòng theo id và tên
        ViewBag.Search = search;
        
        // Tạo ViewModel để truyền dữ liệu tính toán
        var showtimeViewModels = new List<dynamic>();
        
        foreach (var showtime in paginatedShowtimes)
        {
            var room = rooms.FirstOrDefault(r => r.Id == showtime.RoomId); // tìm phòng theo id nếu kh thấy sẽ null
            var totalSeats = room != null ? (room.TotalRows * room.SeatsPerRow) : 0; // tính tổng số ghế nếu kh thấy sẽ là 0
            var bookedSeats = await GetBookedSeatsCountAsync(showtime.Id); // đếm số ghế đã đặt 
            
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
        
        return View(paginatedShowtimes);
    }

    // GET: /Admin/Showtimes/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns(); // hiển thị danh sách phim và phòng  
        return View();
    }

    // POST: /Admin/Showtimes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Showtime showtime) // tạo lịch chiếu
    {
        try
        {
            ModelState.Remove("Movie");
            ModelState.Remove("Room");
            ModelState.Remove("EndTime"); // xoá thời gian kết thúc
            ModelState.Remove("CreatedAt");
            ModelState.Remove("IsActive"); // xoá trạng thái hoạt động

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                await PopulateDropdowns(); // hiển thị danh sách phim và phòng  
                return View(showtime);
            }

            // Kiểm tra giá trị hợp lệ của thời gian bắt đầu (phải trong tương lai và không thể là default)
            if (showtime.StartTime == default || showtime.StartTime < DateTime.Now)
            {
                TempData["Error"] = "Thời gian bắt đầu chiếu không hợp lệ hoặc ở trong quá khứ!";
                await PopulateDropdowns();
                return View(showtime);
            }

            // Kiểm tra giá vé cơ bản
            if (showtime.BasePrice <= 0)
            {
                TempData["Error"] = "Giá vé cơ bản phải lớn hơn 0!";
                await PopulateDropdowns();
                return View(showtime);
            }

            // Tính EndTime dựa trên thời lượng phim
            var movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
            if (movie == null)
            {
                TempData["Error"] = "Không tìm thấy phim đã chọn!";
                await PopulateDropdowns();
                return View(showtime);
            }
            showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + 15); // +15 phút dọn dẹp

            // Kiểm tra phòng chiếu có bị trùng lịch không
            var conflict = await CheckRoomConflictAsync(showtime.RoomId, showtime.StartTime, showtime.EndTime, excludeId: null);
            if (conflict != null)
            {
                var rooms = await _unitOfWork.Rooms.GetAllAsync();
                var conflictRoom = rooms.FirstOrDefault(r => r.Id == showtime.RoomId);
                var conflictMovies = await _unitOfWork.Movies.GetAllAsync();
                var conflictMovie = conflictMovies.FirstOrDefault(m => m.Id == conflict.MovieId);

                TempData["Error"] = $"⚠️ Phòng \"{conflictRoom?.Name ?? ""}\" đã có lịch chiếu phim " +
                    $"\"{conflictMovie?.Title ?? ""}\" từ {conflict.StartTime:HH:mm} đến {conflict.EndTime:HH:mm} " +
                    $"ngày {conflict.StartTime:dd/MM/yyyy}. Vui lòng chọn giờ chiếu khác!";
                await PopulateDropdowns();
                return View(showtime);
            }

            showtime.CreatedAt = DateTime.Now;
            showtime.IsActive = true;
            
            await _unitOfWork.Showtimes.AddAsync(showtime);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo lịch chiếu phim \"{movie.Title}\" lúc {showtime.StartTime:HH:mm dd/MM/yyyy} thành công!";
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
    public async Task<IActionResult> Edit(int id) // cập nhật lịch chiếu
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(id);
        if (showtime == null)
        {
            return NotFound(); // nếu không tìm thấy lịch chiếu thì trả về 404
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

            // Kiểm tra giá trị hợp lệ của thời gian bắt đầu
            if (showtime.StartTime == default)
            {
                TempData["Error"] = "Thời gian bắt đầu chiếu không hợp lệ!";
                await PopulateDropdowns();
                return View(showtime);
            }

            // Kiểm tra giá vé cơ bản
            if (showtime.BasePrice <= 0)
            {
                TempData["Error"] = "Giá vé cơ bản phải lớn hơn 0!";
                await PopulateDropdowns();
                return View(showtime);
            }

            // Tính lại EndTime theo thời lượng phim mới
            var movie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
            if (movie == null)
            {
                TempData["Error"] = "Không tìm thấy phim đã chọn!";
                await PopulateDropdowns();
                return View(showtime);
            }
            showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + 15);

            // Kiểm tra trùng lịch phòng chiếu (loại trừ chính lịch đang sửa)
            var conflict = await CheckRoomConflictAsync(showtime.RoomId, showtime.StartTime, showtime.EndTime, excludeId: showtime.Id);
            if (conflict != null)
            {
                var rooms = await _unitOfWork.Rooms.GetAllAsync();
                var conflictRoom = rooms.FirstOrDefault(r => r.Id == showtime.RoomId);
                var conflictMovies = await _unitOfWork.Movies.GetAllAsync();
                var conflictMovie = conflictMovies.FirstOrDefault(m => m.Id == conflict.MovieId);

                TempData["Error"] = $"⚠️ Phòng \"{conflictRoom?.Name ?? ""}\" đã có lịch chiếu phim " +
                    $"\"{conflictMovie?.Title ?? ""}\" từ {conflict.StartTime:HH:mm} đến {conflict.EndTime:HH:mm} " +
                    $"ngày {conflict.StartTime:dd/MM/yyyy}. Vui lòng chọn giờ chiếu khác!";
                await PopulateDropdowns();
                return View(showtime);
            }

            _unitOfWork.Showtimes.Update(showtime);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật lịch chiếu phim \"{movie.Title}\" thành công!";
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

        // Kiểm tra xem có đặt chỗ nào không.
        var hasBookings = await HasBookingsAsync(showtime.Id);
        if (hasBookings) // nếu có đặt chỗ thì không thể xóa
        {
            TempData["Error"] = "Không thể xóa lịch chiếu đã có người đặt vé!";
            return RedirectToAction(nameof(Index));
        }

        _unitOfWork.Showtimes.Delete(showtime);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Đã xóa lịch chiếu thành công!";
        return RedirectToAction(nameof(Index));
    }

    // Các phương thức hỗ trợ    
    #region Helper Methods 

    private async Task PopulateDropdowns()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync();
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        
        ViewBag.MovieSelectList = new SelectList(
            movies.Where(m => m.IsActive).OrderBy(m => m.Title), // lọc danh sách phim đang hoạt động
            "Id", "Title"
        );
        
        // Hiển thị tên phòng kèm theo tên rạp để tránh trùng lặp
        var roomSelectListData = rooms
            .Where(r => r.IsActive)
            .Select(r => new
            {
                Id = r.Id,
                Name = $"{r.Name} ({cinemas.FirstOrDefault(c => c.Id == r.CinemaId)?.Name ?? "Không rõ rạp"})"
            })
            .OrderBy(r => r.Name);
        
        ViewBag.RoomSelectList = new SelectList(
            roomSelectListData,
            "Id", "Name"
        );
        
        ViewBag.CinemaSelectList = new SelectList(
            cinemas.Where(c => c.IsActive).OrderBy(c => c.Name), // lọc danh sách rạp đang hoạt động
            "Id", "Name"
        );
    }

    private async Task<int> GetBookedSeatsCountAsync(int showtimeId) // lấy số ghế đã đặt
    {
        // Lấy danh sách booking đã thanh toán cho lịch chiếu này
        var bookings = await _unitOfWork.Bookings.FindAsync(b => b.ShowtimeId == showtimeId && b.Status == Core.Enums.BookingStatus.Paid);
        var bookingIds = bookings.Select(b => b.Id).ToList();
        
        if (!bookingIds.Any()) return 0;
        
        // Lấy danh sách chi tiết đặt vé tương ứng với các booking này
        var bookingDetails = await _unitOfWork.BookingDetails.FindAsync(bd => bookingIds.Contains(bd.BookingId));
        
        // Đếm số lượng ghế duy nhất đã được đặt
        return bookingDetails.Select(bd => bd.SeatId).Distinct().Count();
    }

    /// <summary>
    /// Kiểm tra xem phòng có bị trùng lịch chiếu trong khoảng thời gian không.
    /// excludeId: bỏ qua lịch chiếu có id này (dùng khi Edit để không conflict với chính mình)
    /// </summary>
    private async Task<Showtime?> CheckRoomConflictAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeId)
    {
        return await _unitOfWork.Showtimes.FirstOrDefaultAsync(s => 
            s.RoomId == roomId &&
            s.IsActive &&
            (!excludeId.HasValue || s.Id != excludeId.Value) && // loại trừ chính lịch đang chỉnh sửa
            s.StartTime < endTime &&
            s.EndTime > startTime // overlap check
        );
    }

    private async Task<bool> HasBookingsAsync(int showtimeId) // kiểm tra xem có đặt chỗ nào không
    {
        var bookedCount = await GetBookedSeatsCountAsync(showtimeId); // lấy số ghế đã đặt
        return bookedCount > 0; //đặt chỗ thì trả về true, ngược lại trả về false
    }

    #endregion // kết thúc các phương thức hỗ trợ    
}
