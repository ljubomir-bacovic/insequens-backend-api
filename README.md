# Insequens

A task management Web API built with .NET 10, CQRS with MediatR, and Clean Architecture.

## Tech Stack

- .NET 10 / ASP.NET Core 10
- Entity Framework Core 10 with SQL Server
- MediatR 12 (CQRS command/query pipeline)
- FluentValidation (automatic input validation via pipeline behavior)
- ASP.NET Core Identity (user management)
- JWT Bearer authentication (15-min access tokens, 7-day refresh tokens with rotation)
- AutoMapper (query projections)
- MailKit (transactional email)
- Serilog (structured logging)
- Scalar (OpenAPI documentation)
- xUnit + FluentAssertions + NSubstitute (testing)

## Solution Structure

```
Insequens.sln
├── src/
│   ├── Insequens.Api                    → Controllers, middleware, DI composition root
│   ├── Insequens.Application            → Commands, queries, handlers, validators, behaviors
│   ├── Insequens.Domain                 → Entities, enums, DTOs, data access interfaces
│   └── Infrastructure/
│       ├── Insequens.Infrastructure.Data         → EF Core DbContext, Identity, migrations
│       └── Insequens.Infrastructure.DataAccess   → Generic Repository<T>, DataContext (UoW)
├── tests/
│   ├── Insequens.Application.Tests      → Handler + validator + behavior unit tests
│   └── Insequens.Api.Tests              → Integration tests (WebApplicationFactory)
└── docs/
    ├── insequens-v1-architecture-and-guidelines.md          → Full architecture & coding guidelines reference
    ├── insequens-v1-modernisation-plan.md                   → v1 implementation roadmap
    └── insequens-v2-enterprise-architecture-assessment.md   → v2 assessment and transformation backlog
```

## Architecture

The system follows Clean Architecture with CQRS. Every operation is a discrete command (write) or query (read) dispatched through MediatR. Three pipeline behaviors handle cross-cutting concerns automatically:

1. **LoggingBehavior** — logs request name and elapsed time for every operation.
2. **ValidationBehavior** — runs FluentValidation validators before the handler executes.
3. **OwnershipBehavior** — verifies the requesting user owns the resource via the `IOwned` marker interface.

Controllers are thin HTTP adapters that inject only `IMediator`, extract the user ID from JWT claims, and return `IActionResult`.

See [docs/insequens-v1-architecture-and-guidelines.md](docs/insequens-v1-architecture-and-guidelines.md) for the full reference.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (local or remote)

### Setup

1. Clone the repository:
   ```
   git clone https://github.com/your-org/insequens-backend-api.git
   cd insequens-backend-api
   ```

2. Initialize user secrets for local development:
   ```
   cd src/Insequens.Api
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "your-256-bit-secret-key-here-minimum-32-chars"
   ```
   `appsettings.Development.json` already points at a local SQL Express instance and a mail catcher on `localhost:1025`. Override either in User Secrets if your machine differs; see [docs/configuration.md](docs/configuration.md).

3. Restore the EF Core tools and apply migrations:
   ```
   dotnet tool restore
   dotnet ef database update --project src/Infrastructure/Insequens.Infrastructure.Data --startup-project src/Insequens.Api
   ```

4. Run the API:
   ```
   dotnet run --project src/Insequens.Api
   ```

5. Open the API docs at `http://localhost:5000/scalar/v1` (Development mode only).

### Running Tests

```
dotnet test
```

## Configuration

Settings come from `appsettings.json` (shape and safe defaults only), `appsettings.{Environment}.json` (localhost values only), User Secrets in Development, and environment variables, in increasing order of precedence. Deployed environments supply every real value through environment variables such as `Jwt__Key` and `Email__SmtpServer`.

[docs/configuration.md](docs/configuration.md) lists every setting, its environment variable, its validation and the per-environment matrix. `.env.example` is the template for container deployments.

Never commit real credentials, hostnames or IP addresses to configuration files.

## API Endpoints

All endpoints are under `/v1/` and require JWT authentication unless noted.

### Auth (no auth required)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/v1/auth/register` | Register a new user |
| GET | `/v1/auth/confirm-email` | Confirm email address |
| POST | `/v1/auth/login` | Login, returns JWT + refresh token |
| POST | `/v1/auth/refresh-token` | Refresh an expired access token |
| POST | `/v1/auth/forgot-password` | Request a password reset email |
| POST | `/v1/auth/reset-password` | Reset password with token |
| POST | `/v1/auth/logout` | Invalidate refresh token (auth required) |

### ToDoItem (auth required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/v1/todoitem` | List user's tasks (paginated) |
| POST | `/v1/todoitem` | Create a new task |
| GET | `/v1/todoitem/{id}` | Get task details |
| DELETE | `/v1/todoitem/{id}` | Delete a task |
| PATCH | `/v1/todoitem/{id}/togglecomplete` | Toggle completion status |
| PATCH | `/v1/todoitem/{id}/priority` | Update priority |
| PATCH | `/v1/todoitem/{id}/name` | Update name |
| PATCH | `/v1/todoitem/{id}/description` | Update description |
| PATCH | `/v1/todoitem/{id}/duedate` | Update due date |

## Project References

For contributor guidelines, coding standards, and architectural rules, see:

- [AGENTS.md](AGENTS.md) — Coding guidelines for AI agents and reviewers
- [docs/insequens-v1-architecture-and-guidelines.md](docs/insequens-v1-architecture-and-guidelines.md) — Full architecture, SOLID principles, and conventions
- [docs/insequens-v1-modernisation-plan.md](docs/insequens-v1-modernisation-plan.md) — v1 implementation roadmap
- [docs/insequens-v2-enterprise-architecture-assessment.md](docs/insequens-v2-enterprise-architecture-assessment.md) — Enterprise architecture assessment and v2 transformation backlog

## License

Proprietary. All rights reserved.
