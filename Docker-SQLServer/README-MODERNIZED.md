# SensorApp — Modernized IoT Sensor API

Modernized version of the legacy SensorApp assessment project in this folder.

- **Original brief:** [README.md](./README.md)
- **Analysis and decisions:** [ANALYSIS.md](./ANALYSIS.md)
- **Detailed change guide (what/why/where/trade-offs):** [CHANGES_GUIDE.md](./CHANGES_GUIDE.md)
- **File comparison (deleted/modified/new):** [FILE_COMPARISON.md](./FILE_COMPARISON.md)

## Quick Start (Docker — recommended)

```bash
docker-compose up --build
```

API: **http://localhost:5000/swagger**

## What Changed

| Area | Before | After |
|------|--------|-------|
| Data access | Static `SM` class, raw SQL strings | EF Core + service layer with DI |
| Schema | Inline `CREATE TABLE` at startup | Versioned EF Core migrations |
| Entities | Single `D` model + mixed `t_dat` table | `Device`, `SensorReading`, `Alert`, `AuditLog` |
| Security | SQL injection via string concatenation | Parameterized EF Core queries |
| Config | Hardcoded connection string | `appsettings.json` + env vars |

Original API routes are preserved for backward compatibility.

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/data` | List readings/alerts (filters: `tp`, `did`, `df`, `dt`) |
| POST | `/api/data` | Ingest a sensor reading |
| GET | `/api/dev` | List devices (filter: `st`) |
| POST | `/api/dev` | Create/update a device |
| GET | `/api/calc?did=1` | Hourly avg/max vs threshold |
| GET | `/api/log` | Audit log (filters: `did`, `flg`) |
| GET | `/api/stats?did=1` | Aggregated stats for a device |

## Local Development (without Docker)

Requirements: .NET 8 SDK, SQL Server

```bash
cd SensorApp
dotnet ef database update
dotnet run
```

Update `appsettings.Development.json` with your SQL Server connection string.

## Project Structure

```
SensorApp/
├── Controllers/     # Thin API controllers
├── Data/            # DbContext, migrations, seed data
├── Dtos/            # Legacy-compatible API contracts
├── Models/          # Domain entities
└── Services/        # Business logic
```

## Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SensorDb` | SQL Server connection string (set in docker-compose) |

## Stopping

```bash
docker-compose down
```

To remove persisted database data:

```bash
docker-compose down -v
```
