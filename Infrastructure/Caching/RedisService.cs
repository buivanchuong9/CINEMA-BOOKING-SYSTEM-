using BE.Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BE.Infrastructure.Caching;

/// <summary>
/// Redis Service Implementation
/// QUAN TRỌNG: Service này xử lý seat holding và distributed locking
/// </summary>
public class RedisService : IRedisService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisService> _logger;
    private const int DEFAULT_SEAT_HOLD_MINUTES = 10;

    public RedisService(IDistributedCache cache, ILogger<RedisService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    #region Seat Holding

    public async Task<bool> HoldSeatAsync(int showtimeId, int seatId, string userId, TimeSpan? ttl = null)
    {
        try
        {
            // 1. Tạo KEY định danh duy nhất cho ghế (VD: "Seat:100:55")
            var key = BuildSeatKey(showtimeId, seatId);
            
            // 2. CHECK: Đọc từ Redis xem Key này đã tồn tại chưa? (Có ai giữ chưa?)
            var existingHolder = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(existingHolder))
            {
                // Nếu Redis trả về dữ liệu => Đã có người giữ => Từ chối
                _logger.LogWarning($"Seat {seatId} at showtime {showtimeId} is already held by {existingHolder}");
                return false;
            }

            // 3. Cấu hình Cache Option: Thiết lập thời gian tự hủy (TTL)
            // Nếu ttl null thì dùng mặc định 10 phút. Sau thời gian này Redis TỰ ĐỘNG XÓA key.
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(DEFAULT_SEAT_HOLD_MINUTES)
            };

            // 4. WRITE: Ghi vào Redis. Key="Seat:...", Value="UserId"
            await _cache.SetStringAsync(key, userId, options);
            _logger.LogInformation($"Seat {seatId} held by user {userId} for showtime {showtimeId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error holding seat {seatId} for showtime {showtimeId}");
            return false;
        }
    }

    public async Task<bool> ReleaseSeatAsync(int showtimeId, int seatId)
    {
        try
        {
            // Tạo Key tương ứng
            var key = BuildSeatKey(showtimeId, seatId);
            // XÓA key khỏi Redis => Ghế trở thành trống
            await _cache.RemoveAsync(key);
            _logger.LogInformation($"Seat {seatId} released for showtime {showtimeId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error releasing seat {seatId} for showtime {showtimeId}");
            return false;
        }
    }

    public async Task<bool> IsSeatHeldAsync(int showtimeId, int seatId)
    {
        var key = BuildSeatKey(showtimeId, seatId);
        var holder = await _cache.GetStringAsync(key);
        return !string.IsNullOrEmpty(holder);
    }

    public async Task<string?> GetSeatHolderAsync(int showtimeId, int seatId)
    {
        var key = BuildSeatKey(showtimeId, seatId);
        return await _cache.GetStringAsync(key);
    }

    // Hàm quan trọng: Giữ nhiều ghế cùng lúc (Atomic Simulation)
    public async Task<bool> HoldMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, TimeSpan? ttl = null)
    {
        try
        {
            // Bước 1: Kiểm tra trước (Pre-check) - Quét 1 lượt xem có ghế nào bị kẹt không
            foreach (var seatId in seatIds)
            {
                if (await IsSeatHeldAsync(showtimeId, seatId))
                {
                    _logger.LogWarning($"One or more seats already held for showtime {showtimeId}");
                    return false; // Nếu vướng dù chỉ 1 ghế -> Hủy toàn bộ yêu cầu
                }
            }

            // Bước 2: Giữ ghế (Action) - Thực hiện giữ từng ghế
            foreach (var seatId in seatIds)
            {
                await HoldSeatAsync(showtimeId, seatId, userId, ttl);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error holding multiple seats for showtime {showtimeId}");
            // Bước 3: ROLLBACK (Hoàn tác)
            // Nếu đang chạy giữa chừng mà lỗi (ví dụ giữ được 2/3 ghế thì sập mạng)
            // Phải nhả những ghế đã giữ ra để tránh tình trạng "treo ghế"
            await ReleaseMultipleSeatsAsync(showtimeId, seatIds);
            return false;
        }
    }

    public async Task<bool> ReleaseMultipleSeatsAsync(int showtimeId, List<int> seatIds)
    {
        try
        {
            foreach (var seatId in seatIds)
            {
                await ReleaseSeatAsync(showtimeId, seatId);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error releasing multiple seats for showtime {showtimeId}");
            return false;
        }
    }

    public async Task<List<int>> GetHeldSeatsAsync(int showtimeId)
    {
        // Note: This is a simplified implementation
        // In production, you'd use Redis SCAN or maintain a separate set
        var heldSeats = new List<int>();
        
        // This is a placeholder - actual implementation would require
        // maintaining a Redis Set of held seats per showtime
        _logger.LogWarning("GetHeldSeatsAsync is not fully implemented - requires Redis Set structure");
        
        return heldSeats;
    }

    #endregion

    #region Distributed Locking

    // Hàm Distributed Locking (Khóa phân tán)
    // Dùng để đảm bảo tại 1 thời điểm chỉ CÓ DUY NHẤT 1 luồng được xử lý thanh toán
    public async Task<bool> AcquireLockAsync(string lockKey, TimeSpan expiry)
    {
        try
        {
            var key = BuildLockKey(lockKey);
            var lockValue = Guid.NewGuid().ToString(); // Tạo một giá trị ngẫu nhiên
            
            // 1. Kiểm tra xem cái khóa này đã ai lấy chưa?
            var existingLock = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(existingLock))
            {
                return false; // Đã có người khóa => Tôi phải chờ hoặc từ bỏ
            }

            // 2. Thiết lập thời gian tự mở khóa (expiry)
            // Để tránh Deadlock (Khóa chết) nếu server sập mà quên mở khóa
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            // 3. Đóng khóa
            await _cache.SetStringAsync(key, lockValue, options);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error acquiring lock {lockKey}");
            return false;
        }
    }

    public async Task<bool> ReleaseLockAsync(string lockKey)
    {
        try
        {
            var key = BuildLockKey(lockKey);
            await _cache.RemoveAsync(key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error releasing lock {lockKey}");
            return false;
        }
    }

    #endregion

    #region Generic Caching

    // Hàm Cache dữ liệu tùy ý (Generic)
    public async Task<bool> SetCacheAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            // 1. Serialize: Biến Object C# thành chuỗi JSON (vì Redis chỉ lưu chuỗi)
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions();
            
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }

            // 2. Lưu chuỗi JSON vào Redis
            await _cache.SetStringAsync(key, json, options);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting cache for key {key}");
            return false;
        }
    }

    public async Task<T?> GetCacheAsync<T>(string key)
    {
        try
        {
            // 1. Đọc chuỗi JSON từ Redis
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
            {
                return default; // Không thấy thì trả về null
            }

            // 2. Deserialize: Biến chuỗi JSON ngược lại thành Object C#
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting cache for key {key}");
            return default;
        }
    }

    public async Task<bool> RemoveCacheAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing cache for key {key}");
            return false;
        }
    }

    #endregion

    #region Helper Methods

    private string BuildSeatKey(int showtimeId, int seatId)
    {
        return $"Seat:{showtimeId}:{seatId}";
    }

    private string BuildLockKey(string key)
    {
        return $"Lock:{key}";
    }

    #endregion
}
