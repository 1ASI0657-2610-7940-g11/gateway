namespace Fuel.Reporting.Service.Features.Company.Domain;

public class CompanyDetail
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Ruc { get; set; } = default!;
    public string ContactName { get; set; } = default!;
    public string ContactEmail { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int TotalOrders { get; set; }
    public double TotalSpent { get; set; }
    public IEnumerable<CompanyOrderHistoryItem> OrderHistory { get; set; } = new List<CompanyOrderHistoryItem>();
}