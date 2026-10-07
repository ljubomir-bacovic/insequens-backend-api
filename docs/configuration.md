# Configuration

The API reads configuration through the default ASP.NET Core sources. A source lower in this list overrides one above it:

1. `src/Insequens.Api/appsettings.json`: the shape of every setting, with empty values or safe defaults only.
2. `src/Insequens.Api/appsettings.{Environment}.json`: localhost values for that environment, and nothing else.
3. User Secrets (Development only): local secrets and personal overrides.
4. Environment variables: every deployed environment. Nested keys use `__`, so `Jwt:Key` becomes `Jwt__Key`.
5. Command-line arguments.

Committed configuration files never contain secrets, IP addresses, hostnames other than `localhost`, or usernames. Every real value for a deployed environment comes from environment variables.

Each section is bound to an options record and validated when the host starts, so a missing or malformed value stops the API with an `OptionsValidationException` that names the setting, instead of failing on the first request that needs it.

## Settings

| Setting | Environment variable | Required | Development default | Notes |
|---------|----------------------|----------|---------------------|-------|
| `AllowedHosts` | `AllowedHosts` | Outside Development | `localhost` | Semicolon-separated host names the API answers to; other hosts get 400. Startup fails outside Development when it is empty or `*`. |
| `ConnectionStrings:InsequensConnection` | `ConnectionStrings__InsequensConnection` | Yes | Local SQL Express (`.\SQLEXPRESS`) | Staging also uses the local SQL Express instance on its host. |
| `Jwt:Key` | `Jwt__Key` | Unless `Jwt:Keys` is set | none | Secret. At least 32 characters. The single signing key; see [Signing key rotation](#signing-key-rotation) for the alternative. |
| `Jwt:Keys` | `Jwt__Keys__0__Id`, `Jwt__Keys__0__Secret`, `Jwt__Keys__0__ActiveFrom`, … | Unless `Jwt:Key` is set | none | Secrets. Signing keys for rotation. Set either `Jwt:Key` or `Jwt:Keys`, not both. |
| `Jwt:Issuer` | `Jwt__Issuer` | Yes | `https://localhost:7269` | |
| `Jwt:Audience` | `Jwt__Audience` | Yes | `http://localhost:3000` | |
| `Jwt:AccessTokenLifetime` | `Jwt__AccessTokenLifetime` | No | `00:15:00` | 1 minute to 1 day. |
| `Jwt:RefreshTokenLifetime` | `Jwt__RefreshTokenLifetime` | No | `7.00:00:00` | 1 minute to 365 days. |
| `Frontend:BaseUrl` | `Frontend__BaseUrl` | Yes | `http://localhost:3000` | Base URL of the web app. Links in confirmation and password-reset emails point to `{BaseUrl}/confirm-email` and `{BaseUrl}/reset-password`. Must be an absolute URL. |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … | Outside Development | `http://localhost:5173`, `http://localhost:8081` | Absolute `http` or `https` origins. Startup fails outside Development when the list is empty. In Development an empty list falls back to open CORS. |
| `ReverseProxy:KnownProxies` | `ReverseProxy__KnownProxies__0`, … | No | none | IP addresses of proxies whose `X-Forwarded-For` and `X-Forwarded-Proto` headers are trusted. |
| `ReverseProxy:KnownNetworks` | `ReverseProxy__KnownNetworks__0`, … | No | none | CIDR ranges of trusted proxies. With both lists empty the forwarded headers are ignored, and rate limiting sees the proxy's IP. |
| `RateLimiting:Auth:PermitLimit`, `RateLimiting:Auth:Window` | `RateLimiting__Auth__PermitLimit`, `RateLimiting__Auth__Window` | No | `10`, `00:01:00` | Login, register, refresh-token, forgot-password and reset-password: per client and, separately, per email address. |
| `RateLimiting:Write:TokenLimit`, `TokensPerPeriod`, `ReplenishmentPeriod` | `RateLimiting__Write__TokenLimit`, … | No | `60`, `60`, `00:01:00` | Token bucket for POST, PATCH and DELETE, per user. |
| `RateLimiting:Global:PermitLimit`, `RateLimiting:Global:Window` | `RateLimiting__Global__PermitLimit`, `RateLimiting__Global__Window` | No | `300`, `00:01:00` | Every request, per user, or per client IP when anonymous. |
| `AccountDeletion:GracePeriod` | `AccountDeletion__GracePeriod` | No | `30.00:00:00` | 0 to 365 days. How long a deleted account stays recoverable before `PurgeDeletedAccountsCommand` deletes it with all its data. |
| `Email:SmtpServer` | `Email__SmtpServer` | Yes | `localhost` | |
| `Email:Port` | `Email__Port` | Yes | `1025` | 1–65535. Base default `587`. |
| `Email:UseTls` | `Email__UseTls` | No | `false` | Base default `true`. Port 465 connects with implicit TLS; any other port requires STARTTLS. `false` sends in plain text and is for local mail catchers only. Must be `true` when `Username` is set, so credentials never travel unencrypted. |
| `Email:Username` | `Email__Username` | No | none | When empty, the sender skips SMTP authentication. |
| `Email:Password` | `Email__Password` | With `Username` | none | Secret. Required when `Username` is set. |
| `Email:From` | `Email__From` | Yes | `no-reply@localhost` | Must be an email address. |

Rate-limited requests get `429 Too Many Requests` with a `Retry-After` header and a ProblemDetails body. Each rejection is logged at Warning with a hash of the partition key, never the raw IP, user ID or email address.

## Signing key rotation

A single `Jwt:Key` is enough until a key has to change. To rotate without logging everyone out, switch to `Jwt:Keys`:

1. Deploy with the current key and the new one, the new one with an `ActiveFrom` in the future. Every instance now accepts tokens signed with either key.
2. From `ActiveFrom` on, new tokens are signed with the new key and carry its `Id` in the `kid` header. Tokens signed with the old key keep validating.
3. Once the longest-lived old token has expired (`Jwt:AccessTokenLifetime`), remove the old key. Tokens it signed stop validating.

```
Jwt__Keys__0__Id=2026-01
Jwt__Keys__0__Secret=<current secret>
Jwt__Keys__0__ActiveFrom=2026-01-01T00:00:00Z
Jwt__Keys__1__Id=2026-07
Jwt__Keys__1__Secret=<new secret>
Jwt__Keys__1__ActiveFrom=2026-07-01T00:00:00Z
```

When moving from `Jwt:Key` to `Jwt:Keys`, list the old secret as the first key. Tokens it signed have no `kid` and are matched against every configured key. Startup fails if a key has no `Id`, two keys share an `Id`, a secret is shorter than 32 characters, or no key is active yet.

## Roles

Migrations seed two roles, `Admin` and `Support`. No endpoint assigns them; an operator adds a user to a role in the database:

```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT Id, '8d0c6c39-2f4e-4c1a-9a57-3b8f1e2d4a01' FROM AspNetUsers WHERE NormalizedEmail = 'ADMIN@EXAMPLE.COM';
```

The role reaches the access token at the next login or refresh. Removing a role takes effect at once for requests that check it in the Application layer (`[RequiresRole]`).

## Environments

| Environment | Committed values | Must be supplied |
|-------------|------------------|------------------|
| Development | `appsettings.Development.json`: `localhost` as the allowed host, local SQL Express, localhost JWT issuer and audience, localhost web app URL, localhost CORS origins, a local mail catcher on `localhost:1025` | `Jwt:Key` in User Secrets |
| Staging | `appsettings.Staging.json`: local SQL Express | Environment variables for `AllowedHosts`, `Jwt__*`, `Frontend__BaseUrl`, `Cors__AllowedOrigins__*` and `Email__*`, plus `ReverseProxy__*` behind a proxy |
| Production | none beyond `appsettings.json` | Environment variables for every required setting, plus `ReverseProxy__*` behind a proxy |

Outside Development the API also sends `Strict-Transport-Security`.

## Local development

The `src/Insequens.Api` project needs a User Secrets ID before secrets can be stored. `dotnet user-secrets init` adds it.

```
cd src/Insequens.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "a-local-signing-key-of-at-least-32-characters"
```

Emails go to an SMTP server on `localhost:1025` without TLS. Run a local mail catcher such as [smtp4dev](https://github.com/rnwood/smtp4dev) or [Mailpit](https://github.com/axllent/mailpit) to receive them. To send through a real server instead, override `Email:SmtpServer`, `Email:Port`, `Email:UseTls`, `Email:Username`, `Email:Password` and `Email:From` in User Secrets.

Development accepts requests addressed to `localhost` only. To call the API from a phone on your network, add your machine's address to `AllowedHosts` in User Secrets, for example `localhost;my-laptop.local`.

## Containers

`.env.example` at the repository root lists the environment variables for a container deployment. Copy it to `.env`, which Git ignores, and fill in the values.
