# =======================================================
# OPTIONAL helper: records the Part 3 work as a series of small,
# logical commits on a feature branch.
#
# Run it from the repository root in PowerShell:
#     powershell -ExecutionPolicy Bypass -File scripts/part3_commits.ps1
#
# Tip: for the best history, commit as you work on future changes
# (git add -p, then git commit) instead of in one batch.
# =======================================================
$ErrorActionPreference = "Stop"

git rev-parse --is-inside-work-tree | Out-Null
git checkout -B part3-mvc

function Save-Commit([string]$Message, [string[]]$Paths) {
    foreach ($p in $Paths) {
        if (Test-Path $p) { git add -- $p }
    }
    git diff --cached --quiet
    if ($LASTEXITCODE -ne 0) {
        git commit -m $Message
    } else {
        Write-Host "Nothing to commit for: $Message"
    }
}

Save-Commit "chore(api): add Azure Blob package and XML documentation" @("RaceDay/RaceDay.csproj")
Save-Commit "feat(api): add profile picture and validation to models" @("RaceDay/Models")
Save-Commit "feat(api): add DTOs for profile, events, enrolments and results" @("RaceDay/DTOs")
Save-Commit "feat(api): add blob storage service and image validator" @("RaceDay/Services", "RaceDay/Extensions")
Save-Commit "feat(api): add database initialiser and demo data seeder" @("RaceDay/Data")
Save-Commit "feat(api): role-aware registration with organiser access code" @("RaceDay/Controllers/AuthController.cs")
Save-Commit "feat(api): add profile and profile picture endpoints" @("RaceDay/Controllers/UsersController.cs")
Save-Commit "feat(api): event ownership, filters, banners and event categories" @("RaceDay/Controllers/EventsController.cs", "RaceDay/Controllers/CategoriesController.cs")
Save-Commit "feat(api): enrolment rules, status updates and capacity checks" @("RaceDay/Controllers/EnrolmentsController.cs")
Save-Commit "feat(api): result validation and finisher counts" @("RaceDay/Controllers/ResultsController.cs")
Save-Commit "chore(api): configure Swagger, blob options, health check and settings" @("RaceDay/Program.cs", "RaceDay/appsettings.json", "RaceDay/appsettings.Development.json", "RaceDay/RaceDay.http", "RaceDay/Properties")
Save-Commit "test: add WebApplicationFactory test infrastructure" @("RaceDay.Tests/TestSupport", "RaceDay.Tests/RaceDay.Tests.csproj")
Save-Commit "test: cover authentication and role access" @("RaceDay.Tests/AuthenticationTests.cs", "RaceDay.Tests/RoleAccessTests.cs")
Save-Commit "test: cover events, enrolments, results and profile" @("RaceDay.Tests")
Save-Commit "feat(web): scaffold MVC project and configuration" @("RaceDay.Web/RaceDay.Web.csproj", "RaceDay.Web/Program.cs", "RaceDay.Web/appsettings.json", "RaceDay.Web/Properties")
Save-Commit "feat(web): add typed API client, bearer handler and session service" @("RaceDay.Web/Services", "RaceDay.Web/Filters")
Save-Commit "feat(web): add API models, view models and format helpers" @("RaceDay.Web/Models", "RaceDay.Web/Helpers")
Save-Commit "feat(web): add theme, layouts and shared partials" @("RaceDay.Web/wwwroot", "RaceDay.Web/Views/Shared", "RaceDay.Web/Views/_ViewImports.cshtml", "RaceDay.Web/Views/_ViewStart.cshtml")
Save-Commit "feat(web): add login, registration and profile with picture upload" @("RaceDay.Web/Controllers/AppController.cs", "RaceDay.Web/Controllers/AccountController.cs", "RaceDay.Web/Views/Account")
Save-Commit "feat(web): add home page, event browsing and enrolment" @("RaceDay.Web/Controllers/HomeController.cs", "RaceDay.Web/Controllers/EventsController.cs", "RaceDay.Web/Views/Home", "RaceDay.Web/Views/Events")
Save-Commit "feat(web): add participant enrolments and results pages" @("RaceDay.Web/Controllers/ParticipantController.cs", "RaceDay.Web/Views/Participant")
Save-Commit "feat(web): add organiser dashboard and event management" @("RaceDay.Web/Controllers/OrganiserController.cs", "RaceDay.Web/Controllers/OrganiserEventsController.cs", "RaceDay.Web/Views/Organiser", "RaceDay.Web/Views/OrganiserEvents")
Save-Commit "feat(web): add organiser category management" @("RaceDay.Web/Controllers/OrganiserCategoriesController.cs", "RaceDay.Web/Views/OrganiserCategories")
Save-Commit "feat(web): add organiser enrolment review" @("RaceDay.Web/Controllers/OrganiserEnrolmentsController.cs", "RaceDay.Web/Views/OrganiserEnrolments")
Save-Commit "feat(web): add organiser result recording" @("RaceDay.Web/Controllers/OrganiserResultsController.cs", "RaceDay.Web/Views/OrganiserResults")
Save-Commit "build: add Dockerfiles, dockerignore and docker-compose" @("RaceDay/Dockerfile", "RaceDay.Web/Dockerfile", ".dockerignore", "docker-compose.yml")
Save-Commit "ci: build API and MVC, run tests and build Docker images" @(".github/workflows/validation.yml", "scripts/validation.py", "RaceDay.slnx")
Save-Commit "docs: update database script and API endpoint plan" @("Database", "API-Plan")
Save-Commit "docs: rewrite README and add video script" @("README.md", "docs/Part3_Video_Script.md", "video", "scripts/part3_commits.ps1")
Save-Commit "chore: add remaining files" @(".")

Write-Host "Done. Push with: git push -u origin part3-mvc  (then open a pull request into main)"
