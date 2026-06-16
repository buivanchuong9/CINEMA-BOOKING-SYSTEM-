using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE.Areas.Admin.Controllers;

/// <summary>
/// Controller xử lý upload ảnh chung cho Admin area (Phim, Đồ ăn, v.v.)
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UploadController : Controller
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Upload ảnh qua AJAX, trả về URL đã lưu.
    /// POST: /Admin/Upload/Image?folder=movies
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Image(IFormFile file, string folder = "general")
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Vui lòng chọn file ảnh." });
        }

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return Json(new { success = false, message = "File ảnh không được vượt quá 5MB." });
        }

        // Validate file extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowed.Contains(ext))
        {
            return Json(new { success = false, message = "Chỉ chấp nhận file ảnh: JPG, PNG, WEBP, GIF." });
        }

        try
        {
            // Sanitize folder name
            folder = folder.ToLowerInvariant().Replace("..", "").Replace("/", "").Replace("\\", "");
            
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/{folder}/{fileName}";
            return Json(new { success = true, url = url });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi khi upload: {ex.Message}" });
        }
    }
}
