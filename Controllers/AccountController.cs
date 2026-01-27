using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BE.Core.Entities.Business;
using BE.Application.DTOs;

namespace BE.Controllers;

/// <summary>
/// Account Controller - Xử lý đăng ký, đăng nhập, đăng xuất
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly BE.Core.Interfaces.IUnitOfWork _unitOfWork;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        BE.Core.Interfaces.IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            MembershipLevel = "Bronze",
            Points = 0,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Gán role Customer cho user mới
            await _userManager.AddToRoleAsync(user, "Customer");

            // Tự động đăng nhập sau khi đăng ký
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "Đăng ký thành công! Chào mừng bạn đến với CineMax.";
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Update last login time
                user.LastLoginAt = DateTime.Now;
                await _userManager.UpdateAsync(user);

                // Check if user is admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                TempData["Success"] = $"Chào mừng {user.FullName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirect based on role
                if (isAdmin)
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Home");
            }
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng thử lại sau.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
        }

        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Đăng xuất thành công!";
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Profile
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        // Load active vouchers
        ViewBag.Vouchers = (await _unitOfWork.Vouchers.GetAllAsync())
            .Where(v => v.UserId == user.Id && v.IsActive && v.ExpiryDate > DateTime.Now && v.UsedCount < (v.UsageLimit ?? 1))
            .OrderByDescending(v => v.CreatedAt)
            .ToList();

        return View(user);
    }

    // POST: /Account/RedeemVoucher
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedeemVoucher()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        const int VOUCHER_COST = 1000;

        if (user.Points < VOUCHER_COST)
        {
            TempData["Error"] = $"Bạn cần tích lũy đủ {VOUCHER_COST} điểm để đổi voucher!";
            return RedirectToAction("Profile");
        }

        // Deduct points
        user.Points -= VOUCHER_COST;
        await _userManager.UpdateAsync(user);

        // Randomize discount 30-70%
        var rnd = new Random();
        int discount = rnd.Next(30, 71); // 30 to 70

        // Create Voucher
        var voucher = new Voucher
        {
            Code = $"REWARD-{DateTime.Now.Ticks.ToString().Substring(10)}-{discount}",
            Name = $"Voucher Ưu Đãi {discount}%",
            Description = "Voucher đổi từ điểm tích lũy",
            UserId = user.Id,
            DiscountPercent = discount,
            MaxAmount = 100000, // Max 100k
            MinOrderAmount = 0,
            StartDate = DateTime.Now,
            ExpiryDate = DateTime.Now.AddDays(30),
            IsActive = true,
            UsageLimit = 1,
            UsedCount = 0,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Vouchers.AddAsync(voucher);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Chúc mừng! Bạn nhận được voucher giảm {discount}% (Code: {voucher.Code})";
        return RedirectToAction("Profile");
    }
}
