# 🎬 CINEMA BOOKING SYSTEM

Hệ thống đặt vé xem phim trực tuyến với ASP.NET Core MVC

## 📋 YÊU CẦU HỆ THỐNG

- **.NET 8.0 SDK** trở lên
- **SQL Server 2019** trở lên (hoặc SQL Server Express)
- **Redis Server** (localhost:6379)
- **Visual Studio 2022** hoặc **VS Code** hoặc **JetBrains Rider**

## 🚀 HƯỚNG DẪN CÀI ĐẶT

### Bước 1: Clone Repository

```bash
git clone https://github.com/your-username/CINEMA-BOOKING-SYSTEM.git
cd CINEMA-BOOKING-SYSTEM
```

### Bước 2: Cấu hình Database

Mở file `appsettings.json` và sửa connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CinemaBooking_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
    "Redis": "localhost:6379"
  }
}
```

**Ví dụ:**
- **Windows Authentication**: `Server=localhost;Database=CinemaBooking_DB;Trusted_Connection=True;TrustServerCertificate=True;`
- **SQL Server Authentication**: `Server=localhost;Database=CinemaBooking_DB;User Id=sa;Password=123456aA@$;TrustServerCertificate=True;`

### Bước 3: Cài đặt Dependencies

```bash
dotnet restore
```

### Bước 4: Tạo Database và Seed Data

```bash
# Tạo database từ migrations
dotnet ef database update

# Hoặc nếu chưa có migrations:
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Lưu ý:** Khi chạy lần đầu, hệ thống sẽ tự động:
- ✅ Tạo database schema
- ✅ Seed roles (Admin, User)
- ✅ Tạo tài khoản Admin mặc định
- ✅ Seed dữ liệu mẫu (phim, rạp, lịch chiếu, đồ ăn)

### Bước 5: Cài đặt Redis (Bắt buộc)

**Windows:**
```bash
# Download Redis for Windows
# https://github.com/microsoftarchive/redis/releases

# Hoặc dùng Docker
docker run -d -p 6379:6379 redis
```

**macOS:**
```bash
brew install redis
brew services start redis
```

**Linux:**
```bash
sudo apt-get install redis-server
sudo systemctl start redis
```

### Bước 6: Chạy Ứng Dụng

```bash
dotnet run
```

Hoặc trong Visual Studio: nhấn **F5**

Ứng dụng sẽ chạy tại: `https://localhost:5293`

## 👤 TÀI KHOẢN MẶC ĐỊNH

### Admin
- **Email**: `admin@cinemax.com`
- **Password**: `Admin@123`
- **URL**: `https://localhost:5293/Admin/Dashboard`

### User (Tự đăng ký)
- Truy cập `/Account/Register` để tạo tài khoản mới

## 📁 CẤU TRÚC DỰ ÁN

```
CINEMA-BOOKING-SYSTEM/
├── Areas/
│   └── Admin/              # Admin area (CRUD phim, rạp, lịch chiếu)
├── Controllers/            # MVC Controllers
├── Views/                  # Razor Views
├── wwwroot/               # Static files (CSS, JS, images)
├── Core/
│   ├── Entities/          # Domain models
│   ├── Enums/             # Enums
│   └── Interfaces/        # Interfaces
├── Application/
│   └── Services/          # Business logic
├── Infrastructure/
│   ├── Caching/           # Redis service
│   ├── Payment/           # VNPay integration
│   └── Repositories/      # Data access
└── Data/
    ├── AppDbContext.cs    # EF Core DbContext
    └── DbInitializer.cs   # Seed data
```

## 🎯 TÍNH NĂNG CHÍNH

### User (Khách hàng)
- ✅ Xem danh sách phim (Đang chiếu, Sắp chiếu)
- ✅ Xem chi tiết phim, trailer
- ✅ Chọn lịch chiếu, rạp, phòng
- ✅ Chọn ghế (real-time seat status với Redis)
- ✅ Chọn đồ ăn, nước uống
- ✅ Thanh toán VNPay / Test Payment
- ✅ Xem danh sách vé đã đặt
- ✅ Hủy vé
- ✅ Tải QR code vé

### Admin
- ✅ Dashboard thống kê
- ✅ Quản lý phim (CRUD)
- ✅ Quản lý rạp chiếu (CRUD)
- ✅ Quản lý phòng chiếu (CRUD)
- ✅ Quản lý lịch chiếu (CRUD)
- ✅ Quản lý thể loại phim (CRUD)
- ✅ Quản lý đồ ăn (CRUD)

## 🔧 CÔNG NGHỆ SỬ DỤNG

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server + Entity Framework Core
- **Caching**: Redis (StackExchange.Redis)
- **Authentication**: ASP.NET Core Identity
- **Payment**: VNPay Gateway
- **Frontend**: Bootstrap 5, jQuery, Swiper.js, AOS
- **Real-time**: SignalR (seat status updates)

## 📝 MIGRATIONS

### Tạo migration mới
```bash
dotnet ef migrations add MigrationName
```

### Apply migrations
```bash
dotnet ef database update
```

### Rollback migration
```bash
dotnet ef database update PreviousMigrationName
```

### Xóa database và tạo lại
```bash
dotnet ef database drop
dotnet ef database update
```

## 🐛 TROUBLESHOOTING

### Lỗi: "Cannot connect to SQL Server"
- Kiểm tra SQL Server đang chạy
- Kiểm tra connection string trong `appsettings.json`
- Thử dùng Windows Authentication thay vì SQL Authentication

### Lỗi: "Redis connection failed"
- Kiểm tra Redis đang chạy: `redis-cli ping` (phải trả về `PONG`)
- Nếu dùng Docker: `docker ps` để xem Redis container

### Lỗi: "No migrations found"
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Lỗi: "Seat already held by another user"
- Đây là tính năng bảo vệ ghế! Ghế đang được giữ bởi user khác trong 15 phút
- Chờ 15 phút hoặc chọn ghế khác

## 📞 HỖ TRỢ

Nếu gặp vấn đề, vui lòng tạo issue trên GitHub hoặc liên hệ:
- **Email**: buivanchuong91510@gmail.com
- **GitHub Issues**: https://github.com/your-username/CINEMA-BOOKING-SYSTEM/issues
---

**Developed with ❤️ by Bùi Văn Chương**
