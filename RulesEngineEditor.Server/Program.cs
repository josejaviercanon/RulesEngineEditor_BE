using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RulesEngineEditor.Server.Business.Entities.Models;
using RulesEngineEditor.Server.Business.Services;
using RulesEngineEditor.Server.Infrastructure.Data;
using RulesEngineEditor.Server.Infrastructure.Identity;
using RulesEngineEditor.Server.Infrastructure.Repositories;
using RulesEngineEditor.Server.Middleware;
using Scalar.AspNetCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options => options.AddPolicy(
    "allowAll",
    policy => policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<RulesEngineEditorContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity + Passkey
builder.Services.AddWebApiIdentity();
builder.Services.AddPasskeySupport();
builder.Services.AddAuthorization();

// Business Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IRulesRepository, RulesRepository>();
builder.Services.AddScoped<IRulesEvaluationService, RulesEvaluationService>();

// Error Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler();

app.UseCors("allowAll");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<IdentityUser>();


// Program.cs

// 1. SECURED ENDPOINT: Only Administrators can request a registration challenge
app.MapPost("/api/passkey/register-options", async (UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ClaimsPrincipal user) => {
    var appUser = await userManager.GetUserAsync(user);
    if (appUser == null) return Results.Unauthorized();

    var userId = await userManager.GetUserIdAsync(appUser);
    var userName = await userManager.GetUserNameAsync(appUser) ?? appUser.Email ?? userId;

    var options = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
    {
        Id = userId,
        Name = userName,
        DisplayName = userName
    });

    return Results.Content(options, "application/json");
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

// 2. SECURED ENDPOINT: Only Administrators can verify and save a hardware key
app.MapPost("/api/passkey/register-verify", async ([FromBody] string clientResponse, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ClaimsPrincipal user) => {
    var appUser = await userManager.GetUserAsync(user);
    if (appUser == null) return Results.Unauthorized();

    var attestationResult = await signInManager.PerformPasskeyAttestationAsync(clientResponse);
    if (!attestationResult.Succeeded)
    {
        return Results.BadRequest("Verification failed");
    }

    var addResult = await userManager.AddOrUpdatePasskeyAsync(appUser, attestationResult.Passkey);
    return addResult.Succeeded ? Results.Ok() : Results.BadRequest("Verification failed");
})
.RequireAuthorization(new AuthorizeAttribute { Roles = "Administrator" });

// 3. ANONYMOUS ENDPOINT: Allows unauthenticated passkey login challenge retrieval
app.MapPost("/api/passkey/login-options", async (SignInManager<IdentityUser> signInManager) =>
{
    var options = await signInManager.MakePasskeyRequestOptionsAsync(user: null);
    return Results.Content(options, "application/json");
})
.AllowAnonymous();

// 4. ANONYMOUS ENDPOINT: Allows unauthenticated passkey assertion verification
app.MapPost("/api/passkey/login-verify", async ([FromBody] string clientResponse, SignInManager<IdentityUser> signInManager) =>
{
    var signInResult = await signInManager.PasskeySignInAsync(clientResponse);
    return signInResult.Succeeded ? Results.Ok() : Results.Unauthorized();
})
.AllowAnonymous();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Automatically applies seeding only on localhost/development machine
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            // Execute the async initializer
            await DatabaseSeeder.SeedAdminUserAsync(userManager, roleManager, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the development database.");
        }
    }

}

app.MapGet("/", () => "RulesEngine Editor Web API.");

await app.RunAsync();
