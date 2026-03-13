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
    IRepository<Cinema> Cinemas { get; } // Lấy danh sách cinemas
    IRepository<Room> Rooms { get; } // Lấy danh sách rooms
    IRepository<SeatType> SeatTypes { get; } // Lấy danh sách seat types
    IRepository<Seat> Seats { get; } // Lấy danh sách seats
    
    // Movies Repositories
    IRepository<Movie> Movies { get; } // Lấy danh sách movies
    IRepository<Genre> Genres { get; } // Lấy danh sách genres
    IRepository<MovieGenre> MovieGenres { get; } // Lấy danh sách movie genres
    IShowtimeRepository Showtimes { get; } // Lấy danh sách showtimes
    
    // Booking Repositories
    IRepository<Booking> Bookings { get; } // Lấy danh sách bookings
    IRepository<BookingDetail> BookingDetails { get; } // Lấy danh sách booking details
    IRepository<Ticket> Tickets { get; } // Lấy danh sách tickets
    
    // Concessions Repositories
    IRepository<Food> Foods { get; } // Lấy danh sách foods
    IRepository<BookingFood> BookingFoods { get; } // Lấy danh sách booking foods
    
    // Business Repositories
    IRepository<User> Users { get; } // Lấy danh sách users
    IRepository<Voucher> Vouchers { get; } // Lấy danh sách vouchers
    IRepository<Transaction> Transactions { get; } // Lấy danh sách transactions
    
    // Transaction Management
    Task<int> SaveChangesAsync(); // Lưu thay đổi
    Task BeginTransactionAsync(); // Bắt đầu transaction
    Task CommitTransactionAsync(); // Commit transaction
    Task RollbackTransactionAsync(); // Rollback transaction
}
