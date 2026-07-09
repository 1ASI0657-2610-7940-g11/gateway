using Fuel.Events;
using Fuel.Identity.Service.Features.Auth.Data;
using Fuel.Identity.Service.Features.Auth.Domain;
using Fuel.Identity.Service.Features.Profile.Data;
using Fuel.Identity.Service.Features.Profile.Domain;
using Fuel.Identity.Service.Infrastructure.Auth;
using Fuel.Identity.Service.Infrastructure.Data;
using Fuel.Identity.Service.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT")
    ?? (builder.Environment.IsDevelopment() ? "5001" : "8080");
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Trazabilidad
builder.Services.AddCorrelationId();
builder.Services.AddHttpContextAccessor();

// Base de datos
var connStr = MySqlConnectionStrings.FromConfiguration(
    builder.Configuration,
    "IdentityConnection",
    "identity_db");
builder.Services.AddDbContext<IdentityDbContext>(opts => opts.UseMySQL(connStr));

// JWT
var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.AddScoped<TokenService>();

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

// Mensajería
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// Repositorios
builder.Services.AddScoped<IAuthRepository, MySqlAuthRepository>();
builder.Services.AddScoped<IProfileRepository, MySqlProfileRepository>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
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
        Description = "Ingrese el token JWT así: Bearer {token}"
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

// Crear/esquema de base de datos automáticamente (para desarrollo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await EnsureDatabaseCreatedWithRetryAsync(db.Database, app.Logger, "Identity");
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Fuel.Identity.Service" }))
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
