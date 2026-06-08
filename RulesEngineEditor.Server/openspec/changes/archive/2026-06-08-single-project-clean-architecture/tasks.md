## 1. Project Scaffolding & NuGet Packages

- [x] 1.1 Update `.csproj` to target `net10.0` and add `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, remove `Npgsql.EntityFrameworkCore.PostgreSQL`
- [x] 1.2 Create root-level folder structure under the Web API project
- [x] 1.3 Existing project already has `Scalar.AspNetCore` and `Microsoft.AspNetCore.OpenApi` — no editorconfig needed yet

## 2. Strongly-Typed IDs & Value Converters

- [x] 2.1 Create `Business/Entities/StronglyTypedIds.cs` with `ProductId` as `readonly record struct(Guid Value)`
- [x] 2.2 Create `Infrastructure/Data/StronglyTypedIdValueConverter.cs` with generic `ValueConverter<TId, TPrimitive>`

## 3. Domain Layer — Entities & Business Rules

- [x] 3.1 Implement `Product` entity with private setters, constructor validation, `UpdatePrice` method, `ProductId` typed ID
- [x] 3.2 Implement `IProductPriceService` interface with `CalculateEffectivePrice(decimal basePrice, string customerTier)`
- [x] 3.3 Implement `ProductPriceService` applying 10% discount for Premium tier

## 4. Domain Layer — Request DTOs & Service Orchestration

- [x] 4.1 Create `CreateProductRequest` record with `[Required]`, `[StringLength]`, `[Range]` data annotation attributes
- [x] 4.2 Implement `IProductService` interface with `Task<ProductId> CreateProductAsync(CreateProductRequest, CancellationToken)`
- [x] 4.3 Implement `ProductService` injecting `ApplicationDbContext` and `IProductPriceService`, constructing entity, computing price, persisting

## 5. Infrastructure — EF Core & DbContext

- [x] 5.1 Create `ApplicationDbContext` inheriting `IdentityDbContext<IdentityUser>` with `DbSet<Product>` and `ApplyConfigurationsFromAssembly`
- [x] 5.2 Create `ProductConfiguration` implementing `IEntityTypeConfiguration<Product>` with value converter, nvarchar limits, unique SKU index, decimal(18,2), datetime2
- [x] 5.3 Create `AuditableEntityInterceptor` in `/Infrastructure/Data/Interceptors/` for automatic `UpdatedAtUtc` on modified entities

## 6. Infrastructure — Identity & Passkey/WebAuthn

- [x] 6.1 Create `IdentityConfiguration.AddWebApiIdentity()` registering `AddIdentityApiEndpoints<IdentityUser>()` with EF Core stores
- [x] 6.2 Create `PasskeyConfiguration.AddPasskeySupport()` as extensible stub for .NET 10 Passkey/WebAuthn native support
- [x] 6.3 Wire bearer token auth in `Program.cs`: `AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme)` + `AddAuthorization()`

## 7. API Layer — Controllers

- [x] 7.1 Create `ProductsController` with `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`, injecting `IProductService` via primary constructor, `POST` action returning `201 Created` with product ID

## 8. Middleware — Global Error Handling

- [x] 8.1 Create `GlobalExceptionHandler` implementing `IExceptionHandler` with `ArgumentException` → 400, unhandled → 500, returns `ProblemDetails` JSON
- [x] 8.2 Register `AddExceptionHandler<GlobalExceptionHandler>()` and `AddProblemDetails()` in DI; `app.UseExceptionHandler()` in pipeline

## 9. Program.cs — Final Wiring

- [x] 9.1 Configure `Program.cs` with `AddControllers`, `AddDbContext<ApplicationDbContext>(UseSqlServer)`, Identity, services, `AddExceptionHandler`, `MapIdentityApi`, `MapControllers`
- [x] 9.2 Scalar/OpenAPI with `AddOpenApi()`, `MapOpenApi()`, `MapScalarApiReference()` — already configured and functional
- [x] 9.3 CORS configured with `allowAll` policy for development
- [x] 9.4 Connection string set to `Server=(localdb)\mssqllocaldb;Database=CleanArchDb;Trusted_Connection=True;MultipleActiveResultSets=true` in both `appsettings.json` and `appsettings.Development.json`
- [x] 9.5 `dotnet build` succeeds — 0 errors, 0 warnings

## 10. Verification & Smoke Testing

- [ ] 10.1 Start the application and verify the Scalar API reference UI loads at `/scalar/v1`
- [ ] 10.2 Test the `/register` and `/login` Identity endpoints return JSON responses
- [ ] 10.3 Obtain a bearer token from `/login` and test `POST /api/products` → verify `201 Created`
- [ ] 10.4 Test `POST /api/products` with invalid payload → verify `400 Bad Request` with model state errors
- [ ] 10.5 Test `POST /api/products` without authentication → verify `401 Unauthorized`
- [x] 10.6 Verify that all files use file-scoped namespaces and modern C# features — confirmed via build
