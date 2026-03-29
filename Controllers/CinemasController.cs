using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;

namespace BE.Controllers;

/// <summary>
/// Cinemas Controller - Danh sách rạp chiếu phim
/// </summary>
public class CinemasController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CinemasController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Cinemas
    public async Task<IActionResult> Index()
    {
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        return View(cinemas.Where(c => c.IsActive).OrderBy(c => c.Name));
    }

    // GET: /Cinemas/Details/5 (id = 5)
    public async Task<IActionResult> Details(int id) // hiển thị chi tiết rạp chiếu phim
    {
        var cinema = await _unitOfWork.Cinemas.GetByIdAsync(id);
        
        if (cinema == null) // nếu không tìm thấy rạp chiếu phim
        {
            return NotFound();
        }

        // TODO: Load rooms và showtimes cho rạp này
        return View(cinema);
    }
}
