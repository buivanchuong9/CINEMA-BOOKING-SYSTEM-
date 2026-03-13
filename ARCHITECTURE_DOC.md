# TÀI LIỆU KIẾN TRÚC DỰ ÁN — CINEMA BOOKING SYSTEM

> **Mục đích:** Tài liệu chuẩn bị cho buổi vấn đáp 1:1 về kiến trúc và code.

---

## 1. KIẾN TRÚC TỔNG QUAN — KHÔNG PHẢI MVC THUẦN

Dự án **KHÔNG** theo mô hình MVC thuần (Model-View-Controller). Đây là mô hình **Layered Architecture (Kiến trúc phân tầng)** kết hợp nhiều design pattern:

```
┌─────────────────────────────────────────────────────────┐
│              PRESENTATION LAYER (Tầng trình bày)        │
│   Controllers/  +  Areas/Admin/  +  Views/  +  wwwroot/ │
└────────────────────────┬────────────────────────────────┘
                         │ gọi xuống
┌────────────────────────▼────────────────────────────────┐
│              APPLICATION LAYER (Tầng ứng dụng)          │
│         Application/Services/  +  Application/DTOs/     │
└────────────────────────┬────────────────────────────────┘
                         │ gọi xuống
┌────────────────────────▼────────────────────────────────┐
│                CORE LAYER (Tầng lõi / Domain)           │
│   Core/Entities/  +  Core/Interfaces/  +  Core/Enums/   │
└───────────┬────────────────────────────┬────────────────┘
            │ implement                  │ implement
┌───────────▼────────────┐  ┌───────────▼────────────────┐
│  INFRASTRUCTURE LAYER  │  │       DATA LAYER           │
│ Infrastructure/        │  │   Data/AppDbContext.cs      │
│  - Repositories/       │  │   Data/Migrations/         │
│  - Caching/ (Redis)    │  │   Data/DbInitializer.cs    │
│  - Payment/ (VNPay)    │  └────────────────────────────┘
└────────────────────────┘
```

### So sánh với MVC thuần:

| Mô hình MVC thuần | Dự án này |
|---|---|
| Model = Entity + Business logic | Entity (Core) tách riêng khỏi Business logic |
| Controller gọi thẳng DB | Controller → Service → Repository → DB |
| Không có layer phân tách | 4 tầng rõ ràng: Presentation, Application, Core, Infrastructure |
| Không có interface | Mọi dependency đều qua Interface (Dependency Inversion) |

---

## 2. CÁC DESIGN PATTERN ĐƯỢC SỬ DỤNG

### 2.1 Repository Pattern
**Vị trí:** `Core/Interfaces/IRepository.cs` + `Infrastructure/Repositories/GenericRepository.cs`

**Mục đích:** Tách biệt logic business khỏi tầng truy cập dữ liệu (EF Core). Controller/Service không bao giờ gọi `DbContext` trực tiếp.

```csharp
// Interface (Core layer - không phụ thuộc EF Core)
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}

// Implementation (Infrastructure layer - có EF Core)
public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;
    // ...
}
```

**Câu hỏi thầy hay hỏi:**
- *"Tại sao cần Repository Pattern?"* → Dễ test (mock), dễ thay ORM, tách biệt concern.
- *"GenericRepository giải quyết bài toán gì?"* → Tránh code lặp cho 14 entity khác nhau.

---

### 2.2 Unit of Work Pattern
**Vị trí:** `Core/Interfaces/IUnitOfWork.cs` + `Infrastructure/Repositories/UnitOfWork.cs`

**Mục đích:** Đảm bảo tính toàn vẹn dữ liệu khi một nghiệp vụ cần thao tác trên nhiều bảng (atomic transaction).

```csharp
// Ví dụ: Tạo booking cần ghi vào 3 bảng cùng lúc
await _unitOfWork.BeginTransactionAsync();
try {
    await _unitOfWork.Bookings.AddAsync(booking);           // bảng Bookings
    await _unitOfWork.BookingDetails.AddRangeAsync(details); // bảng BookingDetails
    await _unitOfWork.SaveChangesAsync();
    await _unitOfWork.CommitTransactionAsync();
} catch {
    await _unitOfWork.RollbackTransactionAsync();           // rollback nếu lỗi
}
```

