using Microsoft.EntityFrameworkCore;
using BE.Core.Entities.Movies;
using BE.Core.Interfaces;
using BE.Data;

namespace BE.Infrastructure.Repositories;

/// <summary>
/// Showtime Repository - Xử lý các truy vấn đặc biệt cho Showtime
/// </summary>
public class ShowtimeRepository : GenericRepository<Showtime>, IShowtimeRepository // kế thừa từ GenericRepository<Showtime> và implement IShowtimeRepository
{
    public ShowtimeRepository(AppDbContext context) : base(context) // constructor tiêm dependency từ Program.cs
    {
    }

    /// <summary>
    /// Lấy tất cả showtimes với thông tin Room và Cinema
    /// </summary>
    public async Task<IEnumerable<Showtime>> GetShowtimesWithDetailsAsync(int movieId) // lấy tất cả showtimes với thông tin Room và Cinema
    {
        return await _dbSet
            .Include(st => st.Room)
                .ThenInclude(r => r.Cinema)
            .Where(st => st.MovieId == movieId && st.StartTime >= DateTime.Now.Date && st.IsActive) // lọc theo movie id, thời gian bắt đầu >= thời gian hiện tại và showtime còn hoạt động
            .OrderBy(st => st.StartTime) // sắp xếp theo thời gian bắt đầu
            .AsNoTracking() // không theo dõi thay đổi
            .ToListAsync(); // chuyển sang list
    }

    /// <summary>
    /// Lấy showtime theo ID với thông tin Room và Cinema
    /// </summary>
    public async Task<Showtime?> GetShowtimeWithDetailsAsync(int showtimeId) // lấy showtime theo id với thông tin Room và Cinema
    {
        return await _dbSet
            .Include(st => st.Room)
                .ThenInclude(r => r.Cinema) // bao gồm thông tin rạp chiếu
            .Include(st => st.Movie) // bao gồm thông tin phim
            .AsNoTracking() // không theo dõi thay đổi
            .FirstOrDefaultAsync(st => st.Id == showtimeId); // lấy showtime theo id
    }
}
