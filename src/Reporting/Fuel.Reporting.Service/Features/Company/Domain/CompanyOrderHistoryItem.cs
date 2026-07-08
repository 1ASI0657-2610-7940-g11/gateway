namespace Fuel.Reporting.Service.Features.Company.Domain;

public class CompanyOrderHistoryItem
{
    public string OrderId { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int QuantityGallons { get; set; }
    public string Date { get; set; } = default!;
    public double Amount { get; set; }
}