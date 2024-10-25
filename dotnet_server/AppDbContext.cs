using Microsoft.EntityFrameworkCore;

class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options)
      : base(options) { }

  public DbSet<Video> Videos => Set<Video>();
}
