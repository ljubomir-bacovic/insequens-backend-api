# Insequens Backend — Enterprise Architecture Assessment & v2 Transformation Backlog

**Status:** Draft for review
**Date:** 2026-10-02
**Scope:** `ljubomir-bacovic/insequens-backend-api` at commit `4370b49` (branch `master`, after PR #73)
**Audience:** Repository owner, contributors, and AI coding agents (Claude Code, Copilot) that will work the backlog in Section 8.

> **How to use this document.** Sections 1–6 are the assessment: what exists, what is good, what is wrong, and why it matters. Section 7 is the target architecture and the decisions behind it. Section 8 is the backlog: every item is written so it can be pasted into a GitHub issue as-is (title, labels, priority, effort, context, files, acceptance criteria, dependencies). Section 9 sequences the backlog into milestones. Section 10 sets the working agreement for agents executing it.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Method and Verification Notes](#2-method-and-verification-notes)
3. [Architecture As Built](#3-architecture-as-built)
4. [What Is Working Well](#4-what-is-working-well)
5. [Assessment by Aspect](#5-assessment-by-aspect)
6. [Scorecard](#6-scorecard)
7. [Target Enterprise Architecture (v2)](#7-target-enterprise-architecture-v2)
8. [Issue Backlog](#8-issue-backlog)
9. [Milestones and Sequencing](#9-milestones-and-sequencing)
10. [Working Agreement for Agents](#10-working-agreement-for-agents)
11. [Appendix A — Documentation Drift Register](#appendix-a--documentation-drift-register)
12. [Appendix B — Mapping to Existing GitHub Issues](#appendix-b--mapping-to-existing-github-issues)

---

## 1. Executive Summary

Insequens is a small, well-intentioned .NET 10 Web API that has completed Phases 1–4 of its v1 modernisation plan: the task (ToDoItem) feature now flows through MediatR with logging, validation and ownership pipeline behaviors, controllers are thin, list endpoints are paginated, and there are 110 tests across two test projects. That is a solid skeleton.

It is not yet an enterprise-grade platform. The gap is not in the CQRS core, which is in decent shape, but in everything around it:

- **Authentication is the weakest part of the system and has zero tests.** `AuthController` still contains every line of auth business logic, injects `UserManager`, `SignInManager`, `IConfiguration` and `IEmailSender` directly, reveals whether an email is registered on four endpoints, performs no lockout, has no rate limiting, stores refresh tokens in plaintext, and turns malformed refresh requests into HTTP 500s.
- **The persistence layer contradicts its own stated design.** A generic repository exists but handlers use EF Core directly through `AsQueryable()`, so the Application project references EF Core anyway. The repository exposes `Clone`, `Find`, sync `SaveChanges`, and an `AddOrUpdate` that only adds. Audit timestamps still use `DateTime.Now`. There are no indexes, no max lengths, no foreign key to users, and no concurrency token.
- **The ownership mechanism is hard-wired to one entity.** `OwnershipBehavior` always loads `ToDoItem`. The first new aggregate (projects, tags, lists) will break authorization unless this is generalised first.
- **Operational maturity is near zero.** No health checks (the warmup endpoint is a stand-in), no request logging, no correlation IDs, no metrics or tracing, no rate limiting, no security headers, no containerisation, and a CI pipeline that still uses Windows VSBuild and almost certainly runs no tests.
- **Documentation has drifted from the code.** README, CLAUDE.md and AGENTS.md describe MailKit, UTC timestamps and single Identity registration as done; none of them are. README and `coderabbit.yaml` link to `docs/architecture.md`, which does not exist.
- **Two dependencies may carry licensing obligations for a proprietary product.** AutoMapper 16.x and FluentAssertions 8.x both moved to commercial licensing for non-open-source use. This needs an explicit decision.

The recommended path is a staged transformation across six milestones (Section 9). Milestone 1 closes out the existing v1 plan and security blockers. Milestone 2 restructures the core so it can host more than one aggregate. Milestones 3–4 add the operational and testing foundations an enterprise system needs. Milestones 5–6 turn the API into a platform: workspaces, collaboration, domain events, background processing, sync for mobile, and integration points.

Total backlog: **63 issues** in 10 epics: 29 small, 25 medium, 7 large, 2 extra-large.

---

## 2. Method and Verification Notes

Every source, test, configuration, pipeline and documentation file in the repository was read in full. Findings cite `path:line` so they can be verified and so agents can navigate directly.

**What could not be verified in this session:** the `dotnet` SDK is not installed in the review container, so the solution was not built and tests were not executed. Statements such as "110 tests" are counts of `[Fact]`/`[Theory]` attributes, not run results. Any claim about compiler warnings is based on reading the code (for example, non-nullable string properties with no initialiser under `<Nullable>enable</Nullable>`), not on compiler output. Issue INS-071 adds a CI job so this never has to be inferred again.

**Repository state used:** `master` at `4370b49` (merge of PR #73). Open issues #67–#71 (Phase 5.1–5.5) and open PR #74 (dual Identity registration) were taken into account; Appendix B maps them to this backlog.

---

## 3. Architecture As Built

### 3.1 Projects and dependencies

```
Insequens.Api  ──────────────┬──► Insequens.Application ──► Insequens.Domain
   (controllers, middleware, │
    Program.cs, EmailSender) │
                             └──► Insequens.Infrastructure.DataAccess ──► Insequens.Infrastructure.Data ──► Insequens.Domain
                                     (Repository<T>, DataContext)            (InsequensContext, Identity, Migrations)
```

The dependency rule in CLAUDE.md is honoured at the project-reference level. It is **not** honoured at the namespace level: `InsequensContext` lives in `src/Infrastructure/Insequens.Infrastructure.Data/InsequensContext.cs:8` but declares `namespace Insequens.Domain.Data;`, so Api code that uses it looks like it depends on Domain when it actually depends on Infrastructure (`src/Insequens.Api/Program.cs:2`, `src/Insequens.Api/Controllers/WarmupController.cs:2`).

The Application project references `Microsoft.EntityFrameworkCore` directly (`src/Insequens.Application/Insequens.Application.csproj:15`) because query handlers call `AsNoTracking()`, `CountAsync()`, `ToListAsync()`, `FirstAsync()`.

### 3.2 Request flow (ToDoItem)

```
HTTP ─► [Authorize(JwtBearer)] ToDoItemController
     ─► TryGetUserId() from ClaimTypes.NameIdentifier
     ─► IMediator.Send(command|query)
         ─► LoggingBehavior        (all requests)
         ─► ValidationBehavior     (if validators registered)
         ─► OwnershipBehavior      (if request : IOwned; loads ToDoItem via IRepository.FindAsync)
         ─► Handler                (IDataContext.GetRepository<ToDoItem>() ...)
     ─► IActionResult (200/201/204)
Exceptions ─► ExceptionMiddleware ─► ProblemDetails (404/403/400/500)
```

### 3.3 Request flow (Auth)

```
HTTP ─► AuthController (no MediatR)
     ─► UserManager / SignInManager / IConfiguration / IEmailSender directly
     ─► inline JWT generation, refresh-token rotation, email sending
```

Two architectures coexist. The second one is the one with no tests.

### 3.4 Inventory

| Area | Count | Notes |
|---|---|---|
| Entities | 1 (`ToDoItem`) + `ApplicationUser` (Identity) | No relationships, no FK between them |
| Commands | 7 | Create, Delete, ToggleComplete, UpdateName/Description/DueDate/Priority |
| Queries | 2 | GetToDoItem, GetUserToDoItems |
| Validators | 4 | Create, UpdateName, UpdatePriority, GetUserToDoItems |
| Pipeline behaviors | 3 | Logging, Validation, Ownership |
| Controllers | 3 | ToDoItem (MediatR), Auth (legacy), Warmup (direct DbContext) |
| Migrations | 3 | Snapshot still says EF `ProductVersion 9.0.0` |
| Tests | 110 attributes | 66 Application.Tests, 44 Api.Tests; 0 for AuthController, EmailSender, Repository, DataContext |
| CI | 1 Azure Pipeline | Windows, VSBuild/NuGet tasks, `master` only |

---

## 4. What Is Working Well

These should be preserved through the transformation.

- **CQRS shape is right.** One record per command/query, one handler each, co-located, named consistently. Commands return `Unit` or a DTO. Queries project with `ProjectTo` and `AsNoTracking`. Pagination is a real `PaginatedResult<T>` with metadata (`src/Insequens.Application/Models/PaginatedResult.cs`).
- **Pipeline behaviors are small, correct and tested.** `ValidationBehavior` short-circuits when there are no validators; `OwnershipBehavior` distinguishes 404 from 403; `LoggingBehavior` uses structured logging. Behavior order is asserted in `tests/Insequens.Api.Tests/ProgramStartupTests.cs:58`.
- **The ToDoItem controller is genuinely thin** (`src/Insequens.Api/Controllers/ToDoItemController.cs`): one dependency, claim extraction, `Send`, status code. Cancellation tokens are forwarded. `ProducesResponseType` is declared on every action.
- **CORS is handled sensibly** (`src/Insequens.Api/Program.cs:31-69`): fail-fast outside Development if no origins are configured; explicit development fallback.
- **The error contract is ProblemDetails** with grouped validation errors under `extensions.errors` (`src/Insequens.Api/ExceptionMiddleware.cs:85-109`) and no stack traces outside Development.
- **Governance tooling exists.** `AGENTS.md`, `CLAUDE.md` and `coderabbit.yaml` encode the rules. That is unusual for a project this size and is the main reason the ToDoItem slice is consistent.
- **The v1 plan was executed as written.** Phases 1–4 are demonstrably complete. This backlog builds on that plan rather than replacing it.

---

## 5. Assessment by Aspect

Each subsection states what exists, the specific defects with file references, and the target. Defects carry the ID of the backlog issue that resolves them.

### 5.1 Solution structure and layering

**What exists.** Four source projects plus two test projects. Infrastructure is split into `Data` (DbContext, Identity, migrations) and `DataAccess` (repository, unit of work) with no benefit; `DataAccess` references `Data`.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | Infrastructure type uses a Domain namespace, hiding the real dependency | `InsequensContext.cs:8` (`namespace Insequens.Domain.Data`) | INS-020 |
| 2 | Two Infrastructure projects where one suffices | `src/Infrastructure/*` | INS-020 |
| 3 | Api references `Microsoft.EntityFrameworkCore.InMemory`, `.SqlServer`, `AutoMapper` directly; InMemory is a test-only provider | `Insequens.Api.csproj:12,20,21` | INS-021 |
| 4 | Api owns `EmailSender` (an infrastructure adapter) | `src/Insequens.Api/EmailSender.cs` | INS-024 |
| 5 | Api uses `InsequensContext` and `ApplicationUser` directly | `WarmupController.cs:11`, `AuthController.cs:24-25` | INS-010, INS-040 |
| 6 | DTOs that are the HTTP contract live in Domain, and several are unused (`ToDoItemUpdateModel`, `ToDoItemUpdateNameModel`, `LoginRequestModel`, `RegisterRequestModel`) | `src/Insequens.Domain/Models/**` | INS-022 |
| 7 | Namespace does not match folder (`Insequens.Domain.Model.ToDoItem` in `Models/`, `Insequens.Domain.Models.RefreshToken` in `Models/Auth/`) | `ToDoItemCreateModel.cs:3`, `RefreshTokenRequestModel.cs:1` | INS-022 |
| 8 | No `Directory.Build.props`, no central package management, no `global.json`, no `.editorconfig`, no analyzers | repo root | INS-060, INS-061 |
| 9 | Custom `Staging` build configuration conflates build config with runtime environment | every `.csproj` `<Configurations>` | INS-061 |
| 10 | Azure Web Deploy leftovers named `journey-api` and a stale `.http` file pointing at `/weatherforecast` | `Properties/ServiceDependencies/**`, `Insequens.Api.http:3` | INS-003 |

**Target.** `Insequens.Domain`, `Insequens.Application`, `Insequens.Infrastructure` (single project), `Insequens.Api`, plus `Insequens.Contracts` for request/response DTOs shared with clients (see 7.2). Build conventions centralised in `Directory.Build.props` and `Directory.Packages.props`.

### 5.2 Domain model

**What exists.** `ToDoItem : AuditableEntity : BaseEntity<Guid>` with public setters for everything. `TaskPriority` enum (`High=1, Medium=2, Low=3`). `BaseEntity.IsNew` and `IRepository.Clone` are unused.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | Anemic entity: no invariants, no behaviour, no factory. Name length is enforced only in a validator and not in the entity or the database | `src/Insequens.Domain/Entities/ToDoItem.cs` | INS-023 |
| 2 | Priority contract is inconsistent: create accepts `int` 0–3 and casts to `TaskPriority?`, so "none" is stored as `(TaskPriority)0`, an undefined member, not `null`; update-priority uses `IsInEnum()` and therefore cannot clear a priority; enum has no `None = 0` | `CreateToDoItemHandler.cs:21`, `CreateToDoItemValidator.cs:14-16`, `UpdateToDoItemPriorityValidator.cs:11`, `TaskPriority.cs` | INS-030 |
| 3 | `ToDoItemGetListModel.DueDate` is non-nullable `DateOnly` but `ToDoItem.DueDate` is `DateOnly?`; AutoMapper projects `null` to `0001-01-01` in list responses | `ToDoItemGetListModel.cs:5`, `ToDoItem.cs:10` | INS-031 |
| 4 | Due date can be set but never cleared (`UpdateToDoItemDueDateCommand.DueDate` is non-nullable) while create accepts `null` | `UpdateToDoItemDueDateCommand.cs:10` | INS-031 |
| 5 | Audit fields have no actor (`CreatedBy`/`UpdatedBy` commented out) | `AuditableEntity.cs:7-8` | INS-023, INS-004 |
| 6 | No concurrency token, no soft delete, no domain events | `ToDoItem.cs` | INS-034, INS-035, INS-080 |
| 7 | Single aggregate; no model for lists/projects, tags, subtasks, recurrence, reminders, sharing | — | Epic 9 |

**Target.** Rich-enough entities with private setters and intention-revealing methods (`Rename`, `Reschedule`, `Complete`, `Reopen`, `ChangePriority`), a `None` priority, nullable due dates end to end, `RowVersion` concurrency, optional soft delete, and a domain-event list on the aggregate root.

### 5.3 Application layer and CQRS pipeline

**What exists.** Three behaviors, seven commands, two queries, four validators, one AutoMapper profile.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | `OwnershipBehavior` is hard-coded to `ToDoItem`; a second owned aggregate cannot be authorised | `OwnershipBehavior.cs:18` | INS-025 |
| 2 | Ownership check and handler each call `FindAsync`; `GetToDoItemHandler` then issues a second projected query, so a GET is two round-trips | `OwnershipBehavior.cs:19`, `GetToDoItemHandler.cs:18-23` | INS-025 |
| 3 | Five handlers rely on `(await FindAsync(...))!` and one throws `ToDoItemNotFoundException`; inconsistent and brittle if behavior order changes | `DeleteToDoItemHandler.cs:14`, `UpdateToDoItemNameHandler.cs:16` | INS-025 |
| 4 | `GetToDoItemHandler` uses `FirstAsync`, which yields HTTP 500 rather than 404 if the behavior is bypassed | `GetToDoItemHandler.cs:23` | INS-025 |
| 5 | `IRepository.FindAsync` does not accept a `CancellationToken`, so behavior and handlers cannot forward it | `IRepository.cs:11` | INS-026 |
| 6 | AutoMapper profile contains write maps (`ToDoItemCreateModel → ToDoItem`, `ToDoItemUpdateModel → ToDoItem`) that the rules forbid and nothing uses | `ToDoItemProfile.cs:12-15` | INS-022 |
| 7 | `ValidationBehavior` runs validators concurrently with `Task.WhenAll`; any future validator that touches the scoped `DbContext` would race | `ValidationBehavior.cs:28-31` | INS-027 |
| 8 | `LoggingBehavior` logs two Information lines per request with no user or correlation context and no slow-request threshold | `LoggingBehavior.cs:17,29` | INS-050 |
| 9 | `ToggleToDoItemComplete` is a non-idempotent PATCH; retries flip state | `ToggleToDoItemCompleteHandler.cs:16` | INS-032 |
| 10 | List query has no sort options, no free-text filter, cannot return "all" (IsCompleted is a required bool), and orders by `DueDate` without a deterministic tiebreaker, so pages can overlap | `GetUserToDoItemsQuery.cs`, `GetUserToDoItemsHandler.cs:23-30` | INS-033 |
| 11 | Serialisable-exception boilerplate with obsolete binary-serialisation constructors; parameterless `ToDoItemNotFoundException()` allows `Id = Guid.Empty` | `Exceptions/*.cs` | INS-027 |
| 12 | Explanatory comments on command records point at document section numbers, which will rot | `DeleteToDoItemCommand.cs:6-8` and three others | INS-027 |

**Target.** A generic `IOwnedResource<TEntity>` / `IOwnershipPolicy` so any aggregate can be owned; handlers that receive the already-loaded aggregate or at least never use `!`; a `Set` style idempotent completion command; a list query with sort, filter, search and stable ordering; validators run sequentially.

### 5.4 Persistence

**What exists.** `InsequensContext : IdentityDbContext<ApplicationUser>` with one `DbSet`, `Repository<T>` over `DbSet<T>`, `DataContext` as unit of work setting audit timestamps.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | `DateTime.Now` for audit timestamps | `DataContext.cs:80` | INS-004 (= #68) |
| 2 | `catch (DbException)` that assigns a local and rethrows: dead code masquerading as handling | `DataContext.cs:28-39, 41-52` | INS-026 |
| 3 | `DataContext` disposes a pooled `DbContext` it does not own | `DataContext.cs:66` | INS-026 |
| 4 | Generic repository leaks EF (`IQueryable`, includes) and carries unused API (`Clone`, sync `Find`, `AddOrUpdate(bool? isNew)` which only adds, `Remove(IEnumerable)`) | `IRepository.cs`, `Repository.cs` | INS-026 (= #69 superset) |
| 5 | No max length on `Name` or `Description` (both `nvarchar(max)`), no index on `(UserId, IsCompleted, DueDate)`, no FK from `ToDoItem.UserId` to `AspNetUsers.Id`, no check constraint on `Priority` | `InsequensContext.cs:18-19`, snapshot | INS-034 |
| 6 | `ToDoItem.UserId` is `Guid` while `IdentityUser.Id` is `string` (`nvarchar(450)`), so a FK is impossible without changing Identity to `IdentityUser<Guid>` | `ApplicationUser.cs:5`, `ToDoItem.cs:7` | INS-041 |
| 7 | No concurrency token; last write wins silently between web and mobile clients | — | INS-034 |
| 8 | No soft delete, no change history; deletion is unrecoverable | — | INS-035 |
| 9 | `dotnet-ef` tool pinned to 9.0.0 with `rollForward: false` while EF Core is 10.0.4; snapshot `ProductVersion` is 9.0.0 | `.config/dotnet-tools.json:6-9`, `InsequensContextModelSnapshot.cs:19` | INS-005 |
| 10 | No migration bundle or startup migration strategy for deployments | — | INS-073 |
| 11 | `AddDbContextPool` combined with `AddIdentity` stores is fine, but pooled contexts with the `required DbSet` property force the awkward `[SetsRequiredMembers]` test subclass | `WarmupControllerTests.cs:57-64` | INS-026 |

**Target.** Replace `IRepository<T>`/`IDataContext` with an `IApplicationDbContext` interface exposing `DbSet<T>` and `SaveChangesAsync`, implemented by `InsequensContext` (the approach used by the Clean Architecture reference templates). Audit via a `SaveChanges` interceptor with `TimeProvider` and `ICurrentUser`. Entity configuration in `IEntityTypeConfiguration<T>` classes. Indexes, lengths, FK, `RowVersion`.

### 5.5 API surface and contracts

**What exists.** Routes under `/v1/[controller]`. Fine-grained PATCH endpoints with primitive JSON bodies. Enums serialised as integers. OpenAPI via `Microsoft.AspNetCore.OpenApi` + Scalar in Development.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | Versioning is a string constant, not a versioning scheme; no way to run v1 and v2 side by side | `Constants.cs:5` | INS-036 |
| 2 | Enums serialised as integers; clients must know `High=1, Low=3` | `Program.cs:120` (no JSON options) | INS-030 |
| 3 | PATCH bodies are bare JSON primitives (`"name"`, `null`), which is awkward for clients and OpenAPI | `ToDoItemController.cs:79,95,111` | INS-032 |
| 4 | `togglecomplete` is non-idempotent | `ToDoItemController.cs:152` | INS-032 |
| 5 | `ProblemDetails.Type` is the literal `"Error"`, not a URI; `Instance` is empty; no `traceId` | `ExceptionMiddleware.cs:41,60,79,102,132` | INS-037 |
| 6 | Auth endpoints return ad-hoc strings and anonymous objects, no `ProducesResponseType`, mix of 400/401/404 for the same condition | `AuthController.cs:44,89,151,190` | INS-010 |
| 7 | OpenAPI security requirement applied to anonymous endpoints too | `JwtBearerSecurityDocumentTransformer.cs:40-56` | INS-036 |
| 8 | No ETag / `If-Match` support, no idempotency keys for POST | — | INS-034, INS-038 |
| 9 | No typed client generation for the React and Expo apps | — | INS-076 |
| 10 | `AllowedHosts: "*"` | `appsettings.json:7` | INS-013 |

**Target.** `Asp.Versioning.Mvc` with URL segment versioning; string enums; a single `PATCH /v1/tasks/{id}` with a partial-update body plus explicit `PUT /{id}/completion`; RFC 9457 ProblemDetails with `type` URIs and `traceId`; OpenAPI published as a build artifact; generated TypeScript client.

### 5.6 Authentication and identity

**What exists.** ASP.NET Core Identity with `IdentityUser` (string key), both `AddIdentity` and `AddIdentityCore` registered (`Program.cs:104-114`, PR #74 in progress), HS256 JWT with 15-minute lifetime, 7-day refresh token stored on the user row, email confirmation and password reset via emailed links.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | Zero tests for any auth behaviour | `tests/**` | INS-011 |
| 2 | User enumeration on register ("Email already registered."), login ("User doesn't exist." vs "Wrong password."), forgot-password and reset-password ("User not found.") | `AuthController.cs:44,89,99,151,168` | INS-010 |
| 3 | `CheckPasswordAsync` bypasses lockout; `AccessFailedCount` never increments; unlimited password guessing | `AuthController.cs:97` | INS-010 |
| 4 | No rate limiting on login, register, forgot-password, refresh | `Program.cs` | INS-012 |
| 5 | Refresh tokens stored in plaintext, one per user (login on a second device logs the first out), no reuse detection, no revocation of a token family | `ApplicationUser.cs:5-6`, `AuthController.cs:105-107` | INS-042 |
| 6 | Malformed or wrongly signed token on `/refresh-token` throws from `ValidateToken` and becomes HTTP 500 | `AuthController.cs:245` | INS-010 |
| 7 | `Jwt:Audience` doubles as the frontend base URL for email links | `AuthController.cs:36` | INS-014 |
| 8 | Empty `Jwt:Key` in config produces an `ArgumentNullException` deep in `Encoding.UTF8.GetBytes` at startup instead of a clear options validation error; key length (≥ 256 bits for HS256) is never validated | `Program.cs:87`, `appsettings.json:12` | INS-014 |
| 9 | No key rotation support (single symmetric key, no `kid`) | — | INS-043 |
| 10 | Roles registered but unused; no admin/support role model | `Program.cs:112` | INS-044 |
| 11 | `RequireConfirmedEmail` is commented out and re-implemented by hand in `Login` | `Program.cs:95-99`, `AuthController.cs:92` | INS-002 (= #67) |
| 12 | Email sent inline in the request; SMTP failure after `CreateAsync` leaves a user who can never receive a confirmation link and returns 500 | `AuthController.cs:54-65` | INS-024, INS-081 |
| 13 | Password-reset link not HTML-encoded; `Uri.EscapeDataString(user.Email)` when `Email` is nullable | `AuthController.cs:156-158` | INS-010 |
| 14 | No account deletion / data export (GDPR), no change-email flow, no session listing | — | INS-045 |
| 15 | `JwtSecurityTokenHandler` (legacy) used instead of `JsonWebTokenHandler` | `AuthController.cs:220,242` | INS-040 |

**Target.** Auth moved into Application as commands (`RegisterUserCommand`, `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`, `ConfirmEmailCommand`) behind `IIdentityService` and `ITokenService` abstractions in Domain/Application; `IdentityUser<Guid>`; hashed, per-device refresh tokens in their own table with family revocation; lockout and rate limiting; options validated at startup; full integration test suite.

### 5.7 Authorization

**What exists.** `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` on the ToDoItem controller; `IOwned` + `OwnershipBehavior`.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | No fallback authorization policy; any new controller is anonymous by default | `Program.cs` (no `FallbackPolicy`) | INS-013 |
| 2 | 403 for another user's item reveals that the item exists (ID enumeration). Enterprise APIs usually return 404 for both cases | `OwnershipBehavior.cs:22-25` | INS-025 (decision recorded in 7.4) |
| 3 | Ownership is user-to-item only; no concept of membership, roles within a workspace, or shared lists | — | INS-090, INS-091 |
| 4 | No policy-based authorization (`IAuthorizationHandler`) for future admin endpoints | — | INS-044 |

**Target.** `FallbackPolicy = RequireAuthenticatedUser`; resource-based authorization through an `IAuthorizationService`-style abstraction in Application that can evaluate ownership or membership; 404 for non-owned resources unless the product explicitly wants 403.

### 5.8 Security hardening

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | No rate limiting middleware | `Program.cs` | INS-012 |
| 2 | No security headers (`HSTS`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`); `UseHsts()` absent | `Program.cs:145` | INS-013 |
| 3 | Request body size limits are defaults; `Description` is unbounded | — | INS-013, INS-034 |
| 4 | Committed config carries a production IP as JWT issuer, SMTP host and username, and a developer machine name in the connection string | `appsettings.json:9,13,22-24`, `appsettings.Staging.json` | INS-006 |
| 5 | No dependency vulnerability scanning, no secret scanning in CI | — | INS-072 |
| 6 | `ExceptionMiddleware` registered after routing, CORS and Scalar, so failures in those layers are unhandled | `Program.cs:147-176` | INS-037 |
| 7 | No audit log of security events (login success/failure, password reset, token refresh) | — | INS-051 |

### 5.9 Error handling

**What exists.** Custom `ExceptionMiddleware` with five catch blocks that each build and serialise a `ProblemDetails`.

**Defects.** Dead branch for `System.ComponentModel.DataAnnotations.ValidationException` (never thrown anywhere) at `ExceptionMiddleware.cs:66`; five copies of the serialise-and-write code; non-URI `type`; no `traceId`; registered too late in the pipeline. See INS-037.

**Target.** Built-in `AddProblemDetails()` + `UseExceptionHandler()` with one `IExceptionHandler` per exception family, `CustomizeProblemDetails` adding `traceId` and `instance`, registered first in the pipeline.

### 5.10 Validation

**What exists.** FluentValidation for four requests.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | No validators for any auth request | — | INS-010 |
| 2 | `Description` has no max length anywhere | `CreateToDoItemValidator.cs` | INS-031 |
| 3 | `DueDate` has no sanity range | — | INS-031 |
| 4 | Validators only on commands; request body DTOs bound by MVC are unvalidated, so the `[ApiController]` auto-400 never fires and all validation happens after the controller | design | acceptable, document in INS-027 |

### 5.11 Configuration and secrets

**What exists.** `IConfiguration` string indexers throughout; `Jwt:Key` and `Email:Password` empty in committed JSON (correct); `AddJsonFile` re-registered manually.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | No options pattern, no startup validation; magic strings (`"Jwt:Key"`) in four files | `Program.cs:87-91`, `AuthController.cs:210-238`, `EmailSender.cs:21-33` | INS-014 |
| 2 | `SetBasePath(Directory.GetCurrentDirectory())` and manual `AddJsonFile` duplicate the default host configuration and force every integration test to call `Directory.SetCurrentDirectory(AppContext.BaseDirectory)` | `Program.cs:21-25`, `ProgramStartupTests.cs:74`, `GetToDoItemEndpointTests.cs:61` | INS-014 |
| 3 | `AddSingleton<IConfiguration>` is redundant | `Program.cs:116` | INS-003 |
| 4 | `Console.WriteLine` for startup diagnostics | `Program.cs:27,48` | INS-050 |
| 5 | No documented environment matrix (Development, Staging, Production) or `.env.example` | — | INS-006, INS-100 |

### 5.12 Logging and observability

**What exists.** Serilog with console and daily file sinks configured in code; `LoggingBehavior`.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | No `UseSerilogRequestLogging`, so there is no per-request line with status and duration | `Program.cs` | INS-050 |
| 2 | Serilog ignores the `Logging` section (no `ReadFrom.Configuration`), so levels cannot be changed per environment | `Program.cs:134-139` | INS-050 |
| 3 | File sink writes to a relative `Logs/` folder; unsuitable for containers and lost on redeploy | `Program.cs:136` | INS-050 |
| 4 | No correlation ID, no user ID enrichment, no environment/version enrichment | — | INS-050 |
| 5 | No OpenTelemetry traces or metrics; no exporter | — | INS-052 |
| 6 | No health checks; `GET /warmup` is a hand-rolled liveness probe that opens a DbContext | `WarmupController.cs` | INS-053 |
| 7 | `WarmKeeper` background service pings a hard-coded production URL every 10 seconds and is dead code | `WarmKeeper.cs:20,28` | INS-003 |

### 5.13 Email and background work

**What exists.** `System.Net.Mail.SmtpClient` in the Api project, invoked synchronously inside auth requests.

**Defects.** Deprecated client (`EmailSender.cs:21`); `int.Parse(_configuration["Email:Port"])` on a possibly null value; new client per call; no retry; failures surface as 500 to the user after state has already changed. No queue, no outbox, no scheduler for anything (reminders, digests, cleanup). See INS-024 (= #70), INS-081, INS-082.

### 5.14 Testing

**What exists.** 110 test attributes. Unit tests for handlers, validators, behaviors, profile, exceptions, DI. Reflection-based controller metadata tests. One HTTP-level integration test.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | No tests at all for `AuthController`, the highest-risk code | — | INS-011 |
| 2 | Only one true HTTP integration test (`GetToDoItem` happy path); no 401/403/404/400 HTTP tests, no create/list/delete through HTTP | `GetToDoItemEndpointTests.cs` | INS-070 |
| 3 | `GetUserToDoItemsHandlerTests` lives in `Api.Tests` because it needs InMemory EF and `DataContext`; it is a handler test in the wrong project | `tests/Insequens.Api.Tests/Queries/GetUserToDoItemsHandlerTests.cs` | INS-070 |
| 4 | Three test files each re-implement `WebApplicationFactory<Program>`; no shared fixture, no JWT helper, no seeding helper | `ProgramStartupTests.cs:70`, `GetToDoItemEndpointTests.cs:52` | INS-070 |
| 5 | `WarmupControllerTests` opens a real TCP connection to `127.0.0.1:1` to simulate an unavailable database; slow and environment-dependent | `WarmupControllerTests.cs:51` | INS-053 |
| 6 | EF InMemory provider is not relational: no constraints, no SQL translation, so index/length/FK bugs are invisible | `Insequens.Api.csproj:20` | INS-070 |
| 7 | Reflection tests assert attributes rather than behaviour (16 facts in `ToDoItemControllerTests`) | `ToDoItemControllerTests.cs` | INS-070 (reduce, keep a few) |
| 8 | No architecture tests enforcing the dependency rule or naming conventions | — | INS-062 |
| 9 | No coverage threshold, no mutation testing, no load testing | — | INS-071, INS-074 |
| 10 | No test data builders; entities constructed inline with object initialisers in every test | — | INS-070 |

**Target.** Three test projects: `Domain.Tests` (pure), `Application.Tests` (handlers with SQLite in-memory or a fake `IApplicationDbContext`), `Api.Tests` (Testcontainers SQL Server, shared `ApiFactory` fixture, `TestAuth` helper, full auth and task suites); `Architecture.Tests` with NetArchTest; coverage gate in CI.

### 5.15 Build, CI/CD and DevOps

**What exists.** `azure-pipelines.yml` using `NuGetToolInstaller`, `NuGetCommand`, `VSBuild` and `VSTest` on `windows-latest`, triggered on `master` only.

**Defects.**

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | Pipeline is the legacy .NET Framework template; `VSTest@2` with no `testAssemblyVer2` pattern will likely discover nothing, so tests do not gate merges | `azure-pipelines.yml:18-35` | INS-071 |
| 2 | No PR validation; repository is on GitHub with CodeRabbit but has no GitHub Actions workflow | repo root (no `.github/`) | INS-071 |
| 3 | No Dockerfile, no compose file for SQL Server + API, no container registry publish | — | INS-073 |
| 4 | No deployment pipeline, no environments, no migration step, no smoke test after deploy | — | INS-073 |
| 5 | No dependency update automation (Dependabot/Renovate), no vulnerability scan | — | INS-072 |
| 6 | No release versioning (semantic version, changelog, tags) | — | INS-075 |
| 7 | `AspNetCoreHostingModel=InProcess` is IIS-specific | `Insequens.Api.csproj:8` | INS-061 |

### 5.16 Code quality and tooling

| # | Finding | Evidence | Issue |
|---|---|---|---|
| 1 | Commented-out code in `Program.cs` (four blocks) and `AuditableEntity.cs` | `Program.cs:95-103,122,151-159`, `AuditableEntity.cs:7-8` | INS-003 |
| 2 | Non-nullable string properties without initialisers under `Nullable=enable` (warnings) | `LoginRequestModel.cs`, `RegisterRequestModel.cs`, `ResetPasswordRequestModel.cs`, `RefreshTokenRequestModel.cs` | INS-022 |
| 3 | Block-scoped namespaces in three files despite the file-scoped rule | `AuthController.cs:18`, `WarmupController.cs:5`, `ApplicationUser.cs:3` | INS-003 |
| 4 | `TreatWarningsAsErrors` off; no analyzers (`Microsoft.CodeAnalysis.NetAnalyzers` at `AnalysisLevel=latest-all`, `SonarAnalyzer`, or `StyleCop`) | all `.csproj` | INS-060 |
| 5 | `Newtonsoft.Json` referenced by Domain and unused | `Insequens.Domain.csproj:11` | INS-007 (= #71) |
| 6 | `.gitkeep` files in non-empty folders | `src/Insequens.Application/**/.gitkeep` | INS-003 |
| 7 | Package drift: `Microsoft.OpenApi 2.0.0` pinned separately, `Serilog` core package pinned alongside `Serilog.AspNetCore` | `Insequens.Api.csproj:22-26` | INS-060 |

### 5.17 Documentation and governance

See Appendix A for the full drift register. The headline: three governance documents describe a state the code has not reached, and two of them link to files that do not exist. A reviewer or agent following CLAUDE.md will believe UTC timestamps and MailKit are already in place. INS-100 fixes this and INS-101 adds ADRs so decisions stop living only in a plan document.

### 5.18 Dependencies and licensing

| Package | Version in repo | Consideration | Issue |
|---|---|---|---|
| MediatR | 12.5.0 | Apache 2.0. Versions 13+ are dual-licensed (RPL / commercial). Pin to 12.x or budget for a licence. | INS-063 |
| AutoMapper | 16.1.1 | Versions 15+ are dual-licensed (RPL / commercial) for commercial use. README states the product is proprietary. Either obtain a licence, pin to 14.x, or replace with hand-written projections (recommended; the project has four maps). | INS-063 |
| FluentAssertions | 8.10.0 | Version 8 requires a commercial licence for non-open-source use. Pin to 7.x or switch to Shouldly / AwesomeAssertions. | INS-063 |
| xunit | 2.9.3 | Fine; xunit v3 is available if desired. | — |
| Scalar.AspNetCore | 1.2.66 | Fine. | — |

Licensing facts above should be re-verified against the current licence texts before acting; the issue asks for exactly that.

---

## 6. Scorecard

Grades: **A** enterprise-ready, **B** solid with gaps, **C** functional but needs structural work, **D** significant risk, **F** absent.

| Aspect | Grade | One-line verdict |
|---|---|---|
| CQRS core (ToDoItem slice) | B | Right shape; ownership and persistence abstractions need generalising before a second aggregate |
| Domain model | C | Anemic, single aggregate, contract inconsistencies (priority, due date) |
| Persistence | C | Works, but no indexes, lengths, FK, concurrency; repository abstraction contradicts usage |
| API contract | C | Consistent for tasks; auth endpoints ad hoc; no versioning scheme; integer enums |
| Authentication | D | All logic in a controller, untested, user enumeration, no lockout, plaintext refresh tokens |
| Authorization | C | Ownership works for one entity; no fallback policy; no membership model |
| Security hardening | D | No rate limiting, no headers, no scanning |
| Error handling | B | ProblemDetails everywhere; needs built-in handler, `type` URIs, `traceId`, earlier registration |
| Validation | B | Good where present; absent for auth; no description bound |
| Configuration | C | No options pattern or startup validation |
| Observability | D | Serilog only; no request log, correlation, metrics, traces, health checks |
| Background processing | F | None; email sent inline |
| Testing | C | Good unit coverage of the slice; auth untested; one HTTP test; InMemory provider |
| CI/CD and DevOps | D | Legacy pipeline that likely runs no tests; no containers, no deploy |
| Code quality tooling | D | No analyzers, warnings tolerated, commented code |
| Documentation | C | Extensive but drifted; broken links |
| Platform readiness | F | Single-user, single-entity; no workspaces, sharing, events, sync, integrations |

---

## 7. Target Enterprise Architecture (v2)

### 7.1 Principles

1. **One architecture, not two.** Every HTTP operation, including auth, goes through MediatR and the same behaviors.
2. **Abstractions earn their place.** Keep an abstraction only if more than one implementation exists or tests need it. The generic repository fails this test; `IApplicationDbContext`, `IIdentityService`, `IEmailSender`, `ICurrentUser`, `TimeProvider` pass it.
3. **Authorization is a policy, not a special case.** Ownership, membership and roles are evaluated by one mechanism that any aggregate can opt into.
4. **Everything observable.** Every request has a correlation ID, a structured log line, a trace, and metrics; every dependency has a health check.
5. **Side effects are asynchronous and reliable.** Email, notifications and integrations go through an outbox and a job runner, never inline in a request.
6. **Clients are first-class.** The React and Expo apps consume a generated client from a versioned OpenAPI document; breaking changes are versioned, not slipped in.
7. **The build is the gate.** Analyzers, architecture tests, coverage, vulnerability scan and integration tests run on every PR.

### 7.2 Target solution layout

```
src/
  Insequens.Domain/            Entities, value objects, enums, domain events, domain exceptions,
                               repository-free. No package references except (optionally) MediatR.Contracts.
  Insequens.Application/       Commands, queries, handlers, validators, behaviors, DTO mapping,
                               interfaces (IApplicationDbContext, IIdentityService, ITokenService,
                               IEmailSender, ICurrentUser, IDateTime/TimeProvider, IAuthorizationPolicy).
  Insequens.Contracts/         Request/response records shared with clients; versioned namespaces (V1, V2).
  Insequens.Infrastructure/    EF Core (DbContext, configurations, migrations, interceptors), Identity,
                               tokens, MailKit email, outbox, background jobs, OpenTelemetry wiring.
  Insequens.Api/               Composition root, controllers (or endpoint groups), filters, OpenAPI,
                               health checks, rate limiting, security headers.
tests/
  Insequens.Domain.Tests/
  Insequens.Application.Tests/
  Insequens.Api.Tests/         Testcontainers SQL Server, shared ApiFactory, auth helper, HTTP suites.
  Insequens.Architecture.Tests/ NetArchTest rules for dependency direction and naming.
build/
  Directory.Build.props, Directory.Packages.props, global.json, .editorconfig
.github/workflows/            ci.yml, release.yml, codeql.yml
deploy/                       Dockerfile, docker-compose.yml, migration bundle script
docs/adr/                     Architecture Decision Records
```

### 7.3 Key design decisions (to be recorded as ADRs by INS-101)

| ADR | Decision | Rationale |
|---|---|---|
| 001 | Keep MediatR 12.x and the behavior pipeline | Already in place, tested, Apache-licensed. Revisit only if licensing or performance forces it. |
| 002 | Replace `IRepository<T>`/`IDataContext` with `IApplicationDbContext` | Handlers already depend on EF semantics. The repository adds indirection without isolation and makes tests harder. |
| 003 | Replace AutoMapper with explicit projection expressions | Four maps, one of which has a null-to-default bug; removes a commercial-licence question; `Expression<Func<ToDoItem, Dto>>` statics are testable and visible. |
| 004 | Move auth into Application behind `IIdentityService` and `ITokenService` | Makes auth testable, puts it behind the same validation/logging pipeline, keeps Identity types out of Api. |
| 005 | `IdentityUser<Guid>` | Enables a real FK from owned entities to users and removes the string/Guid parse at every request. |
| 006 | Refresh tokens in a dedicated `RefreshToken` table, hashed, per device, with family revocation | Multi-device support, reuse detection, logout-everywhere. |
| 007 | Non-owned resources return 404, not 403 | Prevents ID enumeration. 403 remains for authenticated-but-insufficient-role cases. |
| 008 | Generic ownership via `IOwnedResource` + `IOwnershipPolicy<TEntity>` | One behavior, any aggregate, membership-aware later. |
| 009 | Built-in ProblemDetails + `IExceptionHandler` | Removes custom middleware; standard `traceId`; RFC 9457. |
| 010 | Serilog + OpenTelemetry (traces, metrics) with OTLP exporter | Vendor-neutral; works with Azure Monitor, Grafana, Seq. |
| 011 | Transactional outbox + domain events + a job runner (Hangfire or Quartz.NET) | Reliable side effects; foundation for reminders, notifications, integrations. |
| 012 | Testcontainers SQL Server for integration tests | Real relational behaviour; InMemory hides schema bugs. |
| 013 | `Asp.Versioning` URL-segment versioning | Side-by-side v1/v2 while mobile clients lag. |
| 014 | Workspace as the tenancy boundary | Enables sharing, roles, future org accounts without a rewrite. |

### 7.4 Target request pipeline

```
Rate limiter ─► Security headers ─► Exception handler (ProblemDetails) ─► HTTPS/HSTS ─► Request logging
 ─► Routing ─► CORS ─► AuthN (JWT) ─► AuthZ (fallback policy) ─► Controller
 ─► IMediator.Send
     ─► LoggingBehavior (Debug; Warning if > threshold)
     ─► ValidationBehavior
     ─► AuthorizationBehavior (ownership / membership / role, generic)
     ─► TransactionBehavior (commands only)
     ─► Handler
     ─► DomainEventDispatch + Outbox write (same transaction)
 ─► Response (ETag where applicable)
Background: OutboxProcessor ─► EmailSender / Push / Webhooks ; Scheduler ─► Reminders, cleanup, digests
```

### 7.5 Target domain model for the platform

```
Workspace (Id, Name, OwnerId, Plan)             ─┬─ WorkspaceMember (WorkspaceId, UserId, Role: Owner|Admin|Member|Viewer)
                                                 ├─ TaskList / Project (Id, WorkspaceId, Name, Color, Archived)
                                                 │     └─ TaskItem (Id, ListId, Title, Description, Priority, DueDate, DueTime?, Completed, CompletedAt,
                                                 │                  AssigneeId?, ParentTaskId?, SortOrder, RowVersion, IsDeleted, Recurrence?)
                                                 │            ├─ TaskTag (TaskId, TagId)      Tag (Id, WorkspaceId, Name, Color)
                                                 │            ├─ Reminder (Id, TaskId, At, Channel, Sent)
                                                 │            ├─ Comment (Id, TaskId, AuthorId, Body)
                                                 │            └─ Attachment (Id, TaskId, BlobKey, Name, Size, ContentType)
                                                 └─ ActivityEntry (Id, WorkspaceId, ActorId, EntityType, EntityId, Action, Payload, At)
User (IdentityUser<Guid>) ─┬─ RefreshToken (Id, UserId, Hash, Family, DeviceName, ExpiresAt, RevokedAt, ReplacedBy)
                           ├─ UserPreference (TimeZone, Locale, NotificationSettings)
                           └─ DeviceToken (push)
OutboxMessage (Id, Type, Payload, OccurredOn, ProcessedOn, Error, Attempts)
```

The existing `ToDoItem` becomes `TaskItem` inside a default personal workspace created at registration. Migration INS-090 moves existing rows without data loss.

---

## 8. Issue Backlog

**Conventions.**
- **ID:** stable reference used in dependencies. Keep it in the GitHub issue title, e.g. `[INS-010] Move authentication into MediatR commands`.
- **Priority:** P0 blocker/security, P1 needed before production hardening is credible, P2 important, P3 nice to have.
- **Effort:** S (≤ ½ day), M (1–2 days), L (3–5 days), XL (> 1 week; should be split into sub-issues when picked up).
- **Labels:** suggested GitHub labels. Create them once: `epic:*`, `type:bug|refactor|feature|chore|security|test|docs|devops`, `priority:P0..P3`, `effort:S..XL`, `good-first-agent-task` where the scope is tight enough for an unattended agent run.
- **Acceptance criteria** are written to be checkable by a reviewer or by CI.

Epics:
- **E1** Close out v1 and hygiene (INS-001 … INS-007)
- **E2** Authentication and security (INS-010 … INS-014, INS-040 … INS-045)
- **E3** Core architecture refactor (INS-020 … INS-027)
- **E4** Domain and API contract (INS-030 … INS-038)
- **E5** Observability (INS-050 … INS-053)
- **E6** Code quality and tooling (INS-060 … INS-063)
- **E7** Testing (INS-070, INS-074)
- **E8** CI/CD and DevOps (INS-071 … INS-073, INS-075, INS-076)
- **E9** Platform capabilities (INS-080 … INS-096)
- **E10** Documentation and governance (INS-100 … INS-102)

---

### Epic E1 — Close out v1 and hygiene

#### INS-001 — Remove dual Identity registration and enable confirmed-email sign-in
- **Labels:** `epic:e1`, `type:bug`, `priority:P0`, `effort:S`, `good-first-agent-task`
- **Maps to:** GitHub #67, PR #74 (in progress). Finish or supersede PR #74.
- **Context:** `src/Insequens.Api/Program.cs:104-114` registers both `AddIdentity<ApplicationUser, IdentityRole>()` and `AddIdentityCore<ApplicationUser>()`. `AddIdentity` also registers cookie schemes and makes them the default, which is why every controller must specify `AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme`. The commented `RequireConfirmedEmail` block at lines 95-103 is re-implemented by hand in `AuthController.Login`.
- **Change:** Keep a single `AddIdentityCore<ApplicationUser>(options => { Password.*; Lockout.*; SignIn.RequireConfirmedEmail = true; User.RequireUniqueEmail = true; }).AddRoles<IdentityRole>().AddEntityFrameworkStores<InsequensContext>().AddDefaultTokenProviders().AddSignInManager()`. Keep `AddAuthentication` defaulting to JWT. Remove the commented block.
- **Acceptance criteria:**
  - Exactly one Identity registration in `Program.cs`.
  - `ProgramStartupTests` gains a test asserting the default authentication scheme is `Bearer`.
  - Login for an unconfirmed user still fails with the same generic message (coordinate with INS-010).
  - All existing tests pass.
- **Depends on:** none.

#### INS-002 — Switch audit timestamps to UTC and inject `TimeProvider`
- **Labels:** `epic:e1`, `type:bug`, `priority:P0`, `effort:S`, `good-first-agent-task`
- **Maps to:** GitHub #68.
- **Context:** `src/Infrastructure/Insequens.Infrastructure.DataAccess/DataContext.cs:80` uses `DateTime.Now`. CLAUDE.md, AGENTS.md and README already claim UTC.
- **Change:** Inject `TimeProvider` (`TimeProvider.System` in production, `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` in tests) into `DataContext`; use `timeProvider.GetUtcNow().UtcDateTime`.
- **Acceptance criteria:**
  - No `DateTime.Now` anywhere in `src/` (add a grep step or analyzer rule, see INS-060).
  - Unit test: `SaveChangesAsync_WhenEntityAdded_SetsCreatedOnAndUpdatedOnFromTimeProvider`.
  - Unit test: `SaveChangesAsync_WhenEntityModified_UpdatesOnlyUpdatedOn`.
- **Depends on:** none. INS-026 will later move this into an interceptor; keep the test.

#### INS-003 — Delete dead code, commented code, stale files and block-scoped namespaces
- **Labels:** `epic:e1`, `type:chore`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Context:** Commented blocks at `Program.cs:95-103,122,151-159` and `AuditableEntity.cs:7-8`; `WarmKeeper.cs` unregistered and pinging a hard-coded production URL every 10 s; `AddHttpClient()` only exists for it; `AddSingleton<IConfiguration>` at `Program.cs:116` redundant; `AddEndpointsApiExplorer()` unnecessary with `AddOpenApi`; `Insequens.Api.http` references `/weatherforecast`; `Properties/ServiceDependencies/journey-api - Web Deploy/*` and `serviceDependencies*.json` are Azure publish leftovers from the previous project name; `.gitkeep` files in non-empty folders; block-scoped namespaces in `AuthController.cs`, `WarmupController.cs`, `ApplicationUser.cs`; unused `IRepository.Clone` and `BaseEntity.IsNew`.
- **Acceptance criteria:**
  - `grep -rn "^\s*//.*;" src` returns no commented-out statements (reviewer judgement for the rest).
  - Files listed above removed; `Insequens.Api.http` either removed or rewritten for the real endpoints.
  - All namespaces file-scoped.
  - Build and tests pass.
- **Depends on:** none.

#### INS-004 — Replace `System.Net.Mail` with MailKit and move `EmailSender` to Infrastructure
- **Labels:** `epic:e1`, `type:refactor`, `priority:P1`, `effort:S`
- **Maps to:** GitHub #70.
- **Context:** `src/Insequens.Api/EmailSender.cs` uses the deprecated `SmtpClient`, constructs a client per call, parses a nullable port with `int.Parse`, and lives in the wrong project.
- **Change:** New `Insequens.Infrastructure/Email/MailKitEmailSender.cs` bound to an `EmailOptions` record (INS-014). HTML body via `BodyBuilder`. Log recipient domain only, never the full address at Information level.
- **Acceptance criteria:**
  - No `System.Net.Mail` reference in the solution.
  - `EmailOptions` validated at startup (host, port 1–65535, from address).
  - Unit test with a fake `ISmtpClient` wrapper or a MailKit `SmtpClient` subclass verifying message construction.
  - `IEmailSender` signature extended to `SendEmailAsync(EmailMessage message, CancellationToken ct)` where `EmailMessage(string To, string Subject, string HtmlBody, string? TextBody)`.
- **Depends on:** INS-014 for options (can be done together).

#### INS-005 — Align EF tooling with EF Core 10
- **Labels:** `epic:e1`, `type:chore`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Context:** `src/Insequens.Api/.config/dotnet-tools.json` pins `dotnet-ef` 9.0.0 with `rollForward: false`; `InsequensContextModelSnapshot.cs:19` reports `ProductVersion 9.0.0`. Running `dotnet ef` with the pinned tool against EF Core 10 packages will fail or warn.
- **Change:** Move `dotnet-tools.json` to the repo root, set `dotnet-ef` to `10.0.x`, regenerate the snapshot with an empty migration if the model is unchanged (or fold into INS-034's migration).
- **Acceptance criteria:** `dotnet tool restore && dotnet ef migrations list` succeeds in CI (INS-071 adds the step).
- **Depends on:** none.

#### INS-006 — Scrub environment-specific values from committed configuration
- **Labels:** `epic:e1`, `type:security`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Context:** `appsettings.json` contains a production IP (`Jwt:Issuer`), SMTP host and username, and a developer machine name (`Server=MSI`). `appsettings.Staging.json` duplicates the production issuer with a local SQL Express connection string.
- **Change:** `appsettings.json` keeps only shape and safe defaults (empty strings, port numbers). Environment files contain localhost values only. Add `docs/configuration.md` with the full environment matrix and the environment-variable names (`Jwt__Key`, etc.). Add a `.env.example` for compose (INS-073).
- **Acceptance criteria:** No IP addresses, hostnames other than `localhost`, or usernames in any committed JSON. Secret scanning (INS-072) passes.
- **Depends on:** none.

#### INS-007 — Remove the unused Newtonsoft.Json reference from Domain
- **Labels:** `epic:e1`, `type:chore`, `priority:P2`, `effort:S`, `good-first-agent-task`
- **Maps to:** GitHub #71.
- **Acceptance criteria:** `Insequens.Domain.csproj` has zero `PackageReference` items; build passes.

---

### Epic E2 — Authentication and security

#### INS-010 — Move authentication into MediatR commands behind `IIdentityService` and `ITokenService`
- **Labels:** `epic:e2`, `type:refactor`, `type:security`, `priority:P0`, `effort:L`
- **Context:** `AuthController.cs` holds all auth logic (256 lines), injects four services, leaks user existence on four endpoints, bypasses lockout, and converts token validation failures into HTTP 500 (`AuthController.cs:245` throws `SecurityTokenException` / `SecurityTokenMalformedException` which no catch block handles).
- **Change:**
  1. Application interfaces: `IIdentityService` (`CreateUserAsync`, `FindByEmailAsync`, `CheckPasswordSignInAsync` with lockout, `GenerateEmailConfirmationTokenAsync`, `ConfirmEmailAsync`, `GeneratePasswordResetTokenAsync`, `ResetPasswordAsync`), `ITokenService` (`CreateAccessToken(UserId, claims)`, `CreateRefreshToken()`, `ValidateExpiredAccessToken(token)` returning a result, not throwing).
  2. Commands + handlers + validators: `RegisterUserCommand`, `ConfirmEmailCommand`, `LoginCommand → AuthTokensResponse`, `RefreshTokenCommand → AuthTokensResponse`, `LogoutCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`.
  3. Infrastructure implementations wrapping `UserManager`/`SignInManager` and `JsonWebTokenHandler`.
  4. `AuthController` reduced to `IMediator` only; `ProducesResponseType` on every action; `[AllowAnonymous]` explicit.
  5. Uniform responses: register, forgot-password and reset-password always return `202 Accepted` with the same body regardless of whether the email exists; login and refresh return `401` with one generic ProblemDetails for every failure reason; lockout returns `423 Locked` or the same `401` (decide and document).
  6. Email links built from a dedicated `Frontend:BaseUrl` option (INS-014), HTML-encoded.
- **Acceptance criteria:**
  - `AuthController` constructor has a single `IMediator` parameter (extend the existing reflection test).
  - Three consecutive failed logins with `Lockout.MaxFailedAccessAttempts = 5` increment `AccessFailedCount`; the sixth returns the same generic 401 and `LockoutEnd` is set (integration test).
  - Malformed token to `/refresh-token` returns 401 ProblemDetails, not 500.
  - Response bodies for register/forgot/reset are byte-identical for existing and non-existing emails (integration test compares them).
  - Validators exist for every auth command (email format, password min length, token non-empty).
  - `Microsoft.AspNetCore.Identity.Data` request types are no longer used; request records live in `Insequens.Contracts` (INS-022) or temporarily in Domain/Models/Auth as records.
- **Depends on:** INS-001, INS-014. INS-011 is the test companion.

#### INS-011 — Full integration test suite for authentication
- **Labels:** `epic:e2`, `type:test`, `priority:P0`, `effort:M`
- **Context:** There are no tests of any kind for authentication.
- **Change:** Using the shared `ApiFactory` (INS-070), add `tests/Insequens.Api.Tests/Auth/`:
  - `Register_WithValidData_Returns202AndSendsConfirmationEmail` (fake `IEmailSender` captures the message; token extracted from the link).
  - `Register_WithExistingEmail_Returns202WithIdenticalBody`.
  - `ConfirmEmail_WithValidToken_EnablesLogin`.
  - `Login_BeforeConfirmation_Returns401Generic`.
  - `Login_WithWrongPassword_Returns401Generic_AndIncrementsAccessFailedCount`.
  - `Login_AfterMaxFailures_IsLockedOut`.
  - `Login_WithValidCredentials_ReturnsAccessAndRefreshTokens_AccessTokenExpiresIn15Minutes`.
  - `Refresh_WithValidPair_RotatesRefreshToken_OldOneRejected`.
  - `Refresh_WithTamperedAccessToken_Returns401`.
  - `Refresh_WithExpiredRefreshToken_Returns401`.
  - `Logout_InvalidatesRefreshToken`.
  - `ForgotPassword_ForUnknownEmail_Returns202_NoEmailSent`.
  - `ResetPassword_WithValidToken_AllowsLoginWithNewPassword`.
  - `ProtectedEndpoint_WithoutToken_Returns401`; `WithExpiredToken_Returns401`.
- **Acceptance criteria:** All listed tests exist and pass in CI; auth code coverage ≥ 90 % line.
- **Depends on:** INS-010, INS-070.

#### INS-012 — Rate limiting for authentication and write endpoints
- **Labels:** `epic:e2`, `type:security`, `priority:P0`, `effort:S`
- **Change:** `AddRateLimiter` with named policies: `auth` (fixed window, e.g. 10 requests/minute per IP + per email for login/forgot/register/refresh), `write` (token bucket per user for POST/PATCH/DELETE), `global` (per IP). Return 429 as ProblemDetails with `Retry-After`. Policies configured through `RateLimitingOptions` (INS-014). Partition key for authenticated requests is the user ID claim; for anonymous it is the client IP taking `X-Forwarded-For` into account only when `ForwardedHeadersOptions` trusts the proxy.
- **Acceptance criteria:**
  - Integration test: 11th login attempt within a minute returns 429 with `Retry-After`.
  - `ForwardedHeaders` middleware configured with `KnownProxies`/`KnownNetworks` from options, not `ForwardedHeaders.All` blindly.
  - Rate-limit rejections are logged at Warning with the partition key hashed.
- **Depends on:** INS-014.

#### INS-013 — Security headers, HSTS, fallback authorization policy, request limits
- **Labels:** `epic:e2`, `type:security`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Change:**
  - `UseHsts()` outside Development; `UseHttpsRedirection()` kept.
  - Middleware adding `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, `X-Frame-Options: DENY`, `Permissions-Policy`, `Cache-Control: no-store` on auth responses. (A small custom middleware; avoid the unmaintained NWebsec.)
  - `AddAuthorization(o => o.FallbackPolicy = RequireAuthenticatedUser)`; mark auth, health and OpenAPI endpoints `[AllowAnonymous]`.
  - `AllowedHosts` set per environment instead of `*`.
  - Kestrel `MaxRequestBodySize` explicitly set (e.g. 1 MB; raise for attachments later).
- **Acceptance criteria:** Integration test asserts the headers on a 200 response; a new controller without `[Authorize]` returns 401 (test with a throwaway endpoint in the test project).
- **Depends on:** INS-001.

#### INS-014 — Options pattern with startup validation for Jwt, Email, Cors, Frontend, RateLimiting
- **Labels:** `epic:e2`, `type:refactor`, `priority:P0`, `effort:M`
- **Context:** `IConfiguration["Jwt:Key"]` and friends appear in `Program.cs`, `AuthController.cs`, `EmailSender.cs`; an empty key crashes inside `Encoding.UTF8.GetBytes`; `Jwt:Audience` is reused as the frontend URL.
- **Change:** `JwtOptions { Issuer, Audience, Key (min 32 chars), AccessTokenLifetime = 15m, RefreshTokenLifetime = 7d }`, `EmailOptions`, `CorsOptions`, `FrontendOptions { BaseUrl }`, `RateLimitingOptions`, each registered with `AddOptions<T>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart()` and, where needed, a `IValidateOptions<T>` for cross-field rules. Remove manual `AddJsonFile`/`SetBasePath` (the default builder already does this), which also removes the `Directory.SetCurrentDirectory` hack from tests.
- **Acceptance criteria:**
  - Startup with empty `Jwt:Key` fails with an `OptionsValidationException` naming the property (test via `WebApplicationFactory` expecting the host build to throw).
  - No `IConfiguration[...]` indexer use outside `Program.cs`.
  - `ProgramStartupTests` and `GetToDoItemEndpointTests` no longer call `Directory.SetCurrentDirectory`.
- **Depends on:** none.

#### INS-040 — Remove Identity types from the Api project and adopt `JsonWebTokenHandler`
- **Labels:** `epic:e2`, `type:refactor`, `priority:P1`, `effort:S`
- **Context:** After INS-010 the only remaining Identity usage in Api should be DI registration. `JwtSecurityTokenHandler` is the legacy handler; `Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler` is the supported one and is what the JWT bearer middleware uses by default in .NET 8+.
- **Acceptance criteria:** `grep -rn "Insequens.Infrastructure.Data.Models" src/Insequens.Api` returns only `Program.cs`; `JwtSecurityTokenHandler` not referenced in `src/`.
- **Depends on:** INS-010.

#### INS-041 — Migrate Identity to `IdentityUser<Guid>` and add a foreign key from owned entities to users
- **Labels:** `epic:e2`, `type:refactor`, `priority:P1`, `effort:M`
- **Context:** `ToDoItem.UserId` is `Guid`; `AspNetUsers.Id` is `nvarchar(450)`. No FK can exist; orphaned tasks are possible; every request parses a string claim into a Guid.
- **Change:** `ApplicationUser : IdentityUser<Guid>`, `IdentityRole<Guid>`, `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`. Migration converts the Identity key columns from `nvarchar(450)` to `uniqueidentifier` (values are already Guid strings; use `TRY_CONVERT` and fail the migration if any row does not convert). Add FK `ToDoItem.UserId → AspNetUsers.Id` with `OnDelete(Cascade)` or `Restrict` (decide with INS-045).
- **Acceptance criteria:**
  - Migration applies on a database seeded with v1 data (integration test using Testcontainers applies all migrations from scratch, inserts a string-keyed user with the pre-migration schema, then applies the new migration).
  - `ICurrentUser.UserId` is `Guid` without parsing failures.
- **Depends on:** INS-010, INS-070 (Testcontainers).

#### INS-042 — Refresh tokens in a dedicated table: hashed, per device, with reuse detection and family revocation
- **Labels:** `epic:e2`, `type:security`, `priority:P1`, `effort:M`
- **Context:** `ApplicationUser.RefreshToken` is a single plaintext column; a second device logs out the first; a stolen database yields usable tokens.
- **Change:** Entity `RefreshToken(Id, UserId, TokenHash (SHA-256), FamilyId, DeviceName?, CreatedAt, ExpiresAt, RevokedAt?, ReplacedByTokenHash?, CreatedByIp?)`. On refresh: look up by hash; if revoked or already replaced, revoke the whole family (reuse detected) and return 401; otherwise rotate. Logout revokes the current family; "logout everywhere" revokes all for the user. Scheduled cleanup of expired rows (INS-082).
- **Acceptance criteria:**
  - Tests: rotation, reuse detection revokes family, two devices refresh independently, logout-everywhere.
  - Migration drops `RefreshToken`/`RefreshTokenExpiryTime` columns from `AspNetUsers` after copying nothing (old tokens are simply invalidated; document in release notes).
- **Depends on:** INS-010, INS-041.

#### INS-043 — Signing key rotation support
- **Labels:** `epic:e2`, `type:security`, `priority:P2`, `effort:M`
- **Change:** Support a list of keys in `JwtOptions.Keys[] { Id, Secret, ActiveFrom }`; sign with the newest active key and include `kid`; validate against all keys that are not expired. Optionally support RS256 with PEM keys for a future external verifier.
- **Acceptance criteria:** Token signed with key A still validates after key B becomes active; after A is removed it does not.
- **Depends on:** INS-014, INS-040.

#### INS-044 — Role and policy foundation (Admin, Support) with resource-based authorization in Application
- **Labels:** `epic:e2`, `type:feature`, `priority:P2`, `effort:M`
- **Change:** Seed `Admin` role; `[Authorize(Policy = "Admin")]` for future admin endpoints; `IAuthorizationPolicy` abstraction in Application used by the generic authorization behavior (INS-025) so a request can require a role, ownership, or workspace membership.
- **Acceptance criteria:** Policy unit tests; an admin-only ping endpoint under `/v1/admin` returns 403 for a normal user and 200 for an admin.
- **Depends on:** INS-025.

#### INS-045 — Account lifecycle: change email, change password, delete account, export data
- **Labels:** `epic:e2`, `type:feature`, `priority:P2`, `effort:M`
- **Change:** Commands `ChangePasswordCommand` (requires current password; revokes all refresh tokens), `RequestEmailChangeCommand` + `ConfirmEmailChangeCommand`, `DeleteAccountCommand` (soft-delete user, anonymise PII, cascade or anonymise tasks after a grace period via a scheduled job), `ExportUserDataQuery` (JSON archive). Needed for GDPR-style obligations.
- **Acceptance criteria:** Integration tests for each; deletion leaves no row with the user's email in any table after the job runs.
- **Depends on:** INS-010, INS-042, INS-082.

---

### Epic E3 — Core architecture refactor

#### INS-020 — Merge the two Infrastructure projects and fix namespaces
- **Labels:** `epic:e3`, `type:refactor`, `priority:P1`, `effort:S`
- **Change:** Single `src/Insequens.Infrastructure/` with folders `Persistence/`, `Identity/`, `Email/`, `Migrations/`. `InsequensContext` moves to `Insequens.Infrastructure.Persistence`. Migrations namespace updated (EF tolerates this; the `[DbContext]` attribute must reference the new type).
- **Acceptance criteria:** Solution has four `src` projects; `grep -rn "namespace Insequens.Domain" src/Insequens.Infrastructure` returns nothing; migrations still apply.
- **Depends on:** none. Do before INS-026 to avoid moving files twice.

#### INS-021 — Trim Api project package references
- **Labels:** `epic:e3`, `type:chore`, `priority:P2`, `effort:S`, `good-first-agent-task`
- **Change:** Remove `Microsoft.EntityFrameworkCore.InMemory`, `.SqlServer`, `AutoMapper`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` from `Insequens.Api.csproj`; they belong to Infrastructure or tests. Keep `Microsoft.EntityFrameworkCore.Design` for migrations (or move tooling to Infrastructure with a design-time factory).
- **Acceptance criteria:** Build passes; Api has no direct EF provider references.
- **Depends on:** INS-020, INS-070 (tests take the InMemory/Testcontainers packages).

#### INS-022 — Introduce `Insequens.Contracts`; delete unused DTOs; fix namespaces; remove write maps
- **Labels:** `epic:e3`, `type:refactor`, `priority:P1`, `effort:M`
- **Context:** DTOs in `Domain/Models` are the HTTP contract but live in the innermost layer; four are unused (`ToDoItemUpdateModel`, `ToDoItemUpdateNameModel`, `LoginRequestModel`, `RegisterRequestModel`); namespaces mismatch folders; the AutoMapper profile has two unused write maps that violate the "no AutoMapper for writes" rule.
- **Change:** New project `Insequens.Contracts` (no dependencies) with `V1/Tasks/*` and `V1/Auth/*` request/response records. Application references Contracts for response types; Api references it for request binding. Delete unused records. Remove write maps.
- **Acceptance criteria:** `src/Insequens.Domain/Models` no longer exists; architecture test (INS-062) asserts Contracts references nothing; all nullable warnings in former auth models gone.
- **Depends on:** none. Coordinate with INS-010 which creates the auth request records.

#### INS-023 — Encapsulate `ToDoItem` behaviour and invariants
- **Labels:** `epic:e3`, `type:refactor`, `priority:P1`, `effort:M`
- **Change:** Private setters; static factory `ToDoItem.Create(userId, name, description, priority, dueDate)`; methods `Rename`, `UpdateDescription`, `ChangePriority`, `Reschedule(DateOnly?)`, `MarkCompleted()`, `Reopen()` returning void and throwing `DomainException` subclasses on invariant violation (name empty or > 200, description > 4000). Handlers call these methods. Add `CreatedBy`/`UpdatedBy` populated from `ICurrentUser` by the audit interceptor (INS-026).
- **Acceptance criteria:**
  - `tests/Insequens.Domain.Tests/ToDoItemTests.cs` covers every method and invariant.
  - No handler assigns entity properties directly (reviewer check; architecture test optional).
  - EF still materialises the entity (private parameterless constructor).
- **Depends on:** INS-002.

#### INS-024 — Move `EmailSender` behind the outbox
- **Labels:** `epic:e3`, `type:refactor`, `priority:P1`, `effort:S`
- **Change:** After INS-081, auth handlers raise `UserRegistered`, `PasswordResetRequested` domain events; an outbox consumer sends the email. Request latency drops and SMTP outages no longer fail registration.
- **Acceptance criteria:** Registration integration test passes with a throwing `IEmailSender`; the outbox row records the failure and the retry succeeds once the sender is fixed.
- **Depends on:** INS-004, INS-081.

#### INS-025 — Generic authorization behavior; single aggregate load; 404 for non-owned resources
- **Labels:** `epic:e3`, `type:refactor`, `type:security`, `priority:P0`, `effort:M`
- **Context:** `OwnershipBehavior.cs:18` loads `ToDoItem` unconditionally; handlers reload and use `!`; `GetToDoItemHandler` queries twice; 403 leaks existence.
- **Change:**
  - `IOwnedResource` becomes `IResourceRequest { Guid ResourceId; }` plus a generic marker `IOwned<TEntity> where TEntity : IOwnedEntity` (entity exposes `OwnerId`). `AuthorizationBehavior<TRequest, TResponse>` resolves `IOwnershipPolicy<TEntity>` which performs a single `AnyAsync(e => e.Id == id && e.OwnerId == userId)`. Not found or not owned → `NotFoundException` (ADR 007). Membership-aware policies plug in later (INS-091).
  - Handlers no longer use `!`; they load with `SingleOrDefaultAsync` and throw `NotFoundException` defensively, or receive the entity through a request-scoped `IResourceContext<TEntity>` populated by the behavior (preferred; one DB hit for commands).
  - `GetToDoItemHandler` becomes a single projected query filtered by `UserId` and throws `NotFoundException` when empty; it drops `IOwned` because the filter is the authorization.
- **Acceptance criteria:**
  - Behavior tests: owned passes; not found → 404; other user's → 404; a second fake entity type works through the same behavior.
  - `grep -rn ")!" src/Insequens.Application/Commands` returns nothing.
  - SQL logging in an integration test shows one query for `PATCH /{id}/name` and one for `GET /{id}`.
  - `ResourceForbiddenException` retained for role failures only; README and AGENTS.md updated (INS-100).
- **Depends on:** INS-026 (can be developed in parallel on the current repository with `AsQueryable()`).

#### INS-026 — Replace the generic repository and `DataContext` with `IApplicationDbContext`, interceptors and entity configurations
- **Labels:** `epic:e3`, `type:refactor`, `priority:P1`, `effort:L`
- **Maps to:** supersedes GitHub #69 (rename `AddOrUpdate`).
- **Context:** `IRepository<T>`/`Repository<T>` leak `IQueryable`, carry unused members, lack cancellation tokens; `DataContext` has pointless catch blocks and disposes a pooled context; Application already references EF Core.
- **Change:**
  - `IApplicationDbContext { DbSet<ToDoItem> ToDoItems { get; } Task<int> SaveChangesAsync(CancellationToken); }` in Application; `InsequensContext` implements it.
  - `AuditableEntityInterceptor : SaveChangesInterceptor` using `TimeProvider` and `ICurrentUser`.
  - `IEntityTypeConfiguration<ToDoItem>` in `Infrastructure/Persistence/Configurations/` applied via `ApplyConfigurationsFromAssembly`.
  - Delete `IRepository`, `Repository`, `IDataContext`, `DataContext`, `IEntity`, `BaseEntity.IsNew`.
  - Update all handlers and tests. Handler unit tests use SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite` with `DataSource=:memory:`) through a `TestDbContextFactory`, which gives real LINQ translation.
- **Acceptance criteria:**
  - No `IRepository`/`IDataContext` in `src/`.
  - Every async EF call receives a `CancellationToken` (analyzer CA2016 enabled by INS-060 enforces this).
  - All handler tests pass on SQLite.
- **Depends on:** INS-020, INS-002.

#### INS-027 — Application layer polish: sequential validators, exception cleanup, comment removal, `Unit` → `void`-style commands
- **Labels:** `epic:e3`, `type:refactor`, `priority:P2`, `effort:S`, `good-first-agent-task`
- **Change:** Run validators sequentially in `ValidationBehavior` (or document why parallel is safe); replace `[Serializable]` boilerplate with plain exception classes deriving from a `DomainException`/`ApplicationException` base with a required `Id`; remove the "No FluentValidation validator:" comments from command files and put the rule in `docs/` once; consider `IRequest` (no response) instead of `IRequest<Unit>` for void commands (MediatR 12 supports both).
- **Acceptance criteria:** Behaviour unchanged; tests updated; no doc-section references in code comments.
- **Depends on:** none.

---

### Epic E4 — Domain and API contract

#### INS-030 — Fix the priority contract: `None = 0`, nullable removed, string enums over the wire
- **Labels:** `epic:e4`, `type:bug`, `priority:P1`, `effort:S`
- **Context:** See 5.2 defect 2. Create stores `(TaskPriority)0`; update cannot clear; wire format is an integer.
- **Change:** `TaskPriority { None = 0, Low = 1, Medium = 2, High = 3 }` (ascending importance; document the reorder as a breaking change under v1 or ship as part of v2 per INS-036). `ToDoItem.Priority` non-nullable with default `None`. Migration: `UPDATE ToDoItem SET Priority = 0 WHERE Priority IS NULL`, then remap old values (1→3, 3→1) in the same migration. `JsonStringEnumConverter` registered globally; validators use `IsInEnum()`.
- **Acceptance criteria:** Round-trip test: create with `"priority": "high"` reads back `"high"`; create without priority reads back `"none"`; list ordering by priority places `High` first.
- **Depends on:** INS-036 if shipped as v2 only.

#### INS-031 — Nullable due date end to end; description max length; due-date range
- **Labels:** `epic:e4`, `type:bug`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Context:** `ToDoItemGetListModel.DueDate` is non-nullable, projecting `null` as `0001-01-01`; `UpdateToDoItemDueDateCommand` cannot clear; `Description` unbounded.
- **Change:** `DateOnly?` in the list model and the update command (body `{ "dueDate": null }` clears); validators: `Description.MaximumLength(4000)`, `DueDate` between `today - 10 years` and `today + 10 years` via `TimeProvider`.
- **Acceptance criteria:** List test with a null due date returns `null`; update to null succeeds; description of 4001 chars returns 400.
- **Depends on:** none.

#### INS-032 — Idempotent completion endpoint and a single partial-update endpoint
- **Labels:** `epic:e4`, `type:feature`, `priority:P1`, `effort:M`
- **Context:** `PATCH /{id}/togglecomplete` flips state on retry; five PATCH endpoints take bare JSON primitives.
- **Change:**
  - `PUT /v1/tasks/{id}/completion` with body `{ "completed": true }` → `SetToDoItemCompletionCommand`. Keep `togglecomplete` in v1 marked `[Obsolete]`/deprecated in OpenAPI until the mobile app migrates.
  - `PATCH /v1/tasks/{id}` with `UpdateToDoItemRequest { string? Name; Optional<string?> Description; TaskPriority? Priority; Optional<DateOnly?> DueDate }` where `Optional<T>` distinguishes "absent" from "null" (small struct + `JsonConverter`), mapped to `UpdateToDoItemCommand`. The entity methods from INS-023 are called only for present fields. Keep the fine-grained v1 endpoints until v2 cut-over.
- **Acceptance criteria:** Tests: PUT completion twice yields the same state; PATCH with `{ "dueDate": null }` clears, with `{}` changes nothing, with `{ "name": "" }` returns 400.
- **Depends on:** INS-023, INS-031, INS-036.

#### INS-033 — List query: filters, sorting, search, stable pagination
- **Labels:** `epic:e4`, `type:feature`, `priority:P1`, `effort:M`
- **Change:** `GetUserToDoItemsQuery(UserId, Completed?: bool?, Priority?: TaskPriority?, DueFrom?, DueTo?, Search?: string, SortBy: enum {DueDate, Priority, CreatedOn, Name}, SortDirection, Page, PageSize)`. Always append `ThenBy(Id)` for stable paging. Nulls last for `DueDate` (`OrderBy(x => x.DueDate == null).ThenBy(x => x.DueDate)`). `Search` uses `EF.Functions.Like` on Name/Description (full-text is INS-095). Validator: `Search` max 100 chars, `SortBy` in enum.
- **Acceptance criteria:** Handler tests on SQLite for each filter and ordering; integration test pages through 25 items with page size 10 and sees each ID exactly once.
- **Depends on:** INS-026, INS-034 (index).

#### INS-034 — Schema hardening: lengths, indexes, FK, concurrency token, check constraints
- **Labels:** `epic:e4`, `type:refactor`, `priority:P1`, `effort:M`
- **Change (in `ToDoItemConfiguration`):** `Name` `HasMaxLength(200).IsRequired()`; `Description` `HasMaxLength(4000)`; index `(UserId, IsCompleted, DueDate)`; index `(UserId, CreatedOn)`; FK to users (INS-041); `RowVersion` `IsRowVersion()`; check constraint `Priority IN (0,1,2,3)`; table renamed to plural `Tasks` if INS-090 is not imminent (otherwise do it there). `If-Match`/`ETag` support: GET returns `ETag: "<base64 rowversion>"`; PATCH/PUT/DELETE honour `If-Match` and return 412 on mismatch; `ConcurrencyConflictException` → 409 when no `If-Match` was supplied and EF detects a conflict.
- **Acceptance criteria:** Migration applies on Testcontainers; inserting a 201-char name fails at the DB level; two concurrent PATCHes with the same `If-Match` produce one 204 and one 412 (integration test).
- **Depends on:** INS-026, INS-041.

#### INS-035 — Soft delete with restore and purge
- **Labels:** `epic:e4`, `type:feature`, `priority:P2`, `effort:M`
- **Change:** `ISoftDeletable { bool IsDeleted; DateTime? DeletedOn; Guid? DeletedBy }`; global query filter; `DELETE` sets the flag; `POST /v1/tasks/{id}/restore`; `GET /v1/tasks?deleted=true` lists trash; scheduled purge after 30 days (INS-082). Index on `IsDeleted` included in the composite indexes.
- **Acceptance criteria:** Deleted item returns 404 on GET, appears in trash, restore returns it; purge job removes rows older than the retention window (test with `FakeTimeProvider`).
- **Depends on:** INS-026, INS-082.

#### INS-036 — API versioning with `Asp.Versioning`, OpenAPI per version, deprecation headers
- **Labels:** `epic:e4`, `type:feature`, `priority:P1`, `effort:M`
- **Change:** `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer`; `[ApiVersion(1.0)]` on existing controllers; route template `v{version:apiVersion}/[controller]`; OpenAPI documents per version (`/openapi/v1.json`, `/openapi/v2.json`); `api-deprecated-versions`/`Sunset` headers for v1 endpoints slated for removal. `JwtBearerSecurityDocumentTransformer` applies security only to operations without `[AllowAnonymous]`. OpenAPI document generation enabled in all environments but UI (Scalar) only in Development/Staging.
- **Acceptance criteria:** `/v1/todoitem` keeps working byte-for-byte; `/v2/tasks` exists (even if it mirrors v1 initially); the OpenAPI document is produced at build time by `Microsoft.Extensions.ApiDescription.Server` and committed or published as an artifact (INS-076).
- **Depends on:** none.

#### INS-037 — Built-in ProblemDetails, `IExceptionHandler`, `traceId`, `type` URIs, early registration
- **Labels:** `epic:e4`, `type:refactor`, `priority:P1`, `effort:S`
- **Context:** Custom middleware with duplicated code, a dead `DataAnnotations.ValidationException` branch, literal `"Error"` types, and late registration.
- **Change:** `AddProblemDetails(o => o.CustomizeProblemDetails = ctx => { ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier; ctx.ProblemDetails.Instance = ctx.HttpContext.Request.Path; })`; `UseExceptionHandler()` first in the pipeline; handlers `NotFoundExceptionHandler`, `ForbiddenExceptionHandler`, `ValidationExceptionHandler`, `ConcurrencyExceptionHandler`, `DomainExceptionHandler`, fallback. `type` values are stable URIs under `https://docs.insequens.com/errors/{code}` (or `urn:insequens:error:{code}`). `UseStatusCodePages()` so framework 401/403/404 also return ProblemDetails.
- **Acceptance criteria:** `ExceptionMiddleware.cs` deleted; existing `ExceptionMiddlewareTests` rewritten as HTTP tests asserting status, `type`, `title`, `traceId`, `errors`; a 401 from the JWT middleware returns `application/problem+json`.
- **Depends on:** none.

#### INS-038 — Idempotency keys for POST
- **Labels:** `epic:e4`, `type:feature`, `priority:P3`, `effort:M`
- **Change:** Optional `Idempotency-Key` header on `POST /tasks`; store `(UserId, Key) → (StatusCode, BodyHash, ResponseBody, ExpiresAt)` for 24 h; replay returns the stored response; mismatch of body returns 422. Needed for offline mobile sync (INS-094).
- **Acceptance criteria:** Two identical POSTs with the same key create one task and return identical bodies.
- **Depends on:** INS-026.

---

### Epic E5 — Observability

#### INS-050 — Serilog from configuration, request logging, enrichment, correlation ID
- **Labels:** `epic:e5`, `type:feature`, `priority:P1`, `effort:S`
- **Change:** `builder.Host.UseSerilog((ctx, services, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).ReadFrom.Services(services).Enrich.FromLogContext().Enrich.WithEnvironmentName().Enrich.WithProperty("Version", assemblyVersion))`; `UseSerilogRequestLogging` with `EnrichDiagnosticContext` adding `UserId` (hashed or raw per policy) and `ClientId`; correlation ID middleware honouring `X-Correlation-ID` or generating one, echoed in the response and pushed to `LogContext`; file sink removed from the default configuration (console JSON in containers; file only in a `Development` override); `Console.WriteLine` removed; `LoggingBehavior` logs at Debug and emits a Warning when a request exceeds a configurable threshold (default 500 ms).
- **Acceptance criteria:** A request without `X-Correlation-ID` gets one back; log lines for that request carry it (test with Serilog `TestCorrelator` or an in-memory sink); `Logging` section changes take effect without code changes.
- **Depends on:** INS-014.

#### INS-051 — Security audit log
- **Labels:** `epic:e5`, `type:security`, `priority:P2`, `effort:S`
- **Change:** `ISecurityAuditLogger` with events `LoginSucceeded`, `LoginFailed`, `Lockout`, `PasswordResetRequested`, `PasswordChanged`, `TokenRefreshed`, `TokenReuseDetected`, `AccountDeleted`, written as structured logs with an `EventType` property (and optionally to an `AuditEvents` table).
- **Acceptance criteria:** Each auth command handler emits the matching event (unit tests with a fake logger).
- **Depends on:** INS-010.

#### INS-052 — OpenTelemetry traces and metrics with OTLP exporter
- **Labels:** `epic:e5`, `type:feature`, `priority:P1`, `effort:M`
- **Change:** `OpenTelemetry.Extensions.Hosting` + instrumentation for ASP.NET Core, HttpClient, EF Core (`OpenTelemetry.Instrumentation.EntityFrameworkCore`), runtime metrics; custom `ActivitySource("Insequens")` in `LoggingBehavior` so each MediatR request is a span; custom meters (`insequens.tasks.created`, `insequens.auth.login.failed`); OTLP exporter configured through `OpenTelemetry:*` options; Serilog `TraceId`/`SpanId` enrichment. Local stack via compose (INS-073): Grafana + Tempo + Prometheus + Loki or Seq/Jaeger.
- **Acceptance criteria:** Integration test asserts an `Activity` named `CreateToDoItemCommand` is produced (use an in-process `ActivityListener`); `docker compose up` shows traces in the local UI.
- **Depends on:** INS-050.

#### INS-053 — Health checks replacing the warmup controller
- **Labels:** `epic:e5`, `type:feature`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Change:** `AddHealthChecks().AddDbContextCheck<InsequensContext>().AddCheck<SmtpHealthCheck>("smtp", tags: ["ready"])`; `/health/live` (no checks) and `/health/ready` (tagged); JSON writer with per-check status; `[AllowAnonymous]`; delete `WarmupController` and its tests (replace with a readiness integration test).
- **Acceptance criteria:** `/health/ready` returns 503 with the DB stopped (Testcontainers pause); `/health/live` stays 200.
- **Depends on:** none.

---

### Epic E6 — Code quality and tooling

#### INS-060 — `Directory.Build.props`, analyzers, warnings as errors, `.editorconfig`, format check
- **Labels:** `epic:e6`, `type:chore`, `priority:P1`, `effort:S`
- **Change:** `Directory.Build.props` with `TargetFramework`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `Deterministic`, `ContinuousIntegrationBuild` in CI; `.editorconfig` encoding the repository's existing style (file-scoped namespaces as error, `_camelCase` fields, `var` preferences) plus rules for the hard rules in CLAUDE.md where an analyzer exists (CA2016 cancellation token forwarding, CA1848 logger message delegates as suggestion not error, banned API `DateTime.Now` via `Microsoft.CodeAnalysis.BannedApiAnalyzers` with `BannedSymbols.txt` listing `DateTime.Now`, `System.Net.Mail.SmtpClient`, `Newtonsoft.Json`, `Task.Result`, `Task.Wait`); `dotnet format --verify-no-changes` in CI.
- **Acceptance criteria:** Build is warning-free; CI fails on a `DateTime.Now` usage (verify by a deliberately failing PR, then revert).
- **Depends on:** INS-003 (clean-up first so the error list is manageable).

#### INS-061 — Central package management, `global.json`, remove custom build configuration and IIS hosting model
- **Labels:** `epic:e6`, `type:chore`, `priority:P2`, `effort:S`, `good-first-agent-task`
- **Change:** `Directory.Packages.props` with every version in one place; `global.json` pinning the SDK major (`10.0.x`, `rollForward: latestFeature`); remove `<Configurations>Debug;Release;Staging</Configurations>` from all projects (Staging is a runtime environment, not a build flavour); remove `AspNetCoreHostingModel`.
- **Acceptance criteria:** `dotnet build -c Release` is the only configuration used by CI; no `Version=` attributes in `.csproj` files.
- **Depends on:** none.

#### INS-062 — Architecture tests
- **Labels:** `epic:e6`, `type:test`, `priority:P1`, `effort:S`
- **Change:** `tests/Insequens.Architecture.Tests` with NetArchTest.Rules (or ArchUnitNET): Domain depends on nothing in the solution; Application depends only on Domain and Contracts; Infrastructure not referenced by Application; controllers depend only on `IMediator`/`ISender`; every `IRequest` that implements `IResourceRequest` has a handler; every command with user input has a validator (reflection: for each `IRequest` type in `Commands/`, assert an `IValidator<T>` exists unless the type is attributed `[NoValidator("reason")]`); handlers are `sealed`; naming rules (`*Command`, `*Query`, `*Handler`, `*Validator`).
- **Acceptance criteria:** Tests pass; a deliberate violation in a scratch branch fails them.
- **Depends on:** INS-022 (Contracts), INS-026.

#### INS-063 — Dependency licence review: AutoMapper, FluentAssertions, MediatR
- **Labels:** `epic:e6`, `type:chore`, `priority:P0`, `effort:S`
- **Context:** See 5.18. The product is described as proprietary. AutoMapper ≥ 15 and FluentAssertions ≥ 8 are commercially licensed for non-OSS use; MediatR ≥ 13 likewise, while 12.x is Apache.
- **Change:** Verify current licence texts. Recommended outcome: replace AutoMapper with static projection expressions (`ToDoItemProjections.ToDetails`, `ToListItem`) and delete the profile and its tests; replace FluentAssertions with Shouldly (or pin 7.x); pin MediatR to `[12.*,13)` in `Directory.Packages.props` and add a comment. Record the decision in an ADR (INS-101).
- **Acceptance criteria:** No package in the dependency graph requires a commercial licence for the project's use, or a licence is on file and referenced in `docs/licensing.md`.
- **Depends on:** none. Do early; it affects INS-026 and INS-070.

---

### Epic E7 — Testing

#### INS-070 — Test infrastructure overhaul: shared `ApiFactory`, Testcontainers, auth helper, builders, project layout
- **Labels:** `epic:e7`, `type:test`, `priority:P0`, `effort:L`
- **Context:** See 5.14. Three private factories; InMemory provider; handler test in the wrong project; no 4xx HTTP tests; TCP-based "unavailable DB" test.
- **Change:**
  - `tests/Insequens.Api.Tests/Infrastructure/ApiFactory.cs` (`IAsyncLifetime`, `MsSqlContainer` from `Testcontainers.MsSql`, migrations applied once per collection, `Respawn` to reset between tests, fake `IEmailSender` capturing messages, `FakeTimeProvider`, options overrides for JWT).
  - `TestAuth` helper: `CreateUserAsync(email, confirmed: true)`, `AuthenticateAsync(client, userId)` issuing a real JWT with the test key.
  - Builders: `ToDoItemBuilder`, `UserBuilder`.
  - Move `GetUserToDoItemsHandlerTests` to `Application.Tests` on SQLite (INS-026).
  - HTTP suites for tasks: create (201 + Location), get (200/404 other user/404 missing), list (pagination metadata, filters), each PATCH (204/400/404), delete (204/404), unauthenticated (401).
  - Reduce `ToDoItemControllerTests` reflection tests to the two that encode rules (single `IMediator` dependency; class-level `[Authorize]`), the rest are covered by HTTP tests.
  - `coverlet` with `Threshold=80` for Application and Infrastructure; HTML report artifact in CI.
- **Acceptance criteria:** `dotnet test` runs green locally with Docker and in CI; no `UseInMemoryDatabase` in the solution except where explicitly justified; every ToDoItem endpoint has at least happy path + 401 + 404 HTTP tests.
- **Depends on:** INS-014 (config simplification), INS-063 (assertion library decision).

#### INS-074 — Load and soak test baseline
- **Labels:** `epic:e7`, `type:test`, `priority:P3`, `effort:M`
- **Change:** `tests/load/` with k6 (or NBomber) scripts for login, list, create; thresholds (p95 < 200 ms for list at 50 RPS on the CI runner against a compose stack); run nightly, not per PR.
- **Acceptance criteria:** Script committed; nightly workflow publishes a summary; a documented baseline number in `docs/performance.md`.
- **Depends on:** INS-073.

---

### Epic E8 — CI/CD and DevOps

#### INS-071 — GitHub Actions CI: restore, build, format, test with Testcontainers, coverage, migration check
- **Labels:** `epic:e8`, `type:devops`, `priority:P0`, `effort:M`
- **Context:** `azure-pipelines.yml` is the .NET Framework template on Windows; it likely runs no tests; there is no PR validation on GitHub.
- **Change:** `.github/workflows/ci.yml` on `pull_request` and `push` to `master`: `actions/setup-dotnet` from `global.json`; `dotnet restore --locked-mode` (enable `RestorePackagesWithLockFile`); `dotnet format --verify-no-changes`; `dotnet build -c Release --no-restore -warnaserror`; `dotnet test -c Release --no-build --collect:"XPlat Code Coverage"` (Docker is available on `ubuntu-latest` for Testcontainers); `dotnet tool restore && dotnet ef migrations has-pending-model-changes` (EF 9+) to fail if the model and migrations diverge; upload coverage and TRX; branch protection requires the job. Delete `azure-pipelines.yml` or reduce it to a mirror if Azure DevOps is still used for deployment.
- **Acceptance criteria:** A PR that breaks a test is blocked; a PR that changes an entity without a migration is blocked; CI finishes in under 10 minutes.
- **Depends on:** INS-060, INS-061, INS-070.

#### INS-072 — Dependency updates, vulnerability scanning, secret scanning, CodeQL
- **Labels:** `epic:e8`, `type:security`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Change:** `.github/dependabot.yml` (nuget weekly, github-actions weekly, grouped minor/patch); `dotnet list package --vulnerable --include-transitive` step failing on High/Critical; GitHub secret scanning + push protection enabled (repo settings; document); `.github/workflows/codeql.yml` for C#.
- **Acceptance criteria:** Workflows green; Dependabot opens its first PRs.
- **Depends on:** INS-071.

#### INS-073 — Containerisation and local stack: Dockerfile, compose with SQL Server and observability, migration bundle
- **Labels:** `epic:e8`, `type:devops`, `priority:P1`, `effort:M`
- **Change:** Multi-stage `deploy/Dockerfile` (SDK build → `mcr.microsoft.com/dotnet/aspnet:10.0` runtime, non-root user, `HEALTHCHECK` on `/health/live`); `docker-compose.yml` with `api`, `mssql` (healthcheck), `seq` or the Grafana stack, `mailpit` for email; `.env.example`; `dotnet ef migrations bundle` produced in CI and executed as a deploy step (never `Database.Migrate()` at app start in multi-instance deployments); `docs/deployment.md`.
- **Acceptance criteria:** `docker compose up` yields a working API with Scalar at `/scalar/v1`, a registered user receiving mail in Mailpit, and traces in the local UI.
- **Depends on:** INS-053, INS-052 (observability stack optional).

#### INS-075 — Release process: semantic versioning, changelog, tags, image publish
- **Labels:** `epic:e8`, `type:devops`, `priority:P2`, `effort:S`
- **Change:** `MinVer` or `Nerdbank.GitVersioning` for assembly/image versions; `release.yml` on tag `v*` builds, tests, pushes the image to GHCR, attaches the migration bundle and the OpenAPI document; `CHANGELOG.md` maintained via Conventional Commits (`release-please` or `git-cliff`); version exposed at `/health/live` and in logs (INS-050).
- **Acceptance criteria:** Tagging `v2.0.0-alpha.1` produces a GHCR image and a GitHub release with artifacts.
- **Depends on:** INS-071, INS-073.

#### INS-076 — OpenAPI as a build artifact and generated TypeScript client for web and Expo
- **Labels:** `epic:e8`, `type:devops`, `priority:P2`, `effort:M`
- **Change:** `Microsoft.Extensions.ApiDescription.Server` emits `openapi/v1.json` and `v2.json` at build; CI diffs them against the committed copies and fails on uncommitted contract changes (forces conscious API changes); `openapi-typescript` + `openapi-fetch` (or Kiota) generates a client package published to GitHub Packages on release; `oasdiff` breaking-change check against the previous release.
- **Acceptance criteria:** Changing a response record without updating the committed document fails CI; the generated client compiles.
- **Depends on:** INS-036, INS-071.

---

### Epic E9 — Platform capabilities

These issues turn a single-user task API into a platform. Each is sized as a feature slice following the existing CQRS conventions; large ones should be split into sub-issues on pickup.

#### INS-080 — Domain events and in-process dispatch
- **Labels:** `epic:e9`, `type:feature`, `priority:P1`, `effort:M`
- **Change:** `IDomainEvent` and `AggregateRoot` base with `AddDomainEvent`; `DomainEventDispatchInterceptor` collects events before `SaveChanges` and publishes via `IPublisher` after commit (same scope); events `ToDoItemCreated`, `ToDoItemCompleted`, `ToDoItemDeleted`, `UserRegistered`, `PasswordResetRequested`. Handlers for logging/metrics initially.
- **Acceptance criteria:** Unit test: completing an item raises `ToDoItemCompleted` once; integration test: an `INotificationHandler<ToDoItemCompleted>` runs after a successful PATCH and not after a failed one.
- **Depends on:** INS-023, INS-026.

#### INS-081 — Transactional outbox and background processor
- **Labels:** `epic:e9`, `type:feature`, `priority:P1`, `effort:L`
- **Change:** `OutboxMessage` table; interceptor serialises integration events into the outbox in the same transaction as the aggregate change; `OutboxProcessor` hosted service (polling with `SELECT ... WITH (UPDLOCK, READPAST)` or a job via INS-082) dispatches to `IIntegrationEventHandler<T>` with retries, exponential backoff, dead-lettering after N attempts, and idempotent consumers keyed by message ID. First consumers: `SendConfirmationEmailOnUserRegistered`, `SendPasswordResetEmail`.
- **Acceptance criteria:** Tests: message written atomically with the aggregate (rollback removes both); processor delivers exactly once under a simulated crash between send and mark-processed (consumer idempotency); poisoned message lands in dead-letter after 5 attempts; metrics for queue depth and age (INS-052).
- **Depends on:** INS-080, INS-026.

#### INS-082 — Job scheduler (Quartz.NET or Hangfire) for recurring work
- **Labels:** `epic:e9`, `type:feature`, `priority:P1`, `effort:M`
- **Change:** Choose Quartz.NET (Apache, SQL Server job store) unless a dashboard is a hard requirement (Hangfire core is LGPL; the dashboard is fine for internal use). Jobs: purge expired refresh tokens, purge soft-deleted tasks past retention, outbox retry sweeper, reminder dispatcher (INS-093), daily digest (INS-096). Jobs are thin and dispatch MediatR commands. Clustered mode for multi-instance deployments.
- **Acceptance criteria:** Each job has a unit test on its command; an integration test triggers a job manually and asserts the effect; jobs are observable (span per job run).
- **Depends on:** INS-026, INS-052.

#### INS-090 — Workspaces: personal workspace at registration, data migration, workspace-scoped tasks
- **Labels:** `epic:e9`, `type:feature`, `priority:P1`, `effort:XL`
- **Change:** Entities `Workspace`, `WorkspaceMember(Role)`; `TaskItem.WorkspaceId` + `ListId` (default list "Inbox"); migration creates a personal workspace and inbox per existing user and reparents `ToDoItem` rows; `ICurrentWorkspace` resolved from a route segment (`/v2/workspaces/{workspaceId}/tasks`) or a header, validated by membership; v1 endpoints keep working by resolving the personal workspace implicitly. Commands: `CreateWorkspaceCommand`, `RenameWorkspaceCommand`, `ArchiveWorkspaceCommand`. Queries: `GetMyWorkspacesQuery`.
- **Acceptance criteria:** Migration test on v1 data; every existing task is reachable through both v1 and v2 routes; a user cannot see another workspace's tasks (404).
- **Depends on:** INS-025, INS-034, INS-036, INS-041.

#### INS-091 — Membership-aware authorization: invite, roles, resource policies
- **Labels:** `epic:e9`, `type:feature`, `type:security`, `priority:P1`, `effort:L`
- **Change:** `IOwnershipPolicy<TaskItem>` becomes `IResourcePolicy<TaskItem>` checking workspace membership and role (Viewer read-only, Member edit, Admin manage members, Owner delete workspace); `InviteMemberCommand` (email, role) with emailed accept link via outbox; `AcceptInvitationCommand`; `ChangeMemberRoleCommand`; `RemoveMemberCommand`; `LeaveWorkspaceCommand`. Policy tests cover every role × action combination (truth table).
- **Acceptance criteria:** Truth-table test; integration test for invite → accept → read tasks → attempt delete workspace as Member → 403 (role failure, not existence leak).
- **Depends on:** INS-090, INS-044.

#### INS-092 — Lists/projects, tags, subtasks, assignees, manual ordering
- **Labels:** `epic:e9`, `type:feature`, `priority:P2`, `effort:L`
- **Change:** `TaskList` CRUD and archive; `Tag` CRUD per workspace, many-to-many with tasks; `TaskItem.ParentTaskId` with depth limit 2 and completion rules (completing a parent completes children, configurable); `AssigneeId` must be a workspace member; `SortOrder` with a `MoveTaskCommand(afterTaskId)` using fractional indexing or periodic renumbering; list query filters for list, tag, assignee, parent.
- **Acceptance criteria:** Each command has handler + validator + tests; list query tests for new filters; OpenAPI v2 updated and committed (INS-076).
- **Depends on:** INS-090.

#### INS-093 — Reminders and notifications (email, Expo push, web push)
- **Labels:** `epic:e9`, `type:feature`, `priority:P2`, `effort:L`
- **Change:** `Reminder(TaskId, At, Channel)`; `DeviceToken` registration endpoint for Expo push tokens; `NotificationDispatcher` job (INS-082) selects due reminders, enqueues per-channel integration events (outbox), marks sent; `INotificationChannel` implementations for email (MailKit), Expo Push API (`https://exp.host/--/api/v2/push/send`, batching, receipt checking), web push (VAPID); user preferences for quiet hours and time zone (`UserPreference`). Due-date reminders default to 09:00 user-local time on the due date.
- **Acceptance criteria:** Fake channels in tests; reminder fires once even if the job runs twice; preferences respected (test with `FakeTimeProvider` across a DST boundary).
- **Depends on:** INS-081, INS-082, INS-090.

#### INS-094 — Delta sync endpoint for offline mobile clients
- **Labels:** `epic:e9`, `type:feature`, `priority:P2`, `effort:M`
- **Change:** `GET /v2/workspaces/{id}/sync?since=<cursor>` returning created/updated/deleted (tombstones from soft delete) tasks, lists and tags in `UpdatedOn` order with a cursor; `POST /v2/workspaces/{id}/sync` accepting a batch of client operations with idempotency keys (INS-038) and `RowVersion` conflict reporting; documented conflict policy (server wins with client notification, or last-writer-wins by field).
- **Acceptance criteria:** Integration test: create, update, delete on the server; sync from a cursor returns exactly those changes; replaying the batch is a no-op.
- **Depends on:** INS-034, INS-035, INS-038, INS-090.

#### INS-095 — Full-text search and saved filters
- **Labels:** `epic:e9`, `type:feature`, `priority:P3`, `effort:M`
- **Change:** SQL Server full-text index on `Name`, `Description`, comments; `SearchTasksQuery` with ranking; `SavedFilter` entity (name, JSON filter) per user per workspace; `GET /v2/workspaces/{id}/search?q=`.
- **Acceptance criteria:** Full-text migration with a conditional on `FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')` and a `LIKE` fallback; tests on Testcontainers (full-text image variant) or SQLite fallback path.
- **Depends on:** INS-092.

#### INS-096 — Activity log, comments, attachments, webhooks and calendar feed
- **Labels:** `epic:e9`, `type:feature`, `priority:P3`, `effort:XL` (split on pickup)
- **Change:** `ActivityEntry` written from domain events (who did what to which task); `GET /v2/workspaces/{id}/activity`; `Comment` CRUD with mentions raising notifications; `Attachment` upload through pre-signed URLs to blob storage (`Azure.Storage.Blobs` or S3-compatible) with size and type limits, antivirus hook point; outbound `Webhook` subscriptions per workspace with HMAC signatures and retries via outbox; `GET /v2/workspaces/{id}/calendar.ics` token-authenticated feed of tasks with due dates.
- **Acceptance criteria:** Per sub-issue; webhook delivery test with a local receiver and signature verification; attachment upload test with Azurite in compose.
- **Depends on:** INS-081, INS-092.

---

### Epic E10 — Documentation and governance

#### INS-100 — Fix documentation drift; one source of truth per topic
- **Labels:** `epic:e10`, `type:docs`, `priority:P1`, `effort:S`, `good-first-agent-task`
- **Change:** Apply Appendix A. Rename `docs/insequens-v1-architecture-and-guidelines.md` → `docs/architecture.md` and `docs/insequens-v1-modernisation-plan.md` → `docs/v1-modernisation-plan.md` (mark completed phases, point to this document for v2). Fix links in `README.md:34-35,146-147` and `coderabbit.yaml:189`. Remove claims that are not yet true (MailKit, UTC, single Identity registration) or move them to "target state". Make `AGENTS.md` and `CLAUDE.md` reference `docs/architecture.md` instead of duplicating rules, keeping only agent-specific instructions. Add `docs/configuration.md`, `docs/deployment.md`, `docs/testing.md` stubs filled by the owning issues.
- **Acceptance criteria:** A link checker (`lychee` in CI, optional) passes; no statement in README/CLAUDE/AGENTS contradicts the code at merge time.
- **Depends on:** none.

#### INS-101 — Architecture Decision Records
- **Labels:** `epic:e10`, `type:docs`, `priority:P1`, `effort:S`
- **Change:** `docs/adr/` with the MADR template; write ADR 001–014 from Section 7.3; every future backlog item that says "decide" records its decision as an ADR; PR template includes an "ADR needed?" checkbox.
- **Acceptance criteria:** 14 ADRs merged; `.github/pull_request_template.md` exists.
- **Depends on:** none.

#### INS-102 — Issue and PR templates, labels, CODEOWNERS, contributing guide
- **Labels:** `epic:e10`, `type:docs`, `priority:P2`, `effort:S`, `good-first-agent-task`
- **Change:** `.github/ISSUE_TEMPLATE/{bug,feature,tech-debt}.yml` mirroring the backlog format (context, change, acceptance criteria, depends on); PR template with the checklist from `AGENTS.md`; labels script (`gh label create ...`) for the label set in Section 8; `CODEOWNERS`; `CONTRIBUTING.md` with the local setup from INS-073.
- **Acceptance criteria:** New issues created through the template carry the required sections.
- **Depends on:** none.

---

## 9. Milestones and Sequencing

| Milestone | Goal | Issues | Exit criteria |
|---|---|---|---|
| **M1 — Close v1, stop the bleeding** (1–2 weeks) | Finish Phase 5–6 of the v1 plan, remove security blockers that do not require restructuring, make CI real | INS-001, 002, 003, 004, 005, 006, 007, 012, 013, 014, 037, 053, 060, 061, 063, 071, 072, 100, 101, 102 | CI on GitHub blocks merges; options validated at startup; rate limiting and headers live; docs match code; licence decision recorded |
| **M2 — One architecture** (2–3 weeks) | Auth through MediatR with tests; repository replaced; generic authorization; contracts project | INS-010, 011, 020, 021, 022, 023, 025, 026, 027, 040, 041, 042, 070, 062 | `AuthController` has one dependency; auth suite ≥ 90 % coverage; Testcontainers suite green; architecture tests green; no `!` after `FindAsync` |
| **M3 — Contract and data correctness** (1–2 weeks) | Fix the public contract and the schema | INS-030, 031, 032, 033, 034, 036 | v2 routes exist; string enums; nullable due dates; ETag/If-Match; indexes and FK in place |
| **M4 — Operate it** (1–2 weeks) | Observability, containers, release process | INS-050, 051, 052, 073, 075, 076, 043, 044, 045 | `docker compose up` gives a traced, logged, health-checked API; tagged releases publish images and a client |
| **M5 — Platform foundations** (3–4 weeks) | Events, outbox, jobs, workspaces, membership | INS-080, 081, 082, 024, 035, 038, 090, 091 | Email via outbox; personal workspaces migrated; invite flow works; role truth table green |
| **M6 — Platform features** (ongoing) | Product capabilities on the new foundation | INS-092, 093, 094, 095, 096, 074 | Per-issue |

**Critical path:** INS-014 → INS-010 → INS-011 → INS-041 → INS-042 and INS-020 → INS-026 → INS-025 → INS-090 → INS-091. Everything in M1 can run in parallel. Within M2, INS-010/011 and INS-020/026 are independent streams that converge at INS-025.

**Dependency map (abridged):**

```
INS-014 ──► INS-010 ──► INS-011
   │           ├──────► INS-040 ──► INS-043
   │           └──────► INS-041 ──► INS-042 ──► INS-045
   └──► INS-012, INS-050 ──► INS-052 ──► INS-073 ──► INS-075
INS-002 ──► INS-023 ──► INS-080 ──► INS-081 ──► INS-024, INS-093, INS-096
INS-020 ──► INS-026 ──► INS-025 ──► INS-044 ──► INS-091
               ├──────► INS-033, INS-034 ──► INS-094
               ├──────► INS-035, INS-038, INS-082
               └──────► INS-062, INS-070 ──► INS-071 ──► INS-072, INS-076
INS-036 ──► INS-030, INS-032, INS-076
INS-025 + INS-034 + INS-036 + INS-041 ──► INS-090 ──► INS-091, INS-092 ──► INS-095, INS-096
```

---

## 10. Working Agreement for Agents

These rules apply to every issue above when executed by Claude Code or another agent.

1. **One issue, one PR, one branch.** Branch name `feat/INS-010-auth-mediatr` or `chore/INS-003-dead-code`. PR title `[INS-010] Move authentication into MediatR commands`. Reference the issue with `Closes #<n>`.
2. **Read before writing.** Read `CLAUDE.md`, `docs/insequens-v1-architecture-and-guidelines.md` and the ADRs touched by the issue. The code is the fact: where any document contradicts the code, fix the document in the same PR. Where this document and `CLAUDE.md` disagree on a convention, `CLAUDE.md` governs day-to-day work and this document governs target architecture and backlog scope; note the conflict in the PR so INS-100 can reconcile it.
3. **Acceptance criteria are the definition of done.** Every criterion must be demonstrably met, by a test where the criterion is testable. Do not mark an issue done with partial criteria; split the issue instead and say what remains.
4. **Tests accompany code in the same PR.** Handler + validator unit tests, HTTP integration tests for endpoints, architecture tests when a rule changes.
5. **Migrations are generated, never hand-written,** and are reviewed for data-loss operations (`DropColumn`, type changes). Data migrations include a rollback note in the PR.
6. **No new abstractions without two users or a test need.** Cite ADR 002 when tempted.
7. **Breaking API changes go into v2.** v1 behaviour is frozen except for security fixes and bugs (INS-030 and INS-031 are bugs).
8. **Never weaken a guardrail to get green:** no skipped tests, no `TreatWarningsAsErrors=false`, no analyzer suppressions without a justification comment.
9. **Document decisions, not narration.** ADR for a decision; code comment only for a non-obvious "why"; no comments pointing at document section numbers.
10. **Report honestly.** If the SDK, Docker or a service is unavailable, state what was not run.

---

## Appendix A — Documentation Drift Register

| Location | Says | Reality | Fix (INS-100) |
|---|---|---|---|
| `README.md:14` | "MailKit (transactional email)" | `System.Net.Mail.SmtpClient` in `EmailSender.cs:21` | Move to target state until INS-004 |
| `README.md:34-35,146-147` | Links to `docs/architecture.md`, `docs/modernisation-plan.md` | Files are `docs/insequens-v1-architecture-and-guidelines.md`, `docs/insequens-v1-modernisation-plan.md` | Rename files |
| `README.md:84` | Docs at `http://localhost:5000/scalar/v1` | `launchSettings.json` uses ports 5008/7269 | Correct |
| `README.md:60` | `git clone https://github.com/your-org/...` | Placeholder | Correct |
| `CLAUDE.md` Data Access Rules | "DataContext sets audit timestamps automatically with DateTime.UtcNow" | `DateTime.Now` at `DataContext.cs:80` | Fix after INS-002 |
| `CLAUDE.md` Hard Rules | "No System.Net.Mail — use MailKit" | Violated by `EmailSender.cs` | Keep rule; it is a rule, not a claim |
| `AGENTS.md:118` | Same UTC claim | Same | Same |
| `AGENTS.md:123` | "Method `Add()` only adds" | Method is still `AddOrUpdate` | Fix after INS-026 |
| `AGENTS.md:167-171` | "Login/registration errors MUST NOT reveal whether a user account exists" | Violated in `AuthController.cs:44,89,99,151,168` | Keep rule; fix code (INS-010) |
| `coderabbit.yaml:159` | "Only one Identity registration" | Two registrations | Fix after INS-001 |
| `coderabbit.yaml:189` | Knowledge base `**/docs/architecture.md` | File does not exist | Rename |
| `docs/insequens-v1-architecture-and-guidelines.md` §10.5 | Forgot-password always returns success | Returns 400 "User not found." | Fix code (INS-010) |
| `docs/insequens-v1-modernisation-plan.md` §13 | Lists target state as achieved after all phases | Phases 5–8 open | Mark status per phase |
| `src/Insequens.Api/Insequens.Api.http:3` | `GET /weatherforecast/` | No such endpoint | Delete or rewrite |

## Appendix B — Mapping to Existing GitHub Issues

| GitHub | Title | Backlog item | Note |
|---|---|---|---|
| #67 | [Phase 5.1] Remove Dual Identity Registration | INS-001 | PR #74 in progress; INS-001 adds `RequireConfirmedEmail` and a scheme test |
| #68 | [Phase 5.2] Switch Audit Timestamps to UTC | INS-002 | INS-002 adds `TimeProvider` injection and tests |
| #69 | [Phase 5.3] Rename AddOrUpdate to Add | INS-026 | Superseded: the repository is removed. If INS-026 is deferred, do the rename as written in #69 |
| #70 | [Phase 5.4] Replace SmtpClient with MailKit | INS-004 | INS-004 also moves the class to Infrastructure and binds options |
| #71 | [Phase 5.5] Remove Newtonsoft.Json from Domain | INS-007 | Identical |
| — | Phase 6 (cleanup, auth model consolidation, appsettings, WarmKeeper) | INS-003, INS-006, INS-022 | Not yet filed on GitHub |
| — | Phase 7.7 / 7.8 (auth and CRUD integration tests) | INS-011, INS-070 | Not yet filed on GitHub |
| — | Phase 8.1 (pipeline) | INS-071 | Not yet filed; this backlog moves CI to GitHub Actions |
