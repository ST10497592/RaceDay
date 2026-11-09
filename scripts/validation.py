import os
import sys

# Required repository structure (Part 2 + Part 3)
required_paths = [
    "README.md",
    "ERD/RaceDay_ERD.drawio",
    "ERD/ERD_RaceDay.png",
    "API-Plan/RaceDay_API_Endpoint_Plan.md",
    "Database/RaceDayDB.sql",
    "scripts/validation.py",
    "RaceDay/RaceDay.csproj",
    "RaceDay/Dockerfile",
    "RaceDay.Web/RaceDay.Web.csproj",
    "RaceDay.Web/Program.cs",
    "RaceDay.Web/Dockerfile",
    "RaceDay.Tests/RaceDay.Tests.csproj",
    "docker-compose.yml",
    ".github/workflows/validation.yml",
]


def validate_structure():
    print("Validating repository structure...\n")
    all_ok = True
    for path in required_paths:
        if os.path.exists(path):
            print(f"[OK] {path}")
        else:
            print(f"[MISSING] {path}")
            all_ok = False
    return all_ok


if __name__ == "__main__":
    result = validate_structure()
    if result:
        print("\nRepository structure is valid.")
        sys.exit(0)
    print("\nRepository structure is incomplete.")
    sys.exit(1)