**UoW quản lý 14 repositories:**
```
Cinema Infrastructure: Cinemas, Rooms, SeatTypes, Seats
Movies:                Movies, Genres, MovieGenres, Showtimes
Bookings:             Bookings, BookingDetails, Tickets
Concessions:          Foods, BookingFoods
Business:             Users, Vouchers, Transactions
```

---

### 2.3 Dependency Injection (DI)
**Vị trí:** `Program.cs`

**Mục đích:** Các lớp không tự khởi tạo dependency, để DI container quản lý vòng đời.

```csharp
// Program.cs — đăng ký DI
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddSingleton<VNPayHelper>();

// Controller nhận qua constructor — không new() trực tiếp
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService; // DI inject tự động
    }
}
```

**Lifetimes:**
- `Scoped` = 1 instance / 1 HTTP request (IUnitOfWork, IBookingService)
- `Singleton` = 1 instance toàn app (VNPayHelper)

---

### 2.4 DTO Pattern (Data Transfer Object)
**Vị trí:** `Application/DTOs/`

**Mục đích:** Không expose Entity trực tiếp ra ngoài. API nhận/trả DTO, không phải Entity.

```
CreateBookingDto    → Input từ user khi tạo booking
BookingResultDto    → Output trả về sau khi tạo/select seat
SeatStatusDto       → Trạng thái ghế cho UI seat map
LoginDto            → Input đăng nhập
RegisterDto         → Input đăng ký
```

---

### 2.5 Service Layer Pattern
**Vị trí:** `Application/Services/BookingService.cs`

**Mục đích:** Tập trung toàn bộ business logic vào Service, Controller chỉ điều phối.

```
Controller (điều phối)
    → nhận request HTTP
    → validate cơ bản
    → gọi Service
    → trả View/JSON

Service (business logic)
    → xử lý nghiệp vụ phức tạp
    → gọi Repository lấy dữ liệu
    → gọi Redis giữ ghế
    → gọi SignalR broadcast
    → tính giá, discount, loyalty
```

---

## 3. LUỒNG XỬ LÝ ĐẶT VÉ — CHI TIẾT TỪNG BƯỚC

```
User chọn phim → chọn suất chiếu → chọn ghế → thanh toán
```

### Bước 1: Hiển thị sơ đồ ghế

```
GET /Booking/SelectSeats?showtimeId=5
    ↓
BookingController.SelectSeats()
    ↓ gọi
_unitOfWork.Showtimes.GetShowtimeWithDetailsAsync(5)
    ↓ include
Movie, Room, Cinema, Seats, SeatTypes, Foods
    ↓
View render sơ đồ ghế (Razor + JavaScript)
    ↓ JS kết nối WebSocket
SeatHub.JoinShowtimeGroup("Showtime_5")
```

### Bước 2: User chọn ghế

```
User click ghế A1, A2
    ↓ AJAX POST
BookingController.Create()
    ↓ gọi
BookingService.CreateBookingAsync(dto)
    ↓
    ├── Bước 2a: Validate ghế trong DB (chưa Booked?)
    ├── Bước 2b: Check Redis (chưa ai Hold?)
    ├── Bước 2c: Ghi Booking + BookingDetail vào DB (Status = Pending)
    ├── Bước 2d: Lock ghế trong Redis 15 phút
    │     Key: "Seat:5:101", "Seat:5:102"
    │     Value: userId
    │     TTL: 15 phút
    ├── Bước 2e: Tính tổng tiền
    │     SeatPrice = BasePrice × SurchargeRatio
    │     Discount  = Total × VoucherPercent / 100
    └── Bước 2f: Broadcast SignalR
          → Tất cả user xem suất 5 thấy ghế A1, A2 chuyển sang "Đang giữ"
```

### Bước 3: Thanh toán VNPay

