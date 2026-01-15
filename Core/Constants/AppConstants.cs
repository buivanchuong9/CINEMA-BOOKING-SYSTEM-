namespace BE.Core.Constants;

/// <summary>
/// Các hằng số cấu hình hệ thống
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Thời gian giữ ghế (phút)
    /// </summary>
    public const int SEAT_HOLD_TIMEOUT_MINUTES = 10;
    
    /// <summary>
    /// Thời gian timeout cho distributed lock (giây)
    /// </summary>
    public const int DISTRIBUTED_LOCK_TIMEOUT_SECONDS = 30;
    
    /// <summary>
    /// Số lượng ghế tối đa có thể đặt trong 1 lần
    /// </summary>
    public const int MAX_SEATS_PER_BOOKING = 10;
    
    /// <summary>
    /// Hệ số giá ghế VIP (nhân với BasePrice)
    /// </summary>
    public const decimal VIP_SEAT_RATIO = 1.5m;
    
    /// <summary>
    /// Hệ số giá ghế Couple (nhân với BasePrice)
    /// </summary>
    public const decimal COUPLE_SEAT_RATIO = 2.0m;
    
    /// <summary>
    /// Hệ số giá ghế Standard
    /// </summary>
    public const decimal STANDARD_SEAT_RATIO = 1.0m;
    
    /// <summary>
    /// Hệ số tăng giá cuối tuần (%)
    /// </summary>
    public const decimal WEEKEND_SURCHARGE_PERCENT = 20m;
    
    /// <summary>
    /// Số điểm tích lũy cho mỗi 10,000 VND
    /// </summary>
    public const int POINTS_PER_10K = 1;
    
    /// <summary>
    /// Prefix cho mã vé (TicketCode)
    /// Format: CMAX-{BookingId}-{SeatNumber}
    /// </summary>
    public const string TICKET_CODE_PREFIX = "CMAX";
}
