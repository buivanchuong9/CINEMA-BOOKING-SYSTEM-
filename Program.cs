using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BE.Data;
using BE.Core.Entities.Business;
using BE.Application.Services;
using BE.Infrastructure.Filters;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. DATABASE CONFIGURATION =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Server=localhost;Database=CinemaBooking_DB;User Id=sa;Password=123456aA@$;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ===== CHATBOT SERVICES =====
builder.Services.AddHttpClient(); // đăng ký IHttpClientFactory
builder.Services.AddScoped<BE.Infrastructure.Services.CinemaDataService>();
builder.Services.AddScoped<BE.Services.GeminiChatService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var db = sp.GetRequiredService<AppDbContext>();
    var cinemaData = sp.GetRequiredService<BE.Infrastructure.Services.CinemaDataService>();
    var logger = sp.GetRequiredService<ILogger<BE.Services.GeminiChatService>>();
    return new BE.Services.GeminiChatService(httpClient, config, db, cinemaData, logger);
});

// ===== SITE SETTINGS SERVICE =====
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISiteSettingsService, SiteSettingsService>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SiteSettingsFilter>();
});

// ===== 2. IDENTITY CONFIGURATION =====
builder.Services.AddIdentity<User, IdentityRole>(options => // 
{
    // Cài đặt mật khẩu
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Cài đặt khóa tài khoản
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // Cài đặt người dùng
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ===== 3. CẤU HÌNH REDIS =====
var redisConnection = builder.Configuration.GetConnectionString("Redis") 
                      ?? "localhost:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "CineMax_";
});

// ===== 4. CẤU HÌNH SIGNALR =====
builder.Services.AddSignalR();

// ===== 5. DEPENDENCY INJECTION - REPOSITORIES & SERVICES =====
builder.Services.AddScoped<BE.Core.Interfaces.IUnitOfWork, BE.Infrastructure.Repositories.UnitOfWork>();
builder.Services.AddScoped<BE.Core.Interfaces.Services.IRedisService, BE.Infrastructure.Caching.RedisService>();
builder.Services.AddScoped<BE.Core.Interfaces.Services.IBookingService, BE.Application.Services.BookingService>();
builder.Services.AddScoped<BE.Core.Interfaces.Services.IEmailService, BE.Infrastructure.Services.EmailService>();


// Cổng thanh toán VietQR
builder.Services.AddScoped<BE.Infrastructure.Payment.VietQRHelper>(sp =>
{
    var config = new BE.Infrastructure.Payment.VietQRConfig
    {
        BankId = builder.Configuration["VietQr:BankId"] ?? "VPB",
        AccountNo = builder.Configuration["VietQr:AccountNo"] ?? "0964578206",
        AccountName = builder.Configuration["VietQr:AccountName"] ?? "DAO VAN DUONG",
        Template = builder.Configuration["VietQr:Template"] ?? "compact2",
        Username = builder.Configuration["VietQr:Username"] ?? "vietqr_user",
        Password = builder.Configuration["VietQr:Password"] ?? "vietqr_password_123"
    };
    return new BE.Infrastructure.Payment.VietQRHelper(config);
});

// ===== 6. RAZOR PAGES =====
builder.Services.AddRazorPages();

// Cấu hình Antiforgery cho JSON requests
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// ===== 7. CẤU HÌNH SESSION (for cart, temp data) =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// (already registered above with filters)

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // QUAN TRỌNG: Phải đặt trước UseAuthorization
app.UseAuthorization();

app.UseSession();

// Custom Middleware to restrict Staff and Admin users to their respective areas
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path.Value ?? "";
        bool isAdmin = context.User.IsInRole("Admin");
        bool isStaff = context.User.IsInRole("Staff") && !isAdmin; // Admin has priority

        // Allow basic system/logout operations, API calls, and static files/assets
        bool isSystemAllowed = path.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/Identity/", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/seatHub", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase) ||
                               path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase) ||
                               path.Contains(".");

        if (!isSystemAllowed)
        {
            if (isAdmin)
            {
                // Admin must stay in /Admin
                if (!path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/Admin/Dashboard");
                    return;
                }
            }
            else if (isStaff)
            {
                // Staff must stay in /Staff/Booking
                if (!path.StartsWith("/Staff/Booking", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/Staff/Booking");
                    return;
                }
            }
        }
    }
    await next();
});

// ===== ROUTE CONFIGURATION =====
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // For Identity UI

// ===== SIGNALR HUB MAPPING =====
app.MapHub<BE.Web.Hubs.SeatHub>("/seatHub");

// ===== SEED DATABASE =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Seed roles and admin user
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await BE.Infrastructure.Data.DbSeeder.SeedRolesAndAdminAsync(roleManager, userManager);
        
        // Seed sample data (movies, cinemas, showtimes, etc.)
        var context = services.GetRequiredService<AppDbContext>();
        await BE.Data.DbInitializer.SeedAsync(context);

        // Đồng bộ điểm đánh giá thật từ review, xóa hết điểm ảo/mock data cũ
        var allMovies = await context.Movies.ToListAsync();
        foreach (var m in allMovies)
        {
            var reviews = await context.MovieReviews.Where(r => r.MovieId == m.Id).ToListAsync();
            if (reviews.Any())
            {
                double avgStars = reviews.Average(r => r.Rating);
                m.Rating = (decimal)Math.Round(avgStars, 1);
            }
            else
            {
                m.Rating = null; // Trả về null (0.0 ở view) nếu chưa có đánh giá nào
            }
        }
        await context.SaveChangesAsync();
        
        // Cập nhật showtimes cũ sang tương lai
        await BE.Infrastructure.Data.DbSeeder.UpdateShowtimesToFutureAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();