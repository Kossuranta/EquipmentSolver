using EquipmentSolver.Core.Entities;
using EquipmentSolver.Core.Interfaces;
using EquipmentSolver.Core.Models;
using EquipmentSolver.Infrastructure.Data;
using EquipmentSolver.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentSolver.Infrastructure;

/// <summary>
/// Registers Infrastructure services into DI.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Identity
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Caching
        services.AddMemoryCache();

        // IGDB settings
        services.Configure<IgdbSettings>(configuration.GetSection(IgdbSettings.SectionName));

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGameProfileService, GameProfileService>();
        services.AddScoped<ISolverService, SolverService>();
        services.AddScoped<ISocialService, SocialService>();
        services.AddScoped<IImportExportService, ImportExportService>();
        services.AddHttpClient<IIgdbService, IgdbService>();

        return services;
    }
}
