# RaceDay API Endpoint Plan

This document lists the RESTful API endpoints for RaceDay (updated for Part 3).
Each endpoint specifies the HTTP method, route, role required and the response codes.
All endpoints except register, login and the public GET endpoints require a JWT Bearer token.

Roles: **Public** (no token), **Any** (any signed-in user), **Participant**, **Organiser**.

---

## Authentication
- **POST /api/auth/register** (Public) -> Register a Participant, or an Organiser when a valid `organiserCode` is supplied (201; 400; 403 invalid organiser code; 409 duplicate email)
- **POST /api/auth/login** (Public) -> Authenticate; returns token, expiry and user (200; 400; 401)
- **POST /api/auth/logout** (Any) -> End authenticated session (200; 401)

## Users
- **GET /api/users/me** (Any) -> Get current profile (200; 401)
- **PUT /api/users/me** (Any) -> Update current profile (200; 400; 401)
- **POST /api/users/me/profile-picture** (Any) -> Upload profile picture (multipart `file`) to Azure Blob Storage (200; 400; 401; 503)

## Events
- **GET /api/events** (Public) -> List events; filters: `search`, `type`, `status`, `organiserId`, `upcoming`, `fromDate`, `toDate` (200)
- **GET /api/events/mine** (Organiser) -> Organiser's events with enrolment counts (200; 401; 403)
- **GET /api/events/{id}** (Public) -> Get one event with its categories (200; 404)
- **POST /api/events** (Organiser) -> Create an event owned by the caller (201; 400; 401; 403)
- **PUT /api/events/{id}** (Organiser, owner) -> Update an event (204; 400; 401; 403; 404)
- **DELETE /api/events/{id}** (Organiser, owner) -> Delete an event without enrolments (204; 401; 403; 404; 409)
- **POST /api/events/{id}/banner** (Organiser, owner) -> Upload banner image to Azure Blob Storage (200; 400; 401; 403; 404; 503)

## Categories
- **GET /api/categories** (Public) -> List categories (200)
- **GET /api/categories/{id}** (Public) -> Get one category (200; 404)
- **POST /api/categories** (Organiser) -> Create category (201; 400; 401; 403; 409)
- **PUT /api/categories/{id}** (Organiser) -> Update category (204; 400; 401; 403; 404)
- **DELETE /api/categories/{id}** (Organiser) -> Delete category not in use (204; 401; 403; 404; 409)

## Event-Categories
- **GET /api/events/{eventId}/categories** (Public) -> List event categories (200; 404)
- **POST /api/events/{eventId}/categories/{categoryId}** (Organiser, owner) -> Assign category to event (201; 400; 401; 403; 404; 409)
- **DELETE /api/events/{eventId}/categories/{categoryId}** (Organiser, owner) -> Remove category from event (204; 401; 403; 404)

## Enrolments
- **POST /api/enrolments** (Participant) -> Enrol; always created as Pending; checks event is Open, category is offered, no duplicates, capacity (201; 400; 401; 403; 404; 409)
- **GET /api/enrolments** (Organiser) -> Enrolments for the organiser's events (200; 401; 403)
- **GET /api/enrolments/my** (Participant) -> List my enrolments (200; 401)
- **GET /api/enrolments/{id}** (Participant owner / Organiser owner) -> Get an enrolment (200; 401; 403; 404)
- **PATCH /api/enrolments/{id}/status** (Organiser, owner) -> Confirm/cancel and set payment status (200; 400; 401; 403; 404)
- **DELETE /api/enrolments/{id}** (Participant owner / Organiser owner) -> Cancel an enrolment without a result (204; 401; 403; 404; 409)
- **GET /api/events/{eventId}/enrolments** (Organiser, owner) -> View event enrolments (200; 401; 403; 404)

## Results
- **POST /api/results** (Organiser, owner) -> Record a result; finish time and position required for `Finished` (201; 400; 401; 403; 404; 409)
- **GET /api/results/{id}** (Any) -> View a result with total finishers (200; 401; 404)
- **PUT /api/results/{id}** (Organiser, owner) -> Update a result (204; 400; 401; 403; 404; 409)
- **DELETE /api/results/{id}** (Organiser, owner) -> Delete a result (204; 401; 403; 404)
- **GET /api/results/my** (Participant) -> View my results (200; 401)
- **GET /api/events/{eventId}/results** (Public) -> View event results (200; 404)

## Operational
- **GET /health** (Public) -> Liveness check used by Docker (200)
