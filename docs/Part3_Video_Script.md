# Part 3 video script (target length: 6-8 minutes)

Record the screen with your voice. Keep the browser zoomed so the text is readable.

## 1. Introduction (30 s)
- Say your name, student number and the project name: RaceDay.
- One sentence: an ASP.NET Core MVC app that consumes my RaceDay API, with Azure Blob image uploads, Docker and CI.

## 2. Repository and GitHub (45 s)
- Show the GitHub repository: folder structure (`RaceDay`, `RaceDay.Web`, `RaceDay.Tests`, `docker-compose.yml`).
- Show the commit history (20+ commits, meaningful messages).
- Show the green GitHub Actions run (build API, build MVC, tests, validation, Docker images).

## 3. Running with Docker (45 s)
- In a terminal run `docker compose up --build` (or show it already running).
- Show `docker ps`: sqlserver, azurite, api, web.
- Open Swagger at `http://localhost:8080/swagger` for 10 seconds.

## 4. Authentication and role-based navigation (1 min)
- Register a new participant (show validation errors first: empty form, mismatched passwords).
- Show the participant navigation (top bar: Browse events, My enrolments, My results).
- Sign out. Sign in as `james.mokoena@raceday.test` / `Password123!`.
- Show the organiser sidebar layout - clearly different from the participant UI.
- While signed in as a participant, type an organiser URL (`/organiser`) to show *Access denied*.

## 5. Organiser interface (2 min)
- Dashboard statistics.
- Create an event (show validation), upload a banner image, assign categories.
- Edit the event, show the event in the list.
- Categories page: create and edit a category.
- Enrolments: confirm a pending enrolment, change payment status.
- Results: record a result (h:mm:ss finish time), edit it, show a DNF.

## 6. Participant interface (1.5 min)
- Browse and filter events, open event details.
- Enrol in an event with a category (show the "pending" status).
- My enrolments: cancel one.
- My results: show "47th out of 312 finishers"-style text.
- Profile: edit details and upload a profile picture.

## 7. Azure Blob Storage (45 s)
- Show that the image uploaded in the app is served from the blob URL.
- Open Azure Storage Explorer (or the Azure portal) and show the `raceday-images` container with the uploaded files.
- Explain: the MVC app only calls the API; the API validates the image and stores it in Blob Storage; only the URL is saved in SQL.

## 8. Code highlights (1 min)
- `RaceDayApiClient.cs` - the only class that calls the API; friendly error handling.
- `SessionAuthorizeAttribute.cs` - role protection.
- `BlobStorageService.cs` and `ImageUploadValidator.cs` in the API.
- `Dockerfile` (multi-stage) and `docker-compose.yml`.

## 9. Closing (15 s)
- Summarise: MVC + API + Blob + Docker + CI. Thank the viewer.
