# RaceDay — Sports Event Management API

## Project Overview

RaceDay is a sports event management platform developed using
ASP.NET Core Web API and .NET 8.

The application supports the management of sporting events,
categories, participant enrolments and race results.

## Technologies Used

- C#
- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- xUnit automated tests
- GitHub Actions

## Main Features

### Authentication and Authorisation

- Participant registration and login.
- Secure password hashing.
- JWT-based authentication.
- Role-based access control for organisers and participants.

### Event Management

- Retrieve all events and individual event details.
- Create, update and delete events.
- Validate event organisers and associated categories.

### Category Management

- Retrieve, create, update and delete categories.
- Restrict management operations to authorised organisers.

### Enrolment Management

- Create and retrieve participant enrolments.
- Apply authorisation rules to enrolment operations.
- Validate related participants and events.

### Results Management

- Create, retrieve, update and delete race results.
- Validate enrolments and prevent duplicate results.

## Database

The application uses SQL Server with Entity Framework Core.

The repository includes the database script and the
Entity Relationship Diagram in the `Database/` and `ERD/`
directories.

## Running the Application

1. Install the .NET 8 SDK.
2. Ensure SQL Server is available.
3. Configure the database connection string in the
   application's configuration.
4. Configure the JWT settings using appropriate development
   configuration or environment variables.
5. Open a terminal in the repository root.
6. Restore dependencies:

   `dotnet restore RaceDay/RaceDay.csproj`

7. Build the API:

   `dotnet build RaceDay/RaceDay.csproj --configuration Release`

8. Run the application:

   `dotnet run --project RaceDay/RaceDay.csproj`

## Automated Tests

Run the test suite using:

`dotnet test RaceDay.Tests/RaceDay.Tests.csproj --configuration Release`

## Continuous Integration

GitHub Actions automatically validates the project on pushes
and pull requests targeting the `main` branch.

The workflow restores dependencies, builds the API, runs
automated tests and executes the repository validation script.

See `.github/workflows/validation.yml`.

## Repository Structure

- `RaceDay/` — ASP.NET Core Web API.
- `RaceDay.Tests/` — automated tests.
- `Database/` — SQL database script.
- `ERD/` — database relationship diagram.
- `API-Plan/` — API endpoint planning documentation.
- `scripts/` — validation scripts.
- `docs/` — project documentation.
- `screenshots/` — project evidence.

## Demonstration Video

Add the actual Part 2 demonstration video URL here before
submitting the portfolio.

## Project Status

RaceDay Part 2 — API implementation, automated testing
and continuous integration.
