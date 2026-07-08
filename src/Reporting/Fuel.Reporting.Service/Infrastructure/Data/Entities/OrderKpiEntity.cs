namespace Fuel.Reporting.Service.Infrastructure.Data.Entities;

public sealed class OrderKpiEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = default!;
    public string OrderCode { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int QuantityGallons { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}