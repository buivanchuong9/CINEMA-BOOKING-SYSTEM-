namespace BE.Core.Enums;

/// <summary>
/// Trạng thái ghế ngồi
/// </summary>
public enum SeatStatus
{
    /// <summary>
    /// Ghế trống, có thể đặt
    /// </summary>
    Available = 0,
    
    /// <summary>
    /// Ghế đang được giữ bởi user khác (trong Redis, TTL 10 phút)
    /// </summary>
    Holding = 1,
    
    /// <summary>
    /// Ghế đã được đặt và thanh toán thành công
    /// </summary>
    Booked = 2,
    
    /// <summary>
    /// Ghế bị hỏng/bảo trì, không cho phép đặt
    /// </summary>
    Unavailable = 3
}
