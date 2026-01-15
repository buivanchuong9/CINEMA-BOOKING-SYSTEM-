using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BE.Core.Entities.CinemaInfrastructure;
using BE.Core.Entities.Movies;
using BE.Core.Entities.Bookings;
using BE.Core.Entities.Concessions;
using BE.Core.Entities.Business;

namespace BE.Data;

/// <summary>
/// AppDbContext - Kế thừa IdentityDbContext để sử dụng ASP.NET Core Identity
/// </summary>
public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ===== GROUP 1: CINEMA INFRASTRUCTURE (4 tables) =====
    public DbSet<Cinema> Cinemas { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<SeatType> SeatTypes { get; set; } = null!;
    public DbSet<Seat> Seats { get; set; } = null!;

    // ===== GROUP 2: MOVIES & SCHEDULING (4 tables) =====
    public DbSet<Movie> Movies { get; set; } = null!;
    public DbSet<Genre> Genres { get; set; } = null!;
    public DbSet<MovieGenre> MovieGenres { get; set; } = null!;
    public DbSet<Showtime> Showtimes { get; set; } = null!;

    // ===== GROUP 3: BOOKING & SALES (3 tables) =====
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<BookingDetail> BookingDetails { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;

    // ===== GROUP 4: CONCESSIONS (2 tables) =====
    public DbSet<Food> Foods { get; set; } = null!;
    public DbSet<BookingFood> BookingFoods { get; set; } = null!;

    // ===== GROUP 5: BUSINESS (2 tables + Identity tables) =====
    // User được kế thừa từ IdentityUser, không cần DbSet riêng
    public DbSet<Voucher> Vouchers { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // QUAN TRỌNG: Gọi base để tạo bảng Identity

        // ===== CONFIGURE COMPOSITE KEYS =====
        
        // MovieGenres: Composite Primary Key (MovieId, GenreId)
        modelBuilder.Entity<MovieGenre>()
            .HasKey(mg => new { mg.MovieId, mg.GenreId });

        // ===== CONFIGURE INDEXES FOR PERFORMANCE =====
        
        // Index trên Bookings.UserId (query nhanh bookings của user)
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.UserId);
        
        // Index trên Bookings.Status (query nhanh bookings theo trạng thái)
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.Status);
        
        // Index trên Seats (RoomId, Row, Number) - Unique constraint
        modelBuilder.Entity<Seat>()
            .HasIndex(s => new { s.RoomId, s.Row, s.Number })
            .IsUnique();
        
        // Index trên Showtimes.StartTime (query nhanh lịch chiếu)
        modelBuilder.Entity<Showtime>()
            .HasIndex(st => st.StartTime);
        
        // Index trên Vouchers.Code (query nhanh khi apply voucher)
        modelBuilder.Entity<Voucher>()
            .HasIndex(v => v.Code)
            .IsUnique();
        
        // Index trên Tickets.TicketCode (query nhanh khi quét QR)
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.TicketCode)
            .IsUnique();

        // ===== CONFIGURE DELETE BEHAVIORS =====
        
        // Khi xóa Cinema -> Cascade delete Rooms
        modelBuilder.Entity<Cinema>()
            .HasMany(c => c.Rooms)
            .WithOne(r => r.Cinema)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Khi xóa Booking -> Cascade delete BookingDetails
        modelBuilder.Entity<Booking>()
            .HasMany(b => b.BookingDetails)
            .WithOne(bd => bd.Booking)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== SEED INITIAL DATA (Optional - có thể dùng DbSeeder riêng) =====
        // Sẽ tạo trong file Infrastructure/Data/DbSeeder.cs
    }
}