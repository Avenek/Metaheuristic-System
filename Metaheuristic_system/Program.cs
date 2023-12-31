using FluentValidation;
using FluentValidation.AspNetCore;
using Metaheuristic_system.Entities;
using Metaheuristic_system.MappingProfiles;
using Metaheuristic_system.Middleware;
using Metaheuristic_system.Models;
using Metaheuristic_system.Models.Validators;
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

            builder.Services.AddControllers().AddFluentValidation();
            builder.Services.AddDbContext<SystemDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }, ServiceLifetime.Transient);
            builder.Services.AddScoped<AlgorithmSeeder>();
            builder.Services.AddScoped<FitnessFunctionSeeder>();
            builder.Services.AddScoped<ErrorHandlingMiddleware>();
            builder.Services.AddScoped<IAlgorithmService, AlgorithmService>();
            builder.Services.AddScoped<IFitnessFunctionService, FitnessFunctionService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<IValidator<AlgorithmDto>, AlgorithmDtoValidator>();
            builder.Services.AddScoped<IValidator<FitnessFunctionDto>, FitnessFunctionDtoValidator>();
            builder.Services.AddScoped<IValidator<UpdateFitnessFunctionDto>, UpdateFitnessFunctionDtoValidator>();
            builder.Services.AddAutoMapper(typeof(FitnessFunctionMappingProfile));
            builder.Services.AddAutoMapper(typeof(AlgorithmMappingProfile));
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontEndClient", b =>
                    b.AllowAnyMethod()
                        .AllowAnyHeader()
                         .SetIsOriginAllowed(origin => true)
                          //.WithOrigins(builder.Configuration["AllowedOrigins"])
                );
            });
            builder.Services.AddSwaggerGen();
            builder.Services.AddTransient<DbContextFactory>();
            var app = builder.Build();
            app.UseStaticFiles();
            app.UseCors("FrontEndClient");
            var algortihmScope = app.Services.CreateScope();
            var algorithmSeeder = algortihmScope.ServiceProvider.GetRequiredService<AlgorithmSeeder>();
            algorithmSeeder.Seed();
            var fitnessFunctionScope = app.Services.CreateScope();
            var fitnessFunctionSeeder = fitnessFunctionScope.ServiceProvider.GetRequiredService<FitnessFunctionSeeder>();
            fitnessFunctionSeeder.Seed();
            app.UseMiddleware<ErrorHandlingMiddleware>();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
