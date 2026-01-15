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
            var key = BuildSeatKey(showtimeId, seatId);
            
            // Check if already held
            var existingHolder = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(existingHolder))
            {
                _logger.LogWarning($"Seat {seatId} at showtime {showtimeId} is already held by {existingHolder}");
                return false;
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(DEFAULT_SEAT_HOLD_MINUTES)
            };

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
            var key = BuildSeatKey(showtimeId, seatId);
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

    public async Task<bool> HoldMultipleSeatsAsync(int showtimeId, List<int> seatIds, string userId, TimeSpan? ttl = null)
    {
        try
        {
            // Check all seats first
            foreach (var seatId in seatIds)
            {
                if (await IsSeatHeldAsync(showtimeId, seatId))
                {
                    _logger.LogWarning($"One or more seats already held for showtime {showtimeId}");
                    return false;
                }
            }

            // Hold all seats
            foreach (var seatId in seatIds)
            {
                await HoldSeatAsync(showtimeId, seatId, userId, ttl);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error holding multiple seats for showtime {showtimeId}");
            // Rollback: release all seats
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

    public async Task<bool> AcquireLockAsync(string lockKey, TimeSpan expiry)
    {
        try
        {
            var key = BuildLockKey(lockKey);
            var lockValue = Guid.NewGuid().ToString();
            
            var existingLock = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(existingLock))
            {
                return false; // Lock already acquired
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

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

    public async Task<bool> SetCacheAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions();
            
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }

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
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

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
