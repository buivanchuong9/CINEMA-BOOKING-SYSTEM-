using Microsoft.EntityFrameworkCore;
using BE.Core.Entities.Movies;
using BE.Core.Entities.CinemaInfrastructure;
using BE.Core.Entities.Concessions;
using BE.Core.Entities.Business;
using BE.Core.Enums;

namespace BE.Data;

/// <summary>
/// Database Initializer - Seed initial data for development
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

        // Check if already seeded
        if (await context.Movies.AnyAsync())
        {
            return; // DB has been seeded
        }

        // ========== SEED GENRES ==========
        var genres = new List<Genre>
        {
            new Genre { Name = "Hành Động" },
            new Genre { Name = "Kinh Dị" },
            new Genre { Name = "Hài" },
            new Genre { Name = "Tình Cảm" },
            new Genre { Name = "Khoa Học Viễn Tưởng" },
            new Genre { Name = "Phiêu Lưu" },
            new Genre { Name = "Hoạt Hình" }
        };
        await context.Genres.AddRangeAsync(genres);
        await context.SaveChangesAsync();

        // ========== SEED MOVIES ==========
        var movies = new List<Movie>
        {
            new Movie
            {
                Title = "Avengers: Endgame",
                Description = "Trận chiến cuối cùng của các siêu anh hùng",
                Duration = 181,
                ReleaseDate = new DateTime(2019, 4, 26),
                Director = "Anthony Russo, Joe Russo",
                Cast = "Robert Downey Jr., Chris Evans, Mark Ruffalo",
                AgeRating = "T13",
                Rating = 8.3m,
                Status = MovieStatus.NowShowing,
                PosterUrl = "https://www.youtube.com/watch?v=TcMBFSGVi1c",
                TrailerUrl = "https://www.youtube.com/watch?v=TcMBFSGVi1c",
                CreatedAt = DateTime.Now
            },
            new Movie
            {
                Title = "Spider-Man: No Way Home",
                Description = "Peter Parker đối mặt với đa vũ trụ",
                Duration = 148,
                ReleaseDate = new DateTime(2021, 12, 17),
                Director = "Jon Watts",
                Cast = "Tom Holland, Zendaya, Benedict Cumberbatch",
                AgeRating = "T13",
                Rating = 8.1m,
                Status = MovieStatus.NowShowing,
                PosterUrl = "https://www.youtube.com/watch?v=rt-2cxAiPJk",
                TrailerUrl = "https://www.youtube.com/watch?v=rt-2cxAiPJk",
                CreatedAt = DateTime.Now
            },
            new Movie
            {
                Title = "Barbie",
                Description = "Hành trình khám phá thế giới thực của Barbie",
                Duration = 114,
                ReleaseDate = new DateTime(2023, 7, 21),
                Director = "Greta Gerwig",
                Cast = "Margot Robbie, Ryan Gosling",
                AgeRating = "P",
                Rating = 7.0m,
                Status = MovieStatus.NowShowing,
                PosterUrl = "https://www.youtube.com/watch?v=pBk4NYhWNMM",
                TrailerUrl = "https://www.youtube.com/watch?v=pBk4NYhWNMM",
                CreatedAt = DateTime.Now
            },
            new Movie
            {
                Title = "Oppenheimer",
                Description = "Câu chuyện về cha đẻ bom nguyên tử",
                Duration = 180,
                ReleaseDate = new DateTime(2023, 7, 21),
                Director = "Christopher Nolan",
                Cast = "Cillian Murphy, Emily Blunt, Robert Downey Jr.",
                AgeRating = "T16",
                Rating = 8.3m,
                Status = MovieStatus.NowShowing,
                PosterUrl = "https://www.youtube.com/watch?v=uYPbbksJxIg",
                TrailerUrl = "https://www.youtube.com/watch?v=uYPbbksJxIg",
                CreatedAt = DateTime.Now
            },
            new Movie
            {
                Title = "John Wick: Chapter 4",
                Description = "John Wick đối mặt với kẻ thù mạnh nhất",
                Duration = 169,
                ReleaseDate = new DateTime(2023, 3, 24),
                Director = "Chad Stahelski",
                Cast = "Keanu Reeves, Donnie Yen, Bill Skarsgård",
                AgeRating = "T18",
                Rating = 7.7m,
                Status = MovieStatus.NowShowing,
                PosterUrl = "https://www.youtube.com/watch?v=qEVUtrk8_B4",
                TrailerUrl = "https://www.youtube.com/watch?v=qEVUtrk8_B4",
                CreatedAt = DateTime.Now
            },
            new Movie
            {
                Title = "Mission: Impossible",
                Description = "Nhiệm vụ bất khả thi của Ethan Hunt",
                Duration = 163,
                ReleaseDate = new DateTime(2023, 7, 14),
                Director = "Christopher McQuarrie",
                Cast = "Tom Cruise, Hayley Atwell, Ving Rhames",
                AgeRating = "T13",
                Rating = 7.7m,
                Status = MovieStatus.NowShowing,
                PosterUrl = "https://www.youtube.com/watch?v=avz06PDqDbM",
                TrailerUrl = "https://www.youtube.com/watch?v=avz06PDqDbM",
                CreatedAt = DateTime.Now
            },
            new Movie
            {
                Title = "The Flash",
                Description = "Barry Allen du hành thời gian để cứu mẹ",
                Duration = 144,
                ReleaseDate = new DateTime(2023, 6, 16),
                Director = "Andy Muschietti",
                Cast = "Ezra Miller, Michael Keaton, Sasha Calle",
                AgeRating = "T13",
                Rating = 6.9m,
                Status = MovieStatus.NowShowing,
                PosterUrl = "https://www.youtube.com/watch?v=hebWYacbdvc",
                TrailerUrl = "https://www.youtube.com/watch?v=hebWYacbdvc",
                CreatedAt = DateTime.Now
            },
            new Movie
            {
                Title = "Fast X",
                Description = "Gia đình Toretto đối mặt với kẻ thù nguy hiểm nhất",
                Duration = 141,
                ReleaseDate = new DateTime(2023, 5, 19),
                Director = "Louis Leterrier",
                Cast = "Vin Diesel, Michelle Rodriguez, Jason Momoa",
                AgeRating = "T13",
                Rating = 5.8m,
                Status = MovieStatus.ComingSoon,
                PosterUrl = "https://www.youtube.com/watch?v=aOb15GVFZxU",
                TrailerUrl = "https://www.youtube.com/watch?v=aOb15GVFZxU",
                CreatedAt = DateTime.Now
            }
        };
        await context.Movies.AddRangeAsync(movies);
        await context.SaveChangesAsync();

        // ========== SEED CINEMAS ==========
        var cinemas = new List<Cinema>
        {
            new Cinema
            {
                Name = "CineMax Đống Đa",
                Address = "123 Đường Láng, Đống Đa, Hà Nội",
                Phone = "0243 123 4567",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Cinema
            {
                Name = "CineMax Cầu Giấy",
                Address = "456 Trần Duy Hưng, Cầu Giấy, Hà Nội",
                Phone = "0243 234 5678",
                CreatedAt = DateTime.Now,
                IsActive = true
            }
        };
        await context.Cinemas.AddRangeAsync(cinemas);
        await context.SaveChangesAsync();

        // ========== SEED ROOMS ==========
        var rooms = new List<Room>
        {
            new Room { Name = "Room 1", CinemaId = cinemas[0].Id },
            new Room { Name = "Room 2", CinemaId = cinemas[0].Id },
            new Room { Name = "Room 1", CinemaId = cinemas[1].Id },
        };
        await context.Rooms.AddRangeAsync(rooms);
        await context.SaveChangesAsync();

        // ========== SEED SEAT TYPES ==========
        var seatTypes = new List<SeatType>
        {
            new SeatType { Name = "Standard", SurchargeRatio = 1.0m },
            new SeatType { Name = "VIP", SurchargeRatio = 1.5m },
            new SeatType { Name = "Couple", SurchargeRatio = 2.0m }
        };
        await context.SeatTypes.AddRangeAsync(seatTypes);
        await context.SaveChangesAsync();

        // ========== SEED SEATS ==========
        var seats = new List<Seat>();
        foreach (var room in rooms)
        {
            // 10 rows (A-J), 10 seats per row
            for (char row = 'A'; row <= 'J'; row++)
            {
                for (int num = 1; num <= 10; num++)
                {
                    var seatType = row >= 'I' ? seatTypes[2] : // Couple (I, J)
                                   row >= 'D' ? seatTypes[1] : // VIP (D-H)
                                   seatTypes[0]; // Standard (A-C)

                    seats.Add(new Seat
                    {
                        RoomId = room.Id,
                        Row = row.ToString(),
                        Number = num,
                        SeatTypeId = seatType.Id,
                        Status = SeatStatus.Available
                    });
                }
            }
        }
        await context.Seats.AddRangeAsync(seats);
        await context.SaveChangesAsync();

        // ========== SEED FOODS ==========
        var foods = new List<Food>
        {
            new Food
            {
                Name = "Bắp Rang Bơ (S)",
                Description = "Bắp rang bơ size nhỏ",
                Price = 35000,
                ImageUrl = "https://via.placeholder.com/150",
                IsAvailable = true
            },
            new Food
            {
                Name = "Bắp Rang Bơ (M)",
                Description = "Bắp rang bơ size vừa",
                Price = 55000,
                ImageUrl = "https://via.placeholder.com/150",
                IsAvailable = true
            },
            new Food
            {
                Name = "Bắp Rang Bơ (L)",
                Description = "Bắp rang bơ size lớn",
                Price = 75000,
                ImageUrl = "https://via.placeholder.com/150",
                IsAvailable = true
            },
            new Food
            {
                Name = "Combo VIP",
                Description = "1 Bắp lớn + 2 Nước lớn",
                Price = 150000,
                ImageUrl = "https://via.placeholder.com/150",
                IsAvailable = true
            }
        };
        await context.Foods.AddRangeAsync(foods);
        await context.SaveChangesAsync();

        // ========== SEED SHOWTIMES ==========
        var showtimes = new List<Showtime>();
        var today = DateTime.Today;
        
        foreach (var movie in movies.Where(m => m.Status == MovieStatus.NowShowing).Take(5))
        {
            for (int day = 0; day < 7; day++)
            {
                var date = today.AddDays(day);
                var times = new[] { "10:00", "13:30", "16:00", "19:00", "21:30" };
                
                foreach (var time in times)
                {
                    var startTime = DateTime.Parse($"{date:yyyy-MM-dd} {time}");
                    
                    showtimes.Add(new Showtime
                    {
                        MovieId = movie.Id,
                        RoomId = rooms[day % rooms.Count].Id,
                        StartTime = startTime,
                        EndTime = startTime.AddMinutes(movie.Duration + 15), // +15 phút quảng cáo
                        BasePrice = 80000,
                        IsActive = true
                    });
                }
            }
        }
        await context.Showtimes.AddRangeAsync(showtimes);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Database seeded successfully!");
    }
}
