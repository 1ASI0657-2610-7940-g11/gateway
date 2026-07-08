namespace Fuel.Identity.Service.Infrastructure.Messaging;

public interface IMessagePublisher
{
    void Publish<T>(string exchange, string routingKey, T message, string? correlationId = null);
}