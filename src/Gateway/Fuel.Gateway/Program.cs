using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1) Configuración en cascada: archivos base, específicos del entorno y variables de entorno
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Puerto fijo (puede ser sobrescrito por la variable PORT)
var port = Environment.GetEnvironmentVariable("PORT")
    ?? (builder.Environment.IsDevelopment() ? "5000" : "8080");
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 2) YARP: toda la configuración de rutas y clusters se toma del archivo y de las variables de entorno
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Evita que el frontend quede esperando indefinidamente si un microservicio no responde.
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(
            builder.Configuration.GetValue("ProxyTimeoutSeconds", 15)),
        TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
    };
});

// 3) CORS centralizado – lee orígenes desde configuración (archivo o variable de entorno)
var originsConfig = builder.Configuration["AllowedOrigins"]
    ?? builder.Configuration["ALLOWED_ORIGINS"]
    ?? "http://localhost:5173";
var allowedOrigins = originsConfig.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// 4) Endpoint de salud para el orquestador
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Fuel.Gateway" }));

// 5) Middleware de CORS y proxy
app.UseCors("Frontend");
app.UseRequestTimeouts();
app.MapReverseProxy().WithRequestTimeout(TimeSpan.FromSeconds(
    builder.Configuration.GetValue("ProxyTimeoutSeconds", 15)));

app.Run();