```
Redirect → VNPay Gateway (HTTPS)
    ↓ user thanh toán
VNPay callback GET /Payment/VNPayReturn?vnp_ResponseCode=00&...
    ↓
PaymentController.VNPayReturn()
    ├── Validate chữ ký HMAC-SHA512 (chống giả mạo)
    ├── ResponseCode = "00" → thành công
    └── BookingService.ConfirmPaymentAsync(bookingId, transactionId)
            ↓
            ├── Booking.Status = Paid
            ├── Xóa ghế khỏi Redis (release hold)
            ├── Cộng điểm loyalty (100-300 điểm/vé, random)
            ├── Tăng TotalTicketsPurchased
            ├── Nâng hạng thành viên nếu đủ điều kiện
            └── SignalR broadcast: ghế chuyển thành "Đã bán"
```

### Bước 4: Timeout giữ ghế (10-15 phút)

```
Redis TTL hết hạn → key tự động xóa
    ↓
Seat trở lại Available (không cần can thiệp)
    ↓ (hoặc Quartz.NET job chạy)
Booking.Status = Cancelled (cleanup DB)
    ↓
SignalR broadcast: ghế trở lại Available
```

---

## 4. CƠ CHẾ CHỐNG RACE CONDITION (Đặt ghế đồng thời)

**Vấn đề:** 2 user cùng chọn ghế A1 ở cùng thời điểm → cả 2 đều thấy ghế trống.

**Giải pháp: Kết hợp Redis Distributed Locking + Optimistic Concurrency**

```
User A                          User B
chọn ghế A1                    chọn ghế A1
    ↓                               ↓
Check Redis: "Seat:5:101"?      Check Redis: "Seat:5:101"?
→ Không có                      → Có! (A đã lock)
    ↓                               ↓
SetNX "Seat:5:101" = userA      Trả về lỗi:
TTL: 15 phút                    "Ghế A1 vừa được chọn bởi người khác"
    ↓
Lock thành công!
```

**Optimistic Concurrency (Booking entity):**
```csharp
public byte[] RowVersion { get; set; } // Timestamp column SQL Server

// Nếu 2 request cùng update booking → DB throw DbUpdateConcurrencyException
// → Transaction rollback, user được thông báo lỗi
```

---

## 5. REAL-TIME VỚI SIGNALR

**Vị trí:** `Web/Hubs/SeatHub.cs`

**Cách hoạt động:**
```
Tất cả user xem suất chiếu cùng 1 → join Group "Showtime_5"

Khi user A giữ ghế:
    Server → SeatHub.NotifySeatSelected(showtimeId, seatId, "A1")
           → Broadcast đến Group "Showtime_5"
           → Tất cả trình duyệt còn lại nhận message
           → JavaScript cập nhật màu ghế ngay lập tức (không reload)

Khi thanh toán xong:
    Server → SeatHub.NotifySeatSold(showtimeId, seatId)
           → Ghế chuyển sang "Đã bán" trên tất cả màn hình
```

**Client-side (JavaScript):**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/seatHub").build();

connection.on("SeatSelected", (seatId) => {
    document.getElementById(`seat-${seatId}`).classList.add("holding");
});
connection.invoke("JoinShowtimeGroup", showtimeId);
```

---

## 6. CẤU TRÚC DATABASE — 17 BẢNG

```sql
-- Cinema Infrastructure (4 bảng)
Cinemas         (Id, Name, Address, Phone, MapEmbedUrl, IsActive)
Rooms           (Id, CinemaId, Name, TotalRows, SeatsPerRow, SeatMapMatrix)
SeatTypes       (Id, Name, SurchargeRatio, ColorHex)
Seats           (Id, RoomId, Row, Number, SeatTypeId, Status)

-- Movies (4 bảng)
Movies          (Id, Title, Duration, ReleaseDate, Status, BasePrice, AgeRating...)
Genres          (Id, Name, Slug)
MovieGenres     (MovieId, GenreId)  ← junction table M:N
Showtimes       (Id, MovieId, RoomId, StartTime, EndTime, BasePrice, IsWeekendPricing)

-- Bookings (3 bảng)
Bookings        (Id, UserId, ShowtimeId, TotalAmount, Status, VoucherId, RowVersion)
BookingDetails  (Id, BookingId, SeatId, PriceAtBooking)
Tickets         (Id, BookingDetailId, TicketCode, BarcodeImage, IsUsed)

-- Concessions (2 bảng)
Foods           (Id, Name, Price, IsCombo, IsAvailable)
BookingFoods    (Id, BookingId, FoodId, Quantity, UnitPrice)

