namespace Fuel.Payments.Service.Features.Payments.Domain;

public class PaymentHistory
{
    public string Id { get; set; } = default!;
    public string Date { get; set; } = default!;
    public string Description { get; set; } = default!;
    public double Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Status { get; set; } = default!;
}