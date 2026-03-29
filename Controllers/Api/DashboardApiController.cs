using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;

namespace BE.Controllers.Api;

[ApiController] // đánh dấu là API controller
[Route("api/[controller]")] 
[Authorize(Roles = "Admin")]
public class DashboardApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardApiController(IUnitOfWork unitOfWork) // inject IUnitOfWork
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy dữ liệu doanh thu 7 ngày qua
    /// </summary>
    [HttpGet("revenue")] 
    public async Task<IActionResult> GetRevenueData() // lấy dữ liệu doanh thu 7 ngày qua
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList(); // lấy tất cả bookings
            
            var last7Days = Enumerable.Range(0, 7) // 7 ngày gần nhất
                .Select(i => DateTime.Today.AddDays(-6 + i)) // ngày hôm nay - 6 ngày
                .Select(date => new
                {
                    Date = date.ToString("dd/MM"), // định dạng ngày tháng
                    Revenue = (decimal)allBookings // lấy tất cả bookings
                        .Where(b => b.BookingDate.Date == date && b.Status == Core.Enums.BookingStatus.Paid) // lọc bookings đã thanh toán
                        .Sum(b => b.TotalAmount) // tính tổng tiền
                })
                .ToList(); 
            
            // Nếu không có dữ liệu 7 ngày gần nhất, lấy từ tất cả bookings
            if (last7Days.All(d => d.Revenue == 0) && allBookings.Any()) 
            {
                var bookingsByDate = allBookings 
                    .Where(b => b.Status == Core.Enums.BookingStatus.Paid) // lọc booking đã thanh toán
                    .GroupBy(b => b.BookingDate.Date) // theo nhóm ngày
                    .OrderByDescending(g => g.Key) // sắp xếp giảm dần theo ngày
                    .Take(7) // lấy 7 ngày
                    .OrderBy(g => g.Key) // sắp xếp tăng dần theo ngày
                    .Select(g => new 
                    {
                        Date = g.Key.ToString("dd/MM"),
                        Revenue = (decimal)g.Sum(b => b.TotalAmount) // tính tổng doanh thu
                    })
                    .ToList();
                
                if (bookingsByDate.Any()) // nếu có dữ liệu
                {
                    return Ok(bookingsByDate); // trả về dữ liệu
                }
            }
            
            return Ok(last7Days); // trả về dữ liệu
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); 
        }
    }

    /// <summary>
    /// Lấy dữ liệu trạng thái đặt vé
    /// </summary>
    [HttpGet("booking-status")]
    public async Task<IActionResult> GetBookingStatus() // lấy dữ liệu trạng thái đặt vé
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            
            if (!allBookings.Any()) // kiểm tra danh sách có ít nhất 1 hay không
            {
                return Ok(new List<object>()); // trả về danh sách rỗng nếu không có bookings
            }
            
            var bookingByStatus = allBookings.GroupBy(b => b.Status) // theo nhóm trạng thái
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() }) // đếm số lượng theo từng trạng thái
                .ToList();
            
            return Ok(bookingByStatus); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy dữ liệu Top 5 phim hot nhất
    /// </summary>
    [HttpGet("top-movies")]
    public async Task<IActionResult> GetTopMovies() // lấy dữ liệu top 5 phim hot nhất
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            var allShowtimes = (await _unitOfWork.Showtimes.GetAllAsync()).ToList();
            var allMovies = (await _unitOfWork.Movies.GetAllAsync()).ToList();

            var movieBookingCounts = allMovies
                .Select(m => new
                {
                    Movie = m, // giữ thông tin phim
                    Count = allBookings.Count(b =>  // số lượng booking đã thanh toán
                        b.Status == Core.Enums.BookingStatus.Paid && // những đặt đã thanh toán
                        allShowtimes.Any(st => st.Id == b.ShowtimeId && st.MovieId == m.Id) // thuộc một suất chiếu của phim
                    )
                })
                .Where(x => x.Count > 0) // lọc những phim có booking
                .OrderByDescending(x => x.Count) // sắp xếp giảm dần theo số lượng booking
                .Take(5) 
                .ToList();

            // Nếu không có booking, lấy 5 phim mới nhất
            if (!movieBookingCounts.Any() && allMovies.Any())
            {
                movieBookingCounts = allMovies 
                    .OrderByDescending(m => m.CreatedAt) // sắp xếp giảm dần theo ngày tạo
                    .Take(5)
                    .Select((m, index) => new 
                    {
                        Movie = m,
                        Count = 5 - index // Phim mới nhất -> Count = 5, phim thứ 2 -> Count = 4, ...
                    })
                    .ToList();
            }

            var result = movieBookingCounts.Select(x => new 
            { 
                Title = x.Movie.Title, 
                Count = x.Count 
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy dữ liệu lịch chiếu cho biểu đồ Gantt
    /// </summary>
    [HttpGet("movie-schedules")]
    public async Task<IActionResult> GetMovieSchedules()
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            var allShowtimes = (await _unitOfWork.Showtimes.GetAllAsync()).ToList();
            var allMovies = (await _unitOfWork.Movies.GetAllAsync()).ToList();

            // Lấy top 5 phim
            var movieBookingCounts = allMovies
                .Select(m => new
                {
                    Movie = m,
                    Count = allBookings.Count(b => 
                        b.Status == Core.Enums.BookingStatus.Paid && 
                        allShowtimes.Any(st => st.Id == b.ShowtimeId && st.MovieId == m.Id)
                    )
                })
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            if (!movieBookingCounts.Any() && allMovies.Any())
            {
                movieBookingCounts = allMovies
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .Select((m, index) => new
                    {
                        Movie = m,
                        Count = 5 - index
                    })
                    .ToList();
            }

            var topMoviesList = movieBookingCounts.Select(x => x.Movie).ToList();

            // Lấy lịch chiếu từ hôm nay đến 7 ngày tới
            var today = DateTime.Today;
            var endDate = today.AddDays(7);
            var activeShowtimes = allShowtimes
                .Where(s => s.StartTime >= today && s.StartTime <= endDate && s.IsActive)
                .ToList();

            var movieSchedules = topMoviesList.Select(m => new
            {
                id = m.Id.ToString(),
                name = m.Title,
                data = activeShowtimes
                    .Where(s => s.MovieId == m.Id)
                    .Select(s => new 
                    {
                        start = new DateTimeOffset(s.StartTime).ToUnixTimeMilliseconds(),
                        end = new DateTimeOffset(s.EndTime).ToUnixTimeMilliseconds(),
                        taskName = m.Title,
                        id = s.Id.ToString(),
                        y = topMoviesList.IndexOf(m) // Index for Gantt chart
                    })
                    .OrderBy(s => s.start)
                    .ToList()
            }).ToList();

            return Ok(movieSchedules);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy tất cả dữ liệu dashboard trong 1 request
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllDashboardData()
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            var allShowtimes = (await _unitOfWork.Showtimes.GetAllAsync()).ToList();
            var allMovies = (await _unitOfWork.Movies.GetAllAsync()).ToList();

            // Revenue data
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-6 + i))
                .Select(date => new
                {
                    Date = date.ToString("dd/MM"),
                    Revenue = (decimal)allBookings
                        .Where(b => b.BookingDate.Date == date && b.Status == Core.Enums.BookingStatus.Paid) // chỉ lấy booking đã thanh toán
                        .Sum(b => b.TotalAmount) // tính tổng doanh thu
                })
                .ToList();

            if (last7Days.All(d => d.Revenue == 0) && allBookings.Any()) // kh có doanh thu trong 7 ngày
            {
                var bookingsByDate = allBookings
                    .Where(b => b.Status == Core.Enums.BookingStatus.Paid) 
                    .GroupBy(b => b.BookingDate.Date) // nhóm theo ngày
                    .OrderByDescending(g => g.Key) // sắp xếp giảm dần theo ngày
                    .Take(7) // lấy 7 ngày gần nhất 
                    .OrderBy(g => g.Key) // sắp xếp theo ngày
                    .Select(g => new
                    {
                        Date = g.Key.ToString("dd/MM"),
                        Revenue = (decimal)g.Sum(b => b.TotalAmount) // tính tổng tiền
                    })
                    .ToList();
                
                if (bookingsByDate.Any()) // nếu có dữ liệu
                {
                    last7Days = bookingsByDate; // gán dữ liệu cho last7Days
                }
            }

            // Booking status
            var bookingByStatus = !allBookings.Any() // nếu không có bookings
                ? new List<object>() // trả về danh sách rỗng
                : allBookings.GroupBy(b => b.Status) // nhóm theo trạng thái
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() } as object) // chọn trạng thái và số lượng
                    .ToList(); // chuyển sang danh sách

            // Top movies
            var movieBookingCounts = allMovies 
                .Select(m => new
                {
                    Movie = m,
                    Count = allBookings.Count(b => // 
                        b.Status == Core.Enums.BookingStatus.Paid && 
                        allShowtimes.Any(st => st.Id == b.ShowtimeId && st.MovieId == m.Id) 
                    )
                })
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            if (!movieBookingCounts.Any() && allMovies.Any())
            {
                movieBookingCounts = allMovies // nếu không có bookings
                    .OrderByDescending(m => m.CreatedAt) // sắp xếp theo ngày tạo
                    .Take(5) // lấy 5 phim
                    .Select((m, index) => new // chọn phim và số lượng
                    {
                        Movie = m,
                        Count = 5 - index // số lượng
                    })
                    .ToList();
            }

            var topMoviesList = movieBookingCounts.Select(x => x.Movie).ToList(); // danh sách phim
            var topMovies = movieBookingCounts.Select(x => new { Title = x.Movie.Title, Count = x.Count }).ToList(); // danh sách phim và số lượng

            // Movie schedules for Gantt
            var today = DateTime.Today;
            var endDate = today.AddDays(7);
            var activeShowtimes = allShowtimes
                .Where(s => s.StartTime >= today && s.StartTime <= endDate && s.IsActive)
                .ToList();

            var movieSchedules = topMoviesList.Select(m => new
            {
                id = m.Id.ToString(),
                name = m.Title,
                data = activeShowtimes
                    .Where(s => s.MovieId == m.Id)
                    .Select(s => new 
                    {
                        start = new DateTimeOffset(s.StartTime).ToUnixTimeMilliseconds(),
                        end = new DateTimeOffset(s.EndTime).ToUnixTimeMilliseconds(),
                        taskName = m.Title,
                        id = s.Id.ToString(),
                        y = topMoviesList.IndexOf(m)
                    })
                    .OrderBy(s => s.start)
                    .ToList()
            }).ToList();

            return Ok(new
            {
                revenue = last7Days,
                bookingStatus = bookingByStatus,
                topMovies = topMovies,
                movieSchedules = movieSchedules
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
