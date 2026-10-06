# CLAUDE.md

Project context for Claude Code working on the Insequens backend. Keep this file short and true. If a statement here contradicts the code, the code is the fact and this file has a bug: fix the file in the same PR.

## What this is

A .NET 10 task-management Web API using CQRS with MediatR 12 and Clean Architecture. A React web app and an Expo mobile app consume it. The product is proprietary.

The v1 modernisation (Phases 1–4) is complete. The v2 transformation is tracked as GitHub issues `[INS-001]` … `[INS-102]`, generated from `docs/insequens-v2-enterprise-architecture-assessment.md`. Read that document's Section 7 (target architecture) before any structural change.

## Commands

```
dotnet build                                   # whole solution
dotnet test                                    # all tests (xUnit); no Docker needed yet
dotnet test tests/Insequens.Application.Tests  # fast unit tests only
dotnet run --project src/Insequens.Api         # API on http://localhost:5008, Scalar UI at /scalar/v1 in Development
dotnet tool restore                            # once per clone: dotnet-ef pinned in .config/dotnet-tools.json
dotnet ef migrations add <Name> --project src/Infrastructure/Insequens.Infrastructure.Data --startup-project src/Insequens.Api
dotnet ef database update     --project src/Infrastructure/Insequens.Infrastructure.Data --startup-project src/Insequens.Api
```

Local secrets go in User Secrets for `src/Insequens.Api` (`Jwt:Key` ≥ 32 chars; the API does not start without it; anything else that differs on your machine). Committed `appsettings*.json` hold only shape, safe defaults and localhost values; deployed environments use environment variables with `__` nesting (`Jwt__Key`). `docs/configuration.md` is the full matrix.

## Layout and dependency rule

```
src/Insequens.Api                              Controllers, ExceptionMiddleware, Program.cs (DI root), Configuration/ (Cors, AllowedHosts, ReverseProxy options), Security/ (headers, JWT bearer setup), RateLimiting/
src/Insequens.Application                      Commands/, Queries/, Validators/, Behaviors/, Profiles/, Exceptions/, Options/ (FrontendOptions), Models/PaginatedResult
src/Insequens.Domain                           Entities/, Types/ (enums), Models/ (DTO records), DataAccess/ (IRepository, IDataContext), ServiceContracts/ (IEmailSender, IIdentityService, ITokenService)
src/Infrastructure/Insequens.Infrastructure.Data        InsequensContext (IdentityDbContext), ApplicationUser, Migrations/
src/Infrastructure/Insequens.Infrastructure.DataAccess  Repository<T>, DataContext (unit of work, audit timestamps), Email/ (MailKitEmailSender, EmailOptions), Identity/ (IdentityService, TokenService, JwtOptions, signing key ring)
tests/Insequens.Application.Tests              Handler, validator, behavior unit tests (NSubstitute)
tests/Insequens.Infrastructure.Tests           DataContext, email sender, JWT options/key ring/token service unit tests (EF InMemory, FakeTimeProvider), source guard tests
tests/Insequens.Api.Tests                      WebApplicationFactory tests (shared Support/InsequensApiFactory), Auth/ flows, security and rate-limit tests
docs/                                          architecture guidelines, v1 plan, v2 assessment
```

Domain references nothing. Application references only Domain (plus the EF Core package, by accepted exception). Infrastructure references only Domain. Api references Application and Infrastructure. Never add a reference in the other direction; if an inner layer needs an outer type, define an interface in Domain or Application.

Known wart: `InsequensContext` declares `namespace Insequens.Domain.Data` although it lives in Infrastructure. Do not copy that pattern; it is removed by INS-020.

## How a request flows

Controller extracts `UserId` from the `ClaimTypes.NameIdentifier` claim and calls `IMediator.Send`. Three pipeline behaviors run in this order on every request:

1. `LoggingBehavior` — request name and elapsed time.
2. `ValidationBehavior` — FluentValidation, if a validator is registered for the request type.
3. `OwnershipBehavior` — for requests implementing `IOwned` (`UserId`, `ItemId`): loads the `ToDoItem`, throws `ToDoItemNotFoundException` (404) or `ResourceForbiddenException` (403).

