using Fuel.Events;
using Fuel.Payments.Service.Features.Payments.Data;
using Fuel.Payments.Service.Features.Payments.Domain;
using Fuel.Payments.Service.Infrastructure.Auth;
using Fuel.Payments.Service.Infrastructure.Data;
using Fuel.Payments.Service.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT")
    ?? (builder.Environment.IsDevelopment() ? "5003" : "8080");
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddCorrelationId();
builder.Services.AddHttpContextAccessor();

var connStr = MySqlConnectionStrings.FromConfiguration(
    builder.Configuration,
    "PaymentsConnection",
    "payments_db");
builder.Services.AddDbContext<PaymentsDbContext>(opts => opts.UseMySQL(connStr));

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

builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IPaymentsRepository, MySqlPaymentsRepository>();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await EnsureDatabaseCreatedWithRetryAsync(db.Database, app.Logger, "Payments");
    await EnsurePaymentsSchemaAsync(db.Database);
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Fuel.Payments.Service" }))
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

static async Task EnsurePaymentsSchemaAsync(
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database)
{
    await database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `payment_methods` (
            `Id` varchar(32) NOT NULL,
            `UserId` varchar(32) NOT NULL,
            `Brand` varchar(40) NOT NULL,
            `Last4` varchar(4) NOT NULL,
            `Holder` varchar(160) NOT NULL,
            `Expires` varchar(5) NOT NULL,
            `IsDefault` tinyint(1) NOT NULL,
            `CreatedAtUtc` datetime(6) NOT NULL,
            PRIMARY KEY (`Id`),
            KEY `IX_payment_methods_UserId` (`UserId`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);

    await database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `payment_history` (
            `Id` varchar(32) NOT NULL,
            `UserId` varchar(32) NOT NULL,
            `DateUtc` datetime(6) NOT NULL,
            `Description` varchar(300) NOT NULL,
            `Amount` decimal(14,2) NOT NULL,
            `Currency` varchar(3) NOT NULL,
            `Status` varchar(30) NOT NULL,
            PRIMARY KEY (`Id`),
            KEY `IX_payment_history_UserId` (`UserId`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);
}
