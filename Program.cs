using Microsoft.EntityFrameworkCore; // 1. Gọi thư viện EF Core
using BE.Data; // 2. Gọi thư mục chứa AppDbContext (Nếu namespace của bạn khác thì sửa lại nhé)

var builder = WebApplication.CreateBuilder(args);

// --- BẮT ĐẦU ĐOẠN CẤU HÌNH DATABASE ---

// Bước 1: Lấy chuỗi kết nối
// (Nó sẽ tìm trong file appsettings.json, nếu không thấy thì dùng chuỗi mặc định phía sau ??)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Server=localhost;Database=BE_Db;User Id=sa;Password=123456aA@$;TrustServerCertificate=True;";
// Bước 2: Đăng ký AppDbContext sử dụng SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- KẾT THÚC ĐOẠN CẤU HÌNH ---

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();