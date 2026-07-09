using Fuel.Events;
using Fuel.Reporting.Service.Infrastructure.Data;
using Fuel.Reporting.Service.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Fuel.Reporting.Service.Infrastructure.Messaging;

public class EventConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public EventConsumerService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<EventConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsumers();
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is BrokerUnreachableException or SocketException or TimeoutException)
            {
                _logger.LogWarning(ex, "RabbitMQ unavailable for Reporting consumer. Retrying in 10 seconds.");
                DisposeRabbitMq();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void StartConsumers()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "admin",
            Password = _configuration["RabbitMQ:Password"] ?? "ChangeMe123",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3),
            SocketReadTimeout = TimeSpan.FromSeconds(3),
            SocketWriteTimeout = TimeSpan.FromSeconds(3),
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare("order-events", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("payment-events", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("user-events", ExchangeType.Fanout, durable: true);

        var orderQueue = _channel.QueueDeclare("reporting-order-queue", durable: true, exclusive: false, autoDelete: false).QueueName;
        var paymentQueue = _channel.QueueDeclare("reporting-payment-queue", durable: true, exclusive: false, autoDelete: false).QueueName;
        var userQueue = _channel.QueueDeclare("reporting-user-queue", durable: true, exclusive: false, autoDelete: false).QueueName;

        _channel.QueueBind(orderQueue, "order-events", "");
        _channel.QueueBind(paymentQueue, "payment-events", "");
        _channel.QueueBind(userQueue, "user-events", "");

        var orderConsumer = new AsyncEventingBasicConsumer(_channel);
        orderConsumer.Received += async (sender, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var correlationId = ea.BasicProperties.CorrelationId;
            _logger.LogInformation("Order event received with correlation {CorrelationId}", correlationId);

            var eventType = GetEventType(ea.BasicProperties.Headers);
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                await ProcessOrderEvent(eventType, body);
            }
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        _channel.BasicConsume(orderConsumer, orderQueue, false);

        var paymentConsumer = new AsyncEventingBasicConsumer(_channel);
        paymentConsumer.Received += async (sender, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("Payment event received");
            await ProcessPaymentEvent(GetEventType(ea.BasicProperties.Headers), body);
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        _channel.BasicConsume(paymentConsumer, paymentQueue, false);

        var userConsumer = new AsyncEventingBasicConsumer(_channel);
        userConsumer.Received += async (sender, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("User event received");
            await ProcessUserEvent(GetEventType(ea.BasicProperties.Headers), body);
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        _channel.BasicConsume(userConsumer, userQueue, false);
    }

    private async Task ProcessOrderEvent(string eventType, string body)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        try
        {
            if (eventType == nameof(OrderCreatedEvent))
            {
                var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(body)!;
                var kpi = new OrderKpiEntity
                {
                    UserId = evt.UserId,
                    OrderCode = evt.Code,
                    Status = "Scheduled",
                    FuelType = evt.FuelType,
                    QuantityGallons = evt.QuantityGallons,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.OrderKpis.Add(kpi);
                await UpdateDashboardFromOrder(db, evt.UserId);
            }
            else if (eventType == nameof(OrderStatusUpdatedEvent))
            {
                var evt = JsonSerializer.Deserialize<OrderStatusUpdatedEvent>(body)!;
                var kpi = await db.OrderKpis.FirstOrDefaultAsync(x => x.OrderCode == evt.Code);
                if (kpi != null)
                {
                    kpi.Status = evt.NewStatus;
                    await UpdateDashboardFromOrder(db, evt.UserId);
                }
            }
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order event");
        }
    }

    private async Task ProcessPaymentEvent(string? eventType, string body)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        try
        {
            // Simplificación: actualiza dashboard con último pago simbólico
            if (eventType == nameof(PaymentMethodAddedEvent))
            {
                var evt = JsonSerializer.Deserialize<PaymentMethodAddedEvent>(body)!;
                var dashboard = await db.Dashboards.FindAsync(evt.UserId);
                if (dashboard != null)
                {
                    dashboard.LastPaymentMethod = $"{evt.Brand} ****{evt.Last4}";
                    dashboard.LastPaymentStatus = "Activo";
                    dashboard.LastUpdatedUtc = DateTime.UtcNow;
                }
            }
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment event");
        }
    }

    private async Task ProcessUserEvent(string? eventType, string body)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        try
        {
            if (eventType == nameof(UserRegisteredEvent))
            {
                var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(body)!;
                var dashboard = new DashboardEntity
                {
                    UserId = evt.UserId,
                    CompanyName = evt.FullName,
                    LastUpdatedUtc = DateTime.UtcNow
                };
                db.Dashboards.Add(dashboard);
            }
            else if (eventType == nameof(ProfileUpdatedEvent))
            {
                var evt = JsonSerializer.Deserialize<ProfileUpdatedEvent>(body)!;
                var dashboard = await db.Dashboards.FindAsync(evt.UserId);
                if (dashboard != null)
                {
                    dashboard.CompanyName = evt.FullName;
                    dashboard.AvatarUrl = evt.AvatarUrl;
                    dashboard.LastUpdatedUtc = DateTime.UtcNow;
                }
            }
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user event");
        }
    }

    private async Task UpdateDashboardFromOrder(ReportingDbContext db, string userId)
    {
        var dashboard = await db.Dashboards.FindAsync(userId);
        if (dashboard == null) return;

        var activeOrder = await db.OrderKpis
            .Where(x => x.UserId == userId && x.Status != "Delivered" && x.Status != "Cancelled")
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (activeOrder != null)
        {
            dashboard.ActiveOrderFuelType = activeOrder.FuelType;
            dashboard.ActiveOrderQuantityGallons = activeOrder.QuantityGallons;
            dashboard.ActiveOrderStatus = activeOrder.Status;
            dashboard.NextDeliveryStatus = activeOrder.Status;
        }
        dashboard.LastUpdatedUtc = DateTime.UtcNow;
    }

    public override void Dispose()
    {
        DisposeRabbitMq();
        base.Dispose();
    }

    private static string? GetEventType(IDictionary<string, object>? headers)
    {
        if (headers?.TryGetValue("EventType", out var value) != true)
            return null;

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value?.ToString()
        };
    }

    private void DisposeRabbitMq()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
    }
}
