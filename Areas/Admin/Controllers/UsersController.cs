using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE.Data;
using BE.Core.Entities.Business;
using Microsoft.AspNetCore.Identity;

namespace BE.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public UsersController(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Admin/Users
    public async Task<IActionResult> Index(string? role, string? search, int page = 1)
    {
        if (role == "Admin")
        {
            return RedirectToAction(nameof(Profile));
        }

        ViewBag.CurrentRole = role;
        ViewBag.CurrentSearch = search;
        
        IEnumerable<User> usersQuery;
        
        if (string.IsNullOrEmpty(role))
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminIds = adminUsers.Select(u => u.Id).ToHashSet();
            usersQuery = (await _context.Users.ToListAsync()).Where(u => !adminIds.Contains(u.Id));
            ViewData["Title"] = "Tất Cả Tài Khoản";
        }
        else
        {
            usersQuery = await _userManager.GetUsersInRoleAsync(role);
            if (role == "Customer") ViewData["Title"] = "Quản Lý Người Dùng";
            else if (role == "Staff") ViewData["Title"] = "Quản Lý Nhân Viên Bán Hàng";
            else ViewData["Title"] = $"Quản Lý {role}";
        }

        // Áp dụng tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            usersQuery = usersQuery.Where(u => 
                (u.FullName != null && u.FullName.ToLower().Contains(search)) ||
                (u.Email != null && u.Email.ToLower().Contains(search)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
            );
        }

        // Phân trang (20 items per page)
        int pageSize = 20;
        int totalItems = usersQuery.Count();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

        var pagedUsers = usersQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedUsers);
    }

    // GET: Admin/Users/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string FullName, string Email, string Password, string Role, string PhoneNumber)
    {
        if (ModelState.IsValid)
        {
            var user = new User
            {
                UserName = Email,
                Email = Email,
                FullName = FullName,
                PhoneNumber = PhoneNumber,
                MembershipLevel = "Bronze",
                Points = 0,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(Role))
                {
                    await _userManager.AddToRoleAsync(user, Role);
                }
                TempData["Success"] = "Thêm tài khoản thành công!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View();
    }

    // GET: Admin/Users/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return Forbid();
        }

        return View(user);
    }

    // POST: Admin/Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string FullName, string PhoneNumber, string MembershipLevel, int Points, string? NewPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return Forbid();
        }

        user.FullName = FullName;
        user.PhoneNumber = PhoneNumber;
        user.MembershipLevel = MembershipLevel;
        user.Points = Points;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded && !string.IsNullOrEmpty(NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            result = await _userManager.ResetPasswordAsync(user, token, NewPassword);
        }

        if (result.Succeeded)
        {
            TempData["Success"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(user);
    }

    // POST: Admin/Users/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return Forbid();
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Xóa tài khoản thành công!";
        }
        else
        {
            TempData["Error"] = "Lỗi khi xóa tài khoản!";
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Users/Profile
    public async Task<IActionResult> Profile()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        return View(user);
    }

    // POST: Admin/Users/Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string FullName, string PhoneNumber, string? CurrentPassword, string? NewPassword, string? ConfirmPassword)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.FullName = FullName;
        user.PhoneNumber = PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(user);
        }

        // Handle password change if requested
        if (!string.IsNullOrEmpty(NewPassword))
        {
            if (string.IsNullOrEmpty(CurrentPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập mật khẩu hiện tại để đổi mật khẩu.");
                return View(user);
            }
            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu mới và mật khẩu xác nhận không khớp.");
                return View(user);
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(user);
            }
        }

        TempData["Success"] = "Cập nhật hồ sơ cá nhân thành công!";
        return RedirectToAction(nameof(Profile));
    }
}
