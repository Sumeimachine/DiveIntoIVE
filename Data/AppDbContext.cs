using Microsoft.EntityFrameworkCore;
using DiveIntoIVE.Models;

namespace DiveIntoIVE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<MemberProfile> MemberProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MemberProfile>()
                .HasIndex(profile => profile.MemberKey)
                .IsUnique();
        }
    }
}
