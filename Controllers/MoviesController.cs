using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;
using BE.Core.Enums;
using BE.Application.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BE.Data;
using BE.Core.Entities.Business;

namespace BE.Controllers;

/// <summary>
/// Movies Controller - Quản lý danh sách phim
/// </summary>
public class MoviesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public MoviesController(IUnitOfWork unitOfWork, AppDbContext context, UserManager<User> userManager)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _userManager = userManager;
    }

    // GET: /Movies
    public async Task<IActionResult> Index(string? status, int pageNumber = 1)
    {
        int pageSize = 20;
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
        var sortedMovies = movies.OrderByDescending(m => m.ReleaseDate);
        return View(PaginatedList<Movie>.Create(sortedMovies, pageNumber, pageSize));
    }

    // GET: /Movies/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        
        if (movie == null)
        {
            return NotFound();
        }

        // Load showtimes với thông tin Room và Cinema (sử dụng Include)
        var showtimes = await _unitOfWork.Showtimes.GetShowtimesWithDetailsAsync(id);
        ViewBag.Showtimes = showtimes.ToList();

        // Load reviews và thông tin User
        var reviews = await _context.MovieReviews
            .Include(r => r.User)
            .Where(r => r.MovieId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        ViewBag.Reviews = reviews;

        // Kiểm tra xem user hiện tại đã đánh giá chưa
        bool hasReviewed = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                hasReviewed = await _context.MovieReviews.AnyAsync(r => r.MovieId == id && r.UserId == userId);
            }
        }
        ViewBag.HasReviewed = hasReviewed;

        return View(movie);
    }

    // GET: /Movies/NowShowing
    public async Task<IActionResult> NowShowing(int pageNumber = 1)
    {
        int pageSize = 20;
        var movies = await _unitOfWork.Movies.FindAsync(m => m.Status == MovieStatus.NowShowing); // lấy phim đang chiếu
        ViewBag.CurrentStatus = "NowShowing"; // gán trạng thái cho ViewBag
        var sortedMovies = movies.OrderByDescending(m => m.ReleaseDate);
        return View("Index", PaginatedList<Movie>.Create(sortedMovies, pageNumber, pageSize)); // trả về danh sách phim đang chiếu
    }

    // GET: /Movies/ComingSoon
    public async Task<IActionResult> ComingSoon(int pageNumber = 1)
    {
        int pageSize = 20;
        var movies = await _unitOfWork.Movies.FindAsync(m => m.Status == MovieStatus.ComingSoon); // lấy phim sắp chiếu
        ViewBag.CurrentStatus = "ComingSoon"; // gán trạng thái cho ViewBag
        var sortedMovies = movies.OrderByDescending(m => m.ReleaseDate);
        return View("Index", PaginatedList<Movie>.Create(sortedMovies, pageNumber, pageSize)); // trả về danh sách phim sắp chiếu
    }
}
