using BE.Core.Entities.Business;
using BE.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BE.Application.Services;

/// <summary>
/// Service cung cấp SiteSettings cho toàn ứng dụng, có cache để tránh query DB mỗi request.
/// </summary>
public interface ISiteSettingsService
{
    Task<SiteSettings> GetSettingsAsync();
    void InvalidateCache();
}

public class SiteSettingsService : ISiteSettingsService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "SiteSettings_v1";

    public SiteSettingsService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<SiteSettings> GetSettingsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached != null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.SiteSettings.FirstOrDefaultAsync() ?? new SiteSettings();

        _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(10));
        return settings;
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }
}
