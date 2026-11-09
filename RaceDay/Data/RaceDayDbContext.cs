
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

        // START: DELETE BEHAVIOUR
        // SQL Server rejects tables that can be reached by several cascade paths
        // (Users -> Events -> Enrolments and Users -> Enrolments). The Part 1 SQL script
        // uses NO ACTION for these keys, so the model does the same. The API already checks
        // for dependent rows before deleting.
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany()
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant)
            .WithMany()
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        // END: DELETE BEHAVIOUR

        // START: UNIQUE CONSTRAINTS (match the Part 1 SQL script)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.CategoryName)
            .IsUnique();

        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantId, e.EventId })
            .IsUnique();
        // END: UNIQUE CONSTRAINTS

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
