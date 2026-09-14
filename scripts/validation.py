import os

# Required structure
required_paths = [
    "README.md",
    "ERD/RaceDay_ERD.drawio",
    "ERD/ERD_RaceDay.png",
    "API-Plan/RaceDay_API_Endpoint_Plan.md",
    "Database/RaceDayDB.sql",
    "scripts/validation.py"
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
        print("\n✅ Repository structure is valid.")
    else:
        print("\n❌ Repository structure is incomplete.")