-- Business (3 bảng + Identity)
Users           (IdentityUser + FullName, Points, MembershipLevel, TotalTicketsPurchased)
Vouchers        (Id, Code, DiscountPercent, MaxAmount, ExpiryDate, UsageLimit)
Transactions    (Id, BookingId, VnpayRef, Amount, ResponseCode, RawResponse)

-- ASP.NET Identity (~7 bảng)
AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims...
```

**Quan hệ quan trọng:**
```
Cinema 1──* Room 1──* Seat *──1 SeatType
Movie 1──* Showtime *──1 Room
Movie *──* Genre (qua MovieGenre)
Booking *──1 User
Booking 1──* BookingDetail *──1 Seat
BookingDetail 1──1 Ticket
Booking 1──* BookingFood *──1 Food
```

---

## 7. CÔNG THỨC TÍNH GIÁ VÉ

```
Giá vé = BasePrice × SurchargeRatio × WeekendMultiplier

Trong đó:
  BasePrice       = Showtime.BasePrice (giá gốc của suất chiếu)
  SurchargeRatio  = SeatType.SurchargeRatio
                    Standard = 1.0x
                    VIP      = 1.5x
                    Couple   = 2.0x
  WeekendMultiplier = 1.2 nếu Showtime.IsWeekendPricing = true (tăng 20%)
                    = 1.0 nếu ngày thường

Tổng booking = Σ(Giá từng ghế) + Σ(Food × Quantity) - Discount

Discount = min(Total × VoucherPercent / 100, Voucher.MaxAmount)
```

---

## 8. HỆ THỐNG LOYALTY POINTS

**Cách hoạt động:**
```
Sau khi thanh toán thành công:
  1. Cộng random(100, 300) điểm vào User.Points
  2. Tăng User.TotalTicketsPurchased += 1
  3. Kiểm tra nâng hạng:
     TotalTickets >= 2  → Silver
     TotalTickets >= 10 → Platinum

User có thể đổi điểm thành Voucher:
  AccountController.RedeemVoucher()
  → Tạo Voucher với Code ngẫu nhiên
  → Gắn UserId vào Voucher (personal voucher, không share được)
```

**Membership tiers:**
```
Bronze   → Silver:   >= 2 vé
Silver   → Platinum: >= 10 vé
```

---

## 9. REDIS — LÝ DO DÙNG VÀ CÁCH DÙNG

**Lý do không dùng DB để giữ ghế:**
- Giữ ghế là trạng thái **tạm thời** (10-15 phút), không cần lưu vĩnh viễn
- Redis hỗ trợ TTL tự động xóa key khi hết hạn → ghế tự trả về không cần cronjob
- DB locking kém hiệu năng hơn Redis khi có nhiều concurrent users
- Redis nằm trên RAM → ~1ms vs DB ~10-100ms

**Các key trong Redis:**
```
"Seat:{showtimeId}:{seatId}"           → giữ ghế, value = userId, TTL = 15 phút
"Lock:Booking:{showtimeId}:{seatId}"   → distributed lock, TTL = 30 giây
"Booking:{bookingId}"                  → cache booking data
"Showtime:{showtimeId}:Seats"          → cache danh sách ghế của suất
```

---

## 10. AREA ADMIN

**Vị trí:** `Areas/Admin/Controllers/`

Dự án dùng **ASP.NET Core Areas** để tách biệt admin khỏi user-facing pages.

**Admin controllers:**
```
DashboardController  → Tổng quan + biểu đồ doanh thu
MoviesController     → CRUD phim
ShowtimesController  → Quản lý suất chiếu
CinemasController    → Quản lý rạp
RoomsController      → Quản lý phòng chiếu
GenresController     → Quản lý thể loại
FoodsController      → Quản lý đồ ăn
```

**Route:** `/Admin/Dashboard`, `/Admin/Movies`, v.v.

**Dashboard API** (`Controllers/Api/DashboardApiController.cs`):
```
GET /api/dashboard/revenue        → Doanh thu 7 ngày (JSON cho Chart.js)
GET /api/dashboard/booking-status → Số booking theo status (JSON)
GET /api/dashboard/top-movies     → Top 5 phim có nhiều booking nhất (JSON)
```

---

## 11. VNPAY INTEGRATION

**Flow thanh toán:**
```
1. User confirm booking
   ↓
