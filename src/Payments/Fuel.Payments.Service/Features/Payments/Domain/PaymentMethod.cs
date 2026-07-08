namespace Fuel.Payments.Service.Features.Payments.Domain;

public class PaymentMethod
{
    public string Id { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string Masked { get; set; } = default!;
    public string Holder { get; set; } = default!;
    public string Expires { get; set; } = default!;
    public bool IsDefault { get; set; }
}