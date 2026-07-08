using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Fuel.Events;

public static class CorrelationExtensions
{
    public const string CorrelationHeader = "X-Correlation-Id";

    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId))
                correlationId = Guid.NewGuid().ToString("N");

            context.Response.Headers[CorrelationHeader] = correlationId;
            context.Items[CorrelationHeader] = correlationId;
            await next();
        });
        return app;
    }

    public static string? GetCorrelationId(this HttpContext context)
    {
        context.Items.TryGetValue(CorrelationHeader, out var id);
        return id as string;
    }
}