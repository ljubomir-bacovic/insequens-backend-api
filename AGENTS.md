# Insequens Backend — Coding Guidelines for AI Agents & Code Reviewers

> This document is the single source of truth for all AI coding agents (Copilot, Claude Code, Cursor, Codex) and AI code reviewers (CodeRabbit) working on the Insequens backend. It defines the architecture, patterns, rules, and anti-patterns that must be enforced on every pull request.

## System Overview

Insequens is a .NET 10 Web API for task management. It uses CQRS with MediatR, Clean Architecture, Entity Framework Core with SQL Server, JWT Bearer authentication, FluentValidation, and a generic repository pattern.

## Solution Structure & Dependency Rules

```
src/Insequens.Api              → ASP.NET Core host, thin controllers, middleware, DI composition root
src/Insequens.Application      → Commands, queries, handlers, validators, pipeline behaviors, AutoMapper profiles
src/Insequens.Domain           → Entities, base types, enums, DTOs, data access interfaces (ZERO project references)
src/Infrastructure.Data        → EF Core DbContext, Identity, migrations
src/Infrastructure.DataAccess  → Generic Repository<T>, DataContext (Unit of Work)
tests/                         → xUnit test projects
```

**The Dependency Rule — flag violations as CRITICAL:**
- Domain references NOTHING. If Domain ever imports Application, Api, or Infrastructure, reject the PR.
- Application references only Domain.
- Infrastructure references only Domain.
- Api references Application and Infrastructure (composition root).

## CQRS Architecture

Every operation is either a Command (changes state) or a Query (reads state). Never mixed.

**Request flow:** Controller → `_mediator.Send()` → LoggingBehavior → ValidationBehavior → OwnershipBehavior → Handler → Repository → DB

### Commands

- Immutable `record` types implementing `IRequest<T>`.
- Named `{Verb}{Entity}Command` (e.g., `CreateToDoItemCommand`, `DeleteToDoItemCommand`).
- Commands that mutate an existing user-owned resource MUST implement `IOwned` (provides `Guid ItemId` + `Guid UserId`).
- Commands that create new resources include `Guid UserId` but do NOT implement `IOwned`.
- `UserId` is always set by the controller from JWT claims, never trusted from the request body.
- Return `Unit` for void operations, or a response DTO for create operations.

### Queries

- Immutable `record` types implementing `IRequest<T>`.
- Named `Get{What}Query` (e.g., `GetToDoItemQuery`, `GetUserToDoItemsQuery`).
- Queries reading a specific resource by ID MUST implement `IOwned`.
- List queries filter by `UserId` in the handler and do NOT implement `IOwned`.
- List queries MUST return `PaginatedResult<T>`, never bare `List<T>`.

### Handlers

- One handler per command/query. Never handle multiple request types.
- Named `{Verb}{Entity}Handler`, matching the command/query.
- Live in the same folder as their command/query.
- Inject `IDataContext` and `IMapper` (for queries). Nothing else unless truly necessary.
- For `IOwned` requests, the handler can assume the entity exists and is owned (OwnershipBehavior already validated). Use null-forgiving operator `!` on `FindAsync`.
- Command handlers construct entities explicitly. DO NOT use AutoMapper for writes.
- Query handlers use `ProjectTo<TDto>()` for efficient SQL projection.

### Validators

- Named `{CommandOrQuery}Validator`.
- One validator per command/query that accepts user input.
- Auto-discovered by `AddValidatorsFromAssembly`. No manual registration needed.
- Validate shape and range. Business rules belong in handlers.
- Pagination: `Page > 0`, `PageSize` between 1 and 100.

## Ownership Enforcement — MOST CRITICAL SECURITY CONTROL

The `IOwned` marker interface + `OwnershipBehavior` pipeline behavior is the authorization layer.

**Non-negotiable rule:** Any command or query that accesses a specific resource by ID MUST implement `IOwned`. Missing `IOwned` on a resource-accessing request is a CRITICAL security defect. Flag it immediately.

The OwnershipBehavior:
1. Looks up the entity by `ItemId`
2. Throws `ToDoItemNotFoundException` if not found
3. Throws `ResourceForbiddenException` if `entity.UserId != request.UserId`

## Controller Rules — Flag Violations

Controllers are thin HTTP adapters. They do exactly three things: extract UserId from JWT, send command/query via `_mediator.Send()`, return HTTP status code.

**Flag as defects:**
- Controller injecting anything other than `IMediator`.
- Controller containing `if` statements with business logic.
- Controller performing data transformation or validation.
- Controller calling repository or DbContext directly.
- Controller returning `IResult` (Minimal API type) instead of `IActionResult`.
- Controller action without `UserId` being passed into the command/query.

## Entity Rules

- All entities inherit from `AuditableEntity` (provides `Id: Guid`, `CreatedOn`, `UpdatedOn`).
- Use `Guid` for all primary keys. No `int` auto-increment.
- No data annotations on entities. All EF config uses Fluent API in `OnModelCreating`.
- Every entity holding user data MUST have `Guid UserId`.
- Entities are classes (not records) — EF Core needs mutation tracking.
- No navigation properties unless a specific query requires `Include()`.

## DTO / Response Model Rules

- Records (immutable, value equality).
- Named `{Entity}{Get|Create|Update}{Purpose}Model`.
- Live in `Domain/Models/{Entity}/`.
- These are the API contract — changing a record's properties is a breaking change.
- No logic, no methods, no validation in DTOs.