2. VNPayHelper.CreatePaymentUrl(amount, bookingId, ...)
   → Tạo URL có các param: vnp_Amount, vnp_TxnRef, vnp_OrderInfo...
   → Ký HMAC-SHA512 với HashSecret
   ↓
3. Redirect user → https://sandbox.vnpayment.vn/paymentv2/...
   ↓
4. User thanh toán (test: 9704198526191432198, PIN: 123456)
   ↓
5. VNPay redirect về: GET /Payment/VNPayReturn?vnp_ResponseCode=00&vnp_SecureHash=...
   ↓
6. Server xác minh chữ ký:
   - Lấy all params trừ vnp_SecureHash
   - Sort theo alphabet
   - Hash bằng HMAC-SHA512 với secret
   - So sánh với vnp_SecureHash
   ↓
7. ResponseCode = "00" → ConfirmPaymentAsync()
   ResponseCode ≠ "00" → CancelBookingAsync()
```

**Security của VNPay:**
- HMAC-SHA512 ngăn tamper request
- Sorted params đảm bảo hash nhất quán
- TransactionId lưu vào DB để reconcile

---

## 12. SECURITY — CÁC CƠ CHẾ BẢO MẬT

| Cơ chế | Vị trí | Mục đích |
|---|---|---|
| ASP.NET Identity | Program.cs | Authentication, password hashing (bcrypt) |
| `[Authorize]` attribute | Controllers | Authorization, chặn truy cập không đăng nhập |
| Anti-forgery token | Forms | Chống CSRF attack |
| HMAC-SHA512 | VNPayHelper | Chống giả mạo callback thanh toán |
| Distributed lock | RedisService | Chống double-spending seat |
| Optimistic concurrency | Booking entity (RowVersion) | Chống race condition DB |
| Lockout policy | AccountController | Anti brute-force (5 lần/5 phút) |
| Parameterized query | EF Core | Chống SQL Injection |

---

## 13. CÁC CÔNG NGHỆ & THƯ VIỆN

| Thư viện | Version | Dùng cho |
|---|---|---|
| ASP.NET Core MVC | 8.0 | Web framework |
| Entity Framework Core | 8.0.11 | ORM, migration |
| ASP.NET Core Identity | 8.0.11 | Authentication, user management |
| StackExchange.Redis | 2.7.10 | Distributed cache, seat locking |
| SignalR | built-in | Real-time seat map |
| Quartz.NET | 3.8.0 | Background jobs (cleanup expired bookings) |
| QRCoder | 1.4.3 | Tạo mã QR cho vé điện tử |
| EPPlus | 7.0.5 | Export Excel báo cáo |
| DinkToPdf | 1.0.8 | Tạo PDF vé |
| Newtonsoft.Json | 13.0.3 | JSON serialization |
| SQL Server | - | Database chính |

---

## 14. CÂU HỎI THẦY HAY HỎI + TRẢ LỜI MẪU

### Q1: "Dự án em theo kiến trúc gì?"
**Trả lời:** Dự án theo **Layered Architecture (kiến trúc phân tầng)** gồm 4 tầng: Presentation, Application, Core (Domain), và Infrastructure. Presentation layer vẫn dùng MVC pattern nhưng Controller không chứa business logic — logic được tách riêng vào Service layer ở tầng Application.

---

### Q2: "Repository Pattern là gì, tại sao dùng?"
**Trả lời:** Repository Pattern là lớp trung gian giữa business logic và database. Nó abstract hóa các thao tác dữ liệu để business logic không phụ thuộc trực tiếp vào EF Core. Lợi ích: (1) Dễ unit test vì có thể mock repository, (2) Dễ thay ORM nếu cần, (3) Tránh code lặp CRUD.

---

### Q3: "Unit of Work khác Repository như thế nào?"
**Trả lời:** Repository quản lý thao tác CRUD cho **một** entity. Unit of Work quản lý **nhiều** repository trong cùng một transaction — đảm bảo nếu có lỗi thì tất cả rollback (all-or-nothing). Ví dụ khi tạo booking cần ghi vào 3 bảng (Bookings + BookingDetails + BookingFoods), Unit of Work đảm bảo cả 3 đều thành công hoặc đều thất bại.

---

### Q4: "Tại sao dùng Redis thay vì database để giữ ghế?"
**Trả lời:** (1) Giữ ghế là trạng thái tạm thời, không cần lưu vĩnh viễn → Redis có TTL tự xóa key khi hết hạn. (2) Redis nằm in-memory, latency ~1ms, phù hợp cho real-time concurrent requests. (3) Database row locking gây bottleneck khi nhiều user đồng thời.

---

### Q5: "Race condition là gì và em xử lý như thế nào?"
**Trả lời:** Race condition xảy ra khi 2 user cùng chọn 1 ghế đồng thời — cả 2 đều thấy ghế trống và cùng đặt thành công. Em giải quyết bằng 2 cơ chế: (1) **Redis distributed lock** — dùng `SetNX` (Set if Not Exists) để đảm bảo chỉ 1 user lock được ghế, user còn lại nhận lỗi. (2) **Optimistic Concurrency** với `RowVersion` (Timestamp) trên Booking entity — nếu 2 request cùng update thì DB throw `DbUpdateConcurrencyException` và một bên rollback.

---

### Q6: "SignalR dùng để làm gì?"
**Trả lời:** SignalR cung cấp real-time communication giữa server và tất cả browser đang xem cùng một suất chiếu. Khi user A chọn ghế, server broadcast qua SignalR Hub đến tất cả client trong Group "Showtime_{id}" để cập nhật màu ghế ngay lập tức mà không cần reload trang.

---

### Q7: "Dependency Injection là gì?"
**Trả lời:** DI là kỹ thuật để các class không tự tạo dependency mà nhận từ bên ngoài (qua constructor). DI Container trong Program.cs quản lý việc khởi tạo và inject. Ví dụ `BookingController` không `new BookingService()` mà nhận `IBookingService` qua constructor — giúp loose coupling, dễ test, dễ swap implementation.

---

### Q8: "DTO là gì, tại sao không dùng Entity trực tiếp?"
**Trả lời:** DTO (Data Transfer Object) là object đơn giản chỉ chứa data để truyền giữa các tầng/API. Không dùng Entity trực tiếp vì: (1) Entity có nhiều field nhạy cảm không muốn expose (password hash, internal IDs), (2) Entity có navigation properties có thể gây circular reference khi serialize JSON, (3) DTO cho phép validate input độc lập với DB schema.

---

### Q9: "Tại sao có 2 folder Migrations?"
**Trả lời:** Đây là điểm cần giải thích rõ — `/Migrations/` là migration cũ còn sót lại khi `DbContext` ở default location, `/Data/Migrations/` là migration đúng hiện tại sau khi `DbContext` chuyển vào `Data/` folder. Về production chỉ dùng `/Data/Migrations/`.

---

### Q10: "VNPay chữ ký HMAC-SHA512 hoạt động thế nào?"
**Trả lời:** VNPay yêu cầu ký mọi request để chống tamper. Cách làm: (1) Thu thập tất cả parameters trừ `vnp_SecureHash`, (2) Sort theo alphabet, (3) Ghép thành query string, (4) Hash bằng HMAC-SHA512 với `HashSecret` (chỉ server biết), (5) Gắn vào URL. Khi VNPay callback về, server làm lại quy trình đó và so sánh — nếu hash không khớp = request bị giả mạo → reject.

---

## 15. SƠ ĐỒ LUỒNG DỮ LIỆU TỔNG HỢP

```
REQUEST từ User (HTTP)
    │
    ▼
[Middleware: Auth, Session, CORS, AntiForgery]
    │
    ▼
[Controller] — Điều phối, không có business logic
    │
    ▼
[Service] — Business logic, orchestration
    ├──→ [IUnitOfWork] → [Repository] → [DbContext] → [SQL Server]
    ├──→ [IRedisService] → [Redis Cache] (seat holding)
    └──→ [SeatHub] → [SignalR] → [Browser WebSocket] (real-time)
    │
    ▼
[DTO] — Return object
    │
    ▼
RESPONSE (View/JSON)
```

---

*Tài liệu được tạo cho buổi vấn đáp dự án Cinema Booking System — ASP.NET Core 8.0*
