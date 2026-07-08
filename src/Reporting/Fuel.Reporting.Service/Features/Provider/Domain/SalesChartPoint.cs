namespace Fuel.Reporting.Service.Features.Provider.Domain;

public class SalesChartPoint
{
    public string Label { get; set; } = default!;
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public int TotalGallons { get; set; }
}