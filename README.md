# SupportOS

> A production-grade internal IT support ticket system built with ASP.NET Core 8 Minimal API,
> demonstrating Clean Architecture, CQRS, domain-driven design, and automated Azure deployment.

[![Build & Deploy](https://github.com/dev-k99/SupportOS/actions/workflows/deploy.yml/badge.svg)](https://github.com/dev-k99/SupportOS/actions/workflows/deploy.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com)
[![Live API](https://img.shields.io/badge/Live%20API-Azure-0078D4)](https://supportos-api-bhb2cffee4cmg8f8.westeurope-01.azurewebsites.net/swagger/index.html)

**Live:** https://supportos-api-bhb2cffee4cmg8f8.westeurope-01.azurewebsites.net/swagger/index.html

---

## What this project demonstrates

This is not a CRUD tutorial. Every technical decision maps to a real enterprise engineering concern:

| Concern | Solution |
|---|---|
| Separation of concerns | Clean Architecture — Domain has zero framework dependencies |
| Command/query separation | CQRS via MediatR 12 with a typed pipeline |
| Consistent error handling | `Result<T>` pattern with `ErrorCode` enum, RFC 7807 problem details |
| Preventing duplicate mutations | Idempotency behavior (IMemoryCache, 24 h TTL, `X-Idempotency-Key` header) |
| Input validation | FluentValidation pipeline behavior — errors keyed by field name |
| Observability | Structured logging, performance warnings, full audit trail |
| Data integrity | EF Core interceptor captures before/after JSON for every write |
| Ticket lifecycle | Domain state machine (`CanTransitionTo`) enforced on the entity |
| SLA tracking | Domain service calculates due dates; `IsOverdue` and `FirstResponseTime` are computed properties |
| Security | JWT Bearer auth, per-IP rate limiting (10 req/min auth, 100 req/min API), BCrypt pw hashing |
| Deployment | GitHub Actions → Azure App Service, OIDC workload identity (no stored secrets) |
| Test coverage | Unit tests (Moq), integration tests (real MediatR pipeline + InMemory EF) |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  SupportOS.API  (ASP.NET Core 8 Minimal API)            │
│  • Endpoints (Auth, Tickets, Metrics, Health)           │
│  • GlobalExceptionMiddleware                            │
│  • Rate limiting (per-IP, fixed window)                 │
│  • Result → IResult HTTP mapping                        │
└────────────────────┬────────────────────────────────────┘
                     │ MediatR Send()
┌────────────────────▼────────────────────────────────────┐
│  SupportOS.Application  (Use Cases)                     │
│  MediatR pipeline:                                      │
│    IdempotencyBehavior                                  │
│      → LoggingBehavior                                  │
│        → ValidationBehavior (FluentValidation)          │
│          → PerformanceBehavior (warns > 500 ms)         │
│            → Command / Query Handler                    │
│  Commands: Register, Login, CreateTicket, AssignTicket, │
│            UpdateStatus, AddComment, Escalate, Close    │
│  Queries:  GetTicketById, GetTickets, GetOverdue,       │
│            GetDashboardMetrics                          │
└────────────────────┬────────────────────────────────────┘
                     │ IRepository / IUnitOfWork
┌────────────────────▼────────────────────────────────────┐
│  SupportOS.Infrastructure                               │
│  • EF Core 8 + SQL Server                               │
│  • AuditInterceptor (SaveChangesInterceptor)            │
│  • JwtService (token generation)                        │
│  • DataSeeder (idempotent, BCrypt at runtime)           │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  SupportOS.Domain  (zero framework dependencies)        │
│  • Entities: User, Ticket, Comment, Category, AuditLog  │
│  • Ticket domain logic: CanTransitionTo(), IsOverdue,   │
│    RecordFirstResponse(), FirstResponseTime             │
│  • SLACalculator (Low 48h / Med 24h / High 8h / Crit 2h)│
│  • Domain events: TicketCreated, Assigned, Escalated    │
│  • Result<T> + ErrorCode + IResult marker interface     │
└─────────────────────────────────────────────────────────┘
```

---

## Solution structure

```
SupportOS/
├── src/
│   ├── SupportOS.Domain/           # Entities, value objects, domain services, events
│   ├── SupportOS.Application/      # CQRS handlers, validators, behaviors, DTOs
│   ├── SupportOS.Infrastructure/   # EF Core, repositories, JWT, audit interceptor
│   └── SupportOS.API/              # Minimal API endpoints, middleware, program.cs
├── tests/
│   └── SupportOS.Tests/
│       ├── Behaviors/              # ValidationBehavior unit tests
│       ├── Domain/                 # SLACalculator unit tests
│       ├── Handlers/               # Command/query handler unit tests (Moq)
│       └── Integration/            # Full pipeline integration tests (InMemory EF)
└── .github/workflows/deploy.yml    # CI/CD: build → test → deploy to Azure
```

---

## API endpoints

### Authentication
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/auth/register` | Public | Register a new user |
| POST | `/auth/login` | Public | Login — returns JWT |

### Tickets
| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/tickets` | Any role | Create a ticket |
| GET | `/tickets` | Any role | List tickets (paginated, role-filtered) |
| GET | `/tickets/{id}` | Any role | Get ticket detail (internal comments hidden from Customer) |
| PATCH | `/tickets/{id}/status` | Agent / Admin | Transition ticket status |
| PATCH | `/tickets/{id}/assign` | Admin | Assign to an agent |
| PATCH | `/tickets/{id}/escalate` | Admin | Escalate priority (bump + recalculate SLA) |
| POST | `/tickets/{id}/comments` | Any role | Add comment (internal flag stripped for Customer) |
| DELETE | `/tickets/{id}/close` | Admin | Close a resolved ticket |

### Metrics & Health
| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/metrics/dashboard` | Admin | Open/in-progress/overdue counts, avg response time, per-agent stats |
| GET | `/metrics/overdue` | Agent / Admin | All currently overdue tickets |
| GET | `/health/live` | Public | Liveness probe |
| GET | `/health/ready` | Public | Readiness probe (SQL Server connectivity) |

---

## Ticket lifecycle

```
         ┌──────────┐
         │  Open    │
         └────┬─────┘
              │
         ┌────▼──────────┐
         │  In Progress  │◀──────────────────┐
         └────┬──────────┘                   │
              │                              │
    ┌─────────▼──────┐                       │
    │  Pending       │───────────────────────┘
    │  Customer      │
    └─────────┬──────┘
              │
         ┌────▼──────┐
         │  Resolved │
         └────┬──────┘
              │
         ┌────▼──────┐
         │  Closed   │  (Admin only)
         └───────────┘
```

Invalid transitions (e.g. Open → Resolved, Open → Closed) return `ErrorCode.InvalidOperation`.

---

## Key design decisions

### Result pattern over exceptions
Every handler returns `Result<T>` — a discriminated union carrying `IsSuccess`, `Value`, `ErrorCode`, and a `ValidationErrors` dictionary for field-level errors. HTTP mapping is done once in `ResultExtensions.ToHttpResult()` and never repeated across endpoints.

### Idempotency as a pipeline behavior
`IdempotencyBehavior<TRequest, TResponse>` intercepts any command implementing `IIdempotentCommand`. On cache hit it short-circuits the pipeline and returns the stored result. Only successful results are cached — transient failures are always retried. This prevents double-submit without any endpoint boilerplate.

### Validation returns field-level errors
`ValidationBehavior` groups FluentValidation failures by `PropertyName` into a `Dictionary<string, string[]>` and returns `Result.ValidationFailure(fieldErrors)`. The API maps this to RFC 7807 `ValidationProblemDetails` with HTTP 422 — the same shape frontend libraries like React Hook Form expect.

### Audit trail via EF Core interceptor
`AuditInterceptor` hooks `SavingChangesAsync`, serialises `OriginalValues` (Before) and `CurrentValues` (After) to JSON, and writes an `AuditLog` row for every entity change. The acting user's email is resolved from `IHttpContextAccessor` — the interceptor never needs to be called explicitly.

### SLA calculated from creation time, not mutation time
When a ticket is escalated, `SLADueAt` is recalculated as `ticket.CreatedAt + newPriorityHours`. Using `DateTime.UtcNow` instead would silently extend the SLA deadline every time the ticket is touched — a common bug that this implementation explicitly avoids.

### BCrypt not in EF migrations
BCrypt generates a new random salt on every call. Putting `HashPassword` in `HasData` would generate a different migration diff on every machine, making the migration non-deterministic. `DataSeeder` runs at startup instead, using an idempotent `AnyAsync` check before inserting.

---

## Running locally

**Prerequisites:** .NET 8 SDK, SQL Server (or LocalDB)

```bash
git clone https://github.com/dev-k99/SupportOS.git
cd SupportOS
```

Update `src/SupportOS.API/appsettings.json` with your connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SupportOS;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "your-256-bit-secret-key-here-change-in-production",
    "Issuer": "SupportOS",
    "Audience": "SupportOS"
  }
}
```

```bash
dotnet run --project src/SupportOS.API
# Open http://localhost:5000/swagger
```

The database is created, migrated, and seeded automatically on first run.

---

## Demo credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@supportos.io | Admin@1234 |
| Agent | agent@supportos.io | Agent@1234 |
| Customer | customer@supportos.io | Customer@1234 |

Login via `POST /auth/login`, copy the token, click **Authorize** in Swagger, enter `Bearer <token>`.

---

## Running tests

```bash
dotnet test
```

The test suite covers:

- **Unit tests** — handler logic with Moq (CreateTicket, AssignTicket, UpdateStatus, AddComment, GetOverdue, GetDashboard, SLACalculator, ValidationBehavior)
- **Integration tests** — full MediatR pipeline against InMemory EF Core with per-test DB isolation: ticket status transitions, invalid transitions, SLA escalation, user registration, ticket creation

---

## CI/CD

Every push to `main` triggers the GitHub Actions pipeline:

```
push to main
    │
    ├── build-and-test
    │       dotnet restore
    │       dotnet build --configuration Release
    │       dotnet test
    │
    └── deploy  (needs: build-and-test)
            dotnet publish
            azure/login  ← OIDC Workload Identity Federation (no stored secrets)
            azure/webapps-deploy → Azure App Service
```

Azure credentials are federated via OpenID Connect — no client secrets are stored in GitHub. The service principal is authorised only for the specific App Service resource.

---

## Tech stack

| Layer | Technology |
|---|---|
| Runtime | ASP.NET Core 8 Minimal API |
| CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| ORM | Entity Framework Core 8 + SQL Server |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Password hashing | BCrypt.Net-Next (work factor 12) |
| Testing | xUnit 2.6, Moq 4.20, FluentAssertions 6 |
| Docs | Swashbuckle / Swagger UI |
| CI/CD | GitHub Actions + OIDC |
| Hosting | Azure App Service (F1) + Azure SQL Database (free tier) |
