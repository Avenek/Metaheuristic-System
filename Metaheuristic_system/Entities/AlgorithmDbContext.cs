using Microsoft.EntityFrameworkCore;

namespace Metaheuristic_system.Entities
{
    public class SystemDbContext : DbContext
    {
        private string connectionString;
        public DbSet<Algorithm> Algorithms { get; set; }
        public DbSet<FitnessFunction> FitnessFunctions { get; set; }
        public DbSet<TestResults> TestResults { get; set; }
        public DbSet<Tests> Tests { get; set; }
        public DbSet<Sessions> Sessions { get; set; }

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

            modelBuilder.Entity<TestResults>()
                .Property(t => t.TestId)
                .IsRequired();
            modelBuilder.Entity<TestResults>()
                .Property(t => t.FitnessFunctionId)
                .IsRequired();

            modelBuilder.Entity<TestResults>()
                .Property(t => t.AlgorithmId)
                .IsRequired();

            modelBuilder.Entity<TestResults>()
                .Property(t => t.Parameters)
                .IsRequired();

            modelBuilder.Entity<Sessions>()
                .Property(s => s.AlgorithmIds)
                .IsRequired();

            modelBuilder.Entity<Sessions>()
                .Property(s => s.FitnessFunctionIds)
                .IsRequired();

            modelBuilder.Entity<Sessions>()
                .Property(s => s.State)
                .IsRequired();
        }
    }
}
