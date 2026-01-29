using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;

namespace BE.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardApiController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy dữ liệu doanh thu 7 ngày qua
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueData()
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-6 + i))
                .Select(date => new
                {
                    Date = date.ToString("dd/MM"),
                    Revenue = (decimal)allBookings
                        .Where(b => b.BookingDate.Date == date && b.Status == Core.Enums.BookingStatus.Paid)
                        .Sum(b => b.TotalAmount)
                })
                .ToList();
            
            // Nếu không có dữ liệu 7 ngày gần nhất, lấy từ tất cả bookings
            if (last7Days.All(d => d.Revenue == 0) && allBookings.Any())
            {
                var bookingsByDate = allBookings
                    .Where(b => b.Status == Core.Enums.BookingStatus.Paid)
                    .GroupBy(b => b.BookingDate.Date)
                    .OrderByDescending(g => g.Key)
                    .Take(7)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("dd/MM"),
                        Revenue = (decimal)g.Sum(b => b.TotalAmount)
                    })
                    .ToList();
                
                if (bookingsByDate.Any())
                {
                    return Ok(bookingsByDate);
                }
            }
            
            return Ok(last7Days);
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
    public async Task<IActionResult> GetBookingStatus()
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            
            if (!allBookings.Any())
            {
                return Ok(new List<object>());
            }
            
            var bookingByStatus = allBookings.GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
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
    public async Task<IActionResult> GetTopMovies()
    {
        try
        {
            var allBookings = (await _unitOfWork.Bookings.GetAllAsync()).ToList();
            var allShowtimes = (await _unitOfWork.Showtimes.GetAllAsync()).ToList();
            var allMovies = (await _unitOfWork.Movies.GetAllAsync()).ToList();

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

            // Nếu không có booking, lấy 5 phim mới nhất
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
                        .Where(b => b.BookingDate.Date == date && b.Status == Core.Enums.BookingStatus.Paid)
                        .Sum(b => b.TotalAmount)
                })
                .ToList();

            if (last7Days.All(d => d.Revenue == 0) && allBookings.Any())
            {
                var bookingsByDate = allBookings
                    .Where(b => b.Status == Core.Enums.BookingStatus.Paid)
                    .GroupBy(b => b.BookingDate.Date)
                    .OrderByDescending(g => g.Key)
                    .Take(7)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("dd/MM"),
                        Revenue = (decimal)g.Sum(b => b.TotalAmount)
                    })
                    .ToList();
                
                if (bookingsByDate.Any())
                {
                    last7Days = bookingsByDate;
                }
            }

            // Booking status
            var bookingByStatus = !allBookings.Any() 
                ? new List<object>()
                : allBookings.GroupBy(b => b.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() } as object)
                    .ToList();

            // Top movies
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
            var topMovies = movieBookingCounts.Select(x => new { Title = x.Movie.Title, Count = x.Count }).ToList();

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
