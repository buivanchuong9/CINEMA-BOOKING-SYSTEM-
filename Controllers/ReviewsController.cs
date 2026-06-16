using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Data;
using BE.Core.Interfaces;
using BE.Core.Entities.Movies;
using BE.Core.Entities.Business;

namespace BE.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewsController(AppDbContext context, UserManager<User> userManager, IUnitOfWork unitOfWork)
    {
        _context = context;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int movieId, int rating, string comment)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Vui lòng đăng nhập để đánh giá.";
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        var movie = await _context.Movies.FindAsync(movieId);
        if (movie == null)
        {
            return NotFound();
        }

        // Validate rating (1 - 5 stars)
        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = "Đánh giá không hợp lệ. Vui lòng chọn số sao từ 1 đến 5.";
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "Vui lòng nhập nội dung đánh giá.";
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        // Check if user has already reviewed this movie
        var existingReview = await _context.MovieReviews
            .AnyAsync(r => r.MovieId == movieId && r.UserId == userId);

        if (existingReview)
        {
            TempData["Error"] = "Bạn đã đánh giá phim này rồi và không thể sửa đổi.";
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        var review = new MovieReview
        {
            MovieId = movieId,
            UserId = userId,
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = DateTime.Now
        };

        await _context.MovieReviews.AddAsync(review);
        await _context.SaveChangesAsync();

        // Recalculate average rating of the movie
        var allReviews = await _context.MovieReviews
            .Where(r => r.MovieId == movieId)
            .ToListAsync();

        if (allReviews.Any())
        {
            double avgStars = allReviews.Average(r => r.Rating);
            // Convert to 10-point scale (since our system has Rating out of 10)
            movie.Rating = (decimal)(avgStars * 2.0);
        }
        else
        {
            movie.Rating = 0;
        }

        _context.Movies.Update(movie);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cảm ơn bạn đã gửi đánh giá thành công!";
        return RedirectToAction("Details", "Movies", new { id = movieId });
    }
}
