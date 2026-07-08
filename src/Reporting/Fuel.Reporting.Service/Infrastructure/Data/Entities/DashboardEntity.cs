namespace Fuel.Reporting.Service.Infrastructure.Data.Entities;

public sealed class DashboardEntity
{
    public string UserId { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public string? ActiveOrderFuelType { get; set; }
    public int? ActiveOrderQuantityGallons { get; set; }
    public string? ActiveOrderStatus { get; set; }
    public string? NextDeliveryDateTime { get; set; }
    public string? NextDeliveryLocation { get; set; }
    public string? NextDeliveryStatus { get; set; }
    public string? LastPaymentAmount { get; set; }
    public string? LastPaymentMethod { get; set; }
    public string? LastPaymentStatus { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}