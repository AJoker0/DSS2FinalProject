# Todo Management API

# DSS2 Final Project: Todo Management API

**Student Name:** Titkov Andrii (2419050)  
**Course:** SE2

---

## 1. Project Overview

This document outlines the implementation of my Todo Management API built with ASP.NET Core 9.0. It follows RESTful API principles and runs in Docker containers. I also added optional integrations for caching (Redis) and event publishing (RabbitMQ), along with a PostgreSQL database.

Instead of a long list of features, in short: the API supports JWT authentication, public and private todo lists, and basic operations like filtering, sorting, and pagination. The whole setup is fully covered by the provided Cypress E2E tests and documented via Swagger.

![Project Architecture]
<img width="940" height="325" alt="зображення" src="https://github.com/user-attachments/assets/1679783c-f8b4-4e56-a6f1-355d42740638" />


---

## 2. How to Run Locally

Prerequisites

Make sure you have .NET 9 SDK, Docker, Docker Compose, and Visual Studio 2022 installed.
Quick Start

    Start the Infrastructure Open a terminal in the project root directory and start the required containers:
    Bash

    docker-compose up -d db redis rabbitmq

    This will launch PostgreSQL (port 5433), Redis (port 6379), and RabbitMQ (port 5672).

    Apply Database Migrations Open the Package Manager Console in Visual Studio and run:
    PowerShell

    Update-Database

    This creates the TodoDb database and applies all necessary tables.

    Run the API Start the application using the Todo.Api profile in Visual Studio. The backend will be available at:

    http://localhost:3087

    Run the Frontend Navigate to the frontend folder, install dependencies, and start the development server:
    Bash

    npm install
    npm run dev
---

## 3. Database Setup and Schema

![Database Diagram]
<img width="940" height="623" alt="зображення" src="https://github.com/user-attachments/assets/d92f9af5-5de1-4290-bccf-cb9826178d71" />


![Users Table Schema]
<img width="940" height="694" alt="зображення" src="https://github.com/user-attachments/assets/e5e34ad5-caf1-4d04-99ed-521a60601aa1" />


![Todos Table Schema]
<img width="940" height="618" alt="зображення" src="https://github.com/user-attachments/assets/c61168ca-04e5-4264-86bd-37727edf30e2" />


![EF Core Model Mapping]
<img width="940" height="572" alt="зображення" src="https://github.com/user-attachments/assets/1a989004-d4cf-4924-8f16-a5e80aef6398" />


![Database Migration History]
<img width="940" height="542" alt="зображення" src="https://github.com/user-attachments/assets/03d21387-90a1-45b8-b6c4-9231a718d558" />


---

## 4. API Documentation (Swagger)

 Navigate to http://localhost:3087/swagger to see all available endpoints.

### Authentication Flow
To test protected routes, use the POST /api/auth/register or POST /api/auth/login endpoint to get a JWT token. Then, click the "Authorize" button at the top of the Swagger page and paste your token in the format: Bearer <your_token>.

![Swagger UI - Endpoints Overview]
<img width="929" height="654" alt="зображення" src="https://github.com/user-attachments/assets/b475928d-b96b-4370-9a2f-20afb12b5672" />

![Swagger UI - Authentication]
<img width="940" height="636" alt="зображення" src="https://github.com/user-attachments/assets/4d30f83d-b689-4b84-bf0f-b52a9407041e" />

![Swagger UI - Create Todo]
<img width="940" height="592" alt="зображення" src="https://github.com/user-attachments/assets/e36bc885-5854-4a0d-a1fa-3603b3e0b4cc" />

![Swagger UI - Get Public Todos]
<img width="940" height="673" alt="зображення" src="https://github.com/user-attachments/assets/d47ae113-11bf-4e03-b2d1-994e5292f3b9" />

![Swagger UI - Filtering and Sorting]
<img width="940" height="677" alt="зображення" src="https://github.com/user-attachments/assets/7d0f1654-7dc8-4e0d-9cb7-e8f649b9fe38" />

![Swagger UI - Response Schema]
<img width="940" height="458" alt="зображення" src="https://github.com/user-attachments/assets/794e0e95-a24a-4a00-bff6-db347aa60d85" />

---

## 5. Bonus Integrations

I implemented both optional bonus tasks and added them to the Docker Compose setup.

Implementation: I used Microsoft.Extensions.Caching.StackExchangeRedis to cache the response of the public tasks list (GET /api/todos/public). The cache has a 1-minute TTL. This simple approach prevents the database from being queried every time a guest user opens the main page.

![Redis Cache Monitoring]
<img width="617" height="274" alt="зображення" src="https://github.com/user-attachments/assets/9dacfa2b-5c90-44c4-a203-234ae83e9302" />


### 5.2 RabbitMQ Integration (+10 Points)

Implementation: RabbitMQ is configured to handle domain events. Whenever a task state changes, the backend publishes a JSON event (TodoCreated, TodoUpdated, TodoCompleted, TodoDeleted) to the todo_events fanout exchange. This separates background event logic from the main CRUD operations.

![RabbitMQ Management Console]
<img width="608" height="273" alt="зображення" src="https://github.com/user-attachments/assets/222dd861-eea8-4e17-b973-82000d07dc49" />


---

## 6. Automated E2E Testing (Cypress)

The backend passes the provided Cypress E2E test suite. It checks everything from JWT authentication and user isolation to CRUD operations, pagination, filtering, and sorting.

### Port Configuration
The backend strictly listens on **port 3087** to ensure compatibility with the automated scoring system.

**Configuration in `Properties/launchSettings.json`:**
```json
"applicationUrl": "http://localhost:3087"
```

**Configuration in `Dockerfile`:**
ENV ASPNETCORE_URLS=http://+:3087

### Test Results

![Cypress Test Suite - Overview]
<img width="940" height="367" alt="зображення" src="https://github.com/user-attachments/assets/67ad1201-3020-473b-a182-9b66fef0725c" />


![Cypress Test Suite - Authentication Tests]
<img width="940" height="520" alt="зображення" src="https://github.com/user-attachments/assets/0d1ec842-690c-4669-ab01-5ade9e866307" />


![Cypress Test Suite - CRUD Operations]
<img width="940" height="521" alt="зображення" src="https://github.com/user-attachments/assets/639046bb-a99f-4581-a589-ea7e338ede48" />


![Cypress Test Suite - Filtering and Sorting]
<img width="940" height="581" alt="зображення" src="https://github.com/user-attachments/assets/8a731e92-5038-4f0c-a276-2f6d76812010" />


![Cypress Test Suite - Final Results]
<img width="940" height="516" alt="зображення" src="https://github.com/user-attachments/assets/75442c49-8bf9-4615-ad41-86bd33f8fa5e" />


![Cypress Test Suite - Coverage Summary]
<img width="940" height="523" alt="зображення" src="https://github.com/user-attachments/assets/3c33952c-50ee-4adb-aefb-5064cc0ff701" />

<img width="943" height="406" alt="зображення" src="https://github.com/user-attachments/assets/36beb675-edae-4a3c-acb8-8a900ba38cfd" />



