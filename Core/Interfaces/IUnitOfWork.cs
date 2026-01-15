using BE.Core.Entities.CinemaInfrastructure;
using BE.Core.Entities.Movies;
using BE.Core.Entities.Bookings;
using BE.Core.Entities.Concessions;
using BE.Core.Entities.Business;

namespace BE.Core.Interfaces;

/// <summary>
/// Unit of Work Pattern - Quản lý transactions và repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Cinema Infrastructure Repositories
    IRepository<Cinema> Cinemas { get; }
    IRepository<Room> Rooms { get; }
    IRepository<SeatType> SeatTypes { get; }
    IRepository<Seat> Seats { get; }
    
    // Movies Repositories
    IRepository<Movie> Movies { get; }
    IRepository<Genre> Genres { get; }
    IRepository<MovieGenre> MovieGenres { get; }
    IRepository<Showtime> Showtimes { get; }
    
    // Booking Repositories
    IRepository<Booking> Bookings { get; }
    IRepository<BookingDetail> BookingDetails { get; }
    IRepository<Ticket> Tickets { get; }
    
    // Concessions Repositories
    IRepository<Food> Foods { get; }
    IRepository<BookingFood> BookingFoods { get; }
    
    // Business Repositories
    IRepository<User> Users { get; }
    IRepository<Voucher> Vouchers { get; }
    IRepository<Transaction> Transactions { get; }
    
    // Transaction Management
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
