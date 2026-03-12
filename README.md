# ScholarPath

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Backend CI](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/backend-ci.yml)
[![Frontend CI](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/frontend-ci.yml)

**AI-Powered Scholarship Discovery Platform**

Cairo University Graduation Project

---

## Overview

ScholarPath is an intelligent scholarship discovery platform that helps students find, track, and apply for scholarships worldwide. The platform features AI-powered recommendations that match students with relevant opportunities based on their profiles and an advisor consultation marketplace. ScholarPath supports both English and Arabic with full right-to-left (RTL) layout support.

---

## Key Features

- **Smart Scholarship Discovery** -- AI-driven recommendations matching students with scholarships based on field of study, country, GPA, and interests
- **Role-Based Access Control** -- Distinct experiences for Students, Consultants, Companies, and Admins with tailored dashboards and permissions
- **Real-Time Notifications** -- Live push notifications powered by SignalR for scholarship deadlines and upgrade approval status
- **Advisor / Consultant Marketplace** -- Book consultations with verified advisors for application reviews, essay feedback, and scholarship guidance
- **Application Tracker** -- Track scholarship application statuses (`Planned → Applied → Accepted/Rejected`) with reminder support and status-transition validation
- **Bilingual Support** -- Full English and Arabic localization with proper RTL layout rendering
- **Profile-Based Matching** -- Intelligent scholarship filtering based on GPA, field of study, nationality, and other profile attributes
- **Health Check & Rate Limiting** -- `/health` endpoint for monitoring and built-in rate limiting to protect API resources

> **Note:** Community features (groups, posts, discussions) are planned for a future sprint and are not currently active.

### Auth Lifecycle

- New users register with **no platform role** (`Unassigned`) and `IsOnboardingComplete=false`
- During onboarding:
  - Selecting **Student** activates the account immediately (`Role=Student`, `AccountStatus=Active`)
  - Selecting **Consultant** or **Company** creates an upgrade request and keeps the account `Role=Unassigned`, `AccountStatus=Pending`
- Admin approval is required before consultant/company users receive their requested role
- While onboarding is incomplete or account status is pending, frontend route guards keep the user on the onboarding/pending screen

---

## Tech Stack

| Layer | Technologies |
|---|---|
| **Backend** | .NET 10 / C#, ASP.NET Core Identity, HttpOnly Cookie Auth (JWT + Refresh Tokens), EF Core 10 (SQL Server), MediatR (CQRS), AutoMapper, FluentValidation, SignalR, Hangfire, Serilog, Redis, Swagger / OpenAPI, API Versioning |
| **Frontend** | React 19, TypeScript 5.7, Vite 6, MUI v6, Zustand 5, TanStack Query v5, Axios (with credentials), React Router v7, i18next, SignalR Client, Vitest |
| **DevOps** | GitHub Actions (CI/CD), Docker Compose (SQL Server + Redis) |

---

## Architecture

ScholarPath follows **Clean Architecture** principles, organized into four distinct layers:

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, value objects, domain interfaces — minimal external dependencies (Microsoft.Extensions.Identity.Stores for Identity base types) |
| **Application** | Use cases via CQRS (Commands/Queries) with MediatR, DTOs, FluentValidation validators, AutoMapper profiles |
| **Infrastructure** | EF Core DbContext, services, caching (Redis), background jobs (Hangfire), JWT token service |
| **API** | ASP.NET Core controllers, middleware pipeline, DI composition root |

All controllers delegate business logic to MediatR command and query handlers. Commands mutate state; queries are read-only and leverage Redis caching and `.AsNoTracking()` for performance.

---

## Project Structure

```
ScholarPath/
├── server/                            # .NET Backend
│   ├── src/
│   │   ├── ScholarPath.API/           # Controllers, Middleware, Program.cs
│   │   ├── ScholarPath.Application/   # CQRS Commands/Queries, DTOs, Validators
│   │   ├── ScholarPath.Domain/        # Entities, Enums, Interfaces
│   │   └── ScholarPath.Infrastructure/# EF Core, Services, Caching
│   └── tests/
│       ├── ScholarPath.UnitTests/
│       └── ScholarPath.IntegrationTests/
├── client/                            # React Frontend
│   └── src/
│       ├── components/                # Reusable UI components
│       ├── pages/                     # Page components
│       ├── services/                  # API service layer (Axios)
│       ├── stores/                    # Zustand state stores
│       ├── theme/                     # MUI theming
│       ├── i18n/                      # Internationalization (i18next)
│       ├── hooks/                     # Custom React hooks
│       ├── utils/                     # Utility functions and helpers
│       ├── test/                      # Test utilities and setup
│       └── types/                     # TypeScript type definitions
├── docs/                              # Documentation & Diagrams
└── docker-compose.yml                 # Development infrastructure (SQL Server + Redis)
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [SQL Server](https://www.microsoft.com/sql-server) or [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Infrastructure Setup

Start SQL Server and Redis using Docker Compose:

```bash
docker compose up -d
```

### Backend Setup

```bash
cd server

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project src/ScholarPath.Infrastructure --startup-project src/ScholarPath.API

# Run the API server
dotnet run --project src/ScholarPath.API
```

The API will be available at `http://localhost:5100` by default.

### Frontend Setup

```bash
cd client

# Install dependencies
npm install

# Start the development server
npm run dev
```

The frontend will be available at `http://localhost:3000` by default.

### Run Tests

Backend tests:

```bash
cd server
dotnet test
```

Frontend tests:

```bash
cd client
npm test
```

### Environment Variables

Create an `appsettings.Development.json` in `server/src/ScholarPath.API/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ScholarPathDb;User Id=sa;Password=YourPassword;TrustServerCertificate=true"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Enabled": true
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here-min-32-chars",
    "Issuer": "ScholarPath",
    "Audience": "ScholarPathApp",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

Create a `.env` file in `client/`:

```env
VITE_API_URL=http://localhost:5100/api/v1
```

---

## API Documentation

Interactive API documentation is available via Swagger UI when running in Development mode:

```
http://localhost:5100/swagger
```

---

## Team

| Name | Role | GitHub |
|---|---|---|
| Mahmoud Salem | Project Lead / Full Stack | [@ma7moudalysalem](https://github.com/ma7moudalysalem) |
| Yousra Elnoby | Frontend / Backend | [@yousra-elnoby](https://github.com/yousra-elnoby) |
| Tasneem Shaaban | Backend / Data | [@TasneemShaaban](https://github.com/TasneemShaaban) |
| Madiha | Frontend / Backend | [@Madiha6776](https://github.com/Madiha6776) |
| Nora Mohamed | Backend | [@norra-mmhamed](https://github.com/norra-mmhamed) |

---

## Contributing

We welcome contributions. Please read our [Contributing Guide](CONTRIBUTING.md) for details on the development workflow, branch naming conventions, commit message format, and pull request requirements.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
