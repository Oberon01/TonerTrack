# TonerTrack .NET 10 — Architecture

## Overview

A full enterprise-grade refactor of the Python/FastAPI TonerTrack application,
targeting **.NET 10 LTS** with Clean Architecture, CQRS, and Domain-Driven Design.

---

## Solution Structure

```
TonerTrack.NET/
├── TonerTrack.slnx                         ← .NET 10 default XML solution format
├── src/
│   ├── TonerTrack.Domain/                  ← Pure C# — zero external dependencies
│   ├── TonerTrack.Application/             ← CQRS use-cases, interfaces, validators
│   ├── TonerTrack.Infrastructure/          ← File I/O, SNMP, NinjaRMM, background polling
│   └── TonerTrack.Api/                     ← ASP.NET Core 10 Web API — composition root
└── tests/
    ├── TonerTrack.Domain.Tests/
    └── TonerTrack.Application.Tests/
```

### Dependency flow

```
Api  →  Application  →  Domain
Infrastructure      →  Domain   (via IPrinterRepository, ISnmpService, etc.)
```

Infrastructure depends on Domain but **Domain never depends on Infrastructure**.
That is the Dependency Inversion Principle — the high-level policy defines the
contract; the low-level detail implements it.

---

## Domain Layer

### Printer aggregate root

All state mutation goes through public behavioural methods — never via property setters.

| Method | What it does |
|---|---|
| `Printer.Create()` | Factory — validates IP and name |
| `ApplyPollResult()` | Updates supplies/status/page history; raises domain events |
| `RecordSnmpUnreachable()` | Increments offline counter; marks Offline after 3 failures |
| `Rename()` | Renames and sets `UserOverridden = true` |
| `SetCommunity()` | Changes SNMP community string |
| `SetLocation()` | Updates display location |

### Status evaluation (toner-only business rule)

```
Toner < 10%   →  Error
Toner 10–19%  →  Warning
Toner ≥ 20%   →  Ok
```

Paper jams and SNMP error alerts are **not** reflected in printer status.
This rule lives inside the aggregate — not in the API or a service layer.

### Value objects

| Type | Key behaviour |
|---|---|
| `SupplyLevel` | Immutable percentage; `IsLow` (< 20%), `IsCritical` (< 10%) |
| `Supply` | Name + level + `SupplyCategory` |
| `PrinterAlert` | Description + `AlertSeverity` |

### Domain events

Raised inside `ApplyPollResult` and dispatched *after* the aggregate is saved:

| Event | Trigger |
|---|---|
| `PrinterTonerLowEvent` | Any toner cartridge drops below 20% |
| `PrinterStatusChangedEvent` | Status transitions to `Error` |

---

## Application Layer (CQRS with MediatR)

Every external operation is a **Query** (read, no side-effects) or a **Command**
(write, may have side-effects).

### Queries
`GetAllPrintersQuery` · `GetPrinterByIpQuery` · `GetPrinterStatsQuery` · `GetPrinterUsageQuery`

### Commands
`AddPrinterCommand` · `UpdatePrinterCommand` · `DeletePrinterCommand` ·
`PollPrinterCommand` · `PollAllPrintersCommand` · `ImportPrintersCommand` ·
`CreateTonerTicketCommand`

### Pipeline

```
Request
  → ValidationBehavior<,>   (FluentValidation — rejects bad input before handler runs)
  → Handler
```

### Domain event flow

```
PollPrinterHandler
  → printer.ApplyPollResult()          // events queued inside aggregate
  → repo.UpdateAsync()                 // persisted first
  → dispatcher.DispatchAsync(events)   // MediatR publishes as INotification
  → TonerLowTicketHandler              // creates NinjaRMM ticket
```

`PollPrinterHandler` knows nothing about NinjaRMM — it only calls the dispatcher.
The coupling is through events, not direct method calls.

---

## Infrastructure Layer

