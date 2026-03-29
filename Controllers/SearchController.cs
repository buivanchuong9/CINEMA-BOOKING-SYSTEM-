using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using System.Text.Json;

namespace BE.Controllers;

public class SearchController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IUnitOfWork unitOfWork, ILogger<SearchController> logger) 
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// API endpoint for global search - Returns JSON for AJAX calls
    /// </summary>
    [HttpGet("/api/search")]
    public async Task<IActionResult> Search([FromQuery] string q) // tìm kiếm phim và rạp theo tên 
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)   
            { // kiểm tra q có rỗng hoặc nhỏ hơn 2 ký tự
                return Json(new List<object>());
            }

            var query = q.ToLower().Trim(); // chuyển q sang chữ thường và loại bỏ khoảng trắng
            var results = new List<object>(); 

            // Search Movies
            var movies = await _unitOfWork.Movies.GetAllAsync(); // lấy tất cả phim
            var matchedMovies = movies 
                .Where(m => m.Title.ToLower().Contains(query)) // tìm kiếm phim theo tên
                .Take(5) // lấy 5 phim
                .Select(m => new
                {
                    type = "movie",
                    title = m.Title,
                    id = m.Id
                });
            results.AddRange(matchedMovies); // thêm phim tìm thấy vào danh sách kết quả

            // Search Cinemas
            var cinemas = await _unitOfWork.Cinemas.GetAllAsync(); // lấy tất cả rạp
            var matchedCinemas = cinemas // tìm kiếm rạp theo tên hoặc địa chỉ
                .Where(c => c.Name.ToLower().Contains(query) || 
                           (c.Address != null && c.Address.ToLower().Contains(query))) // tìm kiếm rạp theo tên hoặc địa chỉ
                .Take(3)
                .Select(c => new
                {
                    type = "cinema",
                    title = c.Name,
                    id = c.Id
                });
            results.AddRange(matchedCinemas); // thêm rạp tìm thấy vào danh sách kết quả

            return Json(results); // trả về danh sách kết quả tìm kiếm
        }
        catch (Exception ex)
        { // xử lý lỗi
            _logger.LogError(ex, "Error during search for query: {Query}", q);
            return Json(new List<object>());
        }
    }
}
