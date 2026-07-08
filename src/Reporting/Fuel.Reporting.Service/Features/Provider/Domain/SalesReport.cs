namespace Fuel.Reporting.Service.Features.Provider.Domain;

public class SalesReport
{
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int PendingOrders { get; set; }
    public int TotalGallons { get; set; }
    public string Period { get; set; } = default!;
    public List<SalesReportItem> Items { get; set; } = new();
}

public class SalesReportItem
{
    public string OrderCode { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public string FuelType { get; set; } = default!;
    public int QuantityGallons { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = default!;
    public string Date { get; set; } = default!;
}