### `JsonPrinterRepository`
- **Atomic writes** — writes to a temp file in the same directory, then `File.Move(overwrite:true)`
- **Audit log** — `printers_audit.log` receives a timestamped line for every ADD/DELETE
- **Thread-safe writes** — `SemaphoreSlim(1,1)` guards all write operations
- **Data compatibility** — JSON property names match the Python version exactly
  (`toner_cartridges`, `pages_history`, `ip`, etc.) so existing `printers.json`
  files load without any migration

### `SharpSnmpService`
Implements `ISnmpService` using **SharpSnmpLib**. Mirrors the OID strategy in
`snmp_utils.py` — same Printer MIB OIDs, same supply discovery walk, same alert
severity mapping. Runs SharpSnmpLib's synchronous API on `Task.Run` thread-pool
threads so the caller always gets an awaitable.

### `NinjaRmmService`
- **OAuth2 client-credentials** token acquisition
- **Thread-safe in-memory token cache** — `SemaphoreSlim(1,1)` double-check locking,
  30-second expiry buffer (same pattern as the Python version)
- Named `HttpClient` managed by `IHttpClientFactory`

### `PrinterPollingBackgroundService`
- `IHostedService` / `BackgroundService` registered via `AddHostedService<>`
- Creates a fresh DI scope (`CreateAsyncScope`) per polling cycle
- Configurable interval and initial startup delay via `PollingOptions`
- Graceful shutdown on `CancellationToken`

---

## API Layer

### Controllers

| Controller | Routes |
|---|---|
| `PrintersController` | `GET/POST /api/printers`, `GET/PUT/DELETE /api/printers/{ip}`, `POST ./{ip}/poll`, `POST ./poll-all`, `GET ./stats`, `GET ./{ip}/usage`, `POST ./import` |
| `ReportsController` | `GET /api/reports/monthly.csv`, `GET /api/reports/printers/{ip}/usage.csv` |
| `NinjaRmmController` | `POST /api/ninja/ticket` |

### Error handling

`GlobalExceptionMiddleware` maps domain exceptions to RFC 7807 problem-detail responses:

| Exception | HTTP Status |
|---|---|
| `ValidationException` | 422 Unprocessable Entity |
| `PrinterNotFoundException` | 404 Not Found |
| `PrinterDomainException` | 400 Bad Request |
| Anything else | 500 Internal Server Error (logged) |

---

## Key .NET Patterns Used

| Pattern | Where | Why |
|---|---|---|
| Aggregate root | `Printer` entity | Single consistency boundary |
| Value object | `SupplyLevel`, `Supply`, `PrinterAlert` | Immutable, equality by value |
| Domain events | `PrinterTonerLowEvent` | Decouple side-effects from core logic |
| CQRS | All MediatR handlers | Explicit read/write separation |
| Repository | `IPrinterRepository` | Persistence ignorance in Domain/Application |
| Pipeline behaviour | `ValidationBehavior<,>` | Cross-cutting validation |
| Options pattern | `NinjaRmmOptions`, `PollingOptions` | Strongly-typed configuration |
| Named HttpClient | `NinjaRmmService` | Proper `HttpClient` lifecycle |
| `IHostedService` | `PrinterPollingBackgroundService` | ASP.NET-managed background work |
| `InternalsVisibleTo` | Domain → Infrastructure/Tests | `RestoreFromPersistence` without leaking reconstitution into the public API |
| Primary constructors | All handlers and services | C# 12+ concise DI injection |
| Collection expressions | Throughout | C# 12+ `[]` instead of `new List<>()` |

---

## Getting Started

```bash
# 1. Set NinjaRMM credentials (never commit real secrets)
cd src/TonerTrack.Api
dotnet user-secrets set "NinjaRmm:ClientId"     "your-client-id"
dotnet user-secrets set "NinjaRmm:ClientSecret" "your-secret"

# 2. Run
dotnet run

# Swagger UI: https://localhost:5001/swagger

# 3. Run tests
dotnet test
```

## Migrating Data from the Python Version

Copy your existing `data/printers.json` into `src/TonerTrack.Api/data/`.
The JSON property names are identical to the Python version, so data loads
immediately with zero migration.
