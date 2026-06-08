## REMOVED Requirements

### Requirement: Single-file CQRS operation structure
**Reason**: CQRS with MediatR has been removed from the template scope. Business logic is now orchestrated via direct service injection rather than command/query handlers.
**Migration**: Use `/Business/Services` interface/implementation pairs with direct controller injection instead of MediatR request/handler pattern.

### Requirement: MediatR pipeline with FluentValidation
**Reason**: Removed alongside MediatR. Request validation is now handled by `[ApiController]` data annotation model validation.
**Migration**: Use `[Required]`, `[StringLength]`, `[Range]` attributes on request DTO records instead of FluentValidation validators.

### Requirement: Handler returns domain result
**Reason**: Removed alongside MediatR.
**Migration**: Service methods return domain types (e.g., `ProductId`) directly or throw domain exceptions caught by global error handling middleware.
