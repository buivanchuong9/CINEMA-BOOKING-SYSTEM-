namespace BE.Core.Constants;

/// <summary>
/// Redis key patterns và cấu hình
/// </summary>
public static class RedisKeys
{
    /// <summary>
    /// Pattern: "Seat:{ShowtimeId}:{SeatId}"
    /// Value: UserId của người đang giữ ghế
    /// TTL: 10 phút
    /// </summary>
    public static string SeatLock(int showtimeId, int seatId) 
        => $"Seat:{showtimeId}:{seatId}";
    
    /// <summary>
    /// Pattern: "Booking:{BookingId}"
    /// Value: Booking data (JSON)
    /// TTL: 15 phút
    /// </summary>
    public static string BookingCache(int bookingId) 
        => $"Booking:{bookingId}";
    
    /// <summary>
    /// Pattern: "Showtime:{ShowtimeId}:Seats"
    /// Value: List of all seat statuses (JSON)
    /// TTL: 5 phút
    /// </summary>
    public static string ShowtimeSeats(int showtimeId) 
        => $"Showtime:{showtimeId}:Seats";
    
    /// <summary>
    /// Distributed lock pattern
    /// Pattern: "Lock:Booking:{ShowtimeId}:{SeatId}"
    /// </summary>
    public static string BookingLock(int showtimeId, int seatId) 
        => $"Lock:Booking:{showtimeId}:{seatId}";
}
