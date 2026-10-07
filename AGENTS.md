# Insequens Backend — Coding Guidelines for AI Agents & Code Reviewers

> This document is the single source of truth for all AI coding agents (Copilot, Claude Code, Cursor, Codex) and AI code reviewers (CodeRabbit) working on the Insequens backend. It defines the architecture, patterns, rules, and anti-patterns that must be enforced on every pull request.

## System Overview

Insequens is a .NET 10 Web API for task management. It uses CQRS with MediatR, Clean Architecture, Entity Framework Core with SQL Server behind `IApplicationDbContext`, JWT Bearer authentication and FluentValidation.

## Solution Structure & Dependency Rules

```
src/Insequens.Api              → ASP.NET Core host, thin controllers, middleware, DI composition root
src/Insequens.Contracts        → HTTP request/response records (V1/Tasks, V1/Auth) shared with clients (ZERO references)
src/Insequens.Application      → Commands, queries, handlers, validators, pipeline behaviors, authorization,
                                 interfaces (IApplicationDbContext, ICurrentUser, IIdentityService, ITokenService, IEmailSender)
src/Insequens.Domain           → Entities with behaviour and invariants, enums, domain exceptions (ZERO references)
src/Insequens.Infrastructure   → EF Core DbContext, configurations, interceptors, migrations, Identity, tokens, MailKit
tests/                         → xUnit test projects (Domain, Application on SQLite, Infrastructure, Api)
```

**The Dependency Rule — flag violations as CRITICAL:**
- Domain references NOTHING. If Domain ever imports Application, Api, or Infrastructure, reject the PR.
- Contracts references NOTHING; clients consume it without the server.
- Application references Domain and Contracts.
- Infrastructure references Application and Domain, and implements Application's interfaces.
- Api references Application, Contracts and Infrastructure (composition root). It has no EF provider package.

## CQRS Architecture

Every operation is either a Command (changes state) or a Query (reads state). Never mixed.

**Request flow:** Controller → `_mediator.Send()` → LoggingBehavior → ValidationBehavior → AuthorizationBehavior → Handler → `IApplicationDbContext` → DB

### Commands

- Immutable `record` types implementing `IRequest` (void) or `IRequest<T>`.
- Named `{Verb}{Entity}Command` (e.g., `CreateToDoItemCommand`, `DeleteToDoItemCommand`).
- Commands that act on an existing user-owned resource MUST implement `IOwned<TEntity>` (`Guid UserId` + `Guid ResourceId`; the ToDoItem commands map `ResourceId` to `ItemId`).
- Commands that create new resources include `Guid UserId` but do NOT implement `IOwned`.
- `UserId` is always set by the controller from JWT claims, never trusted from the request body.
- Void operations implement `IRequest`, not `IRequest<Unit>`. Create operations return a response record.

### Queries

- Immutable `record` types implementing `IRequest<T>`.
- Named `Get{What}Query` (e.g., `GetToDoItemQuery`, `GetUserToDoItemsQuery`).
- Queries reading a specific resource by ID filter by `Id` AND `UserId` in one projected query and throw `NotFoundException` when it is empty. They do not implement `IOwned`; the filter is the authorization.
- List queries filter by `UserId` in the handler and do NOT implement `IOwned`.
- List queries MUST return `PaginatedResult<T>`, never bare `List<T>`.

### Handlers

- One handler per command/query. Never handle multiple request types.
- Named `{Verb}{Entity}Handler`, matching the command/query.
- Live in the same folder as their command/query.
- Inject `IApplicationDbContext`, `IMapper` (for queries) and, for `IOwned<TEntity>` commands, `IResourceContext<TEntity>`. Nothing else unless truly necessary.
- For `IOwned<TEntity>` commands, take the entity from `IResourceContext<TEntity>.Resource`: AuthorizationBehavior already loaded and tracked it. Do not load it again and never use `!` on a load.
- Command handlers create entities with their factory (`ToDoItem.Create`) and change them only through entity methods. DO NOT use AutoMapper for writes.
- Query handlers use `ProjectTo<TDto>()` for efficient SQL projection.

### Validators

- Named `{CommandOrQuery}Validator`.
- One validator per command/query that accepts user input.
- Auto-discovered by `AddValidatorsFromAssembly`. No manual registration needed.
- Validate shape and range. Invariants belong in the entity, other business rules in handlers.
- Pagination: `Page > 0`, `PageSize` between 1 and 100.
- A request with no user-controlled shape or range rule needs no validator and no comment explaining why.

## Ownership Enforcement — MOST CRITICAL SECURITY CONTROL

The `IOwned<TEntity>` marker + `AuthorizationBehavior` pipeline behavior is the authorization layer. It works for any entity implementing `IOwnedEntity` (`Id`, `UserId`).

**Non-negotiable rule:** Any command that acts on a specific resource by ID MUST implement `IOwned<TEntity>`, and any query by ID MUST filter by `UserId` in its own query. Missing either is a CRITICAL security defect. Flag it immediately. `OwnedRequestTests` fails the build for a request carrying an `ItemId` that does neither.

