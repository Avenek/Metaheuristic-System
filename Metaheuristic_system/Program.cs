using Metaheuristic_system.Entities;
using Metaheuristic_system.Middleware;
using Metaheuristic_system.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NLog.Web;


namespace Metaheuristic_system
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseNLog();

            builder.Services.AddControllers();
            builder.Services.AddDbContext<AlgorithmDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });
            builder.Services.AddScoped<AlgorithmSeeder>();
            builder.Services.AddScoped<ErrorHandlingMiddleware>();
            builder.Services.AddScoped<IAlgorithmService, AlgorithmService>();

            
            var app = builder.Build();
            var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<AlgorithmSeeder>();
            seeder.Seed();
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
