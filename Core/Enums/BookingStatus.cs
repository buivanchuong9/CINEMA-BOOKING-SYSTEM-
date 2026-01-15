namespace BE.Core.Enums;

/// <summary>
/// Trạng thái đơn đặt vé
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Chờ thanh toán (vừa tạo đơn, chưa thanh toán)
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Đang giữ ghế (ghế đã được lock trong Redis, chờ thanh toán trong 10 phút)
    /// </summary>
    Holding = 1,
    
    /// <summary>
    /// Đã thanh toán thành công
    /// </summary>
    Paid = 2,
    
    /// <summary>
    /// Đã hủy (do hết thời gian giữ ghế hoặc user tự hủy)
    /// </summary>
    Cancelled = 3
}
