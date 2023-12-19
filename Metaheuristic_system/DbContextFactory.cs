using Metaheuristic_system.Entities;
using Microsoft.EntityFrameworkCore;

namespace Metaheuristic_system
{
    public class DbContextFactory
    {
        private readonly IConfiguration _configuration;

        public DbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SystemDbContext CreateDbContext()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<SystemDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new SystemDbContext(optionsBuilder.Options);
        }
    }
}
