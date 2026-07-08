using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Fuel.Orders.Service.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private IConnection _connection;
    private IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"] ?? "admin",
            Password = configuration["RabbitMQ:Password"] ?? "ChangeMe123"
        };

        var retryPolicy = RetryPolicy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} connecting to RabbitMQ after {TimeSpan}s...", retryCount, timeSpan.TotalSeconds);
                });

        retryPolicy.Execute(() =>
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger.LogInformation("RabbitMQ connection established.");
        });
    }

    public void Publish<T>(string exchange, string routingKey, T message, string? correlationId = null)
    {
        _channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        if (correlationId != null)
            props.CorrelationId = correlationId;

        _channel.BasicPublish(exchange, routingKey, props, body);
        _logger.LogInformation("Published {Event} with correlation {CorrelationId}", typeof(T).Name, correlationId);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}