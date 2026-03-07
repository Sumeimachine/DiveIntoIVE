using Microsoft.EntityFrameworkCore;
using DiveIntoIVE.Models;

namespace DiveIntoIVE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
