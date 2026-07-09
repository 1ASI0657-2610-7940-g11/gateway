using Fuel.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Fuel.Payments.Service.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _syncRoot = new();
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:Username"] ?? "admin",
            Password = configuration["RabbitMQ:Password"] ?? "ChangeMe123",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3),
            SocketReadTimeout = TimeSpan.FromSeconds(3),
            SocketWriteTimeout = TimeSpan.FromSeconds(3),
            AutomaticRecoveryEnabled = true
        };
    }

    public void Publish<T>(string exchange, string routingKey, T message, string? correlationId = null)
    {
        var channel = GetOrCreateChannel();
        if (channel is null)
        {
            _logger.LogWarning("RabbitMQ unavailable. Skipping publish of {Event}.", typeof(T).Name);
            return;
        }

        channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object> { ["EventType"] = typeof(T).Name };
        if (correlationId != null)
            props.CorrelationId = correlationId;

        channel.BasicPublish(exchange, routingKey, props, body);
        _logger.LogInformation("Published {Event} with correlation {CorrelationId}", typeof(T).Name, correlationId);
    }

    private IModel? GetOrCreateChannel()
    {
        if (_channel?.IsOpen == true) return _channel;

        lock (_syncRoot)
        {
            if (_channel?.IsOpen == true) return _channel;

            try
            {
                _connection?.Dispose();
                _connection = _factory.CreateConnection();
                _channel = _connection.CreateModel();
                _logger.LogInformation("RabbitMQ connection established.");
                return _channel;
            }
            catch (Exception ex) when (ex is BrokerUnreachableException or SocketException or TimeoutException)
            {
                _logger.LogWarning(ex, "RabbitMQ connection failed.");
                return null;
            }
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
