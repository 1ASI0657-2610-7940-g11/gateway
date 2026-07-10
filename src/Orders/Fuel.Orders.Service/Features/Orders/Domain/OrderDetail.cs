namespace Fuel.Orders.Service.Features.Orders.Domain;

public class OrderDetail
{
    public string Id { get; set; } = default!;
    public string Code { get; set; } = default!;
    public OrderStatus Status { get; set; }
    public string Product { get; set; } = default!;
    public int QuantityGallons { get; set; }
    public string CreatedAt { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
    public string Eta { get; set; } = default!;
    public string Plant { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string TimeWindow { get; set; } = default!;
    public string? Notes { get; set; }
    public string? PaymentMethod { get; set; }
    public double? Amount { get; set; }
    public string? VehicleId { get; set; }
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? LastStatusComment { get; set; }
}
