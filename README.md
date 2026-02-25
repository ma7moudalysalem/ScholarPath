# ScholarPath

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Backend CI](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/backend-ci.yml)
[![Frontend CI](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/frontend-ci.yml/badge.svg)](https://github.com/ma7moudalysalem/ScholarPath/actions/workflows/frontend-ci.yml)

**AI-Powered Scholarship Discovery and Community Platform**

Cairo University Graduation Project

---

## Overview

ScholarPath is an intelligent scholarship discovery platform that helps students find, track, and apply for scholarships worldwide. The platform features AI-powered recommendations that match students with relevant opportunities based on their profiles, a community hub for collaboration and knowledge sharing, and an advisor consultation marketplace. ScholarPath supports both English and Arabic with full right-to-left (RTL) layout support.

---

## Key Features

- **Smart Scholarship Discovery** -- AI-driven recommendations that match students with scholarships based on profile data, academic background, and preferences
- **Role-Based Access Control** -- Distinct experiences for Students, Consultants, Companies, and Admins with tailored dashboards and permissions
- **Real-Time Communication** -- Live notifications and chat powered by SignalR for instant updates on scholarship deadlines and messages
- **Community Hub** -- Groups, posts, and discussion threads where students collaborate, share experiences, and support each other
- **Advisor / Consultant Marketplace** -- Book consultations with verified advisors for application reviews, essay feedback, and scholarship guidance
- **Bilingual Support** -- Full English and Arabic localization with proper RTL layout rendering
- **Profile-Based Matching** -- Intelligent scholarship filtering based on GPA, field of study, nationality, and other profile attributes
- **Health Check & Rate Limiting** -- `/health` endpoint for monitoring and built-in rate limiting to protect API resources

### Auth Lifecycle (Current Behavior)

- New users register with **no platform role** (`Unassigned`) and `IsOnboardingComplete=false`
- During onboarding:
  - Selecting **Student** activates the account immediately (`Role=Student`, `AccountStatus=Active`)
  - Selecting **Consultant** or **Company** creates an upgrade request and keeps the account `Role=Unassigned`, `AccountStatus=Pending`
- Admin approval is required before consultant/company users receive their requested role
- While onboarding is incomplete or account status is pending, frontend route guards keep the user on the onboarding/pending state

---

## Tech Stack

| Layer | Technologies |
|---|---|
| **Backend** | .NET 10 / C#, ASP.NET Core Identity, JWT + Refresh Tokens, EF Core (SQL Server + SQLite), MediatR (CQRS), AutoMapper, FluentValidation, SignalR, Hangfire, Serilog, Redis, Swagger / OpenAPI, API Versioning |
| **Frontend** | React 19, TypeScript 5.7, Vite 6, MUI v6, Zustand 5, TanStack Query v5, Axios, React Router v7, i18next, SignalR Client, Vitest |
| **DevOps** | GitHub Actions (CI/CD), Docker Compose (SQL Server + Redis) |

---

## Architecture

ScholarPath follows **Clean Architecture** principles, organized into four distinct layers:

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, value objects, domain interfaces -- minimal external dependencies (Microsoft.Extensions.Identity.Stores for Identity base types) |
| **Application** | Use cases implemented via CQRS (Commands/Queries) with MediatR, DTOs, validators, mapping profiles |
| **Infrastructure** | EF Core DbContext, repository implementations, external service integrations, caching (Redis), background jobs (Hangfire) |
| **API** | ASP.NET Core controllers, middleware, filters, dependency injection composition root |

The backend is designed for the **CQRS pattern** with MediatR to separate read and write operations. Currently, the Auth and Admin controllers implement business logic directly at the controller level for bootstrapping speed; these will be migrated to MediatR handlers as the domain stabilizes. All other feature controllers will follow the CQRS pattern from the start.

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
│       ├── services/                  # API service layer
│       ├── stores/                    # Zustand state stores
│       ├── theme/                     # MUI theming
│       ├── i18n/                      # Internationalization
│       ├── hooks/                     # Custom React hooks
│       ├── utils/                     # Utility functions and helpers
│       ├── test/                      # Test utilities and setup
│       └── types/                     # TypeScript type definitions
├── docs/                              # Documentation & Diagrams
└── docker-compose.yml                 # Development infrastructure
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [SQL Server](https://www.microsoft.com/sql-server) or [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Infrastructure Setup

Start the required services (SQL Server and Redis) using Docker Compose:

```bash
docker compose up -d
```

### Backend Setup

```bash
cd server/src/ScholarPath.API

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project ../ScholarPath.Infrastructure

# Run the API server
dotnet run
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

Create an `appsettings.Development.json` in `server/src/ScholarPath.API/` with the following configuration:

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
    "SecretKey": "your-secret-key-here",
    "Issuer": "ScholarPath",
    "Audience": "ScholarPathApp",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

Create a `.env` file in `client/` for frontend configuration:

```env
VITE_API_URL=http://localhost:5100/api/v1
```

---

## API Documentation

Interactive API documentation is available via Swagger UI when the backend is running in Development mode:

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
