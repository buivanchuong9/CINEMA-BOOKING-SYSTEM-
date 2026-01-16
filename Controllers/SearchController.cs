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
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new List<object>());
            }

            var query = q.ToLower().Trim();
            var results = new List<object>();

            // Search Movies
            var movies = await _unitOfWork.Movies.GetAllAsync();
            var matchedMovies = movies
                .Where(m => m.Title.ToLower().Contains(query))
                .Take(5)
                .Select(m => new
                {
                    type = "movie",
                    title = m.Title,
                    id = m.Id
                });
            results.AddRange(matchedMovies);

            // Search Cinemas
            var cinemas = await _unitOfWork.Cinemas.GetAllAsync();
            var matchedCinemas = cinemas
                .Where(c => c.Name.ToLower().Contains(query) || 
                           (c.Address != null && c.Address.ToLower().Contains(query)))
                .Take(3)
                .Select(c => new
                {
                    type = "cinema",
                    title = c.Name,
                    id = c.Id
                });
            results.AddRange(matchedCinemas);

            return Json(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search for query: {Query}", q);
            return Json(new List<object>());
        }
    }
}
