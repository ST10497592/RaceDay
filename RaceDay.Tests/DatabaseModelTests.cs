using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Data;
using RaceDay.Models;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: DATABASE MODEL TESTS
// SQL Server refuses to create tables that can be deleted through
// several cascade paths. The in-memory test database does not notice,
// so this test checks the model itself.
// =======================================================

public class DatabaseModelTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public DatabaseModelTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(typeof(Event))]
    [InlineData(typeof(Enrolment))]
    public void ForeignKeys_ThatCouldCreateCascadePaths_AreRestricted(Type entityClrType)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        var entityType = context.Model.FindEntityType(entityClrType);

        Assert.NotNull(entityType);

        foreach (var foreignKey in entityType!.GetForeignKeys())
        {
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        }
    }
}

// =======================================================
// END: DATABASE MODEL TESTS
// =======================================================
