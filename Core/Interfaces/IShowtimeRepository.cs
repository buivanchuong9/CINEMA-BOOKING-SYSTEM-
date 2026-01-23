using BE.Core.Entities.Movies;

namespace BE.Core.Interfaces;

/// <summary>
/// Interface cho Showtime Repository
/// </summary>
public interface IShowtimeRepository : IRepository<Showtime>
{
    Task<IEnumerable<Showtime>> GetShowtimesWithDetailsAsync(int movieId);
    Task<Showtime?> GetShowtimeWithDetailsAsync(int showtimeId);
}
