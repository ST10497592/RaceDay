
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data;

// =======================================================
// START: RACE DAY DATABASE CONTEXT
// Purpose: Connects entity models to the SQL Server database.
// =======================================================

public class RaceDayDbContext : DbContext
{
    // START: CONSTRUCTOR
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }
    // END: CONSTRUCTOR

    // START: DATABASE SETS
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    // END: DATABASE SETS
}

// =======================================================
// END: RACE DAY DATABASE CONTEXT
// =======================================================
