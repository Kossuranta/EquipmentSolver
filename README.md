# Equipment Solver

A web application that helps gamers find optimal equipment loadouts. Users input available equipment with their stats, define constraints and weighted priorities, and the solver computes the best equipment combinations.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# / ASP.NET Core (.NET 10) |
| Frontend | Angular 21 + TypeScript + Angular Material |
| Database | PostgreSQL 17 via EF Core (Npgsql) |
| Auth | ASP.NET Identity + JWT |
| Logging | Serilog (console + file) |
| Testing | xUnit |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 24+](https://nodejs.org/) (includes npm)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL, or use a local install)

## Getting Started (Development)

### 1. Start PostgreSQL

The easiest way is via docker-compose — this starts only the database container:

```bash
docker compose up db -d
```

This creates a PostgreSQL instance on `localhost:5432` with:
- **Database**: `equipment_solver` (production) / `equipment_solver_dev` (created automatically on first run)
- **Username**: `postgres`
- **Password**: `changeme`

> Alternatively, use any PostgreSQL instance and update the connection string in `src/EquipmentSolver.Api/appsettings.Development.json`.

### 2. Start the Backend API

```bash
dotnet run --project src/EquipmentSolver.Api --launch-profile http
```

The API starts at **http://localhost:5105**. On first run, it automatically applies EF Core migrations to create the database schema.

### 3. Start the Angular Frontend

```bash
cd src/EquipmentSolver.Web
npm install    # first time only
npx ng serve
```

The frontend starts at **http://localhost:4200** with a proxy that forwards `/api/*` requests to the backend at `localhost:5105`.

### Summary

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5105 |
| OpenAPI spec | http://localhost:5105/openapi/v1.json (dev only) |
| PostgreSQL | localhost:5432 |

## Docker (Production)

Build and run the full stack (app + database) in Docker:

```bash
# Set required environment variables
export POSTGRES_PASSWORD=your_secure_password
export JWT_SECRET=your_secret_key_at_least_32_characters_long

# Build and start everything
docker compose up -d
```

The app is available at **http://localhost:8080**. The Angular frontend is built and served as static files from the .NET backend.

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_PASSWORD` | PostgreSQL password | `changeme` |
| `JWT_SECRET` | Secret key for JWT signing (min 32 chars) | placeholder (change in production!) |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET environment | `Production` |

## Project Structure

```
EquipmentSolver/
├── src/
│   ├── EquipmentSolver.Api/            # ASP.NET Core Web API + serves Angular static files
│   ├── EquipmentSolver.Core/           # Domain models, interfaces, services
│   ├── EquipmentSolver.Infrastructure/ # EF Core DbContext, migrations, external services
│   └── EquipmentSolver.Web/            # Angular frontend (Angular Material)
├── tests/
│   └── EquipmentSolver.Tests/          # xUnit tests
├── spec/                               # Project specification and progress tracking
├── docker-compose.yml
├── Dockerfile
└── AGENTS.md                           # AI agent coding guide
```

## Common Commands

```bash
# Build the entire .NET solution
dotnet build

# Run tests
dotnet test

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> \
  --project src/EquipmentSolver.Infrastructure \
  --startup-project src/EquipmentSolver.Api \
  --output-dir Data/Migrations

# Build Angular for production
cd src/EquipmentSolver.Web && npx ng build --configuration production

# Stop the database container
docker compose down
```

## Specs & Progress

- **[spec/README.md](spec/README.md)** — Full project specification
- **[spec/PROGRESS.md](spec/PROGRESS.md)** — Progress tracker (what's done, what's next)
