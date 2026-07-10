using Fuel.Events;
using Fuel.Orders.Service.Features.Orders.Data;
using Fuel.Orders.Service.Features.Orders.Domain;
using Fuel.Orders.Service.Infrastructure.Auth;
using Fuel.Orders.Service.Infrastructure.Data;
using Fuel.Orders.Service.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT")
    ?? (builder.Environment.IsDevelopment() ? "5002" : "8080");
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Trazabilidad
builder.Services.AddCorrelationId();
builder.Services.AddHttpContextAccessor();

// Base de datos
var connStr = MySqlConnectionStrings.FromConfiguration(
    builder.Configuration,
    "OrdersConnection",
    "orders_db");
builder.Services.AddDbContext<OrdersDbContext>(opts => opts.UseMySQL(connStr));

// JWT (validación, misma clave que Identity)
var jwtOptions = Fuel.Orders.Service.Infrastructure.Auth.JwtOptions.FromConfiguration(
    builder.Configuration, builder.Environment);
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

// Mensajería
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// Repositorios
builder.Services.AddScoped<IOrdersRepository, MySqlOrdersRepository>();

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

// Crear base de datos automáticamente (dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await EnsureDatabaseCreatedWithRetryAsync(db.Database, app.Logger, "Orders");
    await EnsureOrdersSchemaAsync(db.Database);
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Fuel.Orders.Service" }))
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

static async Task EnsureOrdersSchemaAsync(
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database)
{
    await database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `orders` (
            `Id` varchar(32) NOT NULL,
            `UserId` varchar(32) NOT NULL,
            `Code` varchar(40) NOT NULL,
            `Status` varchar(20) NOT NULL,
            `Product` varchar(100) NOT NULL,
            `QuantityGallons` int NOT NULL,
            `CreatedAtUtc` datetime(6) NOT NULL,
            `Eta` varchar(200) NOT NULL,
            `Plant` varchar(160) NOT NULL,
            `Address` varchar(300) NOT NULL,
            `TimeWindow` varchar(100) NOT NULL,
            `Notes` varchar(1000) NULL,
            `PaymentMethod` varchar(120) NULL,
            `Amount` decimal(14,2) NULL,
            `VehicleId` varchar(60) NULL,
            `VehiclePlate` varchar(30) NULL,
            `DriverName` varchar(160) NULL,
            `LastStatusComment` varchar(500) NULL,
            PRIMARY KEY (`Id`),
            UNIQUE KEY `IX_orders_Code` (`Code`),
            KEY `IX_orders_UserId_CreatedAtUtc` (`UserId`, `CreatedAtUtc`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);

    await EnsureColumnAsync(database, "orders", "Id", "varchar(32) NOT NULL");
    await EnsureColumnAsync(database, "orders", "UserId", "varchar(32) NOT NULL");
    await EnsureColumnAsync(database, "orders", "Code", "varchar(40) NOT NULL");
    await EnsureColumnAsync(database, "orders", "Status", "varchar(20) NOT NULL DEFAULT 'Scheduled'");
    await EnsureColumnAsync(database, "orders", "Product", "varchar(100) NOT NULL DEFAULT 'Diesel B5'");
    await EnsureColumnAsync(database, "orders", "QuantityGallons", "int NOT NULL DEFAULT 0");
    await EnsureColumnAsync(database, "orders", "CreatedAtUtc", "datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)");
    await EnsureColumnAsync(database, "orders", "Eta", "varchar(200) NOT NULL DEFAULT 'Pendiente'");
    await EnsureColumnAsync(database, "orders", "Plant", "varchar(160) NOT NULL DEFAULT 'Por asignar'");
    await EnsureColumnAsync(database, "orders", "Address", "varchar(300) NOT NULL DEFAULT ''");
    await EnsureColumnAsync(database, "orders", "TimeWindow", "varchar(100) NOT NULL DEFAULT ''");
    await EnsureColumnAsync(database, "orders", "Notes", "varchar(1000) NULL");
    await EnsureColumnAsync(database, "orders", "PaymentMethod", "varchar(120) NULL");
    await EnsureColumnAsync(database, "orders", "Amount", "decimal(14,2) NULL");
    await EnsureColumnAsync(database, "orders", "VehicleId", "varchar(60) NULL");
    await EnsureColumnAsync(database, "orders", "VehiclePlate", "varchar(30) NULL");
    await EnsureColumnAsync(database, "orders", "DriverName", "varchar(160) NULL");
    await EnsureColumnAsync(database, "orders", "LastStatusComment", "varchar(500) NULL");
}

static async Task EnsureColumnAsync(
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database,
    string tableName,
    string columnName,
    string definition)
{
    var connection = database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose)
        await connection.OpenAsync();

    try
    {
        await using var check = connection.CreateCommand();
        check.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;
            """;

        var tableParam = check.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        check.Parameters.Add(tableParam);

        var columnParam = check.CreateParameter();
        columnParam.ParameterName = "@columnName";
        columnParam.Value = columnName;
        check.Parameters.Add(columnParam);

        var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
        if (exists) return;

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {definition};";
        await alter.ExecuteNonQueryAsync();
    }
    finally
    {
        if (shouldClose)
            await connection.CloseAsync();
    }
}
