using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Data;
using BE.Core.Entities.Movies;
using BE.Core.Entities.Business;
using System.Security.Claims;
using BE.Application.Helpers;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/Reviews
    public async Task<IActionResult> Index(int pageNumber = 1, string? search = null)
    {
        int pageSize = 15;
        
        var query = _context.MovieReviews
            .Include(r => r.Movie)
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r => 
                (r.Movie != null && r.Movie.Title.ToLower().Contains(s)) ||
                (r.User != null && r.User.FullName.ToLower().Contains(s)) ||
                (r.Comment.ToLower().Contains(s))
            );
        }

        ViewBag.Search = search;
        var sortedReviews = query.OrderByDescending(r => r.CreatedAt);
        return View(PaginatedList<MovieReview>.Create(sortedReviews, pageNumber, pageSize));
    }

    // POST: /Admin/Reviews/Reply/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string responseText)
    {
        var review = await _context.MovieReviews.FindAsync(id);
        if (review == null)
        {
            TempData["Error"] = "Không tìm thấy đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            TempData["Error"] = "Nội dung phản hồi không được để trống.";
            return RedirectToAction(nameof(Index));
        }

        review.Response = responseText.Trim();
        review.RespondedAt = DateTime.Now;
        review.RespondedBy = User.Identity?.Name ?? "Admin";

        _context.MovieReviews.Update(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã gửi phản hồi thành công!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/Reviews/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.MovieReviews.FindAsync(id);
        if (review == null)
        {
            TempData["Error"] = "Không tìm thấy đánh giá để xóa.";
            return RedirectToAction(nameof(Index));
        }

        int movieId = review.MovieId;
        _context.MovieReviews.Remove(review);
        await _context.SaveChangesAsync();

        // Recalculate average rating of the movie
        var movie = await _context.Movies.FindAsync(movieId);
        if (movie != null)
        {
            var remainingReviews = await _context.MovieReviews
                .Where(r => r.MovieId == movieId)
                .ToListAsync();

            if (remainingReviews.Any())
            {
                double avgStars = remainingReviews.Average(r => r.Rating);
                movie.Rating = (decimal)Math.Round(avgStars, 1);
            }
            else
            {
                movie.Rating = null; // or 0
            }

            _context.Movies.Update(movie);
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Đã xóa đánh giá thành công!";
        return RedirectToAction(nameof(Index));
    }
}
