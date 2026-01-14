using Microsoft.EntityFrameworkCore;

namespace BE.Data 
{
    // Phải kế thừa : DbContext thì nó mới nhận diện được
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Khai báo các bảng ở đây sau này
        // public DbSet<User> Users { get; set; }
    }
}