using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Data;
using BE.Core.Entities.Movies;
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
    public async Task<IActionResult> Index(int pageNumber = 1, string? search = null,
                                           int? filterStar = null, string? filterReply = null)
    {
        int pageSize = 15;

        var query = _context.MovieReviews
            .Include(r => r.Movie)
            .Include(r => r.User)
            .AsQueryable();

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r =>
                (r.Movie != null && r.Movie.Title.ToLower().Contains(s)) ||
                (r.User != null && r.User.FullName.ToLower().Contains(s)) ||
                r.Comment.ToLower().Contains(s));
        }

        // Filter by star rating (1–5)
        if (filterStar.HasValue && filterStar >= 1 && filterStar <= 5)
        {
            query = query.Where(r => r.Rating == filterStar.Value);
        }

        // Filter by reply status
        if (filterReply == "pending")
        {
            query = query.Where(r => r.Response == null || r.Response == "");
        }
        else if (filterReply == "replied")
        {
            query = query.Where(r => r.Response != null && r.Response != "");
        }

        ViewBag.Search = search;
        ViewBag.FilterStar = filterStar;
        ViewBag.FilterReply = filterReply;

        var sortedReviews = query.OrderByDescending(r => r.CreatedAt);
        return View(PaginatedList<MovieReview>.Create(sortedReviews, pageNumber, pageSize));
    }

    // POST: /Admin/Reviews/Reply
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
}
