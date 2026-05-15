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
    ├── architecture.md                  → Full architecture & coding guidelines reference
    └── modernisation-plan.md            → v1 implementation roadmap
```

## Architecture

The system follows Clean Architecture with CQRS. Every operation is a discrete command (write) or query (read) dispatched through MediatR. Three pipeline behaviors handle cross-cutting concerns automatically:

1. **LoggingBehavior** — logs request name and elapsed time for every operation.
2. **ValidationBehavior** — runs FluentValidation validators before the handler executes.
3. **OwnershipBehavior** — verifies the requesting user owns the resource via the `IOwned` marker interface.

Controllers are thin HTTP adapters that inject only `IMediator`, extract the user ID from JWT claims, and return `IActionResult`.

See [docs/architecture.md](docs/architecture.md) for the full reference.

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
   dotnet user-secrets set "ConnectionStrings:InsequensConnection" "Server=localhost;Database=Insequens;Integrated Security=True;TrustServerCertificate=True"
   dotnet user-secrets set "Jwt:Key" "your-256-bit-secret-key-here-minimum-32-chars"
   dotnet user-secrets set "Email:Password" "your-smtp-password"
   ```

3. Apply EF Core migrations:
   ```
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

Configuration is loaded from `appsettings.json`, environment-specific overrides (`appsettings.Development.json`), environment variables, and User Secrets.

| Setting | Description | Where to Set |
|---------|-------------|-------------|
| `ConnectionStrings:InsequensConnection` | SQL Server connection string | User Secrets / Env var |
| `Jwt:Key` | JWT signing key (min 32 chars) | User Secrets / Env var |
| `Jwt:Issuer` | JWT issuer URL | appsettings.json |
| `Jwt:Audience` | JWT audience URL | appsettings.json |
| `Email:SmtpServer` | SMTP server hostname | appsettings.json |
| `Email:Port` | SMTP port | appsettings.json |
| `Email:Username` | SMTP username | appsettings.json |
| `Email:Password` | SMTP password | User Secrets / Env var |
| `Email:From` | Sender email address | appsettings.json |
| `Cors:AllowedOrigins` | Allowed CORS origins array | appsettings.{Env}.json |

Never commit real credentials to appsettings.json. Use User Secrets for local dev and environment variables for deployed environments.

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
- [docs/architecture.md](docs/architecture.md) — Full architecture, SOLID principles, and conventions
- [docs/modernisation-plan.md](docs/modernisation-plan.md) — v1 implementation roadmap

## License

Proprietary. All rights reserved.
