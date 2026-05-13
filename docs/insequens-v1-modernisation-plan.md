# Insequens Backend v1 — Production-Ready MVP Modernisation Plan

**Target:** Transform the existing Insequens .NET 10 Web API into a secure, testable, CQRS-based production system.  
**Approach:** Incremental refactor, not rewrite. Every task preserves backward-compatible API contracts unless explicitly noted.  
**Agent target:** Each task is scoped for a Copilot Coding Agent to execute with full repo context.

---

## Table of Contents

1. [Current State Summary](#1-current-state-summary)
2. [Target Architecture](#2-target-architecture)
3. [Complete Flaw Registry](#3-complete-flaw-registry)
4. [Phase 1 — Critical Security Hardening](#4-phase-1--critical-security-hardening)
5. [Phase 2 — CQRS Foundation & MediatR Pipeline](#5-phase-2--cqrs-foundation--mediatr-pipeline)
6. [Phase 3 — Command & Query Migration](#6-phase-3--command--query-migration)
7. [Phase 4 — Validation, Pagination & API Quality](#7-phase-4--validation-pagination--api-quality)
8. [Phase 5 — Infrastructure Modernisation](#8-phase-5--infrastructure-modernisation)
9. [Phase 6 — Code Cleanup & Consistency](#9-phase-6--code-cleanup--consistency)
10. [Phase 7 — Test Infrastructure](#10-phase-7--test-infrastructure)
11. [Phase 8 — CI/CD & DevOps](#11-phase-8--cicd--devops)
12. [Task Dependency Map](#12-task-dependency-map)
13. [Post-Refactor Target State](#13-post-refactor-target-state)

---

## 1. Current State Summary

### Solution Structure

```
Insequens.sln
├── src/
│   ├── Insequens.Api              → ASP.NET Core host, controllers, middleware, DI root
│   ├── Insequens.Core             → Service implementations, AutoMapper profiles, exceptions
│   ├── Insequens.Domain           → Entities, base abstractions, DTOs, service contracts, enums
│   └── Infrastructure/
│       ├── Insequens.Infrastructure.Data        → EF Core DbContext, Identity, migrations
│       └── Insequens.Infrastructure.DataAccess  → Generic Repository<T>, DataContext (UoW)
```

### Technology Stack

- .NET 10.0 / ASP.NET Core 10
- Entity Framework Core 10.0.4 + SQL Server
- ASP.NET Core Identity with custom `ApplicationUser` (refresh token fields)
- JWT Bearer auth (15-min access tokens, 7-day refresh tokens)
- AutoMapper 12.0.1 for entity → DTO projection
- Serilog structured logging (console + rolling file)
- Scalar for OpenAPI docs
- Azure Pipelines CI (build only)

### Key Metrics

- ~2,900 lines of C# across 5 projects
- 1 domain entity (`ToDoItem`), 1 controller, 10 service methods
- 0 test projects, 0 tests
- 0 validators, 0 authorization policies

---

## 2. Target Architecture

### v1 Target: CQRS with MediatR

```
                    HTTP Request
                         │
                    ┌────▼────┐
                    │Controller│  ← thin HTTP adapter, sends commands/queries via MediatR
                    └────┬────┘
                         │
                  _mediator.Send()
                         │
              ┌──────────▼──────────┐
              │   MediatR Pipeline  │
              │  ┌────────────────┐ │
              │  │ LoggingBehavior│ │  ← cross-cutting: logging every request
              │  ├────────────────┤ │
              │  │ValidationBehav.│ │  ← FluentValidation runs here automatically
              │  ├────────────────┤ │
              │  │OwnershipBehav.│ │  ← ensures UserId matches resource owner
              │  └────────────────┘ │
              └──────────┬──────────┘
                         │
              ┌──────────▼──────────┐
              │   Command/Query     │
              │     Handlers        │  ← one handler per operation
              └──────────┬──────────┘
                         │
              ┌──────────▼──────────┐
              │  Repository / EF    │  ← existing Generic Repository stays
              │  Core DbContext     │
              └─────────────────────┘
```

### Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| CQRS library | MediatR 12 | Industry standard, minimal overhead, pipeline behaviors for cross-cutting |
| Validation | FluentValidation + MediatR pipeline behavior | Validators auto-discovered, run before handler, zero manual calls |
| Ownership enforcement | MediatR pipeline behavior + marker interface | Single enforcement point, impossible to bypass from any handler |
| Repository pattern | Keep existing `IRepository<T>` / `IDataContext` | Works behind handlers same as services; provides testing seam |
| AutoMapper | Keep for query projections | `ProjectTo<>` generates efficient SQL; remove from command paths |
| Service layer | Remove `IToDoItemService` / `ToDoItemService` | Replaced entirely by command/query handlers; no partial migration |
| Exception handling | Typed exceptions + middleware | Keep existing pattern, add `ResourceForbiddenException`, `ValidationException` |
| DTOs | Keep existing records in `Domain/Models` | Reuse as query responses; commands get their own record types |

### New Project Layout

```
Insequens.sln
├── src/
│   ├── Insequens.Api                              → Controllers (thin), Middleware, DI root
│   ├── Insequens.Application                      → NEW: Commands, Queries, Handlers, Validators, Behaviors
│   ├── Insequens.Domain                           → Entities, base types, enums (unchanged)
│   └── Infrastructure/
│       ├── Insequens.Infrastructure.Data           → DbContext, Identity, migrations
│       └── Insequens.Infrastructure.DataAccess     → Repository<T>, DataContext
├── tests/
│   ├── Insequens.Application.Tests                → Unit tests for handlers
│   └── Insequens.Api.Tests                        → Integration tests with WebApplicationFactory
```

The existing `Insequens.Core` project is retired. Its responsibilities move to `Insequens.Application`. The AutoMapper profile moves there too. The `IToDoItemService` interface and `ToDoItemService` class are deleted once all operations are migrated to CQRS handlers.

---

## 3. Complete Flaw Registry

Every issue identified during architectural review, ordered by severity.

| # | Severity | Issue | Primary File(s) |
|---|----------|-------|-----------------|
| F01 | **CRITICAL** | No ownership/authorization checks — any authenticated user can CRUD any user's tasks | `ToDoItemService.cs` |
| F02 | **CRITICAL** | CORS allows any origin, any method, any header in all environments | `Program.cs` |
| F03 | **HIGH** | Exception details (Message + InnerException) leaked to clients in 500 responses | `ExceptionMiddleware.cs` |
| F04 | **HIGH** | WarmupController reads first DB user + runs password hash verification | `WarmupController.cs` |
| F05 | **HIGH** | `System.Net.Mail.SmtpClient` is deprecated; TLS + pooling issues | `EmailSender.cs` |
| F06 | **HIGH** | Dual Identity registration: `AddIdentity<>` AND `AddIdentityCore<>` both called | `Program.cs` |
| F07 | **MEDIUM** | Zero test projects — no unit, integration, or E2E tests | `Insequens.sln` |
| F08 | **MEDIUM** | `DateTime.Now` for audit timestamps instead of `DateTime.UtcNow` | `DataContext.cs` |
| F09 | **MEDIUM** | No input validation — models accepted without FluentValidation or DataAnnotations | `Controllers/*` |
| F10 | **MEDIUM** | No pagination metadata — API returns bare lists without TotalCount | `ToDoItemService.cs` |
| F11 | **MEDIUM** | Login/Register errors reveal user existence ("User doesn't exist" vs "Wrong password") | `AuthController.cs` |
| F12 | **LOW** | `AddOrUpdate` method name misleading — only adds, never updates | `Repository.cs` |
| F13 | **LOW** | Controllers mix `IActionResult` and `IResult` return types | `ToDoItemController.cs` |
| F14 | **LOW** | Commented-out code blocks throughout Program.cs and controllers | `Program.cs` |
| F15 | **LOW** | Auth request models defined twice (custom + Identity.Data types) | `AuthController.cs` |
| F16 | **LOW** | `Newtonsoft.Json` referenced in Domain.csproj but unused | `Domain.csproj` |
| F17 | **LOW** | `appsettings.json` committed with local dev connection string | `appsettings.json` |

---

## 4. Phase 1 — Critical Security Hardening

> **Goal:** Close all CRITICAL and HIGH security vulnerabilities before any architectural changes.  
> **Fixes:** F01, F02, F03, F04, F10, F11

### Task 1.1 — Create `ResourceForbiddenException`

**File to create:** `src/Insequens.Core/Exceptions/ResourceForbiddenException.cs`

Create a new exception class following the `ToDoItemNotFoundException` pattern:

```csharp
namespace Insequens.Core.Exceptions;

public class ResourceForbiddenException : Exception
{
    public Guid Id { get; }

    public ResourceForbiddenException(Guid id)
        : base($"Access denied for resource {id}.")
    {
        Id = id;
    }
}
```

**Acceptance criteria:**
- Class compiles and follows existing exception patterns in the project
- Has a `Guid Id` property for the denied resource

---

### Task 1.2 — Add Ownership Enforcement to `IToDoItemService` and `ToDoItemService`

**Files to modify:**
- `src/Insequens.Domain/ServiceContracts/IToDoItemService.cs`
- `src/Insequens.Core/Services/ToDoItemService.cs`

**Steps:**

1. Update `IToDoItemService` — add `Guid userId` as a parameter to every method that operates on a specific item: `DeleteToDoItemAsync`, `GetToDoItem`, `ToggleToDoItemCompleteAsync`, `UpdateToDoItemPriorityAsync`, `UpdateToDoItemNameAsync`, `UpdateToDoItemDescriptionAsync`, `UpdateToDoItemDueDateAsync`, `UpdateToDoItemAsync`.

2. In `ToDoItemService`, add a private helper:

```csharp
private async Task<ToDoItem> GetOwnedItemAsync(Guid id, Guid userId)
{
    var item = await _toDoItemRepository.FindAsync(id)
        ?? throw new ToDoItemNotFoundException(id);
    if (item.UserId != userId)
        throw new ResourceForbiddenException(id);
    return item;
}
```

3. Replace every `_toDoItemRepository.FindAsync(id)` and `_toDoItemRepository.Find(id)` call in the service with `GetOwnedItemAsync(id, userId)`.

4. Update all method signatures to include the `userId` parameter.

**Acceptance criteria:**
- Every method that touches a specific item validates ownership
- `ResourceForbiddenException` thrown when item exists but belongs to a different user
- `ToDoItemNotFoundException` thrown when item does not exist
- `GetUserToDoItemsAsync` already filters by `userId` — no change needed

---

### Task 1.3 — Update `ToDoItemController` to Pass `UserId`

**File to modify:** `src/Insequens.Api/Controllers/ToDoItemController.cs`

Update every action method to pass the existing `UserId` property to the corresponding service call. Example:

```csharp
// Before:
await _toDoItemService.DeleteToDoItemAsync(id);
// After:
await _toDoItemService.DeleteToDoItemAsync(id, UserId);
```

Apply this to: `DeleteToDoItemAsync`, `GetToDoItem`, `CompleteToDoItem`, `UpdateToDoItemPriorityAsync`, `UpdateToDoItemNameAsync`, `UpdateToDoItemDescriptionAsync`, `UpdateToDoItemDueDateAsync`.

**Acceptance criteria:**
- All controller→service calls include `UserId`
- Solution compiles with zero errors

---

### Task 1.4 — Update `ExceptionMiddleware` for 403 + Production Safety

**File to modify:** `src/Insequens.Api/ExceptionMiddleware.cs`

**Steps:**

1. Add a constructor that accepts `IWebHostEnvironment`:

```csharp
private readonly IWebHostEnvironment _env;

public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    Next = next;
    _env = env;
}
```

2. Add a new catch block **before** the generic `Exception` catch for `ResourceForbiddenException`:

```csharp
catch (ResourceForbiddenException)
{
    context.Response.ContentType = "application/problem+json";
    context.Response.StatusCode = StatusCodes.Status403Forbidden;

    var problemDetails = new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Access denied.",
        Detail = string.Empty,
        Type = "Error"
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
}
```

3. In the generic `Exception` catch, suppress details in production:

```csharp
catch (Exception ex)
{
    context.Response.ContentType = "application/problem+json";
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

    var detail = _env.IsDevelopment()
        ? $"Message: {ex.Message} Inner Exception: {ex.InnerException}"
        : "An unexpected error occurred. Please try again later.";

    var problemDetails = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Internal Server Error",
        Detail = detail,
        Type = "Error"
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
}
```

4. Add the `using` for `ResourceForbiddenException` at the top.

**Acceptance criteria:**
- 403 returned for ownership violations
- 500 responses in non-Development environments contain no exception details
- Development environment still shows full exception info for debugging

---

### Task 1.5 — Lock Down CORS Policy

**Files to modify:**
- `src/Insequens.Api/Program.cs`
- `src/Insequens.Api/appsettings.json`
- Create `src/Insequens.Api/appsettings.Development.json` (if not exists)

**Steps:**

1. In `appsettings.json`, add:

```json
"Cors": {
    "AllowedOrigins": [ "https://www.insequens.com" ]
}
```

2. In `appsettings.Development.json`, add:

```json
"Cors": {
    "AllowedOrigins": [ "http://localhost:5173", "http://localhost:8081" ]
}
```

3. In `Program.cs`, replace the CORS policy:

```csharp
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "InsequensPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});
```

4. Update `app.UseCors("InsequensPolicy")` to use the new policy name.

**Acceptance criteria:**
- Production only allows requests from `www.insequens.com`
- Development allows `localhost:5173` and `localhost:8081`
- `AllowCredentials()` is used (required for Authorization header)
- If config is empty, falls back to `AllowAnyOrigin` (dev safety net only)

---

### Task 1.6 — Fix WarmupController

**File to modify:** `src/Insequens.Api/Controllers/WarmupController.cs`

Replace the entire `Get()` method body. Remove the user query and password hash verification. Replace with:

```csharp
[HttpGet]
public async Task<IActionResult> Get()
{
    var canConnect = await _context.Database.CanConnectAsync();
    return canConnect ? Ok("Healthy") : StatusCode(503, "Database unavailable");
}
```

**Acceptance criteria:**
- No user data is queried
- No password hashing occurs
- Returns 200 if DB is reachable, 503 if not

---

### Task 1.7 — Normalize Auth Error Messages

**File to modify:** `src/Insequens.Api/Controllers/AuthController.cs`

In the `Login` method:

1. Replace `return Unauthorized("User doesn't exist.");` with `return Unauthorized("Invalid email or password.");`
2. Replace `return Unauthorized("Wrong password.");` with `return Unauthorized("Invalid email or password.");`
3. Keep the "Please confirm your email before logging in." message as-is (this is actionable and doesn't leak existence).

In the `ForgotPassword` method:

4. Replace `return BadRequest("User not found.");` with `return Ok("If an account with that email exists, a password reset link has been sent.");` — do not reveal whether the email is registered.

**Acceptance criteria:**
- Login returns identical message for non-existent user and wrong password
- ForgotPassword returns success message regardless of whether user exists
- Email-not-confirmed remains a separate, actionable message

---

## 5. Phase 2 — CQRS Foundation & MediatR Pipeline

> **Goal:** Create the `Insequens.Application` project with MediatR pipeline and cross-cutting behaviors.  
> **Fixes:** Foundation for F01 (systemic), F09

### Task 2.1 — Create `Insequens.Application` Project

**Steps:**

1. Create new class library: `src/Insequens.Application/Insequens.Application.csproj` targeting `net10.0`.

2. Add NuGet packages:
   - `MediatR` (latest 12.x)
   - `FluentValidation` (latest 11.x)
   - `FluentValidation.DependencyInjectionExtensions`
   - `AutoMapper` (12.0.1 — match existing)
   - `Microsoft.EntityFrameworkCore` (10.0.4 — match existing)

3. Add project references:
   - `Insequens.Domain`

4. Create folder structure:

```
src/Insequens.Application/
├── Behaviors/
├── Commands/
│   └── ToDoItem/
├── Queries/
│   └── ToDoItem/
├── Validators/
│   └── ToDoItem/
├── Exceptions/
├── Models/
├── Profiles/
└── DependencyInjection.cs
```

5. Add the project to `Insequens.sln`.

6. Update `Insequens.Api.csproj` to reference `Insequens.Application` (instead of, or in addition to, `Insequens.Core` during migration).

**Acceptance criteria:**
- Project compiles
- Folder structure created
- Referenced from Api project
- Solution builds successfully

---

### Task 2.2 — Create `IOwned` Marker Interface

**File to create:** `src/Insequens.Application/Commands/IOwned.cs`

```csharp
namespace Insequens.Application.Commands;

/// <summary>
/// Marker interface for commands/queries that operate on a user-owned resource.
/// The OwnershipBehavior uses this to enforce access control automatically.
/// </summary>
public interface IOwned
{
    Guid UserId { get; }
    Guid ItemId { get; }
}
```

**Acceptance criteria:**
- Interface defined with `UserId` and `ItemId` properties
- Any command/query implementing this will automatically get ownership validation via the pipeline behavior

---

### Task 2.3 — Create `OwnershipBehavior<TRequest, TResponse>`

**File to create:** `src/Insequens.Application/Behaviors/OwnershipBehavior.cs`

```csharp
using Insequens.Application.Commands;
using Insequens.Core.Exceptions;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using MediatR;

namespace Insequens.Application.Behaviors;

public class OwnershipBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IOwned
{
    private readonly IDataContext _dataContext;

    public OwnershipBehavior(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var repo = _dataContext.GetRepository<ToDoItem>();
        var item = await repo.FindAsync(request.ItemId)
            ?? throw new ToDoItemNotFoundException(request.ItemId);

        if (item.UserId != request.UserId)
            throw new ResourceForbiddenException(request.ItemId);

        return await next();
    }
}
```

**Acceptance criteria:**
- Only runs for requests implementing `IOwned`
- Throws `ToDoItemNotFoundException` if item doesn't exist
- Throws `ResourceForbiddenException` if ownership doesn't match
- Calls `next()` if validation passes

---

### Task 2.4 — Create `ValidationBehavior<TRequest, TResponse>`

**File to create:** `src/Insequens.Application/Behaviors/ValidationBehavior.cs`

```csharp
using FluentValidation;
using MediatR;

namespace Insequens.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

**Acceptance criteria:**
- Runs FluentValidation validators for every request that has them registered
- Throws `FluentValidation.ValidationException` with all failures collected
- Passes through silently if no validators are registered for the request type

---

### Task 2.5 — Create `LoggingBehavior<TRequest, TResponse>`

**File to create:** `src/Insequens.Application/Behaviors/LoggingBehavior.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Insequens.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms",
            requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
```

**Acceptance criteria:**
- Logs request name before and after handling
- Includes elapsed time in milliseconds
- Uses structured logging compatible with Serilog

---

### Task 2.6 — Create `DependencyInjection.cs` Registration

**File to create:** `src/Insequens.Application/DependencyInjection.cs`

```csharp
using FluentValidation;
using Insequens.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        // Pipeline order matters: Logging → Validation → Ownership → Handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(OwnershipBehavior<,>));

        services.AddAutoMapper(assembly);

        return services;
    }
}
```

**Acceptance criteria:**
- MediatR, validators, behaviors, and AutoMapper all registered from the Application assembly
- Pipeline order: Logging → Validation → Ownership → Handler
- Extension method callable from `Program.cs` as `builder.Services.AddApplication()`

---

### Task 2.7 — Add FluentValidation Exception Handler to Middleware

**File to modify:** `src/Insequens.Api/ExceptionMiddleware.cs`

Add a new catch block for `FluentValidation.ValidationException` **before** the generic `Exception` catch:

```csharp
catch (FluentValidation.ValidationException ex)
{
    context.Response.ContentType = "application/problem+json";
    context.Response.StatusCode = StatusCodes.Status400BadRequest;

    var errors = ex.Errors
        .GroupBy(e => e.PropertyName)
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

    var problemDetails = new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Validation failed.",
        Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
        Type = "ValidationError",
        Extensions = { ["errors"] = errors }
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
}
```

**Acceptance criteria:**
- 400 returned with grouped validation errors per property
- Error format matches RFC 7807 ProblemDetails
- Does not leak internal details

---

### Task 2.8 — Wire Application into `Program.cs`

**File to modify:** `src/Insequens.Api/Program.cs`

1. Add `using Insequens.Application;`
2. After the existing service registrations, add: `builder.Services.AddApplication();`
3. Keep the existing `IToDoItemService` registration for now (it will be removed in Phase 3 after all operations are migrated).

**Acceptance criteria:**
- `builder.Services.AddApplication()` called in Program.cs
- Solution compiles with both old service pattern and new MediatR registered
- No runtime conflicts

---

## 6. Phase 3 — Command & Query Migration

> **Goal:** Replace every `IToDoItemService` method with a MediatR command or query handler.  
> **After this phase:** `IToDoItemService`, `ToDoItemService`, and `Insequens.Core` can be deleted.

### Task 3.1 — Move AutoMapper Profile to Application

**File to create:** `src/Insequens.Application/Profiles/ToDoItemProfile.cs`  
**File to delete (after migration):** `src/Insequens.Core/Profiles/ToDoItemProfile.cs`

Copy the existing profile. It will be auto-discovered by the `AddAutoMapper(assembly)` call in `DependencyInjection.cs`.

**Acceptance criteria:**
- Profile compiles in Application project
- All existing mappings preserved

---

### Task 3.2 — Move Exceptions to Application

**Files to create:**
- `src/Insequens.Application/Exceptions/ToDoItemNotFoundException.cs`
- `src/Insequens.Application/Exceptions/ResourceForbiddenException.cs`

Copy both exceptions from `Insequens.Core/Exceptions/`. Update all `using` statements in `ExceptionMiddleware.cs` to point to the new namespace.

**Acceptance criteria:**
- Exceptions in Application project
- ExceptionMiddleware references updated
- Solution compiles

---

### Task 3.3 — Create `PaginatedResult<T>`

**File to create:** `src/Insequens.Application/Models/PaginatedResult.cs`

```csharp
namespace Insequens.Application.Models;

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

**Acceptance criteria:**
- Generic record with computed pagination properties
- Usable as return type for list queries

---

### Task 3.4 — Create `CreateToDoItemCommand` + Handler

**Files to create:**
- `src/Insequens.Application/Commands/ToDoItem/CreateToDoItemCommand.cs`
- `src/Insequens.Application/Commands/ToDoItem/CreateToDoItemHandler.cs`
- `src/Insequens.Application/Validators/ToDoItem/CreateToDoItemValidator.cs`

**Command:**

```csharp
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record CreateToDoItemCommand(
    string Name,
    string? Description,
    int Priority,
    DateOnly? DueDate,
    Guid UserId) : IRequest<ToDoItemGetDetailsModel>;
```

**Handler:**

```csharp
using AutoMapper;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public class CreateToDoItemHandler
    : IRequestHandler<CreateToDoItemCommand, ToDoItemGetDetailsModel>
{
    private readonly IDataContext _dataContext;
    private readonly IMapper _mapper;

    public CreateToDoItemHandler(IDataContext dataContext, IMapper mapper)
    {
        _dataContext = dataContext;
        _mapper = mapper;
    }

    public async Task<ToDoItemGetDetailsModel> Handle(
        CreateToDoItemCommand request, CancellationToken cancellationToken)
    {
        var item = new Domain.Entities.ToDoItem
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            Description = request.Description,
            Priority = (Domain.Types.TaskPriority?)request.Priority,
            DueDate = request.DueDate,
            IsCompleted = false
        };

        _dataContext.GetRepository<Domain.Entities.ToDoItem>().Add(item);
        await _dataContext.SaveChangesAsync();

        return _mapper.Map<ToDoItemGetDetailsModel>(item);
    }
}
```

**Validator:**

```csharp
using FluentValidation;

namespace Insequens.Application.Validators.ToDoItem;

public class CreateToDoItemValidator : AbstractValidator<Commands.ToDoItem.CreateToDoItemCommand>
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

**Acceptance criteria:**
- Command does NOT implement `IOwned` (it creates a new resource, no existing ownership to check)
- Validation runs automatically via pipeline behavior
- Handler maps manually (not AutoMapper) for commands — clearer intent
- Returns `ToDoItemGetDetailsModel` matching existing API contract

---

### Task 3.5 — Create `DeleteToDoItemCommand` + Handler

**Files to create:**
- `src/Insequens.Application/Commands/ToDoItem/DeleteToDoItemCommand.cs`
- `src/Insequens.Application/Commands/ToDoItem/DeleteToDoItemHandler.cs`

**Command:**

```csharp
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record DeleteToDoItemCommand(Guid ItemId, Guid UserId)
    : IRequest<Unit>, IOwned;
```

**Handler:**

```csharp
using Insequens.Domain.DataAccess;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public class DeleteToDoItemHandler : IRequestHandler<DeleteToDoItemCommand, Unit>
{
    private readonly IDataContext _dataContext;

    public DeleteToDoItemHandler(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<Unit> Handle(DeleteToDoItemCommand request, CancellationToken cancellationToken)
    {
        var repo = _dataContext.GetRepository<Domain.Entities.ToDoItem>();
        var item = (await repo.FindAsync(request.ItemId))!; // OwnershipBehavior already validated
        repo.Remove(item);
        await _dataContext.SaveChangesAsync();
        return Unit.Value;
    }
}
```

**Acceptance criteria:**
- Implements `IOwned` → ownership validated automatically by `OwnershipBehavior`
- Handler can safely assume item exists and is owned (behavior already checked)
- Returns `Unit` (void equivalent)

---

### Task 3.6 — Create `ToggleToDoItemCompleteCommand` + Handler

Same pattern as Task 3.5. Command: `record ToggleToDoItemCompleteCommand(Guid ItemId, Guid UserId) : IRequest<Unit>, IOwned;`

Handler: find item (already validated by behavior), flip `IsCompleted`, save.

---

### Task 3.7 — Create `UpdateToDoItemPriorityCommand` + Handler

Command: `record UpdateToDoItemPriorityCommand(Guid ItemId, Guid UserId, TaskPriority Priority) : IRequest<Unit>, IOwned;`

Handler: find item, set `Priority`, save.

---

### Task 3.8 — Create `UpdateToDoItemNameCommand` + Handler + Validator

Command: `record UpdateToDoItemNameCommand(Guid ItemId, Guid UserId, string Name) : IRequest<Unit>, IOwned;`

Validator: `RuleFor(x => x.Name).NotEmpty().MaximumLength(200);`

Handler: find item, set `Name`, save.

---

### Task 3.9 — Create `UpdateToDoItemDescriptionCommand` + Handler

Command: `record UpdateToDoItemDescriptionCommand(Guid ItemId, Guid UserId, string Description) : IRequest<Unit>, IOwned;`

Handler: find item, set `Description`, save.

---

### Task 3.10 — Create `UpdateToDoItemDueDateCommand` + Handler

Command: `record UpdateToDoItemDueDateCommand(Guid ItemId, Guid UserId, DateOnly Date) : IRequest<Unit>, IOwned;`

Handler: find item, set `DueDate`, save.

---

### Task 3.11 — Create `GetToDoItemQuery` + Handler

**Files to create:**
- `src/Insequens.Application/Queries/ToDoItem/GetToDoItemQuery.cs`
- `src/Insequens.Application/Queries/ToDoItem/GetToDoItemHandler.cs`

**Query:**

```csharp
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Queries.ToDoItem;

public record GetToDoItemQuery(Guid ItemId, Guid UserId)
    : IRequest<ToDoItemGetDetailsModel>, IOwned;
```

**Handler:**

```csharp
using AutoMapper;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Queries.ToDoItem;

public class GetToDoItemHandler
    : IRequestHandler<GetToDoItemQuery, ToDoItemGetDetailsModel>
{
    private readonly IDataContext _dataContext;
    private readonly IMapper _mapper;

    public GetToDoItemHandler(IDataContext dataContext, IMapper mapper)
    {
        _dataContext = dataContext;
        _mapper = mapper;
    }

    public async Task<ToDoItemGetDetailsModel> Handle(
        GetToDoItemQuery request, CancellationToken cancellationToken)
    {
        var item = (await _dataContext.GetRepository<Domain.Entities.ToDoItem>()
            .FindAsync(request.ItemId))!; // OwnershipBehavior already validated

        return _mapper.Map<ToDoItemGetDetailsModel>(item);
    }
}
```

**Acceptance criteria:**
- Implements `IOwned` → ownership checked by pipeline
- Uses AutoMapper for entity → DTO projection
- Returns existing `ToDoItemGetDetailsModel` record

---

### Task 3.12 — Create `GetUserToDoItemsQuery` + Handler (with pagination)

**Files to create:**
- `src/Insequens.Application/Queries/ToDoItem/GetUserToDoItemsQuery.cs`
- `src/Insequens.Application/Queries/ToDoItem/GetUserToDoItemsHandler.cs`

**Query:**

```csharp
using Insequens.Application.Models;
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Queries.ToDoItem;

public record GetUserToDoItemsQuery(
    Guid UserId,
    bool IsCompleted,
    int Page,
    int PageSize) : IRequest<PaginatedResult<ToDoItemGetListModel>>;
```

Note: this does NOT implement `IOwned` — it lists the current user's own items, filtered by `UserId` in the query itself.

**Handler:**

```csharp
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Models;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Queries.ToDoItem;

public class GetUserToDoItemsHandler
    : IRequestHandler<GetUserToDoItemsQuery, PaginatedResult<ToDoItemGetListModel>>
{
    private readonly IDataContext _dataContext;
    private readonly IMapper _mapper;

    public GetUserToDoItemsHandler(IDataContext dataContext, IMapper mapper)
    {
        _dataContext = dataContext;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ToDoItemGetListModel>> Handle(
        GetUserToDoItemsQuery request, CancellationToken cancellationToken)
    {
        var query = _dataContext.GetRepository<ToDoItem>().AsQueryable()
            .Where(x => x.UserId == request.UserId && x.IsCompleted == request.IsCompleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Priority)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ProjectTo<ToDoItemGetListModel>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ToDoItemGetListModel>(
            items, totalCount, request.Page, request.PageSize);
    }
}
```

**Acceptance criteria:**
- Returns `PaginatedResult<T>` with `TotalCount`, `Page`, `PageSize`, `HasNext`, `HasPrevious`
- Uses `ProjectTo<>` for efficient SQL projection
- Filters by `UserId` directly in the query (no `IOwned` needed)

---

### Task 3.13 — Rewrite `ToDoItemController` to Use MediatR

**File to modify:** `src/Insequens.Api/Controllers/ToDoItemController.cs`

**Full replacement:**

```csharp
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.Types;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Insequens.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route(Constants.BaseUrl)]
[ApiController]
public class ToDoItemController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid UserId => Guid.Parse(
        User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

    public ToDoItemController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserToDoItems(
        [FromQuery] bool isCompleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1)
            return BadRequest("Page and pageSize must be greater than 0.");

        var result = await _mediator.Send(
            new GetUserToDoItemsQuery(UserId, isCompleted, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateToDoItem(
        [FromBody] CreateToDoItemCommand command)
    {
        var item = await _mediator.Send(command with { UserId = UserId });
        return CreatedAtAction(nameof(GetToDoItem), new { id = item.Id }, item);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetToDoItem(Guid id)
    {
        var item = await _mediator.Send(new GetToDoItemQuery(id, UserId));
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteToDoItem(Guid id)
    {
        await _mediator.Send(new DeleteToDoItemCommand(id, UserId));
        return NoContent();
    }

    [HttpPatch("{id:guid}/togglecomplete")]
    public async Task<IActionResult> ToggleComplete(Guid id)
    {
        await _mediator.Send(new ToggleToDoItemCompleteCommand(id, UserId));
        return Ok();
    }

    [HttpPatch("{id:guid}/priority")]
    public async Task<IActionResult> UpdatePriority(
        Guid id, [FromBody] TaskPriority priority)
    {
        await _mediator.Send(new UpdateToDoItemPriorityCommand(id, UserId, priority));
        return NoContent();
    }

    [HttpPatch("{id:guid}/name")]
    public async Task<IActionResult> UpdateName(
        Guid id, [FromBody] string name)
    {
        await _mediator.Send(new UpdateToDoItemNameCommand(id, UserId, name));
        return NoContent();
    }

    [HttpPatch("{id:guid}/description")]
    public async Task<IActionResult> UpdateDescription(
        Guid id, [FromBody] string description)
    {
        await _mediator.Send(new UpdateToDoItemDescriptionCommand(id, UserId, description));
        return NoContent();
    }

    [HttpPatch("{id:guid}/duedate")]
    public async Task<IActionResult> UpdateDueDate(
        Guid id, [FromBody] DateOnly date)
    {
        await _mediator.Send(new UpdateToDoItemDueDateCommand(id, UserId, date));
        return NoContent();
    }
}
```

**Acceptance criteria:**
- Controller depends only on `IMediator`, no service interface
- All return types are `IActionResult` (consistent, F13 fixed)
- `UserId` injected into every command/query
- API routes and HTTP contracts unchanged (backward compatible)
- No commented-out code (F14 fixed)

---

### Task 3.14 — Remove `Insequens.Core` Project

**Steps:**

1. Verify no other project references `Insequens.Core`.
2. Remove the project reference from `Insequens.Api.csproj`.
3. Remove the project from `Insequens.sln`.
4. Delete the `src/Insequens.Core/` directory.
5. Remove the `IToDoItemService` registration from `Program.cs` (`builder.Services.AddScoped<IToDoItemService, ToDoItemService>()`).
6. Delete `src/Insequens.Domain/ServiceContracts/IToDoItemService.cs`.
7. Verify solution compiles.

**Acceptance criteria:**
- `Insequens.Core` completely removed
- `IToDoItemService` interface deleted
- No orphaned `using` statements
- Solution compiles and runs

---

## 7. Phase 4 — Validation, Pagination & API Quality

> **Goal:** Ensure all inputs are validated, all list responses are paginated, and the API is consistent.  
> **Fixes:** F09, F10, F13

### Task 4.1 — Add Remaining Validators

**Files to create in** `src/Insequens.Application/Validators/ToDoItem/`:

`GetUserToDoItemsValidator.cs`:

```csharp
using FluentValidation;
using Insequens.Application.Queries.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class GetUserToDoItemsValidator : AbstractValidator<GetUserToDoItemsQuery>
{
    public GetUserToDoItemsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
```

Create validators for any other commands that accept user input: `UpdateToDoItemNameCommand` (already done in Task 3.8), `CreateToDoItemCommand` (already done in Task 3.4).

**Acceptance criteria:**
- Pagination parameters validated
- All validators auto-discovered by `AddValidatorsFromAssembly`
- Invalid requests return 400 with structured errors before reaching handlers

---

### Task 4.2 — Remove Inline Validation from Controller

**File to modify:** `src/Insequens.Api/Controllers/ToDoItemController.cs`

Remove the manual `if (page < 1 || pageSize < 1) return BadRequest(...)` check from `GetUserToDoItems`. This is now handled by `GetUserToDoItemsValidator` via the pipeline.

**Acceptance criteria:**
- No manual validation in any controller method
- All validation flows through FluentValidation pipeline behavior

---

## 8. Phase 5 — Infrastructure Modernisation

> **Goal:** Fix deprecated libraries, audit timestamps, Identity registration, and email.  
> **Fixes:** F05, F06, F08, F12, F16

### Task 5.1 — Remove Dual Identity Registration

**File to modify:** `src/Insequens.Api/Program.cs`

Delete the entire `AddIdentityCore<ApplicationUser>` block (lines starting with `builder.Services.AddIdentityCore<ApplicationUser>(options =>` through `.AddDefaultTokenProviders()`).

Keep only the `AddIdentity<ApplicationUser, IdentityRole>()` block. Add password options to its lambda:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<InsequensContext>()
.AddDefaultTokenProviders();
```

**Acceptance criteria:**
- Only one Identity registration exists
- `RequireConfirmedEmail` enabled
- Solution compiles, auth flow still works

---

### Task 5.2 — Fix Audit Timestamps to UTC

**File to modify:** `src/Infrastructure/Insequens.Infrastructure.DataAccess/DataContext.cs`

Change `var now = DateTime.Now;` to `var now = DateTime.UtcNow;` in `SetAuditableProperties()`.

**Acceptance criteria:**
- One-line change
- All new/modified entities get UTC timestamps

---

### Task 5.3 — Rename `AddOrUpdate` to `Add`

**Files to modify:**
- `src/Insequens.Domain/DataAccess/IRepository.cs`
- `src/Infrastructure/Insequens.Infrastructure.DataAccess/Repository.cs`

Rename `AddOrUpdate(T entity, bool? isNew = null)` → `Add(T entity)`. Rename `AddOrUpdate(IEnumerable<T> entities, bool? isNew = null)` → `Add(IEnumerable<T> entities)`. Remove the unused `isNew` parameter.

Update any remaining call sites (if `Insequens.Core` is already deleted, there should be none — but verify with a solution-wide search).

**Acceptance criteria:**
- Method name accurately reflects behavior
- No `AddOrUpdate` references remain in the solution

---

### Task 5.4 — Replace SmtpClient with MailKit

**Files to modify:**
- `src/Insequens.Api/Insequens.Api.csproj` — add `MailKit` NuGet
- `src/Insequens.Api/EmailSender.cs` — full rewrite

```csharp
using Insequens.Domain.ServiceContracts;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Insequens.Api;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _configuration;

    public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(_configuration["Email:From"]));
        mimeMessage.To.Add(MailboxAddress.Parse(email));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("html") { Text = message };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _configuration["Email:SmtpServer"],
            int.Parse(_configuration["Email:Port"]!),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            _configuration["Email:Username"],
            _configuration["Email:Password"]);

        await client.SendAsync(mimeMessage);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Recipient}", email);
    }
}
```

**Acceptance criteria:**
- `System.Net.Mail` no longer used
- MailKit handles TLS properly
- `IEmailSender` interface unchanged
- Connection is disposed via `using`

---

### Task 5.5 — Remove Newtonsoft.Json from Domain

**File to modify:** `src/Insequens.Domain/Insequens.Domain.csproj`

Remove: `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />`

Verify no code in the Domain project uses `Newtonsoft.Json`. (It doesn't — the entire solution uses `System.Text.Json`.)

**Acceptance criteria:**
- Package reference removed
- Solution compiles

---

## 9. Phase 6 — Code Cleanup & Consistency

> **Goal:** Remove dead code, consolidate models, clean up configuration.  
> **Fixes:** F14, F15, F17

### Task 6.1 — Remove All Commented-Out Code

**Files to modify:** `src/Insequens.Api/Program.cs`

Remove:
- The commented `Configure<IdentityOptions>` block
- The commented `MapIdentityApi` line
- The commented `/logout` minimal endpoint block
- The commented `AddHostedService<WarmKeeper>()` line

**Acceptance criteria:**
- Zero commented-out code blocks remain in Program.cs
- If WarmKeeper is still needed, register it properly or delete the file

---

### Task 6.2 — Consolidate Auth Request Models

**File to modify:** `src/Insequens.Api/Controllers/AuthController.cs`

The controller currently imports `Microsoft.AspNetCore.Identity.Data` for `RegisterRequest`, `LoginRequest`, `ForgotPasswordRequest` while custom models exist in `Domain/Models/Auth/`.

1. Update AuthController to use the custom Domain models for all endpoints:
   - `LoginRequest` → `LoginRequestModel`
   - `RegisterRequest` → `RegisterRequestModel`
   - `ForgotPasswordRequest` → use a new `ForgotPasswordRequestModel` record in Domain/Models/Auth
2. Remove `using Microsoft.AspNetCore.Identity.Data;`
3. Convert all auth model classes to records for consistency.

**Acceptance criteria:**
- AuthController uses only models from `Domain/Models/Auth`
- No `Microsoft.AspNetCore.Identity.Data` imports
- All auth models are records

---

### Task 6.3 — Clean Up `appsettings.json`

**File to modify:** `src/Insequens.Api/appsettings.json`

1. Remove the local dev connection string comment (`//Local`)
2. Set connection string to a placeholder: `"Server=(placeholder);Database=Insequens;..."` with a comment directing to User Secrets
3. Ensure `Jwt:Key` is empty (loaded from User Secrets / environment variables)
4. Ensure `Email:Password` is empty

Run `dotnet user-secrets init` in the Api project if not already initialized.

**Acceptance criteria:**
- No real credentials or server names in committed config
- Developers guided to User Secrets for local values

---

### Task 6.4 — Delete WarmKeeper If Unused

If `WarmKeeper` background service is not registered in `Program.cs` (it's currently commented out), delete `src/Insequens.Api/WarmKeeper.cs`.

If it IS needed (for keeping the app warm on a free-tier host), re-evaluate: it currently pings `https://www.insequens.com:5000/warmup` every 10 seconds, which is extremely aggressive. If kept, change the interval to at least 5 minutes and make the URL configurable.

**Acceptance criteria:**
- Either deleted or fixed with configurable URL and reasonable interval

---

## 10. Phase 7 — Test Infrastructure

> **Goal:** Establish unit and integration test projects with meaningful coverage.  
> **Fixes:** F07

### Task 7.1 — Create `Insequens.Application.Tests` Project

**Steps:**

1. Create `tests/Insequens.Application.Tests/Insequens.Application.Tests.csproj` targeting `net10.0`.
2. Add NuGet packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `NSubstitute`, `FluentAssertions`, `AutoMapper`.
3. Add project references: `Insequens.Application`, `Insequens.Domain`.
4. Add to `Insequens.sln` under a `tests/` solution folder.

**Acceptance criteria:**
- Project compiles
- `dotnet test` discovers the project (even with 0 tests initially)

---

### Task 7.2 — Write Handler Unit Tests: `CreateToDoItemHandler`

**File to create:** `tests/Insequens.Application.Tests/Commands/CreateToDoItemHandlerTests.cs`

Test cases:
- `Handle_WithValidCommand_CreatesItemAndReturnsDetails`
- `Handle_SetsUserIdFromCommand`
- `Handle_SetsIsCompletedToFalse`

Mock `IDataContext` and `IRepository<ToDoItem>` using NSubstitute. Verify `Add()` is called with correct entity properties.

---

### Task 7.3 — Write Handler Unit Tests: Ownership Enforcement

**File to create:** `tests/Insequens.Application.Tests/Behaviors/OwnershipBehaviorTests.cs`

Test cases:
- `Handle_WithOwnedItem_CallsNext`
- `Handle_WithNonexistentItem_ThrowsToDoItemNotFoundException`
- `Handle_WithOtherUsersItem_ThrowsResourceForbiddenException`

---

### Task 7.4 — Write Handler Unit Tests: `GetUserToDoItemsHandler`

Test the pagination handler returns correct `PaginatedResult<T>` structure. Mock the repository to return known data sets.

---

### Task 7.5 — Write Validation Unit Tests

**File to create:** `tests/Insequens.Application.Tests/Validators/CreateToDoItemValidatorTests.cs`

Test cases:
- `Validate_EmptyName_Fails`
- `Validate_NameOver200Chars_Fails`
- `Validate_ValidCommand_Passes`
- `Validate_PriorityOutOfRange_Fails`

---

### Task 7.6 — Create `Insequens.Api.Tests` Integration Test Project

**Steps:**

1. Create `tests/Insequens.Api.Tests/Insequens.Api.Tests.csproj`.
2. Add NuGet: `Microsoft.AspNetCore.Mvc.Testing`, `xunit`, `FluentAssertions`, `Microsoft.EntityFrameworkCore.InMemory`.
3. Add project reference to `Insequens.Api`.

4. Create `CustomWebApplicationFactory.cs`:

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real SQL Server registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<InsequensContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add InMemory database
            services.AddDbContext<InsequensContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            // Seed test user
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InsequensContext>();
            db.Database.EnsureCreated();
            // Seed user + test data here
        });
    }
}
```

5. Create a JWT token helper that generates valid test tokens.

**Acceptance criteria:**
- Factory replaces SQL Server with InMemory
- Test user seeded
- JWT helper generates valid tokens for test requests

---

### Task 7.7 — Write Integration Tests: Auth Flow

Test cases covering the full auth flow via HTTP:
- `Register_WithValidData_Returns200`
- `Login_WithValidCredentials_ReturnsTokens`
- `Login_WithInvalidPassword_Returns401WithGenericMessage`
- `Login_WithNonexistentEmail_Returns401WithSameGenericMessage`
- `RefreshToken_WithValidToken_ReturnsNewTokens`

---

### Task 7.8 — Write Integration Tests: ToDoItem CRUD + Ownership

Test cases:
- `CreateToDoItem_ReturnsCreated201`
- `GetToDoItems_ReturnsPaginatedResult`
- `GetToDoItem_WithOtherUsersId_Returns403`
- `DeleteToDoItem_WithOtherUsersId_Returns403`
- `ToggleComplete_WithOwnedItem_Returns200`
- `CreateToDoItem_WithEmptyName_Returns400WithValidationErrors`

---

## 11. Phase 8 — CI/CD & DevOps

> **Goal:** Update the build pipeline to run tests and modernise the build steps.

### Task 8.1 — Update `azure-pipelines.yml`

Replace the current VSBuild + VSTest steps with `dotnet` CLI commands:

```yaml
trigger:
  - master

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
  - task: UseDotNet@2
    inputs:
      packageType: 'sdk'
      version: '10.x'

  - script: dotnet restore
    displayName: 'Restore dependencies'

  - script: dotnet build --configuration $(buildConfiguration) --no-restore
    displayName: 'Build'

  - script: dotnet test --configuration $(buildConfiguration) --no-build --logger trx --results-directory $(Build.ArtifactStagingDirectory)/TestResults
    displayName: 'Run tests'

  - task: PublishTestResults@2
    inputs:
      testResultsFormat: 'VSTest'
      testResultsFiles: '$(Build.ArtifactStagingDirectory)/TestResults/*.trx'
    condition: always()
```

**Acceptance criteria:**
- Uses Linux agent (cheaper, faster than Windows)
- Restores, builds, tests in sequence
- Test results published to Azure DevOps
- Pipeline fails on test failures

---

## 12. Task Dependency Map

Tasks can be parallelised across streams. Within a stream, execute sequentially.

```
Stream A: Security (MUST complete first)
  T1.1 → T1.2 → T1.3 → T1.4 → T1.5 → T1.6 → T1.7

Stream B: CQRS Foundation (after Stream A, Phase 1)
  T2.1 → T2.2 → T2.3 → T2.4 → T2.5 → T2.6 → T2.7 → T2.8

Stream C: Command/Query Migration (after Stream B)
  T3.1 → T3.2 → T3.3 → T3.4 → T3.5 → T3.6 → T3.7 → T3.8
  → T3.9 → T3.10 → T3.11 → T3.12 → T3.13 → T3.14

Stream D: Validation Cleanup (after Stream C)
  T4.1 → T4.2

Stream E: Infrastructure (independent, can run parallel with B/C)
  T5.1, T5.2, T5.3, T5.4, T5.5 — all independent of each other

Stream F: Code Cleanup (independent)
  T6.1, T6.2, T6.3, T6.4 — all independent of each other

Stream G: Tests (after Streams C + E complete)
  T7.1 → [T7.2, T7.3, T7.4, T7.5 in parallel]
  T7.6 → [T7.7, T7.8 in parallel]

Stream H: CI/CD (after Stream G)
  T8.1
```

**Critical path:** A → B → C → G → H

**Parallel tracks:** E and F can execute any time after Phase 1 is complete.

---

## 13. Post-Refactor Target State

After all tasks are completed, the Insequens backend v1 will have:

**Architecture:**
- Clean CQRS with MediatR — every operation is a discrete command or query with its own handler
- Three pipeline behaviors enforcing logging, validation, and ownership automatically
- Thin controllers that only translate HTTP → MediatR → HTTP
- Generic Repository + Unit of Work retained as testing seam and cross-cutting concern hook
- Old service layer completely removed — no `IToDoItemService`, no `ToDoItemService`, no `Insequens.Core` project

**Security:**
- Every resource access is ownership-checked via `IOwned` + `OwnershipBehavior` — impossible to bypass
- CORS locked to configured origins per environment
- Exception details suppressed in production
- Auth error messages don't leak user existence
- WarmupController no longer touches user data

**Validation:**
- All request models validated via FluentValidation before reaching handlers
- Invalid input returns structured 400 ProblemDetails with per-property errors
- Zero manual validation in controllers

**API Quality:**
- Paginated endpoints return `PaginatedResult<T>` with `TotalCount`, `HasNext`, `HasPrevious`
- All controller methods return consistent `IActionResult`
- Error responses use RFC 7807 ProblemDetails uniformly

**Code Quality:**
- Zero commented-out code
- Zero duplicate model definitions
- Zero misleading method names
- Zero deprecated library usage (MailKit replaces SmtpClient)
- Single Identity registration
- UTC timestamps for all audit fields
- No committed secrets

**Testing:**
- Unit tests covering command/query handlers with ownership enforcement
- Validation tests for all validators
- Integration tests for auth flows and CRUD operations via HTTP
- CI pipeline runs tests and blocks merges on failure

**Estimated total effort:** 20–30 hours of agent coding time across all 8 phases, approximately 4–6 working days with code review between phases.
