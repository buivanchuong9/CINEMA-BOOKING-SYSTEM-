using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;
using BE.Core.Enums;

namespace BE.Controllers;

/// <summary>
/// Movies Controller - Quản lý danh sách phim
/// </summary>
public class MoviesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public MoviesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Movies
    public async Task<IActionResult> Index(string? status)
    {
        IEnumerable<Movie> movies;
        var allMovies = await _unitOfWork.Movies.GetAllAsync();

        if (!string.IsNullOrEmpty(status))
        {
            // Lọc theo trạng thái (NowShowing, ComingSoon)
            if (Enum.TryParse<MovieStatus>(status, out var movieStatus))
            {
                movies = allMovies.Where(m => (int)m.Status == (int)movieStatus);
            }
            else
            {
                // Chỉ hiển thị phim đang chiếu và sắp chiếu
                movies = allMovies.Where(m => 
                    ((int)m.Status == (int)MovieStatus.NowShowing || (int)m.Status == (int)MovieStatus.ComingSoon));
            }
        }
        else
        {
            // Mặc định: Chỉ hiển thị phim đang chiếu và sắp chiếu
            movies = allMovies.Where(m => 
                ((int)m.Status == (int)MovieStatus.NowShowing || (int)m.Status == (int)MovieStatus.ComingSoon));
        }

        ViewBag.CurrentStatus = status;
        return View(movies.OrderByDescending(m => m.ReleaseDate));
    }

    // GET: /Movies/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        
        if (movie == null)
        {
            return NotFound();
        }

        // Load showtimes cho phim này (chỉ lấy suất chiếu chưa qua)
        var showtimes = (await _unitOfWork.Showtimes.GetAllAsync())
            .Where(st => st.MovieId == id && st.StartTime >= DateTime.Now)
            .OrderBy(st => st.StartTime)
            .ToList();

        // Load thông tin phòng chiếu và rạp
        foreach (var showtime in showtimes)
        {
            showtime.Room = await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId);
            if (showtime.Room != null)
            {
                showtime.Room.Cinema = await _unitOfWork.Cinemas.GetByIdAsync(showtime.Room.CinemaId);
            }
        }

        ViewBag.Showtimes = showtimes;
        return View(movie);
    }

    // GET: /Movies/NowShowing
    public async Task<IActionResult> NowShowing()
    {
        var movies = await _unitOfWork.Movies.FindAsync(m => m.Status == MovieStatus.NowShowing);
        ViewBag.CurrentStatus = "NowShowing";
        return View("Index", movies.OrderByDescending(m => m.ReleaseDate));
    }

    // GET: /Movies/ComingSoon
    public async Task<IActionResult> ComingSoon()
    {
        var movies = await _unitOfWork.Movies.FindAsync(m => m.Status == MovieStatus.ComingSoon);
        ViewBag.CurrentStatus = "ComingSoon";
        return View("Index", movies.OrderByDescending(m => m.ReleaseDate));
    }
}
