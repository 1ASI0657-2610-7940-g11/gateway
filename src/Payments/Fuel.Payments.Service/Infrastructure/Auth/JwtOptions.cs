using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Fuel.Payments.Service.Infrastructure.Auth;

public sealed record JwtOptions(string Secret, string Issuer, string Audience, int ExpirationMinutes)
{
    public static JwtOptions FromConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        var secret = configuration["JWT_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("JWT_SECRET is required in production.");
            secret = "development-only-secret-change-before-deploying-12345678901234567890";
        }
        if (secret.Length < 64)
            throw new InvalidOperationException("JWT_SECRET must contain at least 64 characters.");

        return new JwtOptions(
            secret,
            configuration["JWT_ISSUER"] ?? "FuelTrack.Api",
            configuration["JWT_AUDIENCE"] ?? "FuelTrack.Web",
            configuration.GetValue("JWT_EXPIRATION_MINUTES", 120));
    }
}