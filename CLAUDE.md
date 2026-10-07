# CLAUDE.md

Project context for Claude Code working on the Insequens backend. Keep this file short and true. If a statement here contradicts the code, the code is the fact and this file has a bug: fix the file in the same PR.

## What this is

A .NET 10 task-management Web API using CQRS with MediatR 12 and Clean Architecture. A React web app and an Expo mobile app consume it. The product is proprietary.

The v1 modernisation (Phases 1–4) is complete. The v2 transformation is tracked as GitHub issues `[INS-001]` … `[INS-102]`, generated from `docs/insequens-v2-enterprise-architecture-assessment.md`. Read that document's Section 7 (target architecture) before any structural change.

## Commands

```
dotnet build                                   # whole solution
dotnet test                                    # all tests (xUnit); Docker must be running for the SQL Server migration tests
dotnet test tests/Insequens.Domain.Tests       # fastest: entity rules only
dotnet test tests/Insequens.Application.Tests  # handlers through the pipeline on SQLite in memory
dotnet run --project src/Insequens.Api         # API on http://localhost:5008, Scalar UI at /scalar/v1 in Development
dotnet tool restore                            # once per clone: dotnet-ef pinned in .config/dotnet-tools.json
dotnet ef migrations add <Name> --project src/Insequens.Infrastructure --startup-project src/Insequens.Api --output-dir Migrations
dotnet ef database update     --project src/Insequens.Infrastructure --startup-project src/Insequens.Api
```

Local secrets go in User Secrets for `src/Insequens.Api` (`Jwt:Key` ≥ 32 chars; the API does not start without it; anything else that differs on your machine). Committed `appsettings*.json` hold only shape, safe defaults and localhost values; deployed environments use environment variables with `__` nesting (`Jwt__Key`). `docs/configuration.md` is the full matrix.

## Layout and dependency rule

```
src/Insequens.Api                    Controllers, ExceptionMiddleware, Program.cs (DI root), Configuration/ (Cors, AllowedHosts, ReverseProxy options), Security/ (headers, JWT bearer setup, HttpContextCurrentUser), RateLimiting/
src/Insequens.Contracts              HTTP request/response records shared with clients: V1/Tasks, V1/Auth, V1/PaginatedResult. References nothing.
src/Insequens.Application            Commands/, Queries/, Validators/, Behaviors/, Authorization/ (IOwned<T>, AuthorizationBehavior, ownership policies), Abstractions/ (IApplicationDbContext, ICurrentUser, Identity/, Email/), Profiles/, Exceptions/, Options/
src/Insequens.Domain                 Entities/ (behaviour and invariants), Types/ (enums), Exceptions/ (DomainException), IOwnedEntity, AuditableEntity
src/Insequens.Infrastructure         Persistence/ (InsequensContext, Configurations/, Interceptors/), Identity/ (ApplicationUser, IdentityService, TokenService, JWT options, key ring), Email/ (MailKit), Migrations/
tests/Insequens.Domain.Tests         Entity behaviour and invariants
tests/Insequens.Application.Tests    Handlers, validators, behaviors and authorization through MediatR on SQLite in memory (Support/TestDbContextFactory)
tests/Insequens.Infrastructure.Tests Audit interceptor, email sender, JWT options/key ring/token service, migrations on SQL Server in Testcontainers (Persistence/Migrations), source guard and project dependency tests
tests/Insequens.Api.Tests            WebApplicationFactory tests (Support/InsequensApiFactory on EF InMemory, or SQLite to observe SQL), Auth/ flows, ToDoItems/, security and rate-limit tests
docs/                                architecture guidelines, v1 plan, v2 assessment
```

Domain and Contracts reference nothing. Application references Domain and Contracts (plus the EF Core package, by accepted exception). Infrastructure references Application and Domain, and implements Application's interfaces. Api references Application, Contracts and Infrastructure, and has no EF provider package. Never add a reference in the other direction; if an inner layer needs an outer type, define an interface in Application. `ProjectDependencyTests` guards the Contracts, Domain and Api rules until INS-062 adds architecture tests.

