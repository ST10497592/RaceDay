# RaceDay

RaceDay is a platform designed to modernize the management of sporting events such as running, walking, and cycling.  
It replaces outdated paper-based systems with a digital solution that supports two distinct user roles:

- **Organiser**: creates, edits, and deletes events; manages categories; views enrolments; records and updates results.  
- **Participant**: registers an account; browses events and categories; enrols in events; views personal results and profile information.  

This repository contains Part 1 of the Portfolio of Evidence, including:  
- Entity Relationship Diagram (ERD)  
- API Endpoint Plan  
- SQL Database Script  
- GitHub Actions CI/CD validation  
- Demonstration video link  


## User Roles

### Organiser
- Create, edit, and delete events  
- Manage categories  
- View enrolments  
- Record and update results  

### Participant
- Register an account  
- Browse events and categories  
- Enrol in events  
- View personal results and profile information

  ## Project Overview

This repository contains Part 1 of the RaceDay Portfolio of Evidence.  
It includes the following artefacts:

- **Entity Relationship Diagram (ERD)** – logical structure of the database  
- **API Endpoint Plan** – RESTful service endpoints for organisers and participants  
- **SQL Database Script** – tables, constraints, seed data, and verification queries  
- **GitHub Actions CI/CD** – workflow validation of repository structure  
- **Demonstration Video** – explanation of design, ERD, API plan, SQL script, and CI/CD evidence  


## CI/CD Validation

This repository uses **GitHub Actions** to automatically validate the structure.  
The workflow runs the `scripts/validation.py` script on every push or pull request to the `main` branch.  

### What it checks:
- Presence of required folders (`ERD/`, `API-Plan/`, `Database/`, `scripts/`)  
- Presence of required files (`README.md`, ERD diagram, API plan, SQL script, validation script)  

### Benefits:
- Ensures consistency across all commits  
- Provides immediate feedback if a file or folder is missing  
- Demonstrates CI/CD integration for the RaceDay project  


## Video Evidence

The demonstration video for RaceDay Part 1 is **not stored in this repository** due to file size limitations.  
The link to the video is provided:  
- In this README (see below)  
- In the submission portal  

👉 [RaceDay Part 1 Video Link] 
