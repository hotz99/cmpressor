using Microsoft.EntityFrameworkCore;

namespace Database;

class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<Entities.Video> Videos { get; set; }
  public DbSet<Entities.User> Users { get; set; }
}
