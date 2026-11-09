using Microsoft.EntityFrameworkCore;
using RaceDay.Data;

namespace RaceDay.Tests;

// =======================================================
// START: DATABASE MODEL TESTS
// SQL Server refuses to create tables that can be deleted through
// several cascade paths (error 1785). The in-memory test database does
// not notice, so these tests inspect the real SQL Server create script.
// No database connection is needed to generate the script.
// =======================================================

public class DatabaseModelTests
{
    private static string BuildSqlServerScript()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer("Server=localhost;Database=ScriptOnly;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        using var context = new RaceDayDbContext(options);

        return context.Database.GenerateCreateScript();
    }

    [Theory]
    [InlineData("FK_Events_Users_OrganiserId")]
    [InlineData("FK_Enrolments_Events_EventId")]
    [InlineData("FK_Enrolments_Users_ParticipantId")]
    [InlineData("FK_Enrolments_Categories_CategoryId")]
    public void ForeignKey_DoesNotCascadeDeletes(string constraintName)
    {
        var script = BuildSqlServerScript();

        var line = script
            .Split('\n')
            .FirstOrDefault(l => l.Contains(constraintName));

        Assert.NotNull(line);
        Assert.DoesNotContain("ON DELETE CASCADE", line);
    }

    [Fact]
    public void ResultsStillCascadeFromEnrolments()
    {
        var script = BuildSqlServerScript();

        var line = script
            .Split('\n')
            .FirstOrDefault(l => l.Contains("FK_Results_Enrolments_EnrolmentId"));

        Assert.NotNull(line);
        Assert.Contains("ON DELETE CASCADE", line);
    }
}

// =======================================================
// END: DATABASE MODEL TESTS
// =======================================================