The AuthorizationBehavior:
1. Resolves the request's scoped `IResourceContext<TEntity>`.
2. Loads the entity through `IOwnershipPolicy<TEntity>`: one query on `Id == ResourceId && UserId == request.UserId`.
3. Throws `NotFoundException` (404) when nothing comes back, whether the resource is missing or someone else's, so IDs cannot be probed.

`ResourceForbiddenException` (403) is only for an authenticated caller who can see a resource but lacks the role for an action.

## Controller Rules — Flag Violations

Controllers are thin HTTP adapters. They do exactly three things: extract UserId from JWT, send command/query via `_mediator.Send()`, return HTTP status code.

**Flag as defects:**
- Controller injecting anything other than `IMediator`.
- Controller containing `if` statements with business logic.
- Controller performing data transformation or validation.
- Controller using `IApplicationDbContext` or a `DbContext` directly.
- Controller returning `IResult` (Minimal API type) instead of `IActionResult`.
- Controller action without `UserId` being passed into the command/query.

## Entity Rules

- All entities inherit from `AuditableEntity` (provides `Id: Guid`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`).
- Use `Guid` for all primary keys. No `int` auto-increment.
- No data annotations on entities. All EF config uses Fluent API in an `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`.
- Every entity holding user data MUST have `Guid UserId` and implement `IOwnedEntity`.
- Entities are classes (not records) with private setters, a private parameterless constructor for EF, a static factory and intention-revealing methods. Invariants are checked in the entity and throw a `DomainException` subclass. Flag a handler that assigns an entity property.
- No navigation properties unless a specific query requires `Include()`.

## DTO / Response Model Rules

- Records (immutable, value equality).
- Named `{Entity}{Get|Create|Update}{Purpose}Model` (tasks) or `{Purpose}Request`/`{Purpose}Response` (auth).
- Live in `Insequens.Contracts`, under `V1/{Area}/`. Wire enums are Contracts types whose values match the domain enum.
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
- Call `SaveChangesAsync(cancellationToken)` once per handler, at the end.
- `AuditableEntityInterceptor` sets `CreatedOn/By` and `UpdatedOn/By` on save from `TimeProvider` (UTC) and `ICurrentUser`. Entities cannot set them.

**No repository layer:** handlers use `IApplicationDbContext` (`DbSet<T>` + `SaveChangesAsync`). Do not reintroduce a generic repository or unit of work.

## Forbidden Patterns — ALWAYS Flag These

| Pattern | Severity | What to Flag |
|---------|----------|-------------|
| `DateTime.Now` | CRITICAL | Never use `DateTime.Now`; code that stamps or compares times takes `TimeProvider` (UTC) |
| Missing `IOwned<TEntity>` on a resource command, or a query by ID without a `UserId` filter | CRITICAL | Security vulnerability — any user can access any resource |
| 403 for a resource the caller does not own | HIGH | Return 404 (`NotFoundException`) so IDs cannot be probed |
| Business logic in controller | HIGH | Move to handler |
| Direct `DbContext` injection outside Infrastructure | HIGH | Use `IApplicationDbContext` |
| Handler assigning an entity property | HIGH | Call the entity's method; it enforces the invariants |
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
- Nullable reference types enabled. Use `?` for intentionally nullable. Do not use `!` to skip a null check on loaded data.
- Constructor injection only. One constructor per class. `private readonly` for injected fields.
- Fields: `_camelCase`. Properties/Methods: `PascalCase`. Parameters/locals: `camelCase`.
- One public type per file. Filename matches type name.

## Exception Handling

- `NotFoundException` → 404 (missing or not owned)
- `ResourceForbiddenException` → 403 (role checks only)
- `DomainException` → 400 with the violated rule as detail
- `FluentValidation.ValidationException` → 400 with grouped errors
- Resource exceptions derive from `ResourceException` (required `Id`); entity invariants from `DomainException`. No `[Serializable]` boilerplate.
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

- xUnit + FluentAssertions + NSubstitute.
- Entity rules: `tests/Insequens.Domain.Tests`, no mocks.
- Handlers: sent through MediatR on SQLite in memory (`TestDbContextFactory`), not with a mocked data layer.
- WebApplicationFactory (`InsequensApiFactory`) with EF InMemory for integration tests, or `relationalDatabase: true` (SQLite) to assert the SQL a request sends.
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`.
- Every handler must have unit tests. Every validator must have tests. Every endpoint must have integration tests.
- CI pipeline must fail on test failures.

## PR Review Checklist — Verify Every PR Against This List

1. Does every new command on a specific resource implement `IOwned<TEntity>`, and does every query by ID filter by `UserId`?
2. Does every new list query return `PaginatedResult<T>`?
3. Does every command/query accepting user input have a validator?
4. Are all dependencies injected via interfaces?
5. Do timestamp operations use an injected `TimeProvider` (UTC), never `DateTime.Now`?
6. Are there tests for the handler (through the pipeline), the validator and any new entity method?
7. Is there an integration test for the endpoint?
8. Does the controller only call `_mediator.Send()` and return a status code?
9. Is `ExceptionMiddleware` updated if a new exception type is introduced?
10. Are no secrets committed in configuration files?
