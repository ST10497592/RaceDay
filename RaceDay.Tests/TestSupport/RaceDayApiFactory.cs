using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Data;
using RaceDay.Services;

namespace RaceDay.Tests.TestSupport;

// =======================================================
// START: TEST HOST
// Runs the real API pipeline (JWT authentication, role
// authorisation, validation) against an in-memory database
// and a fake Azure Blob Storage service.
// =======================================================

public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }
}

/// <summary>Stand-in for Azure Blob Storage so tests never touch the network.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://fake-storage.test/raceday-images/{folder}/{Guid.NewGuid():N}.png";
        return Task.FromResult(url);
    }
}

// =======================================================
// END: TEST HOST
// =======================================================
