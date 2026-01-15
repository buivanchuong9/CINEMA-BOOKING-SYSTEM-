using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BE.Data;
using BE.Core.Entities.Business;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. DATABASE CONFIGURATION =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Server=localhost;Database=CinemaBooking_DB;User Id=sa;Password=123456aA@$;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ===== 2. IDENTITY CONFIGURATION =====
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ===== 3. REDIS CONFIGURATION =====
var redisConnection = builder.Configuration.GetConnectionString("Redis") 
                      ?? "localhost:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "CineMax_";
});

// ===== 4. SIGNALR CONFIGURATION =====
builder.Services.AddSignalR();

// ===== 5. DEPENDENCY INJECTION - REPOSITORIES & SERVICES =====
builder.Services.AddScoped<BE.Core.Interfaces.IUnitOfWork, BE.Infrastructure.Repositories.UnitOfWork>();
builder.Services.AddScoped<BE.Core.Interfaces.Services.IRedisService, BE.Infrastructure.Caching.RedisService>();
builder.Services.AddScoped<BE.Core.Interfaces.Services.IBookingService, BE.Application.Services.BookingService>();

// VNPay Payment Gateway
builder.Services.AddScoped<BE.Infrastructure.Payment.VNPayHelper>(sp =>
{
    var config = new BE.Infrastructure.Payment.VNPayConfig
    {
        TmnCode = builder.Configuration["Vnpay:TmnCode"] ?? "DEMO_TMN",
        HashSecret = builder.Configuration["Vnpay:HashSecret"] ?? "DEMO_SECRET",
        BaseUrl = builder.Configuration["Vnpay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
        ReturnUrl = builder.Configuration["Vnpay:ReturnUrl"] ?? "https://localhost:5001/Payment/VNPayReturn",
        IpnUrl = builder.Configuration["Vnpay:IpnUrl"] ?? "https://localhost:5001/Payment/VNPayIPN"
    };
    return new BE.Infrastructure.Payment.VNPayHelper(config);
});

// ===== 6. MVC & RAZOR PAGES =====
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ===== 6. SESSION CONFIGURATION (for cart, temp data) =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await BE.Infrastructure.Data.DbSeeder.SeedRolesAndAdminAsync(roleManager, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();