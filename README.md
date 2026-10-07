# Insequens

A task management Web API built with .NET 10, CQRS with MediatR, and Clean Architecture.

## Tech Stack

- .NET 10 / ASP.NET Core 10
- Entity Framework Core 10 with SQL Server
- MediatR 12 (CQRS command/query pipeline)
- FluentValidation (automatic input validation via pipeline behavior)
- ASP.NET Core Identity (user management)
- JWT Bearer authentication (15-min access tokens, 7-day refresh tokens stored as hashes, one session per device with rotation and reuse detection, signing key rotation)
- Account self-service: change password or email, export data, delete account
- Admin and Support roles with role-based authorization
- ASP.NET Core rate limiting, security headers, fallback authorization policy
- AutoMapper (query projections)
- MailKit (transactional email)
- Serilog (structured logging)
- API versioning (Asp.Versioning, URL segment), an OpenAPI document per version written to `docs/openapi/` on build, Scalar UI
- ProblemDetails (RFC 7807) for every error, with a stable `type` and a `traceId`
- Optimistic concurrency on tasks with `ETag` and `If-Match`
- xUnit + FluentAssertions + NSubstitute, SQLite in memory for handler tests (testing)

## Solution Structure

```
Insequens.sln
├── src/
│   ├── Insequens.Api                    → Controllers, middleware, DI composition root
│   ├── Insequens.Contracts              → Request/response records shared with clients (no dependencies)
│   ├── Insequens.Application            → Commands, queries, handlers, validators, behaviors, authorization, interfaces
│   ├── Insequens.Domain                 → Entities with behaviour and invariants, enums, domain exceptions
│   └── Insequens.Infrastructure         → EF Core DbContext, configurations, interceptors, migrations, Identity, tokens, email
├── tests/
│   ├── Insequens.Domain.Tests           → Entity behaviour and invariants
│   ├── Insequens.Application.Tests      → Handlers, validators, behaviors and authorization on SQLite in memory
│   ├── Insequens.Infrastructure.Tests   → Audit interceptor, email, JWT, source and dependency guards
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
3. **AuthorizationBehavior** — runs the authorization policies. A request marked `[RequiresRole]` needs that role (`403` otherwise). For requests marked `IOwned<TEntity>`, it loads the resource once, filtered by its owner, and hands it to the handler. A resource that is missing or belongs to someone else returns `404`.

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
   dotnet ef database update --project src/Insequens.Infrastructure --startup-project src/Insequens.Api
   ```

4. Run the API:
   ```
   dotnet run --project src/Insequens.Api
   ```

5. Open the API docs at `http://localhost:5008/scalar/v1` (Development and Staging only). The OpenAPI documents are at `/openapi/v1.json` and `/openapi/v2.json` in every environment, and in `docs/openapi/`.

### Running Tests

```
dotnet test
```

Docker must be running: the migration tests in `Insequens.Infrastructure.Tests` start SQL Server in a container.

## Configuration

Settings come from `appsettings.json` (shape and safe defaults only), `appsettings.{Environment}.json` (localhost values only), User Secrets in Development, and environment variables, in increasing order of precedence. Deployed environments supply every real value through environment variables such as `Jwt__Key` and `Email__SmtpServer`.

[docs/configuration.md](docs/configuration.md) lists every setting, its environment variable, its validation and the per-environment matrix. `.env.example` is the template for container deployments.

Never commit real credentials, hostnames or IP addresses to configuration files.

## API Endpoints

Endpoints are versioned by URL segment (`/v1/`, `/v2/`) and require JWT authentication unless noted. Responses carry `api-supported-versions`. Errors are `application/problem+json` with a stable `type` (`urn:insequens:error:...`) and a `traceId`.

### Auth (no auth required)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/v1/auth/register` | Register a new user. Always `202` with the same body, whether or not the email is already registered |
| GET | `/v1/auth/confirm-email` | Confirm email address |
| POST | `/v1/auth/login` | Login, returns JWT + refresh token and starts a session. Optional `deviceName`. Every failure is the same `401` |
| POST | `/v1/auth/refresh-token` | Rotate an expired access token and its refresh token. Reusing an already rotated refresh token ends that session. Every failure is the same `401` |
| POST | `/v1/auth/forgot-password` | Request a password reset email. Always `202` with the same body |
| POST | `/v1/auth/reset-password` | Reset password with token. Always `202` with the same body |
| POST | `/v1/auth/logout` | End this session (auth required) |
| POST | `/v1/auth/logout-all` | End every session of the user (auth required) |

Five failed logins lock the account for five minutes. Auth endpoints are rate-limited per client and per email address; any endpoint can return `429` with `Retry-After`.

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

A task that does not exist and a task owned by another user both return `404`. A name over 200 characters, a description over 4000 characters or a due date more than ten years from today returns `400`. A due-date body of `null` clears the date.

`GET /v1/todoitem/{id}` returns an `ETag`. Send it back as `If-Match` on a PATCH or DELETE to change the task only if nobody else has: a stale one returns `412`. Without `If-Match`, a change that collides with a concurrent one returns `409`.

The same endpoints are also served under `/v2/tasks`. v2 is where breaking changes (string priorities, a single PATCH) will land; v1 stays as it is.

### Account (auth required)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/v1/account/change-password` | Change the password with the current one; ends every session. `204` |
| POST | `/v1/account/change-email` | Send a confirmation link to the new address; needs the current password. Always `202` with the same body |
| POST | `/v1/account/confirm-email-change` | Apply the link from that email (no auth required); ends every session |
| POST | `/v1/account/deletion` | Delete the account; needs the current password. Sign-in stops at once; data is purged after the grace period. `202` |
| GET | `/v1/account/export` | Download the user's account, tasks and sessions as JSON |

A wrong current password returns `400` and counts towards lockout.

### Admin (Admin role required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/v1/admin/ping` | `200` for an admin, `403` for anyone else |

## Project References

For contributor guidelines, coding standards, and architectural rules, see:

- [AGENTS.md](AGENTS.md) — Coding guidelines for AI agents and reviewers
- [docs/insequens-v1-architecture-and-guidelines.md](docs/insequens-v1-architecture-and-guidelines.md) — Full architecture, SOLID principles, and conventions
- [docs/insequens-v1-modernisation-plan.md](docs/insequens-v1-modernisation-plan.md) — v1 implementation roadmap
- [docs/insequens-v2-enterprise-architecture-assessment.md](docs/insequens-v2-enterprise-architecture-assessment.md) — Enterprise architecture assessment and v2 transformation backlog

## License

Proprietary. All rights reserved.
