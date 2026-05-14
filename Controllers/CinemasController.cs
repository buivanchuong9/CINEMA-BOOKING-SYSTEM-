using Microsoft.AspNetCore.Mvc;
using BE.Core.Interfaces;
using BE.Application.Helpers;
using BE.Core.Entities.CinemaInfrastructure;

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
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        int pageSize = 20;
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
        var sortedCinemas = cinemas.Where(c => c.IsActive).OrderBy(c => c.Name);
        return View(PaginatedList<Cinema>.Create(sortedCinemas, pageNumber, pageSize));
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
