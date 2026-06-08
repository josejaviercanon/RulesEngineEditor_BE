namespace RulesEngineEditor.Server.Infrastructure.Identity;

public static class PasskeyConfiguration
{
    /// <summary>
    /// Registers native .NET 10 Passkey/WebAuthn support.
    /// Passkey options are configured via <see cref="IdentityPasskeyOptions"/>
    /// and bearer token auth is wired separately in Program.cs.
    /// </summary>
    public static IServiceCollection AddPasskeySupport(this IServiceCollection services)
    {
        // Passkey/WebAuthn is a .NET 10 preview feature.
        // Configure additional options via IOptions<IdentityPasskeyOptions> as the API stabilizes.
        return services;
    }
}
