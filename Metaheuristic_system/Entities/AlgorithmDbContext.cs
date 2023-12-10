using Microsoft.EntityFrameworkCore;

namespace Metaheuristic_system.Entities
{
    public class SystemDbContext : DbContext
    {
        private string connectionString;
        public DbSet<Algorithm> Algorithms { get; set; }
        public DbSet<FitnessFunction> FitnessFunctions { get; set; }

        public SystemDbContext(DbContextOptions<SystemDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Algorithm>()
                .Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(30);

            modelBuilder.Entity<Algorithm>()
                .Property(a => a.FileName)
                .IsRequired()
                .HasMaxLength(30);

            modelBuilder.Entity<FitnessFunction>()
                .Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(30);

            modelBuilder.Entity<FitnessFunction>()
                .Property(f => f.FileName)
                .IsRequired()
                .HasMaxLength(30);
        }
    }
}
