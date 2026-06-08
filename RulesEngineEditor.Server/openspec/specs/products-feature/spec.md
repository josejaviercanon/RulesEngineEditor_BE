# Products Feature

## Purpose

Defines the concrete "Products" feature implementation spanning all Clean Architecture layers — rich entity, domain services, EF Core configuration, request DTO, and API controller with authentication.

## Requirements

### Requirement: Product entity with business rules

The system SHALL define a `Product` entity in `/Business/Entities/Product.cs`. The entity SHALL have:

- `ProductId` strongly-typed ID (`readonly record struct ProductId(Guid Value)`)
- `Name` — non-empty string, max 200 characters
- `Description` — string, max 2000 characters
- `BasePrice` — positive `decimal`
- `CurrentPrice` — `decimal` computed from business rules
- `Sku` — non-empty string, unique across all products
- `IsActive` — boolean, defaulting to `true`
- `CreatedAtUtc` — `DateTime` set at construction
- `UpdatedAtUtc` — `DateTime` updated on modification
- Private setters for all properties
- Business rules enforced in the constructor and update methods

#### Scenario: Product is created with valid data
- **WHEN** a new `Product` is constructed with valid `Name`, `BasePrice`, and `Sku`
- **THEN** the entity SHALL have a non-default `ProductId`, `CreatedAtUtc` set to the current UTC time, and `IsActive` SHALL be `true`

#### Scenario: Product creation rejects empty name
- **WHEN** a `Product` is constructed with an empty or whitespace `Name`
- **THEN** the constructor SHALL throw `ArgumentException` with a message containing "Name"

#### Scenario: Product creation rejects non-positive price
- **WHEN** a `Product` is constructed with a `BasePrice` of zero or negative
- **THEN** the constructor SHALL throw `ArgumentException` with a message containing "BasePrice"

#### Scenario: Product creation rejects empty SKU
- **WHEN** a `Product` is constructed with an empty or whitespace `Sku`
- **THEN** the constructor SHALL throw `ArgumentException` with a message containing "Sku"

#### Scenario: Product price can be updated via domain method
- **WHEN** `Product.UpdatePrice(decimal newPrice)` is called with a positive value
- **THEN** `CurrentPrice` SHALL be updated to the new value and `UpdatedAtUtc` SHALL be refreshed

### Requirement: Product Price Service

The system SHALL define `IProductPriceService` in `/Business/Services/IProductPriceService.cs` and its implementation `ProductPriceService` in `/Business/Services/ProductPriceService.cs`. The service SHALL calculate the effective price considering discounts, promotions, and currency conversions.

#### Scenario: Price service applies no discount for standard pricing
- **WHEN** `CalculateEffectivePrice(basePrice, customerTier: "Standard")` is called
- **THEN** the effective price SHALL equal the `basePrice`

#### Scenario: Price service applies percentage discount for premium customers
- **WHEN** `CalculateEffectivePrice(100.00m, customerTier: "Premium")` is called
- **THEN** the effective price SHALL be `90.00m` (10% discount)

### Requirement: Product EF Core configuration

The system SHALL define `ProductConfiguration` implementing `IEntityTypeConfiguration<Product>` in `/Infrastructure/Data/Configurations/ProductConfiguration.cs`. The configuration SHALL:

- Map `ProductId` via a value converter to `Guid`
- Configure `Name` as `nvarchar(200)` required
- Configure `Sku` as `nvarchar(50)` required with a unique index
- Configure `BasePrice` and `CurrentPrice` as `decimal(18,2)`
- Map `CreatedAtUtc` and `UpdatedAtUtc` as `datetime2` required

#### Scenario: ProductConfiguration is auto-applied
- **WHEN** `ApplicationDbContext` is configured
- **THEN** `ProductConfiguration` SHALL be discovered and applied via `modelBuilder.ApplyConfigurationsFromAssembly()`

### Requirement: Create Product Request DTO

The system SHALL define `CreateProductRequest` in `/Business/Services/Models/CreateProductRequest.cs` as an immutable `record` with:

- `string Name` — `[Required]`, `[StringLength(200)]`
- `string? Description` — `[StringLength(2000)]`
- `decimal BasePrice` — `[Required]`, `[Range(0.01, double.MaxValue)]`
- `string Sku` — `[Required]`, `[StringLength(50)]`
- Data annotation validation attributes for automatic model validation

#### Scenario: Request DTO validates via data annotations
- **WHEN** a POST request with invalid properties is sent
- **THEN** `[ApiController]` automatic model validation SHALL return `400 Bad Request` with model state errors

### Requirement: Product Service

The system SHALL define `IProductService` in `/Business/Services/IProductService.cs` and its implementation `ProductService` in `/Business/Services/ProductService.cs`. The service SHALL:

- Accept `CreateProductRequest` DTO
- Construct a `Product` entity from the DTO
- Call `IProductPriceService.CalculateEffectivePrice()` to set `CurrentPrice`
- Persist the entity via `ApplicationDbContext`
- Return the created `ProductId`

#### Scenario: Product service creates product with computed price
- **WHEN** `CreateProductAsync(request, customerTier)` is called
- **THEN** a `Product` SHALL be created with `CurrentPrice` computed by `IProductPriceService` and the entity SHALL be persisted to the database

### Requirement: Products Controller

The system SHALL define `ProductsController` in `/Controllers/ProductsController.cs`. It SHALL:

- Inherit from `ControllerBase`
- Be decorated with `[ApiController]` and `[Route("api/[controller]")]`
- Have an `[Authorize]` attribute at the controller level
- Inject `IProductService` via primary constructor
- Expose a `POST` action that accepts a `CreateProductRequest` and returns `IActionResult`

#### Scenario: POST endpoint creates a product via service
- **WHEN** a POST request is made to `api/products` with a valid JSON body
- **THEN** the controller SHALL call `IProductService.CreateProductAsync()` and return `201 Created` with the new `ProductId`

#### Scenario: POST endpoint returns model validation errors
- **WHEN** a POST request is made to `api/products` with an invalid body (e.g., empty name)
- **THEN** the `[ApiController]` attribute SHALL return `400 Bad Request` with model state errors
