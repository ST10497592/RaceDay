using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data;

// =======================================================
// START: DATABASE INITIALISER
// Purpose: Makes sure the database exists (useful in Docker),
// adds the ProfilePictureUrl column to databases created by the
// Part 1 script, and optionally seeds demo data.
// =======================================================

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        // SQL Server may still be starting (docker compose), so retry for a while.
        const int maxAttempts = 15;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts && context.Database.IsSqlServer())
            {
                logger.LogWarning(
                    ex,
                    "Database not ready (attempt {Attempt} of {Max}). Retrying in 4 seconds.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(4));
            }
        }

        if (context.Database.IsSqlServer())
        {
            // Databases created from the Part 1 SQL script do not have this column yet.
            await context.Database.ExecuteSqlRawAsync(
                "IF COL_LENGTH('Users', 'ProfilePictureUrl') IS NULL " +
                "ALTER TABLE Users ADD ProfilePictureUrl NVARCHAR(500) NULL;");
        }

        if (configuration.GetValue<bool>("Seed:DemoData"))
        {
            await DemoDataSeeder.SeedAsync(scope.ServiceProvider, logger);
        }
    }
}

// =======================================================
// END: DATABASE INITIALISER
// =======================================================
