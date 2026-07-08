namespace Fuel.Orders.Service.Features.Orders.Domain;

public enum OrderStatus
{
    Created,
    Scheduled,
    OnRoute,
    Delivered,
    Cancelled
}