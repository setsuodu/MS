namespace MS.UserService.Data;

using Microsoft.EntityFrameworkCore;
using MS.UserService.Models;

public class AppDbContext : DbContext
{
    public DbSet<GameUser> GameUsers { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameUser>().HasKey(u => u.Uid);
    }
}
