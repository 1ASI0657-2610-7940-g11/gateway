namespace Fuel.Payments.Service.Infrastructure.Data.Entities;

public sealed class PaymentHistoryEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = default!;
    public DateTime DateUtc { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PEN";
    public string Status { get; set; } = default!;
}