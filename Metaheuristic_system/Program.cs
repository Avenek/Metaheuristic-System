using Metaheuristic_system.Entities;
using Metaheuristic_system.MappingProfiles;
using Metaheuristic_system.Middleware;
using Metaheuristic_system.Seeders;
using Metaheuristic_system.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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
            builder.Services.AddScoped<FitnessFunctionSeeder>();
            builder.Services.AddScoped<ErrorHandlingMiddleware>();
            builder.Services.AddScoped<IAlgorithmService, AlgorithmService>();
            builder.Services.AddScoped<IFitnessFunctionService, FitnessFunctionService>();
            builder.Services.AddAutoMapper(typeof(FitnessFunctionMappingProfile));
            builder.Services.AddAutoMapper(typeof(AlgorithmMappingProfile));
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontEndClient", b =>
                    b.AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithOrigins(builder.Configuration["AllowedOrigins"])
                );
            });

            var app = builder.Build();
            app.UseCors("FrontEndClient");
            var algortihmScope = app.Services.CreateScope();
            var algorithmSeeder = algortihmScope.ServiceProvider.GetRequiredService<AlgorithmSeeder>();
            algorithmSeeder.Seed();
            var fitnessFunctionScope = app.Services.CreateScope();
            var fitnessFunctionSeeder = fitnessFunctionScope.ServiceProvider.GetRequiredService<FitnessFunctionSeeder>();
            fitnessFunctionSeeder.Seed();
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
