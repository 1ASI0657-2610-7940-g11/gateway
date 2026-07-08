namespace Fuel.Reporting.Service.Features.Home.Domain;

public class DashboardSummary
{
    public required string CompanyName { get; init; }
    public string? AvatarUrl { get; init; }
    public OrderSummary? ActiveOrder { get; init; }
    public DeliverySummary? NextDelivery { get; init; }
    public PaymentSummary? LastPayment { get; init; }
}

public class OrderSummary
{
    public required string FuelType { get; init; }
    public int QuantityGallons { get; init; }
    public required string Status { get; init; }
}

public class DeliverySummary
{
    public required string DateTimeText { get; init; }
    public required string Location { get; init; }
    public required string Status { get; init; }
}

public class PaymentSummary
{
    public required string AmountText { get; init; }
    public required string Method { get; init; }
    public required string Status { get; init; }
}