## How a request flows

Controller extracts `UserId` from the `ClaimTypes.NameIdentifier` claim and calls `IMediator.Send`. Three pipeline behaviors run in this order on every request:

1. `LoggingBehavior` — request name and elapsed time.
2. `ValidationBehavior` — FluentValidation, if a validator is registered for the request type.
3. `AuthorizationBehavior` — for requests implementing `IOwned<TEntity>` (`UserId`, `ResourceId`): loads the entity once through `IOwnershipPolicy<TEntity>` (ID and owner in one query) into the scoped `IResourceContext<TEntity>`, or throws `NotFoundException`. A missing and a foreign resource both return 404, so IDs cannot be probed. `ResourceForbiddenException` (403) is reserved for role checks.

Command handlers take the authorized entity from `IResourceContext<TEntity>`, call its methods, and save through `IApplicationDbContext`. `AuditableEntityInterceptor` stamps `CreatedOn/By` and `UpdatedOn/By` from `TimeProvider` and `ICurrentUser`. `ExceptionMiddleware` maps exceptions to RFC 7807 ProblemDetails. FluentValidation errors become 400 with grouped `errors`; a `DomainException` (violated entity invariant) becomes 400 with the rule as detail.

Auth follows the same flow: `AuthController` injects only `IMediator` and sends the commands in `Application/Commands/Auth/`, whose handlers use `IIdentityService` and `ITokenService` (implemented in Infrastructure). Every failed login or refresh throws `AuthenticationFailedException`, which becomes one generic 401; register, forgot-password and reset-password return the same 202 body whether or not the email has an account. Keep it that way: no auth response may reveal whether an account exists. `tests/Insequens.Api.Tests/Auth/` covers every flow.

Every endpoint requires an authenticated user through the fallback authorization policy; an anonymous endpoint opts out with `[AllowAnonymous]` on the action. Rate limiting partitions by user ID, or by client IP when anonymous: a global limit on everything, `auth` on login, register, refresh and password reset (plus a per-email limit), `write` on POST/PATCH/DELETE.

## Adding a feature (today's conventions)

1. Command (changes state) or query (reads state)? Create the record in `Application/Commands/{Entity}/` or `Application/Queries/{Entity}/`. Records for requests and DTOs; classes for entities, handlers, validators.
2. A command on an existing resource → implement `IOwned<TEntity>`, mapping `ResourceId` to the ID as the ToDoItem commands do; a new owned entity implements `IOwnedEntity`. A query by ID → filter by `UserId` in its single projected query and throw `NotFoundException` when empty, instead of `IOwned`. Creates a resource → include `UserId`, no `IOwned`. `UserId` always comes from the JWT, never from the body. `OwnedRequestTests` fails if a request carrying an `ItemId` is neither. Void commands implement `IRequest`, not `IRequest<Unit>`.
3. Handler in the same folder, named `{Verb}{Entity}Handler`. Command handlers create entities with their factory (`ToDoItem.Create`), change them only through their methods, and call `SaveChangesAsync(cancellationToken)` once at the end; invariants live in the entity and throw a `DomainException`. Query handlers use `IApplicationDbContext` + LINQ, `AsNoTracking()`, `ProjectTo<TDto>()`, and pass the cancellation token to every EF call. Handler tests send the request through MediatR on `TestDbContextFactory` (SQLite), not mocks.
4. User input → validator in `Application/Validators/{Entity}/` named `{Request}Validator`. Shape and range only; business rules live in the entity or handler. A request with no such rule needs no validator and no comment saying so (`docs/insequens-v1-architecture-and-guidelines.md` 12.2).
5. New request or response shape → record in `Contracts/V1/{Area}/`; a wire enum is a Contracts type with the domain values. New read mapping → `Application/Profiles/`. Never use AutoMapper for writes.
6. Controller action: inject only `IMediator`, return `IActionResult`. POST → `CreatedAtAction` 201. PATCH/DELETE → 204. GET → 200. Lists return `PaginatedResult<T>`, never a bare list. Add `[ProducesResponseType]` for every status. State-changing actions get `[EnableRateLimiting(RateLimitPolicies.Write)]`.
7. New exception type → new catch block in `ExceptionMiddleware`. Resource failures derive from `ResourceException` (required `Id`); entity invariants from `DomainException`.
8. Tests: handler happy path + each error path; validator valid + each invalid field; one HTTP-level test per new endpoint. Name tests `Method_State_Expected`.

