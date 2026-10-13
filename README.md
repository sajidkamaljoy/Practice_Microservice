# Practice_Microservice

A hands-on .NET 8 microservices playground: an API gateway, JWT authentication, a product service exposed over REST, GraphQL and gRPC, and an order service that talks to it.

## Architecture

```
Client ──► ApiGateway (Ocelot) ──┬─► AuthWebApi        POST /api/Accounts/Login  → JWT
                                 ├─► Product           REST + GraphQL (SQL Server)
                                 ├─► ProductsWebApi    REST + gRPC product service
                                 └─► OrderService      GET  /api/Order/GetOrderWithProducts
```

| Project | Role |
|---|---|
| `ApiGateway` | Ocelot gateway with route config in `ocelot.json`, JWT bearer protection on selected routes, and Swagger aggregation |
| `AuthWebApi` | Login endpoint that issues JWT access tokens |
| `JwtConfiguration` | Shared token creation and validation helpers |
| `OcelotAuthentication` | Experiment with authentication at the Ocelot layer |
| `Product` | Product API with REST (`GetAllProducts`, `GetProductById`, `CreateProduct`) and GraphQL, using EF Core and SQL Server; also contains RabbitMQ usage |
| `ProductsWebApi` | Product service with REST endpoints and a gRPC service (`ProductService` in `Protos/`), plus a Dockerfile |
| `OrderService` | Order API that calls the product gRPC service to return orders with their products |
| `Core`, `Data`, `Service` | Layered domain, EF Core data access (migrations included) and business logic |

## Gateway routes

| Gateway path | Goes to | Notes |
|---|---|---|
| `/gateway/accounts/{everything}` | AuthWebApi | POST, anonymous |
| `/gateway/products/{everything}` | Product | GET, POST |
| `/gateway/orders/{everything}` | OrderService | GET, requires a Bearer token |
| `/gateway/ProductService/{everything}` | ProductsWebApi | GET, POST |
| `/gateway/graphql/{everything}` | ProductsWebApi | POST |

## Tech stack

.NET 8, ASP.NET Core Web API, Ocelot, JWT bearer auth, gRPC and Protobuf, GraphQL (HotChocolate, GraphQL.NET, EntityGraphQL), Entity Framework Core 8 with SQL Server and an in-memory option, AutoMapper, RabbitMQ client, Swagger, Docker.

## Run it

1. Install the .NET 8 SDK and a SQL Server instance (or LocalDB).
2. Update `ConnectionStrings:DefaultConnection` in the `appsettings.json` of the data-backed project to point at your SQL Server.
3. Apply migrations: `dotnet ef database update` from the project that owns the `AppDbContext`.
4. Configure the JWT settings (see [Configuration](#configuration) below). The services will not start without a signing key.
5. Open `Product.sln` in Visual Studio, set the services you need as startup projects (or run each with `dotnet run`), and start `ApiGateway`.
6. Request a token from `/gateway/accounts/Login`, then pass it as `Authorization: Bearer <token>` on protected routes.

The ports in `ocelot.json` must match the ports each service listens on. Check each project's `Properties/launchSettings.json`, because they may need adjusting on your machine.

## Configuration

Secrets are not stored in the repository. Every service reads them from configuration, so use .NET user-secrets for local development, or environment variables in Docker and production.

| Setting | Used by | Notes |
|---|---|---|
| `Jwt:Key` | `AuthWebApi`, `ApiGateway`, `Product` | Signing key, at least 32 characters. **It must be the same in all three services.** |
| `Jwt:ValidityMinutes` | `AuthWebApi` | Optional. Token lifetime, 20 by default. |
| `Jwt:Users:0:Username`, `Password`, `Role` | `AuthWebApi` | Demo accounts for the login endpoint. Add more with index `1`, `2` and so on. |

Local development with user-secrets (run from the repo root, using your own values):

```bash
for p in AuthWebApi ApiGateway Product; do
  dotnet user-secrets init --project $p
  dotnet user-secrets set "Jwt:Key" "<a long random string>" --project $p
done

dotnet user-secrets set "Jwt:Users:0:Username" "<admin username>" --project AuthWebApi
dotnet user-secrets set "Jwt:Users:0:Password" "<admin password>" --project AuthWebApi
dotnet user-secrets set "Jwt:Users:0:Role"     "Admin"            --project AuthWebApi
```

Generate a key with `openssl rand -base64 48`, or `[Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }))` in PowerShell.

With environment variables, use double underscores instead of colons, for example `Jwt__Key` and `Jwt__Users__0__Username`.

## Notes

- This is a learning project. The login endpoint compares plain passwords from configuration. For a real application, use ASP.NET Core Identity or an external identity provider with hashed passwords.
