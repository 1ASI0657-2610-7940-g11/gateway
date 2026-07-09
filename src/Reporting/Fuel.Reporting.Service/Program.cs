using Fuel.Events;
using Fuel.Reporting.Service.Features.Client.Data;
using Fuel.Reporting.Service.Features.Client.Domain;
using Fuel.Reporting.Service.Features.Company.Data;
using Fuel.Reporting.Service.Features.Company.Domain;
using Fuel.Reporting.Service.Features.Home.Data;
using Fuel.Reporting.Service.Features.Home.Domain;
using Fuel.Reporting.Service.Features.Provider.Data;
using Fuel.Reporting.Service.Features.Provider.Domain;
using Fuel.Reporting.Service.Infrastructure.Auth;
using Fuel.Reporting.Service.Infrastructure.Cache;
using Fuel.Reporting.Service.Infrastructure.Data;
using Fuel.Reporting.Service.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT")
    ?? (builder.Environment.IsDevelopment() ? "5004" : "8080");
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Trazabilidad
builder.Services.AddCorrelationId();
builder.Services.AddHttpContextAccessor();

// Base de datos del Reporting
var connStr = MySqlConnectionStrings.FromConfiguration(
    builder.Configuration,
    "ReportingConnection",
    "reporting_db");
builder.Services.AddDbContext<ReportingDbContext>(opts => opts.UseMySQL(connStr));

// Redis (caché)
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddSingleton<RedisCacheService>();

// JWT (misma configuración que los otros servicios)
var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Repositorios
builder.Services.AddScoped<IHomeRepository, MySqlHomeRepository>();
builder.Services.AddScoped<IClientRepository, MySqlClientRepository>();
builder.Services.AddSingleton<ICompanyRepository, InMemoryCompanyRepository>();
builder.Services.AddSingleton<IProviderRepository, InMemoryProviderRepository>();

// Consumidor de eventos de RabbitMQ (se levanta como BackgroundService)
builder.Services.AddHostedService<EventConsumerService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Crear la base de datos automáticamente en desarrollo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCorrelationId();

if (app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("ENABLE_SWAGGER"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Fuel.Reporting.Service" }))
    .AllowAnonymous();
app.Run();
