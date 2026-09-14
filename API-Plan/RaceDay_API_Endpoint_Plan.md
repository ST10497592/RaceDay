# RaceDay API Endpoint Plan

This document lists the planned RESTful API endpoints for RaceDay.  
Each endpoint specifies HTTP method, route, description, role required, request body, and response/error codes.

---

## Authentication
- **POST /api/auth/register** → Register a new user (201; 400; 409)  
- **POST /api/auth/login** → Authenticate a user (200; 401)  
- **POST /api/auth/logout** → End authenticated session (200; 401)  

## Users
- **GET /api/users/me** → Get current profile (200; 401)  
- **PUT /api/users/me** → Update current profile (200; 400; 401)  

## Events
- **GET /api/events** → List available events (200)  
- **GET /api/events/{id}** → Get one event (200; 404)  
- **POST /api/events** → Create an event (201; 400; 401; 403)  
- **PUT /api/events/{id}** → Update an event (200; 400; 401; 403; 404)  
- **DELETE /api/events/{id}** → Delete an event (204; 401; 403; 404)  

## Categories
- **GET /api/categories** → List categories (200)  
- **GET /api/categories/{id}** → Get one category (200; 404)  
- **POST /api/categories** → Create category (201; 400; 401; 403)  
- **PUT /api/categories/{id}** → Update category (200; 400; 401; 403; 404)  
- **DELETE /api/categories/{id}** → Delete category (204; 401; 403; 404)  

## Event-Categories
- **GET /api/events/{eventId}/categories** → List event categories (200; 404)  
- **POST /api/events/{eventId}/categories/{categoryId}** → Assign category to event (201; 400; 401; 403; 404; 409)  
- **DELETE /api/events/{eventId}/categories/{categoryId}** → Remove category from event (204; 401; 403; 404)  

## Enrolments
- **POST /api/enrolments** → Create enrolment (201; 400; 401; 404; 409)  
- **GET /api/enrolments/my** → List my enrolments (200; 401)  
- **GET /api/enrolments/{id}** → Get an enrolment (200; 401; 403; 404)  
- **DELETE /api/enrolments/{id}** → Cancel an enrolment (204; 401; 403; 404)  
- **GET /api/events/{eventId}/enrolments** → View event enrolments (200; 401; 403; 404)  

## Results
- **POST /api/results** → Record a result (201; 400; 401; 403; 404; 409)  
- **GET /api/results/{id}** → View a result (200; 401; 404)  
- **PUT /api/results/{id}** → Update a result (200; 400; 401; 403; 404)  
- **GET /api/results/my** → View my results (200; 401)  
- **GET /api/events/{eventId}/results** → View event results (200; 404)  
