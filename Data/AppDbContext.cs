using Microsoft.EntityFrameworkCore;
using CMCSApp.Models;

namespace CMCSApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Claim> Claims { get; set; }
    }
}