using BE.Core.Interfaces;
using BE.Core.Interfaces.Services;
using BE.Core.Entities.Bookings;
using BE.Application.DTOs;
using BE.Core.Enums;
using Microsoft.AspNetCore.SignalR;
using BE.Web.Hubs;
using BE.Data;
using Microsoft.EntityFrameworkCore;

namespace BE.Application.Services;

/// <summary>
/// Booking Service - Logic nghiệp vụ đặt vé đơn giản
/// </summary>
public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly IHubContext<SeatHub> _hubContext;
    private readonly ILogger<BookingService> _logger;
    private readonly AppDbContext _context;

    public BookingService(
        IUnitOfWork unitOfWork,
        IRedisService redisService,
        IHubContext<SeatHub> hubContext,
        ILogger<BookingService> logger,
        AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _hubContext = hubContext;
        _logger = logger;
        _context = context;
    }

    public async Task<BookingSeatResult> SelectSeatsAsync(int showtimeId, List<int> seatIds, string userId)
    {
        try
        {
            _logger.LogInformation($"[SelectSeats] Called with showtimeId={showtimeId}, userId={userId}, seats={string.Join(",", seatIds)}");
            
            // Validate showtime exists - QUERY TRỰC TIẾP từ DbContext
            _logger.LogInformation($"[SelectSeats] Querying showtime ID: {showtimeId}");
            var showtime = await _context.Showtimes
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == showtimeId);
            
            if (showtime == null)
            {
                _logger.LogWarning($"[SelectSeats] Showtime {showtimeId} NOT FOUND in database!");
                
                // Debug: Check if ANY showtimes exist
                var totalShowtimes = await _context.Showtimes.CountAsync();
                _logger.LogWarning($"[SelectSeats] Total showtimes in DB: {totalShowtimes}");
                
                // Debug: Find nearest showtime IDs
                var nearestIds = await _context.Showtimes
                    .OrderBy(s => Math.Abs(s.Id - showtimeId))
                    .Take(5)
                    .Select(s => s.Id)
                    .ToListAsync();
                _logger.LogWarning($"[SelectSeats] Nearest showtime IDs: {string.Join(", ", nearestIds)}");
                
                return new BookingSeatResult
                {
                    Success = false,
                    Message = "Lịch chiếu không tồn tại!"
                };
            }
            
            _logger.LogInformation($"[SelectSeats] Found showtime: Movie={showtime.MovieId}, Room={showtime.RoomId}, Active={showtime.IsActive}");

            // Validate seats exist and are available
            foreach (var seatId in seatIds)
            {
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                if (seat == null || seat.Status != SeatStatus.Available)
                {
                    return new BookingSeatResult
                    {
                        Success = false,
                        Message = $"Ghế không hợp lệ hoặc đã được đặt!"
                    };
                }

                // Check if already held in Redis
                var isHeld = await _redisService.IsSeatHeldAsync(showtimeId, seatId);
                if (isHeld)
                {
                    return new BookingSeatResult
                    {
                        Success = false,
                        Message = "Một hoặc nhiều ghế đang được giữ bởi người khác!"
                    };
                }
            }

            // Hold seats in Redis (10 minutes)
            var holdSuccess = await _redisService.HoldMultipleSeatsAsync(showtimeId, seatIds, userId);
            if (!holdSuccess)
            {
                return new BookingSeatResult
                {
                    Success = false,
                    Message = "Không thể giữ ghế. Vui lòng thử lại!"
                };
            }

            // Notify other clients via SignalR
            foreach (var seatId in seatIds)
            {
                await _hubContext.Clients.Group($"Showtime_{showtimeId}")
                    .SendAsync("SeatSelected", new { showtimeId, seatId, userId });
            }

            return new BookingSeatResult
            {
                Success = true,
                Message = "Đã giữ ghế thành công!",
                SelectedSeatIds = seatIds,
                HoldExpiryTime = DateTime.Now.AddMinutes(10)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting seats");
            return new BookingSeatResult
            {
                Success = false,
                Message = "Có lỗi xảy ra khi chọn ghế!"
            };
        }
    }

    public async Task<CreateBookingResult> CreateBookingAsync(CreateBookingDto dto)
    {
        try
        {
            _logger.LogInformation($"[CreateBooking] START - ShowtimeId={dto.ShowtimeId}, UserId={dto.UserId}, Seats={string.Join(",", dto.SeatIds)}");
            
            // Validate showtime exists
            var showtime = await _unitOfWork.Showtimes.GetByIdAsync(dto.ShowtimeId);
            if (showtime == null)
            {
                _logger.LogWarning($"[CreateBooking] Showtime {dto.ShowtimeId} NOT FOUND!");
                return new CreateBookingResult
                {
                    Success = false,
                    Message = "Lịch chiếu không tồn tại!"
                };
            }

            // MVC PATTERN: Validate và Book ghế TRỰC TIẾP trong transaction
            // Không cần hold seats trước qua Redis/API
            foreach (var seatId in dto.SeatIds)
            {
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                if (seat == null)
                {
                    _logger.LogWarning($"[CreateBooking] Seat {seatId} NOT FOUND!");
                    return new CreateBookingResult
                    {
                        Success = false,
                        Message = $"Ghế không hợp lệ!"
                    };
                }

                if (seat.Status != SeatStatus.Available)
                {
                    _logger.LogWarning($"[CreateBooking] Seat {seatId} NOT AVAILABLE! Current status: {seat.Status}");
                    return new CreateBookingResult
                    {
                        Success = false,
                        Message = $"Ghế {seat.Row}-{seat.Number} đã được đặt. Vui lòng chọn ghế khác!"
                    };
                }
            }

            // Calculate total price
            var totalAmount = await CalculateTotalPriceAsync(dto.ShowtimeId, dto.SeatIds, dto.VoucherId, dto.Foods);

            // Create Booking
            var booking = new Booking
            {
                UserId = dto.UserId,
                ShowtimeId = dto.ShowtimeId,
                BookingDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = BookingStatus.Pending, // Sẽ chuyển Paid sau khi thanh toán
                PaymentMethod = PaymentMethod.VNPAY,
                VoucherId = dto.VoucherId,
                Notes = dto.Notes,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation($"[CreateBooking] Created Booking ID={booking.Id}");

            // Create BookingDetails (seats) và CẬP NHẬT STATUS NGAY
            foreach (var seatId in dto.SeatIds)
            {
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(seat!.SeatTypeId);

                var price = showtime!.BasePrice * seatType!.SurchargeRatio;

                var bookingDetail = new BookingDetail
                {
                    BookingId = booking.Id,
                    SeatId = seatId,
                    PriceAtBooking = price
                };

                await _unitOfWork.BookingDetails.AddAsync(bookingDetail);

                // ĐẶT GHẾ NGAY - Update seat status to Booked
                seat.Status = SeatStatus.Booked;
                _unitOfWork.Seats.Update(seat);
                
                _logger.LogInformation($"[CreateBooking] Booked Seat {seat.Row}-{seat.Number}");
            }

            // Add Foods if any
            if (dto.Foods != null && dto.Foods.Any())
            {
                foreach (var foodItem in dto.Foods)
                {
                    var food = await _unitOfWork.Foods.GetByIdAsync(foodItem.FoodId);
                    if (food != null)
                    {
                        var bookingFood = new Core.Entities.Concessions.BookingFood
                        {
                            BookingId = booking.Id,
                            FoodId = foodItem.FoodId,
                            Quantity = foodItem.Quantity,
                            UnitPrice = food.Price
                        };
                        await _unitOfWork.BookingFoods.AddAsync(bookingFood);
                        _logger.LogInformation($"[CreateBooking] Added Food {food.Name} x{foodItem.Quantity}");
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation($"[CreateBooking] SUCCESS - Booking ID={booking.Id}, Total={totalAmount}");

            return new CreateBookingResult
            {
                Success = true,
                Message = "Đặt vé thành công!",
                BookingId = booking.Id,
                TotalAmount = totalAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateBooking] ERROR - Exception occurred");
            return new CreateBookingResult
            {
                Success = false,
                Message = "Có lỗi xảy ra khi đặt vé! Vui lòng thử lại."
            };
        }
    }

    public async Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        booking.Status = BookingStatus.Paid;
        booking.TransactionId = transactionId;
        booking.PaymentMethod = PaymentMethod.VNPAY;
        booking.UpdatedAt = DateTime.Now;

        _unitOfWork.Bookings.Update(booking);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelBookingAsync(int bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.Now;

        // Release seats
        var bookingDetails = (await _unitOfWork.BookingDetails.GetAllAsync())
            .Where(bd => bd.BookingId == bookingId);

        foreach (var detail in bookingDetails)
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(detail.SeatId);
            if (seat != null)
            {
                seat.Status = SeatStatus.Available;
                _unitOfWork.Seats.Update(seat);
            }
        }

        _unitOfWork.Bookings.Update(booking);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<decimal> CalculateTotalPriceAsync(int showtimeId, List<int> seatIds, int? voucherId = null, List<FoodItemDto>? foods = null)
    {
        decimal total = 0;

        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null) return 0;

        // Calculate seat prices
        foreach (var seatId in seatIds)
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
            if (seat != null)
            {
                var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(seat.SeatTypeId);
                if (seatType != null)
                {
                    var seatPrice = showtime.BasePrice * seatType.SurchargeRatio;
                    total += seatPrice;
                }
            }
        }

        // Add food prices
        if (foods != null)
        {
            foreach (var foodItem in foods)
            {
                var food = await _unitOfWork.Foods.GetByIdAsync(foodItem.FoodId);
                if (food != null)
                {
                    total += food.Price * foodItem.Quantity;
                }
            }
        }

        // Apply voucher discount
        if (voucherId.HasValue)
        {
            var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId.Value);
            if (voucher != null && voucher.IsActive && voucher.ExpiryDate > DateTime.Now)
            {
                var discount = total * (voucher.DiscountPercent / 100m);
                if (voucher.MaxAmount > 0)
                {
                    discount = Math.Min(discount, voucher.MaxAmount);
                }
                total -= discount;
            }
        }

        return total;
    }

    public async Task<List<SeatStatusDto>> GetSeatStatusAsync(int showtimeId)
    {
        var result = new List<SeatStatusDto>();

        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime == null) return result;

        var room = await _unitOfWork.Rooms.GetByIdAsync(showtime.RoomId);
        if (room == null) return result;

        // IMPORTANT: Lấy tất cả seats từ DB để có status mới nhất
        var allSeats = await _unitOfWork.Seats.GetAllAsync();
        var seats = allSeats.Where(s => s.RoomId == room.Id).ToList();

        _logger.LogInformation($"[GetSeatStatus] Showtime {showtimeId}, Room {room.Id}, Total seats: {seats.Count}");

        foreach (var seat in seats)
        {
            var seatType = await _unitOfWork.SeatTypes.GetByIdAsync(seat.SeatTypeId);
            var price = showtime.BasePrice * (seatType?.SurchargeRatio ?? 1);

            var status = "Available";
            string? heldBy = null;

            // CHECK DATABASE STATUS FIRST - Ưu tiên status từ database
            if (seat.Status == SeatStatus.Booked)
            {
                status = "Sold";
                _logger.LogInformation($"[GetSeatStatus] Seat {seat.Row}{seat.Number} is BOOKED (Sold)");
            }
            else if (seat.Status == SeatStatus.Available)
            {
                // Chỉ check Redis khi ghế available trong DB
                if (await _redisService.IsSeatHeldAsync(showtimeId, seat.Id))
                {
                    status = "Held";
                    heldBy = await _redisService.GetSeatHolderAsync(showtimeId, seat.Id);
                    _logger.LogInformation($"[GetSeatStatus] Seat {seat.Row}{seat.Number} is HELD by {heldBy}");
                }
            }
            else
            {
                // Các status khác (Reserved, Maintenance...)
                status = "Sold";
                _logger.LogInformation($"[GetSeatStatus] Seat {seat.Row}{seat.Number} has status: {seat.Status}");
            }

            result.Add(new SeatStatusDto
            {
                SeatId = seat.Id,
                Row = seat.Row,
                Number = seat.Number,
                SeatType = seatType?.Name ?? "Standard",
                Status = status,
                Price = price,
                HeldByUser = heldBy
            });
        }

        return result;
    }

    public async Task<Booking?> GetBookingByIdAsync(int bookingId, string userId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null || booking.UserId != userId) return null;

        return booking;
    }

    public async Task<List<Booking>> GetUserBookingsAsync(string userId)
    {
        var bookings = await _unitOfWork.Bookings.GetAllAsync();
        return bookings.Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToList();
    }
}