## Data Access Rules

**Reads (query handlers):**
- Always use `AsNoTracking()`.
- Always use `ProjectTo<TDto>()` for projection.
- Always pass `CancellationToken` to async EF methods.
- Pagination: execute `CountAsync` for total, then `Skip/Take` for the page.

**Writes (command handlers):**
- Use tracked entities (no `AsNoTracking`).
- Call `SaveChangesAsync()` once per handler, at the end.
- `DataContext.SetAuditableProperties()` sets `CreatedOn`/`UpdatedOn` automatically with `DateTime.UtcNow`. Do not set audit timestamps manually.

**Repository:**
- Never add query-specific methods to the repository. Use `AsQueryable()` + LINQ.
- Never call `SaveChanges` from the repository. That's `DataContext`'s job.
- Method `Add()` only adds. It does not upsert.

## Forbidden Patterns — ALWAYS Flag These

| Pattern | Severity | What to Flag |
|---------|----------|-------------|
| `DateTime.Now` | CRITICAL | Must use `DateTime.UtcNow` everywhere |
| Missing `IOwned` on resource-accessing command/query | CRITICAL | Security vulnerability — any user can access any resource |
| Business logic in controller | HIGH | Move to handler |
| Direct `DbContext` injection outside Infrastructure | HIGH | Use `IDataContext` / `IRepository<T>` |
| Concrete class injection (no interface) | HIGH | Violates Dependency Inversion |
| `catch (Exception) { }` (empty catch) | HIGH | Must log or rethrow |
| `List<T>` return from a list endpoint | HIGH | Must use `PaginatedResult<T>` |
| AutoMapper for command/write operations | MEDIUM | Construct entities explicitly |
| Commented-out code | MEDIUM | Delete it, use version control |
| `TODO` comments persisting past the PR | MEDIUM | Create an issue instead |
| `System.Net.Mail.SmtpClient` | MEDIUM | Use MailKit |
| `Newtonsoft.Json` usage | MEDIUM | Use `System.Text.Json` |
| `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` | MEDIUM | Use async/await |
| Missing `CancellationToken` in async EF calls | LOW | Pass it through |
| `IResult` return type in controllers | LOW | Use `IActionResult` |
| Magic strings for config keys | LOW | Use constants or options pattern |

## C# Coding Standards

- C# 13 (latest with .NET 10).
- File-scoped namespaces: `namespace X;` not `namespace X { ... }`.
- `record` for commands, queries, DTOs. `class` for entities, handlers, validators.
- Nullable reference types enabled. Use `?` for intentionally nullable. Use `!` only when pipeline behavior guarantees non-null.
- Constructor injection only. One constructor per class. `private readonly` for injected fields.
- Fields: `_camelCase`. Properties/Methods: `PascalCase`. Parameters/locals: `camelCase`.
- One public type per file. Filename matches type name.

## Exception Handling

- `ToDoItemNotFoundException` → 404
- `ResourceForbiddenException` → 403
- `FluentValidation.ValidationException` → 400 with grouped errors
- Generic `Exception` → 500 with sanitized message (details only in Development)
- All error responses use RFC 7807 ProblemDetails format.
- New error conditions get their own exception class + middleware catch block.

## Security Rules

- JWT Bearer with 15-min access tokens, 7-day refresh tokens.
- Refresh token rotation on every refresh.
- CORS locked to configured origins per environment. `AllowAnyOrigin()` forbidden in production.
- Login/registration errors MUST NOT reveal whether a user account exists.
- Exception details MUST NOT be included in non-Development HTTP responses.
- No secrets (connection strings, JWT keys, SMTP passwords) in committed config files.

## API Design

- Routes versioned: `/v1/[controller]`.
- JSON request/response bodies. ProblemDetails for errors.
- POST create → 201 Created with Location header.
- PATCH update → 204 No Content.
- DELETE → 204 No Content.
- GET single → 200 with body.
- GET list → 200 with `PaginatedResult<T>` body (items, totalCount, page, pageSize, totalPages, hasNext, hasPrevious).
- `[ProducesResponseType]` attributes on all actions.
- All dates ISO 8601.

## Logging

- Serilog structured logging. Use named parameters: `_logger.LogInformation("Handling {RequestName}", name);`
- Never use string interpolation in log calls: `$"Handling {name}"` defeats structured logging.
- Never log passwords, tokens, or PII.

## Testing

- xUnit + FluentAssertions + NSubstitute for unit tests.
- WebApplicationFactory with InMemory DB for integration tests.
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`.
- Every handler must have unit tests. Every validator must have tests. Every endpoint must have integration tests.
- CI pipeline must fail on test failures.

## PR Review Checklist — Verify Every PR Against This List

1. Does every new command/query accessing a specific resource implement `IOwned`?
2. Does every new list query return `PaginatedResult<T>`?
3. Does every command/query accepting user input have a validator?
4. Are all dependencies injected via interfaces?
5. Is `DateTime.UtcNow` used (not `DateTime.Now`)?
6. Are there unit tests for the handler and validator?
7. Is there an integration test for the endpoint?
8. Does the controller only call `_mediator.Send()` and return a status code?
9. Is `ExceptionMiddleware` updated if a new exception type is introduced?
10. Are no secrets committed in configuration files?
