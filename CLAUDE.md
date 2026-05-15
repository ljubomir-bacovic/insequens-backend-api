# CLAUDE.md

This file provides context for Claude Code when working on the Insequens backend.

## What Is This Project

Insequens is a .NET 10 task management Web API using CQRS with MediatR and Clean Architecture. It has a React web frontend and an Expo mobile client that consume this API.

## Build & Run

```
dotnet build
dotnet run --project src/Insequens.Api
dotnet test
```

EF Core migrations:
```
dotnet ef database update --project src/Infrastructure/Insequens.Infrastructure.Data --startup-project src/Insequens.Api
```

Add a new migration:
```
dotnet ef migrations add MigrationName --project src/Infrastructure/Insequens.Infrastructure.Data --startup-project src/Insequens.Api
```

API docs available at /scalar/v1 in Development mode.

## Project Structure

- src/Insequens.Api — Controllers (thin HTTP adapters), middleware, Program.cs (DI root)
- src/Insequens.Application — Commands, queries, handlers, validators, pipeline behaviors, AutoMapper profiles
- src/Insequens.Domain — Entities, enums, DTOs (records), repository/infrastructure interfaces
- src/Infrastructure/Insequens.Infrastructure.Data — EF Core DbContext, Identity, migrations
- src/Infrastructure/Insequens.Infrastructure.DataAccess — Repository<T>, DataContext (Unit of Work)
- tests/Insequens.Application.Tests — Unit tests for handlers, validators, behaviors
- tests/Insequens.Api.Tests — Integration tests with WebApplicationFactory

## Dependency Rule

Domain references nothing. Application references only Domain. Infrastructure references only Domain. Api references Application and Infrastructure. Never violate this. If you need a type from an outer layer in an inner layer, create an interface in Domain.

## How CQRS Works Here

Every operation is a command (write) or query (read) sent via IMediator.Send(). Three pipeline behaviors intercept every request in order:

1. LoggingBehavior — logs request name + elapsed time
2. ValidationBehavior — runs FluentValidation if validators exist for the request type
3. OwnershipBehavior — checks resource ownership for requests implementing IOwned

The IOwned interface has UserId and ItemId. Any command/query accessing a specific resource by ID must implement IOwned. This is the authorization mechanism. If you're creating a new command that modifies an existing resource, add IOwned. If you're creating a new resource, do not add IOwned.

## When Adding a New Feature

1. Decide: command (changes state) or query (reads state)?
2. Create the request record in Application/Commands/{Entity}/ or Application/Queries/{Entity}/
3. If it accesses an existing resource by ID, implement IOwned
4. Create the handler in the same folder
5. If it accepts user input, create a validator in Application/Validators/{Entity}/
6. If it needs a new response shape, create a record in Domain/Models/{Entity}/
7. If it needs a new AutoMapper mapping, add it to the profile in Application/Profiles/
8. Add a controller action that calls _mediator.Send()
9. Write unit tests for the handler and validator
10. Write an integration test for the endpoint

## Naming Conventions

- Commands: {Verb}{Entity}Command — CreateToDoItemCommand, DeleteToDoItemCommand
- Queries: Get{What}Query — GetToDoItemQuery, GetUserToDoItemsQuery
- Handlers: {Verb}{Entity}Handler — CreateToDoItemHandler
- Validators: {CommandOrQuery}Validator — CreateToDoItemValidator
- DTOs: {Entity}{Purpose}Model — ToDoItemGetDetailsModel
- Exceptions: {Entity}{Condition}Exception — ToDoItemNotFoundException
- Tests: MethodName_StateUnderTest_ExpectedBehavior

## Controller Rules

Controllers inject only IMediator. They extract UserId from JWT claims, call _mediator.Send(), and return IActionResult. No business logic, no validation, no data transformation, no repository calls. POST returns CreatedAtAction (201). PATCH and DELETE return NoContent (204). GET returns Ok (200).

## Entity Rules

Entities inherit AuditableEntity which gives them Guid Id, CreatedOn, UpdatedOn. All entity holding user data must have Guid UserId. No data annotations — use Fluent API in OnModelCreating. Entities are classes, not records.

## Data Access Rules

Query handlers: use AsNoTracking(), ProjectTo<TDto>(), pass CancellationToken. Command handlers: use tracked entities, call SaveChangesAsync() once at the end. DataContext sets audit timestamps automatically with DateTime.UtcNow. Never add query methods to the repository — use AsQueryable() with LINQ. Never call SaveChanges from the repository.

## List Endpoints

Every list endpoint returns PaginatedResult<T> with Items, TotalCount, Page, PageSize, TotalPages, HasNext, HasPrevious. Never return a bare List<T>.

## Error Handling

Handlers throw typed exceptions. ExceptionMiddleware maps them to HTTP responses using ProblemDetails. ToDoItemNotFoundException maps to 404. ResourceForbiddenException maps to 403. FluentValidation.ValidationException maps to 400 with grouped errors. Generic Exception maps to 500 with details suppressed outside Development.

If you add a new exception type, update ExceptionMiddleware with a new catch block.

## Hard Rules — Never Violate These

- DateTime.UtcNow only, never DateTime.Now
- No commented-out code
- No empty catch blocks
- No AutoMapper for writes/commands — construct entities explicitly
- No concrete class injection — always use interfaces
- No System.Net.Mail — use MailKit
- No Newtonsoft.Json — use System.Text.Json
- No .Result or .Wait() or .GetAwaiter().GetResult()
- No secrets in committed config files
- File-scoped namespaces
- One public type per file
- Records for commands, queries, DTOs
- Classes for entities, handlers, validators
- Structured logging with named parameters, not string interpolation
- All controller actions return IActionResult, not IResult

## Testing

- xUnit + FluentAssertions + NSubstitute for unit tests
- WebApplicationFactory + InMemory DB for integration tests
- Mock IDataContext and IRepository<T> with NSubstitute in unit tests
- Use real AutoMapper configuration in query handler tests
- Every handler needs tests: happy path + error paths
- Every validator needs tests: valid passes, each invalid field fails
- Test naming: MethodName_StateUnderTest_ExpectedBehavior

## Config & Secrets

Sensitive values (connection strings, JWT key, SMTP password) go in User Secrets for local dev and environment variables for deployment. appsettings.json contains only non-sensitive defaults. Environment variables use double-underscore for nesting: Jwt__Key, ConnectionStrings__InsequensConnection.
