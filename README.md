# RaceDay — Sports Event Management Platform

[![RaceDay CI](https://github.com/YOUR-USERNAME/YOUR-REPOSITORY/actions/workflows/validation.yml/badge.svg)](https://github.com/YOUR-USERNAME/YOUR-REPOSITORY/actions/workflows/validation.yml)

## Project Overview

RaceDay is a sports event management platform built with ASP.NET Core 8.
It lets **organisers** publish running, walking and cycling events, manage categories,
review enrolments and record results, and lets **participants** browse events, enrol,
and follow their own results.

The solution has three parts:

| Project | Purpose |
|---|---|
| `RaceDay/` | ASP.NET Core Web API (JWT, EF Core, SQL Server, Swagger, Azure Blob uploads) |
| `RaceDay.Web/` | ASP.NET Core MVC web application. It consumes the API only (no database access) |
| `RaceDay.Tests/` | xUnit unit and integration tests for the API |

```
Browser  ->  RaceDay.Web (MVC)  ->  RaceDay API  ->  SQL Server
                                        |
                                        +-------->  Azure Blob Storage (images)
```

The MVC application never talks to the database or to Azure directly. Every action,
including image uploads, goes through the API.

## Technologies Used

- C#, ASP.NET Core 8 (Web API and MVC), Razor views, Bootstrap 5
- Entity Framework Core 8 and SQL Server
- JWT Bearer authentication and role-based authorisation
- Azure Blob Storage (Azurite emulator for local development)
- Docker and Docker Compose
- xUnit, `WebApplicationFactory` integration tests
- GitHub Actions

## User Roles

### Participant
- Register and sign in, edit profile, upload a profile picture.
- Browse and search events, view event details and results.
- Enrol in an event by choosing a category (enrolments start as *Pending*).
- View and cancel own enrolments, and view own results such as "47th out of 312 finishers".

### Organiser
- Dashboard with event and enrolment statistics.
- Create, edit and delete events and upload an event banner.
- Create and edit categories and assign them to events.
- Review enrolments, confirm or cancel them and set payment status.
- Record, edit and delete results.

Navigation and screens differ by role: organisers get a sidebar console, participants get
a top navigation bar. Pages are protected by role on the server, so a participant who types an
organiser URL is sent to an *Access denied* page.

### How organiser accounts are managed
Anyone can register as a participant. To prevent users from simply granting themselves
organiser rights, **organiser registration requires a secret access code** that the API checks
on the server (`Registration:OrganiserCode`). The code is changed through configuration or an
environment variable and is never stored in the web application.

## Running with Docker (recommended)

Requirements: Docker Desktop.

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| MVC web application | http://localhost:8081 |
| API Swagger UI | http://localhost:8080/swagger |
| Azurite Blob endpoint | http://localhost:10000/devstoreaccount1 |

Compose starts SQL Server, Azurite (local Azure Blob emulator), the API and the MVC app.
The database is created on first start and filled with demo data.

**Demo accounts** (password for all: `Password123!`)

| Role | Email |
|---|---|
| Organiser | `james.mokoena@raceday.test` |
| Organiser | `sarah.naidoo@raceday.test` |
| Participant | `john.smith@raceday.test` |
| Participant | `emily.williams@raceday.test` |

Organiser access code for new organiser registrations: `RACEDAY-ORGANISER-2026`
(demo value, **change it for any real deployment**).

Build the images individually:

```bash
docker build -f RaceDay/Dockerfile -t raceday-api .
docker build -f RaceDay.Web/Dockerfile -t raceday-web .
```

## Running without Docker

1. Install the .NET 8 SDK and SQL Server (LocalDB works).
2. Start Azurite (`npm install -g azurite` then `azurite-blob`) or point
   `AzureStorage:ConnectionString` at a real Azure Storage account.
3. Start the API (HTTPS profile, `https://localhost:7239`):

   `dotnet run --project RaceDay/RaceDay.csproj --launch-profile https`

4. In a second terminal start the MVC application:

   `dotnet run --project RaceDay.Web/RaceDay.Web.csproj --launch-profile https`

5. Open `https://localhost:7100`.

To load demo data locally set `Seed__DemoData=true` for the API.
The API address used by the MVC app is `ApiSettings:BaseUrl` in `RaceDay.Web/appsettings.json`.

## Azure Blob Storage

Images (profile pictures and event banners) are uploaded **to the API**, which validates them
(extension, content signature, maximum 5 MB) and stores them in the `raceday-images` container.
Only the resulting URL is saved in the database (`Users.ProfilePictureUrl`, `Events.BannerUrl`).

| Setting | Meaning |
|---|---|
| `AzureStorage:ConnectionString` | Storage connection string. `UseDevelopmentStorage=true` = Azurite |
| `AzureStorage:ContainerName` | Blob container (default `raceday-images`) |
| `AzureStorage:PublicBaseUrl` | Optional public URL override (used by Docker/Azurite) |

For a real Azure account supply the connection string with an environment variable
(`AzureStorage__ConnectionString`) or `dotnet user-secrets`. Never commit it.

## API Endpoints

See [`API-Plan/RaceDay_API_Endpoint_Plan.md`](API-Plan/RaceDay_API_Endpoint_Plan.md) for the
full list. Interactive documentation is available at `/swagger` when the API runs in Development.

## Automated Tests

```bash
dotnet test RaceDay.Tests/RaceDay.Tests.csproj --configuration Release
```

The suite covers authentication, event/category management, enrolment rules, results,
role access, profile management and image validation.

## Continuous Integration

GitHub Actions (`.github/workflows/validation.yml`) runs on every push and pull request:

1. restores and builds the **API**,
2. restores and builds the **MVC web application**,
3. runs the **automated tests**,
4. runs the repository validation script,
5. builds both **Docker images**.

### CI evidence
Add a screenshot of the green workflow run here, for example
`screenshots/ci-success.png`.

## Repository Structure

- `RaceDay/` — ASP.NET Core Web API (+ `Dockerfile`).
- `RaceDay.Web/` — ASP.NET Core MVC application (+ `Dockerfile`).
- `RaceDay.Tests/` — automated tests.
- `docker-compose.yml` — full local stack.
- `Database/` — SQL script. `ERD/` — entity relationship diagram.
- `API-Plan/` — endpoint documentation.
- `scripts/` — validation scripts. `docs/` — documentation. `screenshots/` — evidence.
- `video/` — link to the demonstration video.

## Demonstration Video

Video (Part 3 demonstration): https://youtu.be/REPLACE-WITH-YOUR-VIDEO-ID

The script for the video is in [`docs/Part3_Video_Script.md`](docs/Part3_Video_Script.md).

## Project Status

RaceDay Part 3 — MVC web application, Azure Blob Storage image uploads,
Docker containerisation and continuous integration.