`ExceptionMiddleware` maps exceptions to RFC 7807 ProblemDetails. FluentValidation errors become 400 with grouped `errors`.

Auth follows the same flow: `AuthController` injects only `IMediator` and sends the commands in `Application/Commands/Auth/`, whose handlers use `IIdentityService` and `ITokenService` (implemented in Infrastructure). Every failed login or refresh throws `AuthenticationFailedException`, which becomes one generic 401; register, forgot-password and reset-password return the same 202 body whether or not the email has an account. Keep it that way: no auth response may reveal whether an account exists. `tests/Insequens.Api.Tests/Auth/` covers every flow.

Every endpoint requires an authenticated user through the fallback authorization policy; an anonymous endpoint opts out with `[AllowAnonymous]` on the action. Rate limiting partitions by user ID, or by client IP when anonymous: a global limit on everything, `auth` on login, register, refresh and password reset (plus a per-email limit), `write` on POST/PATCH/DELETE.

## Adding a feature (today's conventions)

1. Command (changes state) or query (reads state)? Create the record in `Application/Commands/{Entity}/` or `Application/Queries/{Entity}/`. Records for requests and DTOs; classes for entities, handlers, validators.
2. Accesses an existing resource by ID → implement `IOwned`. Creates a resource → include `UserId`, no `IOwned`. `UserId` always comes from the JWT, never from the body.
3. Handler in the same folder, named `{Verb}{Entity}Handler`. Command handlers construct entities explicitly, use tracked entities, call `SaveChangesAsync(cancellationToken)` once at the end. Query handlers use `AsQueryable()` + LINQ, `AsNoTracking()`, `ProjectTo<TDto>()`, and pass the cancellation token to every EF call.
4. User input → validator in `Application/Validators/{Entity}/` named `{Request}Validator`. Shape and range only; business rules live in handlers.
5. New response shape → record in `Domain/Models/{Entity}/`. New read mapping → `Application/Profiles/ToDoItemProfile`. Never use AutoMapper for writes.
6. Controller action: inject only `IMediator`, return `IActionResult`. POST → `CreatedAtAction` 201. PATCH/DELETE → 204. GET → 200. Lists return `PaginatedResult<T>`, never a bare list. Add `[ProducesResponseType]` for every status. State-changing actions get `[EnableRateLimiting(RateLimitPolicies.Write)]`.
7. New exception type → new catch block in `ExceptionMiddleware`.
8. Tests: handler happy path + each error path; validator valid + each invalid field; one HTTP-level test per new endpoint. Name tests `Method_State_Expected`.

## Hard rules

- UTC only, never `DateTime.Now`. Code that stamps or compares times takes `TimeProvider` (registered as `TimeProvider.System`) so tests can use `FakeTimeProvider`. `SourceGuardTests` fails the test run if `DateTime.Now` appears in `src/`.
- No commented-out code, no empty or no-op catch blocks, no `TODO` that should be an issue.
- No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`.
- No `System.Net.Mail`; email goes through `IEmailSender`, implemented with MailKit. No `Newtonsoft.Json`.
- No concrete-class injection; depend on interfaces. Controllers inject only `IMediator`.
- No query methods on the repository; no `SaveChanges` inside the repository.
- Settings are bound to an options record and validated at startup (`ValidateDataAnnotations().ValidateOnStart()`); never read `IConfiguration[...]` outside `Program.cs`.
- No secrets, IP addresses, usernames or hostnames other than `localhost` in committed configuration. Development defaults may point at `localhost`; everything else comes from User Secrets or environment variables.
- File-scoped namespaces; one public type per file; `_camelCase` private fields.
- Structured logging with named placeholders; never log passwords, tokens or full email addresses.
- Entities: inherit `AuditableEntity`, `Guid` keys, `Guid UserId` on user data, Fluent API configuration only.

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
