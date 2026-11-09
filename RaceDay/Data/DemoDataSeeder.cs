using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data;

// =======================================================
// START: DEMO DATA SEEDER
// Purpose: Seeds realistic demo data (2 organisers, participants,
// events, categories, enrolments and results) with properly hashed
// passwords. Enabled with the Seed:DemoData setting.
// =======================================================

public static class DemoDataSeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var context = services.GetRequiredService<RaceDayDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();

        // The Part 1 SQL script inserts placeholder hashes. Replace them with real
        // hashes so the seeded accounts can sign in with the demo password.
        var placeholders = await context.Users
            .Where(u => u.PasswordHash.StartsWith("HASHED_PASSWORD_"))
            .ToListAsync();

        foreach (var placeholder in placeholders)
        {
            placeholder.PasswordHash = hasher.HashPassword(placeholder, DemoPassword);
        }

        if (placeholders.Count > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Replaced {Count} placeholder password hashes with the demo password.",
                placeholders.Count);
        }

        if (await context.Users.AnyAsync())
            return;

        var today = DateTime.UtcNow.Date;

        // START: USERS
        User NewUser(string first, string last, string email, string role, string phone, DateTime dob)
        {
            var user = new User
            {
                FirstName = first,
                LastName = last,
                Email = email,
                Role = role,
                PhoneNumber = phone,
                DateOfBirth = dob,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            return user;
        }

        var james = NewUser("James", "Mokoena", "james.mokoena@raceday.test", "Organiser", "0710000001", new DateTime(1988, 4, 12));
        var sarah = NewUser("Sarah", "Naidoo", "sarah.naidoo@raceday.test", "Organiser", "0710000002", new DateTime(1990, 9, 21));
        var john = NewUser("John", "Smith", "john.smith@raceday.test", "Participant", "0710000003", new DateTime(1998, 2, 15));
        var emily = NewUser("Emily", "Williams", "emily.williams@raceday.test", "Participant", "0710000004", new DateTime(2001, 7, 8));
        var thabo = NewUser("Thabo", "Dlamini", "thabo.dlamini@raceday.test", "Participant", "0710000005", new DateTime(1995, 11, 3));
        var lerato = NewUser("Lerato", "Khumalo", "lerato.khumalo@raceday.test", "Participant", "0710000006", new DateTime(1993, 5, 27));
        // END: USERS

        // START: CATEGORIES
        var run5k = new Category { CategoryName = "5K Run", Description = "Short-distance road running category.", MinimumAge = 13 };
        var run10k = new Category { CategoryName = "10K Run", Description = "Ten-kilometre road running category.", MinimumAge = 16 };
        var half = new Category { CategoryName = "Half Marathon", Description = "21.1-kilometre running category.", MinimumAge = 18 };
        var cycling = new Category { CategoryName = "Cycling", Description = "Road cycling race category.", MinimumAge = 16 };
        var familyWalk = new Category { CategoryName = "Family Walk", Description = "Accessible recreational walking category.", MinimumAge = 8 };
        var under20 = new Category { CategoryName = "Under 20", Description = "Age category for athletes under 20.", MinimumAge = 0, MaximumAge = 19 };
        var senior = new Category { CategoryName = "Senior", Description = "Age category for athletes 20 and over.", MinimumAge = 20 };
        // END: CATEGORIES

        // START: EVENTS
        var joburg10k = new Event
        {
            Organiser = james,
            EventName = "Johannesburg 10K",
            Description = "Annual 10-kilometre city road race through the Johannesburg CBD.",
            EventType = "Running",
            EventDate = today.AddDays(21),
            StartTime = new TimeSpan(7, 0, 0),
            Location = "Johannesburg CBD",
            EntryFee = 180m,
            MaxParticipants = 1000,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            Categories = new List<Category> { run5k, run10k, under20, senior }
        };

        var familyWalkEvent = new Event
        {
            Organiser = sarah,
            EventName = "Pretoria Family Walk",
            Description = "Community walking event for the whole family in the Botanical Garden.",
            EventType = "Walking",
            EventDate = today.AddDays(35),
            StartTime = new TimeSpan(8, 0, 0),
            Location = "Pretoria National Botanical Garden",
            EntryFee = 80m,
            MaxParticipants = 500,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            Categories = new List<Category> { familyWalk }
        };

        var cyclingEvent = new Event
        {
            Organiser = james,
            EventName = "Gauteng Cycling Challenge",
            Description = "Competitive road cycling challenge across Midrand.",
            EventType = "Cycling",
            EventDate = today.AddDays(60),
            StartTime = new TimeSpan(6, 30, 0),
            Location = "Midrand",
            EntryFee = 250m,
            MaxParticipants = 600,
            Status = "Upcoming",
            CreatedAt = DateTime.UtcNow,
            Categories = new List<Category> { cycling }
        };

        var pastRun = new Event
        {
            Organiser = james,
            EventName = "Soweto Spring Run",
            Description = "A completed 10-kilometre community run through Soweto.",
            EventType = "Running",
            EventDate = today.AddDays(-20),
            StartTime = new TimeSpan(7, 30, 0),
            Location = "Soweto",
            EntryFee = 120m,
            MaxParticipants = 400,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            Categories = new List<Category> { run10k, half }
        };
        // END: EVENTS

        // START: ENROLMENTS
        Enrolment NewEnrolment(Event ev, User participant, Category category, string status, string payment, int daysAgo)
        {
            return new Enrolment
            {
                Event = ev,
                Participant = participant,
                Category = category,
                EnrolmentDate = DateTime.UtcNow.AddDays(-daysAgo),
                Status = status,
                PaymentStatus = payment
            };
        }

        var johnJoburg = NewEnrolment(joburg10k, john, run10k, "Confirmed", "Paid", 5);
        var emilyJoburg = NewEnrolment(joburg10k, emily, run5k, "Pending", "Pending", 2);
        var johnWalk = NewEnrolment(familyWalkEvent, john, familyWalk, "Confirmed", "Paid", 4);
        var emilyCycling = NewEnrolment(cyclingEvent, emily, cycling, "Pending", "Pending", 1);

        var johnPast = NewEnrolment(pastRun, john, run10k, "Completed", "Paid", 40);
        var emilyPast = NewEnrolment(pastRun, emily, run10k, "Completed", "Paid", 40);
        var thaboPast = NewEnrolment(pastRun, thabo, run10k, "Completed", "Paid", 38);
        var leratoPast = NewEnrolment(pastRun, lerato, half, "Completed", "Paid", 37);
        // END: ENROLMENTS

        // START: RESULTS
        Result NewResult(Enrolment enrolment, string time, int position)
        {
            return new Result
            {
                Enrolment = enrolment,
                FinishTime = TimeSpan.Parse(time),
                Position = position,
                ResultStatus = "Finished",
                RecordedAt = DateTime.UtcNow.AddDays(-19)
            };
        }

        var results = new List<Result>
        {
            NewResult(thaboPast, "00:44:10", 1),
            NewResult(johnPast, "00:48:32", 2),
            NewResult(emilyPast, "00:52:14", 3),
            NewResult(leratoPast, "01:41:55", 4)
        };
        // END: RESULTS

        context.Users.AddRange(james, sarah, john, emily, thabo, lerato);
        context.Categories.AddRange(run5k, run10k, half, cycling, familyWalk, under20, senior);
        context.Events.AddRange(joburg10k, familyWalkEvent, cyclingEvent, pastRun);
        context.Enrolments.AddRange(
            johnJoburg, emilyJoburg, johnWalk, emilyCycling,
            johnPast, emilyPast, thaboPast, leratoPast);
        context.Results.AddRange(results);

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Demo data seeded. All demo accounts use the password '{Password}'.",
            DemoPassword);
    }
}

// =======================================================
// END: DEMO DATA SEEDER
// =======================================================