## Hard rules

- UTC only, never `DateTime.Now`. Code that stamps or compares times takes `TimeProvider` (registered as `TimeProvider.System`) so tests can use `FakeTimeProvider`. `SourceGuardTests` fails the test run if `DateTime.Now` appears in `src/`.
- No commented-out code, no empty or no-op catch blocks, no `TODO` that should be an issue.
- No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.
- No `System.Net.Mail`; email goes through `IEmailSender`, implemented with MailKit. No `Newtonsoft.Json`.
- No concrete-class injection; depend on interfaces. Controllers inject only `IMediator`.
- No repository layer: handlers use `IApplicationDbContext`. Entity configuration lives in `Infrastructure/Persistence/Configurations/` as `IEntityTypeConfiguration<T>`.
- Settings are bound to an options record and validated at startup (`ValidateDataAnnotations().ValidateOnStart()`); never read `IConfiguration[...]` outside `Program.cs`.
- No secrets, IP addresses, usernames or hostnames other than `localhost` in committed configuration. Development defaults may point at `localhost`; everything else comes from User Secrets or environment variables.
- File-scoped namespaces; one public type per file; `_camelCase` private fields.
- Structured logging with named placeholders; never log passwords, tokens or full email addresses.
- Entities: inherit `AuditableEntity`, `Guid` keys, `Guid UserId` on user data (`IOwnedEntity`), private setters with a static factory and intention-revealing methods, a private parameterless constructor for EF, Fluent API configuration only.

## Working an `[INS-xxx]` issue

- Branch `feat/INS-010-short-slug` (or `chore/`, `fix/`, `test/`, `docs/`). PR title `[INS-010] <issue title>`. Body ends with `Closes #<n>`.
- The issue's acceptance criteria are the definition of done. Meet every one, by a test where testable. If a criterion cannot be met, say which and why in the PR instead of silently narrowing the scope.
- Check the issue's **Depends on** list first. If a dependency is open and the work truly needs it, stop and say so rather than re-implementing it.
- Record design decisions as an ADR in `docs/adr/` (INS-101 creates the folder; until then, a short "Decision" section in the PR body).
- Migrations are generated with `dotnet ef migrations add`, never hand-written. Call out any `DropColumn` or type change in the PR.
- Never weaken a guardrail to get green: no skipped tests, no suppressed warnings without a justification comment.
- Breaking API changes go into v2 routes (INS-036). v1 behaviour is frozen except for bugs and security fixes.

## Working the backlog

Take open issues oldest first, one at a time. For each: code it, open a PR, and request a CodeRabbit review (comment `@coderabbitai review`) if one is not posted automatically. Address every review comment and CI check, then merge the PR when everything is green. Only then start the next issue.

## Before you push

```
dotnet build -warnaserror      # warnings are treated as errors once INS-060 lands; keep it clean now
dotnet test
git diff --stat                # confirm only the files the issue needs changed
```

State plainly in the PR what you ran and what you could not run (for example, the .NET SDK or Docker being unavailable in the session). Do not report "tests pass" from reading code.

## Where things are documented

- `docs/insequens-v2-enterprise-architecture-assessment.md` — assessment, target architecture, decisions (Section 7.3), full backlog with acceptance criteria.
- `docs/configuration.md` — every setting, its environment variable, validation and per-environment values.
- `docs/insequens-v1-architecture-and-guidelines.md` — detailed v1 conventions. Where it describes a target state (MailKit, UTC), check the code; INS-100 reconciles it.
- `AGENTS.md` — review checklist used by CodeRabbit and other agents. Same rules as above in checklist form.
- `README.md` — setup and endpoint list.
