
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

    public DbSet<Result> Results => Set<Result>();
    // END: DATABASE SETS



    // START: EVENT CATEGORY RELATIONSHIP
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>()
            .HasMany(e => e.Categories)
            .WithMany(c => c.Events)
            .UsingEntity<Dictionary<string, object>>(
                "EventCategories",
                j => j
                    .HasOne<Category>()
                    .WithMany()
                    .HasForeignKey("CategoryId"),
                j => j
                    .HasOne<Event>()
                    .WithMany()
                    .HasForeignKey("EventId"),
                j =>
                {
                    j.HasKey("EventId", "CategoryId");
                    j.ToTable("EventCategories");
                });

        // START: RESULT CONSTRAINTS
        modelBuilder.Entity<Result>()
            .HasIndex(r => r.EnrolmentId)
            .IsUnique();

        modelBuilder.Entity<Result>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Results_Position",
                "[Position] IS NULL OR [Position] > 0"));
        // END: RESULT CONSTRAINTS
    }
    // END: EVENT CATEGORY RELATIONSHIP

}

// =======================================================
// END: RACE DAY DATABASE CONTEXT
// =======================================================
