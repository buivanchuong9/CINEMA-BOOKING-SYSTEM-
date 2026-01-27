using Microsoft.EntityFrameworkCore;
using BE.Core.Entities.Movies;
using BE.Core.Interfaces;
using BE.Data;

namespace BE.Infrastructure.Repositories;

/// <summary>
/// Showtime Repository - Xử lý các truy vấn đặc biệt cho Showtime
/// </summary>
public class ShowtimeRepository : GenericRepository<Showtime>, IShowtimeRepository
{
    public ShowtimeRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Lấy tất cả showtimes với thông tin Room và Cinema
    /// </summary>
    public async Task<IEnumerable<Showtime>> GetShowtimesWithDetailsAsync(int movieId)
    {
        return await _dbSet
            .Include(st => st.Room)
                .ThenInclude(r => r.Cinema)
            .Where(st => st.MovieId == movieId && st.StartTime >= DateTime.Now.Date && st.IsActive)
            .OrderBy(st => st.StartTime)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Lấy showtime theo ID với thông tin Room và Cinema
    /// </summary>
    public async Task<Showtime?> GetShowtimeWithDetailsAsync(int showtimeId)
    {
        return await _dbSet
            .Include(st => st.Room)
                .ThenInclude(r => r.Cinema)
            .Include(st => st.Movie)
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == showtimeId);
    }
}
