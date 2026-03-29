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
    private readonly RoleManager<IdentityRole> _roleManager; // quản lý phân quyền
    private readonly BE.Core.Interfaces.IUnitOfWork _unitOfWork; // quản lý repository

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
    public IActionResult Register() // hiển thị form đăng ký
    {
        if (User.Identity?.IsAuthenticated == true) // kiểm tra đã đăng nhập chưa
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model) // xử lý đăng ký
    {
        if (!ModelState.IsValid) // kiểm tra model có hợp lệ không
        {
            return View(model);
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            MembershipLevel = "Bronze", // cấp bậc thành viên
            Points = 0,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password); // tạo user mới

        if (result.Succeeded) // nếu tạo user thành công
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
            ModelState.AddModelError(string.Empty, error.Description); // hiển thị lỗi
        }

        return View(model);
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) // hiển thị form đăng nhập
    {
        if (User.Identity?.IsAuthenticated == true) // kiểm tra đã đăng nhập chưa
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null) // xử lý đăng nhập 
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid) // kiểm tra model có hợp lệ không
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync( // đăng nhập
            model.Email, 
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false // không khóa tài khoản khi đăng nhập thất bại
        );

        if (result.Succeeded) // nếu đăng nhập thành công
        {
            var user = await _userManager.FindByEmailAsync(model.Email); // lấy user
            if (user != null) // nếu user tồn tại
            {
                // Cập nhật lần đăng nhập gần đây nhất
                user.LastLoginAt = DateTime.Now;
                await _userManager.UpdateAsync(user);

                // Kiểm tra xem user có phải là admin không
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                TempData["Success"] = $"Chào mừng {user.FullName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) // kiểm tra returnUrl có hợp lệ không
                {
                    return Redirect(returnUrl); // chuyển hướng đến returnUrl
                }

                // Chuyển hướng dựa trên vai trò
                if (isAdmin) // nếu là admin
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" }); // chuyển hướng đến trang admin
                }

                return RedirectToAction("Index", "Home");
            }
        }

        if (result.IsLockedOut) // nếu tài khoản bị khóa
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng thử lại sau.");
        }
        else // nếu tài khoản không tồn tại
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
    public async Task<IActionResult> Profile() // hiển thị thông tin cá nhân
    {
        if (!User.Identity?.IsAuthenticated ?? true) // kiểm tra đã đăng nhập chưa
        {
            return RedirectToAction("Login"); // chuyển hướng đến trang đăng nhập
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        // Load active vouchers
        ViewBag.Vouchers = (await _unitOfWork.Vouchers.GetAllAsync())
            // lọc voucher của user, còn hạn sử dụng và chưa hết lượt sử dụng
            .Where(v => v.UserId == user.Id && v.IsActive && v.ExpiryDate > DateTime.Now && v.UsedCount < (v.UsageLimit ?? 1))
            .OrderByDescending(v => v.CreatedAt) // sắp xếp giảm dần theo ngày tạo
            .ToList();

        return View(user);
    }

    // POST: /Account/RedeemVoucher
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedeemVoucher() // đổi voucher
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login"); // nếu user không tồn tại thì chuyển hướng đến trang đăng nhập

        const int VOUCHER_COST = 1000; // số điểm cần để đổi voucher

        if (user.Points < VOUCHER_COST) // nếu user không đủ điểm
        {
            TempData["Error"] = $"Bạn cần tích lũy đủ {VOUCHER_COST} điểm để đổi voucher!";
            return RedirectToAction("Profile");
        }

        // Deduct points
        user.Points -= VOUCHER_COST; // trừ điểm
        await _userManager.UpdateAsync(user); // cập nhật user

        // Randomize discount 30-70%
        var rnd = new Random(); 
        int discount = rnd.Next(30, 71); // 30 to 70

        // Create Voucher
        var voucher = new Voucher
        {
            Code = $"REWARD-{DateTime.Now.Ticks.ToString().Substring(10)}-{discount}", // tạo mã voucher
            Name = $"Voucher Ưu Đãi {discount}%", // tên voucher
            Description = "Voucher đổi từ điểm tích lũy", // mô tả voucher
            UserId = user.Id, // user sở hữu voucher
            DiscountPercent = discount, // phần trăm giảm giá
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
