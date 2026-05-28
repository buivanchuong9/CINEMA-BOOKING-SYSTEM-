using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;
using BE.Application.Helpers;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MoviesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public MoviesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/Movies
    public async Task<IActionResult> Index(int pageNumber = 1) // danh sách phim
    {
        int pageSize = 20;
        var movies = await _unitOfWork.Movies.GetAllAsync();
        var sortedMovies = movies.OrderByDescending(m => m.CreatedAt);
        
        return View(PaginatedList<Movie>.Create(sortedMovies, pageNumber, pageSize)); // sắp xếp theo ngày tạo và phân trang
    }

    // GET: /Admin/Movies/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Admin/Movies/Create
    [HttpPost]
    [ValidateAntiForgeryToken] // chống hack CSRF
    public async Task<IActionResult> Create(Movie movie, bool forceCreate = false)
    {
        try
        {
            // xóa các trường không bắt buộc
            ModelState.Remove("PosterUrl"); 
            ModelState.Remove("TrailerUrl");
            ModelState.Remove("Description"); // xóa mô tả
            ModelState.Remove("AgeRating"); // xóa đánh giá tuổi
            ModelState.Remove("Rating"); // xóa đánh giá
            ModelState.Remove("Director"); // xóa đạo diễn
            ModelState.Remove("Cast"); // xóa diễn viên
            ModelState.Remove("CreatedAt"); // xóa ngày tạo
            ModelState.Remove("IsActive"); // xóa trạng thái hoạt động

            if (!ModelState.IsValid) 
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                return View(movie);
            }

            // Chỉ kiểm tra trùng lặp nếu người dùng không chọn Force Create
            if (!string.IsNullOrEmpty(movie.Title) && !forceCreate)
            {
                var movies = await _unitOfWork.Movies.GetAllAsync();
                
                // 1. Kiểm tra trùng tên tuyệt đối (100%)
                var exactDuplicate = movies.FirstOrDefault(m => 
                    m.Title != null && 
                    m.Title.Trim().Equals(movie.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (exactDuplicate != null)
                {
                    TempData["DuplicateMovieId"] = exactDuplicate.Id;
                    TempData["DuplicateMovieTitle"] = exactDuplicate.Title;
                    TempData["Error"] = $"Phim '{movie.Title}' đã tồn tại trong hệ thống!";
                    return View(movie);
                }

                // 2. Kiểm tra trùng trên 80% (Levenshtein Distance)
                Movie? similarMovie = null;
                double maxSimilarity = 0;

                foreach (var m in movies)
                {
                    if (m.Title != null)
                    {
                        double similarity = CalculateSimilarity(movie.Title, m.Title);
                        if (similarity >= 0.80 && similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                            similarMovie = m;
                        }
                    }
                }

                if (similarMovie != null)
                {
                    TempData["SimilarMovieTitle"] = similarMovie.Title;
                    TempData["SimilarMoviePercent"] = (int)(maxSimilarity * 100);
                    TempData["IsSimilarWarning"] = true;
                    TempData["Error"] = $"Tên phim gần giống với phim '{similarMovie.Title}' đã có sẵn (trùng {(int)(maxSimilarity * 100)}%)!";
                    return View(movie);
                }
            }

            movie.CreatedAt = DateTime.Now; // gán ngày tạo phim 
            await _unitOfWork.Movies.AddAsync(movie); 
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm phim '{movie.Title}' thành công!";
            return RedirectToAction(nameof(Index)); // chuyển sang trang danh sách phim
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return View(movie);
        }
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private static double CalculateSimilarity(string s, string t)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t)) return 0;
        s = s.ToLower().Trim();
        t = t.ToLower().Trim();
        int distance = LevenshteinDistance(s, t);
        return 1.0 - ((double)distance / Math.Max(s.Length, t.Length));
    }

    // GET: /Admin/Movies/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        if (movie == null)
        {
            return NotFound();
        }
        return View(movie);
    }

    // POST: /Admin/Movies/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Movie movie)
    {
        if (id != movie.Id)
        {
            TempData["Error"] = "ID không khớp!";
            return NotFound();
        }

        try
        {
            // Remove optional fields
            ModelState.Remove("PosterUrl"); 
            ModelState.Remove("TrailerUrl");
            ModelState.Remove("Description"); 
            ModelState.Remove("AgeRating");
            ModelState.Remove("Rating");
            ModelState.Remove("Director");
            ModelState.Remove("Cast");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Vui lòng kiểm tra lại: " + string.Join(", ", errors);
                return View(movie);
            }

            // Update movie
            _unitOfWork.Movies.Update(movie);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật phim '{movie.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi cập nhật: {ex.Message}";
            return View(movie);
        }
    }

    // POST: /Admin/Movies/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        _unitOfWork.Movies.Delete(movie);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa phim '{movie.Title}' thành công!";
        return RedirectToAction(nameof(Index));
    }
}
