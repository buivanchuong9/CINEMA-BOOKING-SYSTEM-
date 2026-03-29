namespace BE.Core.Interfaces.Services;

/// <summary>
/// Redis Caching Service Interface
/// Dùng để quản lý seat holding, distributed locking, và caching
/// </summary>
public interface IRedisService
{
    /// <summary>
    /// Hold ghế trong thời gian nhất định (TTL = 10 phút)
    /// Key format: "Seat:{ShowtimeId}:{SeatId}"
    /// </summary>
    Task<bool> HoldSeatAsync(int showtimeId, int seatId, string userId, TimeSpan? ttl = null); // giữ ghế trong thời gian nhất định (TTL = 10 phút)
    
    /// <summary>
    /// Release ghế (xóa key khỏi Redis)
    /// </summary>
    Task<bool> ReleaseSeatAsync(int showtimeId, int seatId); // giải phóng ghế (xóa key khỏi Redis)
    
    /// <summary>
    /// Check xem ghế đã được hold chưa
    /// </summary>
    Task<bool> IsSeatHeldAsync(int showtimeId, int seatId); // kiểm tra xem ghế đã được hold chưa
    
    /// <summary>
    /// Lấy thông tin user đang hold ghế
    /// </summary>
    Task<string?> GetSeatHolderAsync(int showtimeId, int seatId); // lấy thông tin user đang hold ghế
    
    /// <summary>
    /// Hold multiple seats (atomic operation)
    /// </summary>
    Task<bool> HoldMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, TimeSpan? ttl = null); // giữ nhiều ghế (atomic operation)
    
    /// <summary>
    /// Release multiple seats
    /// </summary>
    Task<bool> ReleaseMultipleSeatsAsync(int showtimeId, List<int> seatIds); // giải phóng nhiều ghế
    
    /// <summary>
    /// Get all held seats for a showtime 
    /// </summary>
    Task<List<int>> GetHeldSeatsAsync(int showtimeId); // lấy tất cả ghế đã được hold cho một showtime
    
    /// <summary>
    /// Distributed Lock - để đảm bảo chỉ 1 request xử lý tại 1 thời điểm
    /// </summary>
    Task<bool> AcquireLockAsync(string lockKey, TimeSpan expiry); 
    
    /// <summary>
    /// Release distributed lock
    /// </summary>
    Task<bool> ReleaseLockAsync(string lockKey);
    
    /// <summary>
    /// Cache generic data với TTL
    /// </summary>
    Task<bool> SetCacheAsync<T>(string key, T value, TimeSpan? expiry = null);
    
    /// <summary>
    /// Get cached data
    /// </summary>
    Task<T?> GetCacheAsync<T>(string key);
    
    /// <summary>
    /// Remove cache
    /// </summary>
    Task<bool> RemoveCacheAsync(string key);
}
