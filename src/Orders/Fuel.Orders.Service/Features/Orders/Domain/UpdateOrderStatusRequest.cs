namespace Fuel.Orders.Service.Features.Orders.Domain;

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
    public string? Comment { get; set; }
}