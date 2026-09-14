import os

required_paths = [
    "README.md",
    "ERD/RaceDay_ERD.drawio",
    "ERD/ERD_RaceDay.png",
    "API-Plan/RaceDay_API_Endpoint_Plan.md",
    "Database/RaceDayDB.sql",
    "scripts/validation.py",
    "video/README.md"
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

def check_video_link():
    print("\nChecking video link in README.md...\n")
    if not os.path.exists("README.md"):
        print("[MISSING] README.md")
        return False
    with open("README.md", "r", encoding="utf-8") as f:
        content = f.read()
        if "http" in content and "Video" in content:
            print("[OK] Video link found in README.md")
            return True
        else:
            print("[MISSING] Video link in README.md")
            return False

if __name__ == "__main__":
    structure_ok = validate_structure()
    video_ok = check_video_link()
    if structure_ok and video_ok:
        print("\n✅ Final check passed: repo structure and video link are valid.")
    else:
        print("\n❌ Final check failed: missing items or video link.")
