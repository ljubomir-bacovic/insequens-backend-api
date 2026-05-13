# Insequens v1 — Architecture, Patterns & Coding Guidelines

> This is the **authoritative reference** for the Insequens backend after the v1 modernisation.
> Every contributor — human or AI agent — must follow these guidelines when writing, reviewing, or modifying code.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Solution Structure & Dependency Rules](#2-solution-structure--dependency-rules)
3. [Architectural Patterns](#3-architectural-patterns)
4. [SOLID Principles — How We Apply Them](#4-solid-principles--how-we-apply-them)
5. [CQRS & MediatR Conventions](#5-cqrs--mediatr-conventions)
6. [Domain Layer Guidelines](#6-domain-layer-guidelines)
7. [Application Layer Guidelines](#7-application-layer-guidelines)
8. [API Layer Guidelines](#8-api-layer-guidelines)
9. [Infrastructure Layer Guidelines](#9-infrastructure-layer-guidelines)
10. [Security Architecture](#10-security-architecture)
11. [Error Handling & Exception Strategy](#11-error-handling--exception-strategy)
12. [Validation Strategy](#12-validation-strategy)
13. [Data Access Patterns](#13-data-access-patterns)
14. [Testing Strategy](#14-testing-strategy)
15. [Naming Conventions](#15-naming-conventions)
16. [C# Coding Standards](#16-c-coding-standards)
17. [API Design Standards](#17-api-design-standards)
18. [Configuration & Secrets Management](#18-configuration--secrets-management)
19. [Logging Standards](#19-logging-standards)
20. [Performance Guidelines](#20-performance-guidelines)
21. [Git & CI/CD Conventions](#21-git--cicd-conventions)

---

## 1. System Overview

Insequens is a task management API built on .NET 10, following CQRS with MediatR, Clean Architecture, and a generic repository pattern backed by Entity Framework Core and SQL Server.

### Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 10.0 |
| Web framework | ASP.NET Core | 10.0 |
| ORM | Entity Framework Core | 10.0.x |
| Database | SQL Server | Latest |
| CQRS / Mediator | MediatR | 12.x |
| Validation | FluentValidation | 11.x |
| Object mapping | AutoMapper | 12.x |
| Identity | ASP.NET Core Identity | 10.0.x |
| Authentication | JWT Bearer (15-min access, 7-day refresh) | — |
| Email | MailKit / MimeKit | Latest |
| Logging | Serilog (Console + File sinks) | Latest |
| API docs | Scalar (OpenAPI) | Latest |
| CI | Azure Pipelines | — |

### High-Level Request Flow

```
Client → HTTPS → Controller → IMediator.Send() → Pipeline Behaviors → Handler → Repository → DB
                                  │
                                  ├── LoggingBehavior     (every request)
                                  ├── ValidationBehavior  (if validators exist for the request type)
                                  └── OwnershipBehavior   (if request implements IOwned)
```

Every HTTP request follows this exact path. There are no shortcuts, no direct repository calls from controllers, and no service classes.

---

## 2. Solution Structure & Dependency Rules

### Project Layout

```
Insequens.sln
├── src/
│   ├── Insequens.Api                              → Composition root, controllers, middleware
│   ├── Insequens.Application                      → Commands, queries, handlers, validators, behaviors
│   ├── Insequens.Domain                           → Entities, value objects, enums, data access contracts
│   └── Infrastructure/
│       ├── Insequens.Infrastructure.Data           → EF Core DbContext, Identity config, migrations
│       └── Insequens.Infrastructure.DataAccess     → Repository<T>, DataContext (Unit of Work)
├── tests/
│   ├── Insequens.Application.Tests                → Handler + validator + behavior unit tests
│   └── Insequens.Api.Tests                        → Integration tests (WebApplicationFactory)
```

### Dependency Rules (The Dependency Rule)

The most fundamental rule of this architecture: **dependencies point inward**. Outer layers know about inner layers. Inner layers never reference outer layers.

```
┌─────────────────────────────────────────────────┐
│                  Insequens.Api                   │  ← Outermost: knows everything
│  References: Application, Infrastructure.*       │
├─────────────────────────────────────────────────┤
│    Insequens.Infrastructure.DataAccess           │  ← Implements Domain contracts
│    Insequens.Infrastructure.Data                 │
│    References: Domain only                       │
├─────────────────────────────────────────────────┤
│              Insequens.Application               │  ← Orchestration layer
│    References: Domain only                       │
├─────────────────────────────────────────────────┤
│                Insequens.Domain                  │  ← Innermost: references NOTHING
│    References: none (zero project references)    │
└─────────────────────────────────────────────────┘
```

**Violations of the dependency rule are blocking defects.** If Domain ever references Application or Infrastructure, the architecture is broken.

### What Goes Where

| Artifact | Project | Rationale |
|----------|---------|-----------|
| Entities (`ToDoItem`) | Domain | Core business objects, no framework dependencies |
| Value objects, Enums (`TaskPriority`) | Domain | Domain concepts |
| Repository interfaces (`IRepository<T>`, `IDataContext`) | Domain | Contracts, not implementations |
| Infrastructure interfaces (`IEmailSender`) | Domain | Dependency Inversion — interfaces owned by domain |
| DTOs / Response models | Domain | Shared between Application and Api; no logic |
| Commands / Queries | Application | CQRS request objects |
| Handlers | Application | Business logic orchestration |
| Validators | Application | Input validation rules |
| Pipeline Behaviors | Application | Cross-cutting concerns |
| AutoMapper profiles | Application | Mapping configuration |
| Custom exceptions | Application | Application-level errors |
| `PaginatedResult<T>` | Application | Application-level response wrapper |
| Controllers | Api | HTTP adapters |
| Middleware | Api | Cross-cutting HTTP concerns |
| `Program.cs` | Api | DI composition root |
| `DbContext` / Migrations | Infrastructure.Data | EF Core implementation details |
| `Repository<T>` / `DataContext` | Infrastructure.DataAccess | Contract implementations |

---

## 3. Architectural Patterns

### 3.1 Clean Architecture

We follow Robert C. Martin's Clean Architecture principles. The core idea: business rules are at the center, independent of frameworks, databases, and delivery mechanisms.

In practice this means:
- `Insequens.Domain` compiles with zero external NuGet packages (besides the language itself).
- EF Core annotations and attributes **never** appear on domain entities. All configuration is done via Fluent API in `InsequensContext.OnModelCreating`.
- If you need to add a new external dependency, ask: "Does the Domain need to know about this?" The answer should almost always be no.

### 3.2 CQRS (Command Query Responsibility Segregation)

Every operation is either a **command** (changes state) or a **query** (reads state). They are never mixed.

- Commands return `Unit` (void) for operations that don't need a response, or a response model for operations like Create that return the created entity.
- Queries always return data and never mutate state.
- Commands and queries are records (immutable value objects).
- Each has exactly one handler.

This separation enables different optimisation strategies: queries can use `AsNoTracking()` and `ProjectTo<>()` for efficient reads, while commands work with tracked entities for reliable writes.

### 3.3 Mediator Pattern (via MediatR)

MediatR decouples the sender (controller) from the handler (business logic). The controller never knows which class handles its request. This enables:
- Pipeline behaviors that intercept every request transparently
- Easy addition of new operations without modifying existing code
- Unit testing of handlers in isolation

### 3.4 Repository Pattern + Unit of Work

`IRepository<T>` abstracts `DbSet<T>` operations. `IDataContext` acts as the Unit of Work, coordinating commits and auditable property injection.

**What the repository is:** A testing seam and a hook for cross-cutting data concerns (audit timestamps, soft-delete filters, multi-tenant query filters).

**What the repository is not:** A full database abstraction. Our handlers use EF Core's `IQueryable` extensions (`AsNoTracking`, `ProjectTo`, `CountAsync`, `ToListAsync`) directly through the repository. We acknowledge this couples us to EF Core's query model, and that is acceptable — the "swap your ORM" argument is not a real concern for this system.

### 3.5 Pipeline Behaviors (Aspect-Oriented Concerns)

Cross-cutting concerns are implemented as MediatR pipeline behaviors, not as scattered code in controllers or handlers.

| Behavior | Responsibility | Applies To |
|----------|---------------|------------|
| `LoggingBehavior` | Logs request name + elapsed time | All requests |
| `ValidationBehavior` | Runs FluentValidation validators | Requests with registered validators |
| `OwnershipBehavior` | Verifies resource belongs to requesting user | Requests implementing `IOwned` |

**Pipeline execution order matters and is:**

```
Logging → Validation → Ownership → Handler
```

This order is intentional: logging captures everything (including validation failures), validation rejects bad input before we hit the database, and ownership checks only run after input is confirmed valid.

---

## 4. SOLID Principles — How We Apply Them

### 4.1 Single Responsibility Principle (SRP)

Every class has one reason to change.

| Class | Single Responsibility |
|-------|---------------------|
| `ToDoItemController` | Translate HTTP ↔ MediatR. Nothing else. |
| `CreateToDoItemHandler` | Execute the "create a task" operation. |
| `CreateToDoItemValidator` | Validate the "create a task" input. |
| `OwnershipBehavior` | Verify resource ownership. |
| `ExceptionMiddleware` | Translate exceptions to HTTP responses. |
| `Repository<T>` | Provide data access operations for entity `T`. |
| `DataContext` | Coordinate saves and inject audit timestamps. |

**Anti-pattern to avoid:** A handler that validates input, checks ownership, logs, and then does the business logic. That's four responsibilities. Our pipeline behaviors exist specifically to prevent this.

### 4.2 Open/Closed Principle (OCP)

The system is open for extension, closed for modification.

Adding a new feature (e.g., a `Project` entity with full CRUD) requires:
- New entity in Domain
- New commands/queries/handlers/validators in Application
- New controller in Api
- New tests

It does **not** require modifying any existing handler, behavior, middleware, or controller. The pipeline behaviors, exception middleware, and repository pattern all work generically.

The `OwnershipBehavior` demonstrates this: it works for any `IOwned` request, whether it's a `ToDoItem`, `Project`, or any future entity. You extend the system by adding new `IOwned` commands, not by modifying the behavior.

### 4.3 Liskov Substitution Principle (LSP)

Derived classes must be substitutable for their base classes without altering program correctness.

Our entity hierarchy demonstrates this:

```
IEntity → IEntity<T> → BaseEntity<T> → AuditableEntity → ToDoItem
```

Any code accepting `IEntity` works with `ToDoItem`. Any code accepting `AuditableEntity` works with `ToDoItem`. The `DataContext.SetAuditableProperties()` method operates on `AuditableEntity` without knowing the concrete type — it just sets `CreatedOn` and `UpdatedOn`.

**Rule:** When creating new entities, always inherit from `AuditableEntity` unless there's a compelling reason not to audit the entity. If you inherit from `BaseEntity<T>` directly, document why.

### 4.4 Interface Segregation Principle (ISP)

Clients should not be forced to depend on interfaces they don't use.

This is why we have:
- `IOwned` as a separate marker interface, not a base class for all commands. Commands that create new resources don't need ownership checks.
- `IRepository<T>` exposing focused methods (`Find`, `FindAsync`, `Add`, `Remove`, `AsQueryable`) rather than a god interface with 30 methods.
- `IDataContext` exposing only `SaveChanges`, `SaveChangesAsync`, and `GetRepository<T>` — not the entire DbContext surface.
- `IEmailSender` with a single `SendEmailAsync` method — not a full email service with templates, queues, and attachments.

**Rule:** If an interface has methods that most implementations leave as `throw new NotImplementedException()`, the interface is too broad. Split it.

### 4.5 Dependency Inversion Principle (DIP)

High-level modules (Application, Domain) do not depend on low-level modules (Infrastructure). Both depend on abstractions.

Concrete examples in this codebase:
- Handlers depend on `IDataContext` and `IRepository<T>` (interfaces in Domain), not on `DataContext` or `Repository<T>` (implementations in Infrastructure).
- `AuthController` depends on `IEmailSender` (interface in Domain), not on `EmailSender` (implementation in Api).
- All wiring happens in `Program.cs` (the composition root), which is the only place that knows about concrete implementations.

**Rule:** Never inject a concrete class. Always inject an interface or abstract class. The only exception is `IMapper` (AutoMapper) and `IMediator` (MediatR), which are already abstractions over their implementations.

---

## 5. CQRS & MediatR Conventions

### 5.1 Command Conventions

```csharp
// File: Application/Commands/ToDoItem/CreateToDoItemCommand.cs

namespace Insequens.Application.Commands.ToDoItem;

public record CreateToDoItemCommand(
    string Name,
    string? Description,
    int Priority,
    DateOnly? DueDate,
    Guid UserId) : IRequest<ToDoItemGetDetailsModel>;
```

**Rules:**
- Commands are `record` types (immutable, value equality).
- Name format: `{Verb}{Entity}Command` — e.g., `CreateToDoItemCommand`, `DeleteToDoItemCommand`, `UpdateToDoItemPriorityCommand`.
- Commands that mutate an existing resource implement `IOwned` and include `Guid ItemId` + `Guid UserId`.
- Commands that create new resources include `Guid UserId` but do **not** implement `IOwned` (there's no existing resource to verify ownership of).
- Commands return `Unit` for void operations, or a response model for operations that return data (Create returns the created entity).
- `UserId` is always set by the controller from the JWT claims, never trusted from the request body.

### 5.2 Query Conventions

```csharp
// File: Application/Queries/ToDoItem/GetUserToDoItemsQuery.cs

namespace Insequens.Application.Queries.ToDoItem;

public record GetUserToDoItemsQuery(
    Guid UserId,
    bool IsCompleted,
    int Page,
    int PageSize) : IRequest<PaginatedResult<ToDoItemGetListModel>>;
```

**Rules:**
- Queries are `record` types.
- Name format: `{Get}{What}Query` — e.g., `GetToDoItemQuery`, `GetUserToDoItemsQuery`.
- Queries that read a specific resource owned by a user implement `IOwned`.
- List queries filter by `UserId` in the handler's WHERE clause; they do not need `IOwned`.
- List queries always return `PaginatedResult<T>`, never bare `List<T>`.

### 5.3 Handler Conventions

```csharp
// File: Application/Commands/ToDoItem/CreateToDoItemHandler.cs

namespace Insequens.Application.Commands.ToDoItem;

public class CreateToDoItemHandler
    : IRequestHandler<CreateToDoItemCommand, ToDoItemGetDetailsModel>
{
    // constructor injection, Handle method
}
```

**Rules:**
- One handler per command or query. No handler handles multiple request types.
- Name format: `{Verb}{Entity}Handler` — matching the command/query name.
- File location: same folder as the command/query it handles.
- Handlers inject `IDataContext` (for repository access), `IMapper` (for query projections), and nothing else unless truly necessary.
- Handlers for `IOwned` requests can safely assume the entity exists and is owned by the user — the `OwnershipBehavior` already verified this. The handler still calls `FindAsync` to get the entity reference for mutation, but it can use the null-forgiving operator (`!`) because the behavior guarantees non-null.
- Command handlers construct entities explicitly (no AutoMapper for commands). Query handlers use AutoMapper's `ProjectTo<>()` for efficient SQL projection.

### 5.4 Folder Structure

```
Application/
├── Behaviors/
│   ├── LoggingBehavior.cs
│   ├── ValidationBehavior.cs
│   └── OwnershipBehavior.cs
├── Commands/
│   └── ToDoItem/
│       ├── CreateToDoItemCommand.cs
│       ├── CreateToDoItemHandler.cs
│       ├── DeleteToDoItemCommand.cs
│       ├── DeleteToDoItemHandler.cs
│       ├── ToggleToDoItemCompleteCommand.cs
│       ├── ToggleToDoItemCompleteHandler.cs
│       ├── UpdateToDoItemPriorityCommand.cs
│       ├── UpdateToDoItemPriorityHandler.cs
│       ├── UpdateToDoItemNameCommand.cs
│       ├── UpdateToDoItemNameHandler.cs
│       ├── UpdateToDoItemDescriptionCommand.cs
│       ├── UpdateToDoItemDescriptionHandler.cs
│       ├── UpdateToDoItemDueDateCommand.cs
│       └── UpdateToDoItemDueDateHandler.cs
├── Queries/
│   └── ToDoItem/
│       ├── GetToDoItemQuery.cs
│       ├── GetToDoItemHandler.cs
│       ├── GetUserToDoItemsQuery.cs
│       └── GetUserToDoItemsHandler.cs
├── Validators/
│   └── ToDoItem/
│       ├── CreateToDoItemValidator.cs
│       ├── UpdateToDoItemNameValidator.cs
│       └── GetUserToDoItemsValidator.cs
├── Exceptions/
│   ├── ToDoItemNotFoundException.cs
│   └── ResourceForbiddenException.cs
├── Models/
│   └── PaginatedResult.cs
├── Profiles/
│   └── ToDoItemProfile.cs
└── DependencyInjection.cs
```

When adding a new entity (e.g., `Project`), create `Commands/Project/`, `Queries/Project/`, `Validators/Project/` subdirectories. Each entity gets its own subfolder.

---

## 6. Domain Layer Guidelines

### 6.1 Entity Design

All entities must inherit from `AuditableEntity`, which provides `Id` (Guid), `CreatedOn`, `UpdatedOn`.

```csharp
// Correct
public class ToDoItem : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = default!;
    // ...
}
```

**Rules:**
- Entities are classes (not records) because EF Core tracks mutations on them.
- Use `Guid` for all primary keys. No `int` auto-increment keys.
- Required string properties use `= default!` to satisfy nullable analysis while allowing EF Core to set them.
- Nullable properties are explicitly marked with `?`.
- No data annotations on entities. All EF configuration uses Fluent API in `OnModelCreating`.
- No navigation properties unless you have a specific query that requires eager loading through Include(). Add them when needed, not speculatively.
- Every entity that holds user data must have a `Guid UserId` property for ownership enforcement.

### 6.2 Enums

```csharp
public enum TaskPriority
{
    High = 1,
    Medium = 2,
    Low = 3,
}
```

**Rules:**
- Explicit integer values, always. Never rely on implicit ordinal position.
- Values must be stable — once assigned, never renumbered. This is a database-persisted value.
- Name format: singular noun, PascalCase members.

### 6.3 DTOs (Response Models)

```csharp
public record ToDoItemGetDetailsModel(
    Guid Id, string Name, string? Description,
    TaskPriority? Priority, DateOnly? DueDate, bool IsCompleted);
```

**Rules:**
- Records (immutable, value equality).
- Name format: `{Entity}{Get|Create|Update}{Purpose}Model`.
- Live in `Domain/Models/{Entity}/`.
- These are the **API contract**. Changing a record's properties is a breaking change that requires frontend coordination.
- DTOs have no logic, no methods, no validation. They are pure data shapes.

---

## 7. Application Layer Guidelines

### 7.1 The `IOwned` Interface

```csharp
public interface IOwned
{
    Guid UserId { get; }
    Guid ItemId { get; }
}
```

**When to implement `IOwned`:**
- Any command that modifies an existing user-owned resource (Delete, Update, Toggle).
- Any query that reads a specific user-owned resource by ID (GetById).

**When NOT to implement `IOwned`:**
- Commands that create new resources (Create).
- List queries that filter by UserId in their handler's WHERE clause (GetAll, GetByFilter).

### 7.2 `PaginatedResult<T>`

```csharp
public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
```

**Rule:** Every list endpoint returns `PaginatedResult<T>`, never a bare `List<T>`. This is a non-negotiable API contract.

### 7.3 AutoMapper Usage

- **Queries:** Use `ProjectTo<TDto>(mapperConfig)` on `IQueryable<T>` for efficient SQL projection. This is the correct use of AutoMapper.
- **Commands:** Construct entities explicitly in the handler. Do not use `_mapper.Map<Entity>(command)` for writes. Explicit construction makes the data flow visible and avoids hidden mapping surprises.

### 7.4 Adding a New Feature (Checklist)

When adding a new operation:

1. Is it a command (changes state) or a query (reads state)?
2. Create the request record in the correct folder (`Commands/` or `Queries/`).
3. If it operates on an existing user-owned resource, implement `IOwned`.
4. Create the handler in the same folder.
5. Does the input need validation? Create a validator in `Validators/`.
6. Does it need a new response model? Create a record in `Domain/Models/`.
7. Does it need a new AutoMapper mapping? Add it to the profile in `Application/Profiles/`.
8. Add a controller action that calls `_mediator.Send()`.
9. Write unit tests for the handler and validator.
10. Write an integration test for the endpoint.

---

## 8. API Layer Guidelines

### 8.1 Controller Design

Controllers are thin HTTP adapters. They do three things:

1. Extract the authenticated `UserId` from JWT claims.
2. Send a command or query via `IMediator`.
3. Return the appropriate HTTP status code.

```csharp
[HttpDelete("{id:guid}")]
public async Task<IActionResult> DeleteToDoItem(Guid id)
{
    await _mediator.Send(new DeleteToDoItemCommand(id, UserId));
    return NoContent();
}
```

**Rules:**
- Controllers inject only `IMediator`. No repositories, no services, no DbContext.
- All action methods return `Task<IActionResult>`. No `IResult` (Minimal API type).
- No business logic in controllers. No `if` statements that check business rules. No data transformation.
- No manual validation. All validation is handled by the pipeline.
- The `UserId` property extracts the user ID from claims and is used to populate every command/query.
- Use `[Authorize]` attribute with JWT bearer scheme on the controller class, not individual actions (unless specific actions are public).

### 8.2 Route Design

```csharp
[Route("/v1/[controller]")]
```

- Routes are versioned (`v1`).
- Controller name is derived from the class name via `[controller]` token.
- Sub-resource actions use `{id:guid}/action` format: `PATCH /v1/todoitem/{id}/priority`.
- Route parameters use type constraints: `{id:guid}`.

### 8.3 HTTP Status Codes

| Operation | Success Code | Notes |
|-----------|-------------|-------|
| GET single | 200 OK | With response body |
| GET list | 200 OK | With `PaginatedResult<T>` body |
| POST create | 201 Created | With `Location` header + created entity |
| PATCH update | 204 No Content | No body |
| DELETE | 204 No Content | No body |
| Validation failure | 400 Bad Request | ProblemDetails with error map |
| Auth failure | 401 Unauthorized | Generic message |
| Ownership failure | 403 Forbidden | ProblemDetails, no detail |
| Not found | 404 Not Found | ProblemDetails with resource ID |
| Server error | 500 Internal Server Error | ProblemDetails, detail only in dev |

---

## 9. Infrastructure Layer Guidelines

### 9.1 DbContext

- One `DbContext` for the entire application: `InsequensContext`.
- Inherits from `IdentityDbContext<ApplicationUser>` to integrate ASP.NET Core Identity.
- All entity configuration uses Fluent API in `OnModelCreating`. No data annotations on entities.
- `DbContextPool` is used in DI registration for connection pooling: `AddDbContextPool<InsequensContext>()`.
- Retry-on-failure is enabled for transient SQL Server errors: `EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)`.

### 9.2 Migrations

- Migrations live in `Infrastructure.Data/Migrations/`.
- Migration names should be descriptive: `AddProjectEntity`, `AddIndexOnToDoItemUserId`.
- Never edit a migration that has been applied to any environment. Create a new migration instead.
- Every migration should be forward-only. No `Down()` methods that drop columns with data.

### 9.3 Repository

The `Repository<T>` provides:
- `Add(T entity)` — attaches and marks for insertion.
- `Remove(T entity)` / `Remove(IEnumerable<T>)` — marks for deletion.
- `Find(params object[])` / `FindAsync(params object[])` — primary key lookup.
- `AsQueryable(params Expression<Func<T, object>>[]?)` — returns `IQueryable<T>` with optional eager loading.

**Rules:**
- Never add query-specific methods to the repository (e.g., `GetByUserId`). Use `AsQueryable()` with LINQ in the handler.
- Never call `SaveChanges` from the repository. That's the `DataContext`'s job.

---

## 10. Security Architecture

### 10.1 Authentication

- JWT Bearer tokens with 15-minute expiry.
- Refresh tokens with 7-day expiry, stored on the `ApplicationUser` entity.
- Refresh token rotation: every refresh generates a new token pair and invalidates the previous refresh token.
- Logout invalidates the refresh token server-side.
- `ClockSkew = TimeSpan.Zero` — no tolerance for expired tokens.

### 10.2 Authorization

- Controller-level `[Authorize]` attribute ensures all endpoints require authentication.
- Resource-level ownership enforcement via `IOwned` + `OwnershipBehavior`.
- No role-based authorization in v1 (all authenticated users have equal access to their own data).

### 10.3 Ownership Enforcement

This is the most critical security control in the system.

The `OwnershipBehavior` pipeline behavior intercepts every MediatR request that implements `IOwned`. It:
1. Looks up the entity by `ItemId`.
2. Verifies `entity.UserId == request.UserId`.
3. Throws `ResourceForbiddenException` if they don't match.

This runs **before** the handler, so the handler can never accidentally operate on another user's data.

**Non-negotiable rule:** Any command or query that accesses a specific resource by ID must implement `IOwned`. There are no exceptions. Code review must catch missing `IOwned` implementations.

### 10.4 CORS

- Allowed origins are configured per environment via `appsettings.{Environment}.json`.
- Production only allows the real frontend domain(s).
- Development allows `localhost` origins.
- `AllowCredentials()` is always used (required for Authorization header).
- `AllowAnyOrigin()` is never used in production.

### 10.5 Error Message Safety

- Login and registration endpoints return generic error messages that do not reveal whether a user account exists.
- The `forgot-password` endpoint always returns a success message regardless of whether the email is registered.
- Exception details are never included in non-Development HTTP responses.

---

## 11. Error Handling & Exception Strategy

### 11.1 Exception Hierarchy

| Exception | HTTP Status | When |
|-----------|------------|------|
| `ToDoItemNotFoundException` | 404 | Entity not found by ID |
| `ResourceForbiddenException` | 403 | Entity exists but belongs to different user |
| `FluentValidation.ValidationException` | 400 | Input validation failures |
| `System.Exception` (catch-all) | 500 | Unexpected errors |

### 11.2 Exception Middleware

All exception-to-HTTP mapping happens in `ExceptionMiddleware`. Handlers throw typed exceptions; they never set HTTP status codes or return HTTP-specific types.

**Rules:**
- Every new application-level error condition gets its own exception class in `Application/Exceptions/`.
- Every new exception class gets a corresponding catch block in `ExceptionMiddleware`.
- The generic `Exception` catch always returns a generic message in production.
- All exceptions are logged server-side before the HTTP response is written.

### 11.3 Error Response Format

All error responses use RFC 7807 ProblemDetails:

```json
{
    "status": 400,
    "title": "Validation failed.",
    "detail": "Task name is required.; Priority must be between 0 and 3.",
    "type": "ValidationError",
    "errors": {
        "Name": ["Task name is required."],
        "Priority": ["Priority must be between 0 and 3."]
    }
}
```

---

## 12. Validation Strategy

### 12.1 FluentValidation + Pipeline

Validation is automatic. Developers create a validator class. The `ValidationBehavior` discovers and runs it before the handler executes. There is no manual validation call anywhere.

```csharp
public class CreateToDoItemValidator : AbstractValidator<CreateToDoItemCommand>
{
    public CreateToDoItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(200).WithMessage("Task name must not exceed 200 characters.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 3)
            .WithMessage("Priority must be between 0 (none) and 3 (low).");
    }
}
```

### 12.2 Validation Rules

- Name format: `{Command/Query}Validator`.
- One validator per command/query that accepts user input.
- Validators live in `Application/Validators/{Entity}/`.
- Validators are auto-discovered by `AddValidatorsFromAssembly(assembly)`.
- Do not validate things the database will catch (e.g., unique constraints). Validate things the user controls.
- Do not put business rules in validators. Validators check shape and range. Business rules live in handlers.
- Pagination parameters are validated: `Page > 0`, `PageSize` between 1 and 100.

---

## 13. Data Access Patterns

### 13.1 Queries (Read Path)

```csharp
var items = await _dataContext.GetRepository<ToDoItem>().AsQueryable()
    .Where(x => x.UserId == request.UserId && x.IsCompleted == request.IsCompleted)
    .OrderBy(x => x.DueDate)
    .ThenBy(x => x.Priority)
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .AsNoTracking()
    .ProjectTo<ToDoItemGetListModel>(_mapper.ConfigurationProvider)
    .ToListAsync(cancellationToken);
```

**Rules:**
- Always use `AsNoTracking()` for read-only queries.
- Always use `ProjectTo<TDto>()` instead of `.Select()` for DTO projection — it uses the AutoMapper configuration and generates efficient SQL.
- Always pass `CancellationToken` to async EF Core methods.
- Pagination: always execute a `CountAsync` for total count, then `Skip/Take` for the page.

### 13.2 Commands (Write Path)

```csharp
var item = (await repo.FindAsync(request.ItemId))!;
item.Priority = request.Priority;
await _dataContext.SaveChangesAsync();
```

**Rules:**
- Use tracked entities for writes (no `AsNoTracking`).
- Call `SaveChangesAsync()` once per handler, at the end. The Unit of Work pattern ensures all changes are committed atomically.
- The `DataContext.SetAuditableProperties()` method automatically sets `CreatedOn` (on insert) and `UpdatedOn` (on insert + update) with `DateTime.UtcNow`. Do not set these manually.

### 13.3 Timestamps

- All timestamps stored in the database are UTC.
- `DateTime.UtcNow` is used everywhere. `DateTime.Now` is forbidden in server-side code.
- The API layer returns UTC timestamps. Clients are responsible for local time display.

---

## 14. Testing Strategy

### 14.1 Test Pyramid

```
         ┌──────┐
         │ E2E  │    ← Not in v1 (future: Playwright)
        ┌┴──────┴┐
        │ Integr.│   ← Insequens.Api.Tests (WebApplicationFactory)
       ┌┴────────┴┐
       │  Unit    │  ← Insequens.Application.Tests (handlers, validators, behaviors)
       └──────────┘
```

### 14.2 Unit Tests

**Project:** `Insequens.Application.Tests`
**Framework:** xUnit + FluentAssertions + NSubstitute

**What to test:**
- Every handler: happy path + error paths.
- Every validator: valid input passes, each invalid field fails with correct message.
- `OwnershipBehavior`: owned item passes, non-existent item throws 404, other user's item throws 403.

**Mocking strategy:**
- Mock `IDataContext` and `IRepository<T>` using NSubstitute.
- Use real AutoMapper configuration (create `IMapper` from the actual profile) for query handler tests.
- Never mock MediatR in unit tests — test handlers directly, not via the pipeline.

### 14.3 Integration Tests

**Project:** `Insequens.Api.Tests`
**Framework:** xUnit + WebApplicationFactory + FluentAssertions

**What to test:**
- Full HTTP request → response cycle.
- Auth flows: register, confirm, login, refresh, logout.
- CRUD operations with valid JWT token.
- Ownership enforcement via HTTP (user A cannot access user B's items).
- Validation error responses (400 with ProblemDetails).

**Setup:**
- `CustomWebApplicationFactory` replaces SQL Server with EF Core InMemory.
- Test users are seeded with known credentials.
- JWT tokens are generated programmatically for test requests.

### 14.4 Test Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:
- `Handle_WithValidCommand_CreatesItemAndReturnsDetails`
- `Handle_WithOtherUsersItem_ThrowsResourceForbiddenException`
- `Validate_EmptyName_ReturnsFailure`
- `GetToDoItems_WithValidToken_ReturnsPaginatedResult`
- `DeleteToDoItem_WithOtherUsersToken_Returns403`

---

## 15. Naming Conventions

### 15.1 General C# Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase, matches folder | `Insequens.Application.Commands.ToDoItem` |
| Class / Record | PascalCase | `CreateToDoItemHandler` |
| Interface | `I` + PascalCase | `IRepository<T>`, `IOwned` |
| Method | PascalCase | `SaveChangesAsync` |
| Property | PascalCase | `UserId`, `IsCompleted` |
| Private field | `_camelCase` | `_dataContext`, `_mapper` |
| Parameter | camelCase | `userId`, `cancellationToken` |
| Local variable | camelCase | `toDoItem`, `totalCount` |
| Constant | PascalCase | `BaseUrl` |
| Enum member | PascalCase | `TaskPriority.High` |

### 15.2 CQRS Naming

| Artifact | Pattern | Example |
|----------|---------|---------|
| Command | `{Verb}{Entity}Command` | `CreateToDoItemCommand` |
| Query | `Get{What}Query` | `GetUserToDoItemsQuery` |
| Handler | `{Verb}{Entity}Handler` | `CreateToDoItemHandler` |
| Validator | `{Command/Query name}Validator` | `CreateToDoItemValidator` |
| Response model | `{Entity}{Verb}{Purpose}Model` | `ToDoItemGetDetailsModel` |
| Exception | `{Entity}{Condition}Exception` | `ToDoItemNotFoundException` |

### 15.3 File Naming

- One public type per file.
- Filename matches the type name exactly: `CreateToDoItemCommand.cs`.
- Command and handler live in the same directory but in separate files.

---

## 16. C# Coding Standards

### 16.1 Language Features

- Target: C# 13 (latest with .NET 10).
- Use `record` types for commands, queries, DTOs, and any immutable data carrier.
- Use `class` for entities (EF Core needs mutation), handlers, validators, behaviors, middleware.
- Use file-scoped namespaces (`namespace X;`, not `namespace X { ... }`).
- Use primary constructors for simple classes where appropriate.
- Use `required` modifier for properties that must be set.
- Prefer `string.Empty` over `""`.
- Prefer pattern matching (`is`, `is not`, `switch` expressions) over type casting.

### 16.2 Async/Await

- All I/O operations must be async. No `.Result`, no `.Wait()`, no `.GetAwaiter().GetResult()`.
- Always pass `CancellationToken` through async chains.
- Suffix async methods with `Async` (except controller actions and MediatR `Handle` methods, which are async by convention).

### 16.3 Nullability

- Nullable reference types are enabled project-wide (`<Nullable>enable</Nullable>`).
- Use `?` for intentionally nullable types.
- Use `!` (null-forgiving) only when a prior pipeline behavior guarantees non-null (e.g., after `OwnershipBehavior` has validated the entity exists).
- Never suppress nullable warnings with `#pragma`. Fix the code instead.

### 16.4 Dependency Injection

- Use constructor injection exclusively.
- One constructor per class (no overloads).
- Mark injected fields as `private readonly`.
- Registration lifetimes: `Scoped` for data access (`IDataContext`, `Repository<T>`, `DbContext`). `Transient` for pipeline behaviors. `Singleton` for `IConfiguration`.
- All DI registration for the Application layer lives in `DependencyInjection.cs`. All infrastructure registration lives in `Program.cs`.

### 16.5 Code That Must Not Exist

- **No `DateTime.Now`** — use `DateTime.UtcNow`.
- **No commented-out code** — delete it; use version control for history.
- **No `TODO` comments that persist past the PR** — create a GitHub/Azure DevOps issue instead.
- **No magic strings for configuration keys** — use strongly-typed options pattern or constants.
- **No business logic in controllers** — if it has an `if` that checks a business condition, it belongs in a handler.
- **No direct `DbContext` injection outside Infrastructure** — use `IDataContext` / `IRepository<T>`.
- **No `catch (Exception) { }` (empty catch)** — always log or rethrow.
- **No `System.Net.Mail`** — use MailKit.

---

## 17. API Design Standards

### 17.1 Versioning

The API is versioned via URL path: `/v1/`. When breaking changes are needed, a `/v2/` prefix is introduced alongside `/v1/`, not as a replacement.

### 17.2 Request/Response Bodies

- Request bodies use `application/json`.
- Response bodies use `application/json`.
- Error responses use `application/problem+json` (RFC 7807).
- All JSON uses camelCase property names (ASP.NET Core default).
- Dates use ISO 8601 format: `2025-03-20` for `DateOnly`, `2025-03-20T14:30:00Z` for `DateTime`.

### 17.3 Pagination

Every list endpoint accepts `page` (default 1) and `pageSize` (default 20, max 100) as query parameters.

Every list endpoint returns:

```json
{
    "items": [...],
    "totalCount": 42,
    "page": 1,
    "pageSize": 20,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
}
```

### 17.4 Filtering

Filters are query parameters on GET endpoints: `?isCompleted=true&page=2&pageSize=10`.

### 17.5 API Documentation

- Scalar UI is available at `/scalar/v1` in Development mode.
- `[ProducesResponseType]` attributes document all possible status codes per action.
- OpenAPI spec includes JWT Bearer security scheme via `JwtBearerSecurityDocumentTransformer`.

---

## 18. Configuration & Secrets Management

### 18.1 Configuration Hierarchy

```
appsettings.json                    → Base defaults (non-sensitive)
appsettings.{Environment}.json      → Environment overrides
Environment variables               → Deployment overrides (highest priority)
User Secrets                        → Local dev secrets (never committed)
```

### 18.2 What Goes Where

| Setting | Where | Example |
|---------|-------|---------|
| Logging levels | `appsettings.json` | `"LogLevel": { "Default": "Warning" }` |
| CORS origins | `appsettings.{Env}.json` | `"Cors": { "AllowedOrigins": [...] }` |
| Connection strings | User Secrets / Env vars | Never in committed files |
| JWT signing key | User Secrets / Env vars | Never in committed files |
| SMTP credentials | User Secrets / Env vars | Never in committed files |

### 18.3 Rules

- `appsettings.json` must never contain real credentials, connection strings, or signing keys.
- Sensitive values use placeholder format: `"Key": ""` with a comment directing to User Secrets.
- Environment variables use the double-underscore separator for nested keys: `Jwt__Key`, `ConnectionStrings__InsequensConnection`.

---

## 19. Logging Standards

### 19.1 Framework

Serilog with two sinks: Console (development) and File (rolling daily, `Logs/app-log-{date}.txt`).

### 19.2 Structured Logging

Always use structured logging with named parameters:

```csharp
// Correct
_logger.LogInformation("Handling {RequestName} for user {UserId}", requestName, userId);

// Wrong — string interpolation defeats structured logging
_logger.LogInformation($"Handling {requestName} for user {userId}");
```

### 19.3 What to Log

| Level | What |
|-------|------|
| `Information` | Request start/end (via `LoggingBehavior`), successful auth, email sent |
| `Warning` | Failed login attempt, token refresh failure, warmup ping failure |
| `Error` | Unhandled exceptions (via middleware), database connection failures |

### 19.4 What NOT to Log

- Passwords, tokens, or any sensitive credentials.
- Full request/response bodies (may contain PII).
- Individual validation failures (returned to client, not logged unless at Debug level).

---

## 20. Performance Guidelines

### 20.1 Database

- Use `AsNoTracking()` for all read-only queries.
- Use `ProjectTo<TDto>()` to select only needed columns.
- Use `CountAsync` + `Skip/Take` for pagination (avoids loading entire tables).
- `DbContextPool` is enabled for connection reuse.
- `EnableRetryOnFailure` handles transient SQL Server errors.

### 20.2 Serialization

- Use `System.Text.Json` (default ASP.NET Core serializer). No Newtonsoft.Json.
- Source generators for JSON serialization are recommended for hot paths in future versions.

### 20.3 Memory

- Avoid `ToListAsync()` on large datasets without pagination.
- Prefer `IQueryable` pipeline (database does the work) over `IEnumerable` (loads everything into memory).
- `PaginatedResult<T>` enforces a maximum page size of 100 via validation.

---

## 21. Git & CI/CD Conventions

### 21.1 Branch Strategy

- `master` — production-ready code.
- `feature/{ticket}-{description}` — feature branches off master.
- `fix/{ticket}-{description}` — bugfix branches.
- All changes go through pull requests. Direct pushes to `master` are blocked.

### 21.2 Commit Messages

Format: `type(scope): description`

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`.

Examples:
- `feat(todoitem): add create command with validation`
- `fix(auth): normalize login error messages`
- `refactor(cqrs): migrate delete operation to MediatR handler`
- `test(ownership): add behavior unit tests`

### 21.3 CI Pipeline

Azure Pipelines runs on every push to master and every PR:

1. `dotnet restore`
2. `dotnet build --configuration Release`
3. `dotnet test --configuration Release --logger trx`
4. Publish test results to Azure DevOps.

The pipeline **must** fail on any test failure. Merging with failing tests is a blocking defect.

### 21.4 Code Review Checklist

Every PR reviewer must verify:

- [ ] Does every new command/query that accesses a specific resource implement `IOwned`?
- [ ] Does every new list query return `PaginatedResult<T>`?
- [ ] Does every handler that accepts user input have a corresponding validator?
- [ ] Are all new dependencies injected via interfaces, not concrete classes?
- [ ] Is `DateTime.UtcNow` used (not `DateTime.Now`)?
- [ ] Are there unit tests for the handler and validator?
- [ ] Is there an integration test for the new endpoint?
- [ ] Does the controller action only call `_mediator.Send()` and return a status code?
- [ ] Is the exception middleware updated if a new exception type is introduced?
- [ ] Are no secrets committed in configuration files?
