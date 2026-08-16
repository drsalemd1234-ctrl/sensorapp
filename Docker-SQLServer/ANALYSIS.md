# Analysis — SensorApp Modernization

## Problems Identified in the Legacy Application

### Architecture and maintainability
- **God class anti-pattern:** All database access and business logic lived in a static `SM` class with no separation of concerns.
- **No dependency injection:** Controllers called static methods directly, making the code impossible to unit test or swap implementations.
- **Cryptic naming:** Classes and methods used opaque abbreviations (`SC`, `SM`, `D`, `G`, `P`) that obscure intent.
- **Schema management in application code:** Tables, stored procedures, and seed data were created inline at startup via raw SQL strings rather than versioned migrations.
- **Duplicated business logic:** Threshold parsing and alert creation were copy-pasted across `Save()`, `Calc()`, and the `sp_calc` stored procedure.

### Security
- **SQL injection:** Every query was built with string concatenation from user input (device names, timestamps, filter values).
- **Silent failures:** Empty `catch` blocks swallowed all exceptions, returning empty results or `false` with no logging.

### Data modeling
- **Single DTO for everything:** One `D` class represented devices, readings, alerts, and audit logs, overloading fields (`V`, `V2`, `Typ`, etc.).
- **Mixed entity types in one table:** Readings (`typ=1`) and alerts (`typ=3`) were stored in the same `t_dat` table, complicating queries and statistics.
- **Unstructured configuration:** Device settings (threshold, unit, interval) were stored as pipe-delimited strings (`thr=75|unit=C|int=30`), which cannot be queried or validated at the database level.
- **No referential integrity:** Foreign keys were commented out; orphaned readings were possible.
- **No indexes:** Time-range and device-filter queries would degrade as data volume grows.
- **Magic numbers:** Status, type, and flag values were undocumented integers scattered through the code.

### Operational gaps
- Connection strings were hardcoded in source rather than read from configuration.
- No structured logging.
- No input validation on API endpoints.
- No authentication or authorization.

---

## Data Stored Incorrectly or Inefficiently

| Legacy design | Problem |
|---------------|---------|
| `t_dat` holds both readings and alerts | Violates single-responsibility; stats queries need `CASE WHEN typ=3` workarounds |
| `cfg` pipe-delimited string on `t_dev` | Not queryable; parsing duplicated in C# and T-SQL |
| `ts` as TEXT (SQLite) / overloaded columns `v`, `v2`, `v3` | Unclear semantics; temperature/humidity/pressure not named explicitly |
| `t_log` with generic `ref` and `msg` | Minimal structure; alert vs. info events distinguished only by `flg` |
| Alerts inserted as fake "readings" with `typ=3` | Pollutes time-series data; skews aggregates unless explicitly filtered |

---

## What Was Changed and Why

### 1. Normalized data model
Split the monolithic schema into four focused tables:

- **`Devices`** — structured columns for threshold, unit, and reporting interval
- **`Readings`** — time-series sensor data with explicit temperature/humidity/pressure fields
- **`Alerts`** — threshold violations separated from raw readings
- **`AuditLogs`** — append-only event log

**Why:** Each entity has a clear purpose, foreign keys enforce integrity, and indexes on `(DeviceId, Timestamp)` support efficient time-range queries.

### 2. EF Core with versioned migrations
Replaced inline `CREATE TABLE` scripts with Entity Framework Core migrations applied at startup.

**Why:** Schema changes are version-controlled, reproducible across environments, and reviewable in pull requests.

### 3. Service layer with dependency injection
Introduced `IReadingService`, `IDeviceService`, and `IAuditService` injected into a renamed `SensorController`.

**Why:** Business logic is testable, controllers are thin, and dependencies are explicit.

### 4. Parameterized queries via EF Core
All database access now goes through LINQ/EF Core — no string-concatenated SQL.

**Why:** Eliminates SQL injection and removes the need for manual parameter handling.

### 5. Legacy API compatibility
Kept original routes (`/api/data`, `/api/dev`, etc.) and response shapes via `LegacyDataDto` mappers.

**Why:** Existing clients continue to work while the internal model is modernized. The legacy pipe-delimited `cfg` format is still accepted on input and emitted on output for device endpoints.

### 6. Configuration from environment
Connection strings are read from `appsettings.json` and overridden via Docker environment variables.

**Why:** Follows twelve-factor app principles; no secrets in source code.

### 7. Structured logging
Replaced silent catch blocks with `ILogger` warnings for rejected operations.

**Why:** Failures are visible during development and operations.

---

## Recommended Database Strategy by Data Type

This application stores **four distinct data types**. They have different access patterns, volumes, and consistency needs — so one database is not ideal at production scale.

### Data type breakdown

| Data type | Examples in app | Access pattern | Volume | Consistency needs |
|-----------|-----------------|----------------|--------|-------------------|
| **Device registry** | Name, location, sensor type, status | Read/write by ID; low frequency | Low (hundreds–thousands) | Strong; relational |
| **Device configuration** | Threshold, unit, reporting interval | Read on every ingest; occasional updates | Low | Strong; queryable fields |
| **Sensor readings** | Temperature, humidity, pressure + timestamp | Append-only; time-range queries | **Very high** (millions+) | Eventual OK; ingest speed matters |
| **Alerts** | Threshold violations | Insert on rule match; query recent history | Medium | Strong link to device |
| **Audit log** | "data saved", "dev updated", alert events | Append-only; query by device/time | Medium–high | Append-only; rarely updated |

### Current implementation (assessment)

| Component | Database used | Verdict for this scope |
|-----------|---------------|------------------------|
| Entire app | **SQL Server 2019** (Docker) | **Acceptable** — matches assessment starter, fixed schema issues, sufficient at seed/demo volume |

