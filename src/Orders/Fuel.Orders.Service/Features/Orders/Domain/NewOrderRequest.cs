namespace Fuel.Orders.Service.Features.Orders.Domain;

public class NewOrderRequest
{
    public string FuelType { get; set; } = default!;
    public int QuantityGallons { get; set; }
    public string Address { get; set; } = default!;
    public string TimeWindow { get; set; } = default!;
    public string? Notes { get; set; }
}