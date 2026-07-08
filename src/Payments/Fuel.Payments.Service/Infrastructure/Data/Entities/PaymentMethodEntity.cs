namespace Fuel.Payments.Service.Infrastructure.Data.Entities;

public sealed class PaymentMethodEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string Last4 { get; set; } = default!;
    public string Holder { get; set; } = default!;
    public string Expires { get; set; } = default!;
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}