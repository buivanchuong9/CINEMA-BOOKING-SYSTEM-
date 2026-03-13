using BE.Core.Entities.Movies;

namespace BE.Core.Interfaces;

/// <summary>
/// Interface cho Showtime Repository
/// </summary>
public interface IShowtimeRepository : IRepository<Showtime>
{
    Task<IEnumerable<Showtime>> GetShowtimesWithDetailsAsync(int movieId); // Lấy danh sách showtimes với thông tin chi tiết
    Task<Showtime?> GetShowtimeWithDetailsAsync(int showtimeId); // Lấy thông tin chi tiết của showtime
}
