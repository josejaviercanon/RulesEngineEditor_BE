# Debug Guide – ASP.NET Core Backend

## Common Issues

### 1. Stored Procedure Execution Errors
- **Symptom**: Repository method throws `SqlException` ("Procedure or function 'sp_Name' expects parameter...").
- **Fix**: Verify `MapToInsertParameters` / `MapToUpdateParameters` returns the correct `SqlParameter[]` array matching the stored procedure signature. Check parameter names, types, and direction.

### 2. Stored Procedure Not Found
- **Symptom**: `SqlException` — "Could not find stored procedure 'sp_InsertX'".
- **Fix**: Verify the stored procedure exists in the database. If it was just added to `efpt.config.json`, run the T-SQL script or EF Core Power Tools Refresh.

### 3. Identity/Output Parameter Issues
- **Symptom**: `CreateAsync` returns default value or throws invalid cast.
- **Fix**: Ensure `MapToInsertParameters` includes `@NewId` with `ParameterDirection.Output` and `SqlDbType.Int`. Verify the stored procedure uses `SET @NewId = SCOPE_IDENTITY()`.

### 4. Validation Failures
- **Symptom**: Backend rejects valid workflow JSON.
- **Fix**: Inspect `RulesEngineService.cs`; confirm schema alignment with Microsoft RulesEngine Library.

### 5. Execution Errors
- **Symptom**: Workflow execution throws exceptions.
- **Fix**: Debug `ExecuteWorkflow` method; add logging for rule evaluation.

### 6. efpt.config.json Validation Failures
- **Symptom**: Agent refuses to proceed — missing table or stored procedure in whitelist.
- **Fix**: Open `efpt.config.json`, verify the `Tables` and `StoredProcedures` arrays. If an object was added to the database but not the whitelist, run EF Core Power Tools "Refresh" from Visual Studio.

## Debugging Steps
- Run backend with `dotnet run`.
- Use Visual Studio 2026 breakpoints in controllers, repositories, and services.
- Debug stored procedures directly in SSMS or Azure Data Studio:
  - `EXEC [dbo].[sp_GetWorkflowById] @WorkflowId = 1`
  - `EXEC [dbo].[sp_InsertWorkflow] @Name = 'Test', @NewId = 0 OUTPUT`
- Inspect SQL Server logs via SSMS or `sys.dm_exec_requests`.
- Test endpoints with Scalar at `/scalar/v1` or Postman.
- Use `dotnet watch` for hot reload during development.

## .NET Hot Reload
- Use `dotnet watch` for hot reload during development.
- Changes to controllers, services, and views apply without restarting the process.
- Run: `dotnet watch run` from the project directory.
- Note: Some changes (e.g., new NuGet packages, static files) still require a restart.

## OpenAPI / Scalar UI
- Scalar UI is available in development mode at: `/scalar/v1`
- Enabled via `app.MapScalarApiReference()` in `Program.cs`
- Use Scalar to explore endpoints, send test requests, and view OpenAPI schema.
- The OpenAPI document is mapped at `/openapi/v1.json`.
- Note: Not Swagger UI — the project uses Scalar exclusively.

## OpenTelemetry Trace Inspection
- OpenTelemetry is **not yet configured** in the current project.
- When added, traces will include: incoming HTTP requests, stored procedure calls, and external service calls.
- Planned integrations:
  - Jaeger / Zipkin (self-hosted trace collectors)
  - Azure Application Insights (production)
  - OTEL-compatible logging tools

## SQL Server Connection Troubleshooting
- Connection string is configured in `appsettings.json` under `ConnectionStrings.DefaultConnection`.
- Common connection errors:
  - **Server not found**: Verify SQL Server instance is running and accessible (check `Server=(localdb)\mssqllocaldb` for local development).
  - **Authentication failed**: For Windows Auth, use `Trusted_Connection=True`. For SQL Auth, provide `User Id` and `Password`.
  - **Database not found**: Ensure the target database exists; create it or verify connection string.
- Test connection: `dotnet ef database update` or use SSMS / Azure Data Studio.
- LocalDB: If using `(localdb)\mssqllocaldb`, ensure LocalDB is installed (ships with Visual Studio).
- Firewall: Ensure SQL Server TCP port (default 1433) is open for remote connections.

## efpt.config.json Validation
- The `efpt.config.json` file IS the source of truth for database objects.
- To verify the whitelist:
  ```json
  {
    "Tables": [
      {"Name": "[dbo].[AspNetUsers]", "ObjectType": 0}
    ],
    "StoredProcedures": [
      {"Name": "[dbo].[sp_InsertWorkflow]", "ObjectType": 1}
    ]
  }
  ```
- **ObjectType 0** = Table, **ObjectType 1** = Stored Procedure
- If an object is missing, run EF Core Power Tools "Refresh" from Visual Studio to regenerate the file.

## Stored Procedure Debugging
- **Test in SSMS**:
  ```sql
  -- Test insert
  DECLARE @NewId INT;
  EXEC [dbo].[sp_InsertWorkflow] @Name = 'Test Workflow', @NewId = @NewId OUTPUT;
  SELECT @NewId AS NewId;

  -- Test get by ID
  EXEC [dbo].[sp_GetWorkflowById] @WorkflowId = 1;

  -- Test get all
  EXEC [dbo].[sp_GetAllWorkflows];

  -- Test update
  EXEC [dbo].[sp_UpdateWorkflow] @WorkflowId = 1, @Name = 'Updated Workflow';

  -- Test delete
  EXEC [dbo].[sp_DeleteWorkflow] @WorkflowId = 1;
  ```
- **Common stored procedure issues**:
  - Parameter name mismatch between C# `SqlParameter` and procedure definition
  - Data type mismatch (e.g., `NVARCHAR` vs `VARCHAR`, `INT` vs `BIGINT`)
  - Missing `OUTPUT` parameter for identity retrieval
  - Schema mismatch (procedure in `[dbo]` but called without schema)

## Human Intervention
- Developers review stored procedure T-SQL scripts before executing on production.
- Approve `efpt.config.json` changes.
- Validate API responses manually.
- Approve agent‑generated backend code via pull requests.
