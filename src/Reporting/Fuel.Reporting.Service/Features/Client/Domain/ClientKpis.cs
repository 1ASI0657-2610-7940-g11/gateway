namespace Fuel.Reporting.Service.Features.Client.Domain;

public class ClientKpis
{
    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int TotalGallons { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public string? LastOrderDate { get; set; }
    public string? NextDeliveryDate { get; set; }
    public IEnumerable<ClientKpiStatusItem> OrdersByStatus { get; set; } = new List<ClientKpiStatusItem>();
}

public class ClientKpiStatusItem
{
    public string Status { get; set; } = default!;
    public int Count { get; set; }
}