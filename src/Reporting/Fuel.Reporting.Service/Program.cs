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
var redisOptions = ConfigurationOptions.Parse(redisConn);
redisOptions.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));
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
    await EnsureDatabaseCreatedWithRetryAsync(db.Database, app.Logger, "Reporting");
    await EnsureReportingSchemaAsync(db.Database);
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

static async Task EnsureDatabaseCreatedWithRetryAsync(
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database,
    ILogger logger,
    string serviceName)
{
    const int maxAttempts = 12;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await database.EnsureCreatedAsync();
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex,
                "{ServiceName} database is not ready. Retry {Attempt}/{MaxAttempts}.",
                serviceName,
                attempt,
                maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    await database.EnsureCreatedAsync();
}

static async Task EnsureReportingSchemaAsync(
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database)
{
    await database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `dashboards` (
            `UserId` varchar(32) NOT NULL,
            `CompanyName` varchar(200) NOT NULL,
            `AvatarUrl` varchar(200) NULL,
            `ActiveOrderFuelType` varchar(100) NULL,
            `ActiveOrderQuantityGallons` int NULL,
            `ActiveOrderStatus` varchar(20) NULL,
            `NextDeliveryDateTime` varchar(200) NULL,
            `NextDeliveryLocation` varchar(200) NULL,
            `NextDeliveryStatus` varchar(20) NULL,
            `LastPaymentAmount` varchar(50) NULL,
            `LastPaymentMethod` varchar(40) NULL,
            `LastPaymentStatus` varchar(30) NULL,
            `LastUpdatedUtc` datetime(6) NOT NULL,
            PRIMARY KEY (`UserId`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);

    await database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `order_kpis` (
            `Id` varchar(32) NOT NULL,
            `UserId` varchar(32) NOT NULL,
            `OrderCode` varchar(40) NOT NULL,
            `Status` varchar(20) NOT NULL,
            `FuelType` varchar(100) NOT NULL,
            `QuantityGallons` int NOT NULL,
            `Amount` decimal(14,2) NOT NULL,
            `CreatedAtUtc` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`),
            KEY `IX_order_kpis_UserId_CreatedAtUtc` (`UserId`, `CreatedAtUtc`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);
}
