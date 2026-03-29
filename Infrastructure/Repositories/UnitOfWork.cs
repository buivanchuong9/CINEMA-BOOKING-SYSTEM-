using Microsoft.EntityFrameworkCore.Storage;
using BE.Core.Interfaces;
using BE.Core.Entities.CinemaInfrastructure;
using BE.Core.Entities.Movies;
using BE.Core.Entities.Bookings;
using BE.Core.Entities.Concessions;
using BE.Core.Entities.Business;
using BE.Data;

namespace BE.Infrastructure.Repositories;

/// <summary>
/// Unit of Work Implementation - Quản lý tất cả repositories và transactions
/// </summary>
public class UnitOfWork : IUnitOfWork 
{
    private readonly AppDbContext _context; // Là class đại diện cho Database trong Entity Framework Core
    private IDbContextTransaction? _transaction;  // cho phép nhóm nhiều thao tác database thành một đơn vị.

    // Kho lưu trữ cơ sở hạ tầng điện ảnh
    public IRepository<Cinema> Cinemas { get; } 
    public IRepository<Room> Rooms { get; }
    public IRepository<SeatType> SeatTypes { get; }
    public IRepository<Seat> Seats { get; }

    // Kho lưu trữ phim
    public IRepository<Movie> Movies { get; }
    public IRepository<Genre> Genres { get; }
    public IRepository<MovieGenre> MovieGenres { get; }
    public IShowtimeRepository Showtimes { get; }

    // Kho lưu trữ đặt vé
    public IRepository<Booking> Bookings { get; }
    public IRepository<BookingDetail> BookingDetails { get; }
    public IRepository<Ticket> Tickets { get; }

    // Kho lưu trữ đồ ăn
    public IRepository<Food> Foods { get; }
    public IRepository<BookingFood> BookingFoods { get; }

    // Kho lưu trữ doanh nghiệp
    public IRepository<User> Users { get; }
    public IRepository<Voucher> Vouchers { get; }
    public IRepository<Transaction> Transactions { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;

        // Initialize all repositories
        Cinemas = new GenericRepository<Cinema>(_context);
        Rooms = new GenericRepository<Room>(_context);
        SeatTypes = new GenericRepository<SeatType>(_context);
        Seats = new GenericRepository<Seat>(_context);

        Movies = new GenericRepository<Movie>(_context);
        Genres = new GenericRepository<Genre>(_context);
        MovieGenres = new GenericRepository<MovieGenre>(_context);
        Showtimes = new ShowtimeRepository(_context);

        Bookings = new GenericRepository<Booking>(_context);
        BookingDetails = new GenericRepository<BookingDetail>(_context);
        Tickets = new GenericRepository<Ticket>(_context);

        Foods = new GenericRepository<Food>(_context);
        BookingFoods = new GenericRepository<BookingFood>(_context);

        Users = new GenericRepository<User>(_context);
        Vouchers = new GenericRepository<Voucher>(_context);
        Transactions = new GenericRepository<Transaction>(_context);
    }

    // Transaction Management
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