SQL Server was kept because the assessment Docker path already uses it, EF Core migrations integrate cleanly, and the current data volume (~200 seed readings) does not stress any engine.

### Recommended production architecture (better solution)

Split storage by data type — **polyglot persistence**:

```
┌─────────────────────────────────────────────────────────────┐
│                     SensorApp API                           │
└────────────┬──────────────────────┬─────────────────────────┘
             │                      │
             ▼                      ▼
   ┌─────────────────┐    ┌──────────────────────┐
   │  SQL Server or  │    │  Time-series store   │
   │  PostgreSQL     │    │  (readings only)     │
   │                 │    │                      │
   │  • Devices      │    │  TimescaleDB         │
   │  • Alerts       │    │  or InfluxDB         │
   │  • AuditLogs    │    │  or Azure Data       │
   │  • Config       │    │  Explorer            │
   └─────────────────┘    └──────────────────────┘
        relational              append + time-range
        low volume              high volume
```

| Data type | Recommended database | Why this is better than one SQL Server |
|-----------|---------------------|--------------------------------------|
| **Devices + config** | **SQL Server** or **PostgreSQL** | FK relationships, transactions, structured columns, easy joins with alerts |
| **Sensor readings** | **TimescaleDB** (PostgreSQL extension) or **InfluxDB** | Optimized for time-range queries, compression, retention policies, high ingest rate |
| **Alerts** | **SQL Server / PostgreSQL** | Must reference `DeviceId`; low volume; needs relational integrity |
| **Audit log** | **SQL Server / PostgreSQL** (or dedicated log store at scale) | Queryable history; can move to Elasticsearch if log volume explodes |

### Why readings should not stay in SQL Server at scale

| Concern | SQL Server (single table) | Time-series DB |
|---------|---------------------------|----------------|
| Ingest rate | Row-by-row INSERT slows under load | Built for bulk / high-frequency writes |
| Storage cost | Full row per reading, no specialized compression | Columnar compression, downsampling |
| Time-range queries | Needs careful indexing/partitioning | Native `time_bucket`, retention, rollups |
| Retention (e.g. drop data > 1 year) | Manual archive/delete jobs | Built-in retention policies |
| Aggregates (`avg`, `max` over 1 hour) | Works with indexes; cost grows with data | Continuous aggregates / pre-computed rollups |

The legacy app already showed this pain: readings and alerts mixed in `t_dat`, and stats required `CASE WHEN typ=3` — wrong model for time-series data even before volume becomes an issue.

### Preferred choice if picking one stack

| Priority | Recommendation |
|----------|----------------|
| **Best balance (my top pick)** | **PostgreSQL + TimescaleDB** — relational for devices/alerts/audit; hypertable for readings; single ops stack, open source |
| **Microsoft / Azure ecosystem** | **SQL Server** (metadata) + **Azure Data Explorer** or **InfluxDB** (readings) |
| **Cloud-native IoT** | **PostgreSQL** (config) + **InfluxDB** (readings) + optional **Kafka** for ingest buffering |
| **Assessment / MVP only** | **SQL Server only** — pragmatic; schema fixes matter more than engine change at this scale |

### What I implemented vs what I recommend

| Layer | Implemented now | Recommended at production scale |
|-------|-----------------|--------------------------------|
| Database engine | SQL Server only | Hybrid: SQL + time-series |
| Schema | Normalized (`Devices`, `Readings`, `Alerts`, `AuditLogs`) | Same logical model; `Readings` moves to time-series store |
| Readings table | `Readings` in SQL Server with index on `(DeviceId, Timestamp)` | TimescaleDB hypertable partitioned by time |
| Ingest path | Synchronous POST → DB | Queue (Kafka/Event Hubs) → batch write for high-frequency sensors |

**Reasoning:** The assessment evaluates **data architecture thinking**, not infrastructure migration. I fixed the data model inside SQL Server (correct types, separation, indexes) and would split readings to a time-series database when ingest volume or retention requirements justify the operational cost of a second store.

### Scaling path

| Phase | Volume | Database approach |
|-------|--------|-------------------|
| **1 — Assessment / MVP** | < 100K readings | SQL Server (current) ✅ |
| **2 — Growth** | 100K – 10M readings | SQL Server + partitioning + pagination + caching |
| **3 — IoT production** | 10M+ readings | Move `Readings` to TimescaleDB/InfluxDB; keep SQL for metadata |
| **4 — Enterprise** | Millions/day | Ingest queue → stream processor → time-series DB; SQL for config + alert engine |

---

## Trade-offs and Future Work

| Decision | Trade-off |
|----------|-----------|
| Kept SQL Server | Familiar to the original Docker setup; PostgreSQL would offer better JSON support if config becomes more complex |
| Legacy DTO mapping | Adds mapping layer complexity but preserves API contract |
| Alerts in separate table | Cleaner model, but `/api/data?tp=3` now queries `Alerts` instead of `t_dat` — behavior is equivalent |
| No authentication | Out of scope for 2–4 hours; would add JWT/API keys in production |
| No time-series DB | SQL Server handles current volume; at scale, TimescaleDB or InfluxDB would be better for readings |
| EF Core over Dapper | Slightly more overhead but safer defaults and easier migrations |

### With more time, I would:
1. Add unit tests for threshold/alert logic and integration tests against Testcontainers
2. Introduce proper request/response DTOs with validation (`FluentValidation`)
3. Add pagination to list endpoints
4. Implement authentication and rate limiting
5. Evaluate a dedicated time-series store for readings with SQL Server for device/config metadata
6. Add OpenTelemetry for distributed tracing
