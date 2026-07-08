namespace Fuel.Orders.Service.Features.Orders.Domain;

public class OrderSummary
{
    public string Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public OrderStatus Status { get; set; }
    public string ScheduledAt { get; set; } = default!;
    public string PlantName { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int QuantityGallons { get; set; }
    public string? VehiclePlate { get; set; }
}