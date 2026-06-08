# Identity Authentication

## Purpose

Defines the native ASP.NET Core Identity configuration with Passkey/WebAuthn support for .NET 10 Web API projects, including bearer token authentication and controller-level authorization.

## Requirements

### Requirement: ASP.NET Core Identity with Web API endpoints

The system SHALL configure native ASP.NET Core Identity using `AddIdentityApiEndpoints<TUser>()` in the service collection. Identity SHALL be configured in `/Infrastructure/Identity` with the EF Core store backing.

#### Scenario: Identity services are registered with EF Core store
- **WHEN** the application service collection is configured
- **THEN** `AddIdentityApiEndpoints<IdentityUser>()` SHALL be called with the `ApplicationDbContext` as the store

#### Scenario: Identity endpoints are mapped as Web API routes
- **WHEN** the application middleware pipeline is configured
- **THEN** `MapIdentityApi<IdentityUser>()` SHALL be called to expose `/register`, `/login`, and `/manage` endpoints as standard JSON Web API routes

#### Scenario: Identity endpoints return JSON, not redirects
- **WHEN** a POST request is made to `/register` with valid credentials
- **THEN** the response SHALL be `200 OK` with JSON body containing the authentication token, NOT an HTML redirect

### Requirement: Passkey/WebAuthn support

The system SHALL configure native .NET 10 Passkey/WebAuthn support via `IdentityPasskeyOptions` (introduced in .NET 10). The configuration SHALL be in `/Infrastructure/Identity/PasskeyConfiguration.cs`.

#### Scenario: Passkey options are registered
- **WHEN** the application service collection is configured
- **THEN** `AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme)` SHALL be called alongside Identity API endpoints

#### Scenario: Passkey registration endpoint is available
- **WHEN** a user calls the passkey registration endpoint
- **THEN** the system SHALL respond with WebAuthn credential creation options per the .NET 10 native Passkey API

### Requirement: Authorization attribute on controllers

All API controllers in `/Controllers` SHALL be decorated with `[Authorize]` at the class level, requiring authenticated access by default. Controllers can opt out per-action with `[AllowAnonymous]`.

#### Scenario: Unauthenticated request is rejected
- **WHEN** a GET or POST request is made to a controller action without a valid bearer token
- **THEN** the response SHALL be `401 Unauthorized`

#### Scenario: Authenticated request succeeds
- **WHEN** a GET or POST request is made with a valid bearer token obtained from the `/login` endpoint
- **THEN** the controller action SHALL execute normally and return the expected response
