# Configuration

The API reads configuration through the default ASP.NET Core sources. A source lower in this list overrides one above it:

1. `src/Insequens.Api/appsettings.json`: the shape of every setting, with empty values or safe defaults only.
2. `src/Insequens.Api/appsettings.{Environment}.json`: localhost values for that environment, and nothing else.
3. User Secrets (Development only): local secrets and personal overrides.
4. Environment variables: every deployed environment. Nested keys use `__`, so `Jwt:Key` becomes `Jwt__Key`.
5. Command-line arguments.

Committed configuration files never contain secrets, IP addresses, hostnames other than `localhost`, or usernames. Every real value for a deployed environment comes from environment variables.

## Settings

| Setting | Environment variable | Required | Development default | Notes |
|---------|----------------------|----------|---------------------|-------|
| `ConnectionStrings:InsequensConnection` | `ConnectionStrings__InsequensConnection` | Yes | Local SQL Express (`.\SQLEXPRESS`) | Staging also uses the local SQL Express instance on its host. |
| `Jwt:Key` | `Jwt__Key` | Yes | none | Secret. At least 32 characters. |
| `Jwt:Issuer` | `Jwt__Issuer` | Yes | `https://localhost:7269` | |
| `Jwt:Audience` | `Jwt__Audience` | Yes | `http://localhost:3000` | Also the base URL of the web app, used for links in confirmation and password-reset emails. |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … | Outside Development | `http://localhost:5173`, `http://localhost:8081` | Startup fails outside Development when the list is empty. In Development an empty list falls back to open CORS. |
| `Email:SmtpServer` | `Email__SmtpServer` | Yes | `localhost` | Validated at startup. |
| `Email:Port` | `Email__Port` | Yes | `1025` | Validated at startup: 1–65535. Base default `587`. |
| `Email:UseTls` | `Email__UseTls` | No | `false` | Base default `true`. Port 465 connects with implicit TLS; any other port requires STARTTLS. `false` sends in plain text and is for local mail catchers only. |
| `Email:Username` | `Email__Username` | No | none | When empty, the sender skips SMTP authentication. |
| `Email:Password` | `Email__Password` | With `Username` | none | Secret. |
| `Email:From` | `Email__From` | Yes | `no-reply@localhost` | Validated at startup: must be an email address. |

`Email` settings are bound to `EmailOptions` and validated when the host starts, so a missing or malformed value stops the API instead of failing on the first email.

## Environments

| Environment | Committed values | Must be supplied |
|-------------|------------------|------------------|
| Development | `appsettings.Development.json`: local SQL Express, localhost JWT issuer and audience, localhost CORS origins, a local mail catcher on `localhost:1025` | `Jwt:Key` in User Secrets |
| Staging | `appsettings.Staging.json`: local SQL Express | Environment variables for `Jwt__*`, `Cors__AllowedOrigins__*` and `Email__*` |
| Production | none beyond `appsettings.json` | Environment variables for every required setting |

## Local development

The `src/Insequens.Api` project needs a User Secrets ID before secrets can be stored. `dotnet user-secrets init` adds it.

```
cd src/Insequens.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "a-local-signing-key-of-at-least-32-characters"
```

Emails go to an SMTP server on `localhost:1025` without TLS. Run a local mail catcher such as [smtp4dev](https://github.com/rnwood/smtp4dev) or [Mailpit](https://github.com/axllent/mailpit) to receive them. To send through a real server instead, override `Email:SmtpServer`, `Email:Port`, `Email:UseTls`, `Email:Username`, `Email:Password` and `Email:From` in User Secrets.

## Containers

`.env.example` at the repository root lists the environment variables for a container deployment. Copy it to `.env`, which Git ignores, and fill in the values.
