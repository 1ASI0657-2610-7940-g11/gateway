using Fuel.Reporting.Service.Features.Client.Domain;
using Fuel.Reporting.Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Reporting.Service.Features.Client.Data;

public sealed class MySqlClientRepository : IClientRepository
{
    private readonly ReportingDbContext _db;

    public MySqlClientRepository(ReportingDbContext db) => _db = db;

    public async Task<ClientKpis> GetClientKpisAsync(string userId)
    {
        var orders = await _db.OrderKpis.Where(x => x.UserId == userId).ToListAsync();
        var totalSpent = orders.Sum(x => x.Amount);
        return new ClientKpis
        {
            TotalOrders = orders.Count,
            ActiveOrders = orders.Count(x => x.Status != "Delivered" && x.Status != "Cancelled"),
            DeliveredOrders = orders.Count(x => x.Status == "Delivered"),
            PendingOrders = orders.Count(x => x.Status == "Scheduled" || x.Status == "Created"),
            CancelledOrders = orders.Count(x => x.Status == "Cancelled"),
            TotalGallons = orders.Sum(x => x.QuantityGallons),
            TotalSpent = totalSpent,
            AverageOrderAmount = orders.Count == 0 ? 0 : totalSpent / orders.Count,
            LastOrderDate = orders.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()?.CreatedAtUtc.ToString("yyyy-MM-dd"),
            NextDeliveryDate = null,
            OrdersByStatus = orders.GroupBy(x => x.Status).Select(g => new ClientKpiStatusItem
            {
                Status = g.Key,
                Count = g.Count()
            }).ToList()
        };
    }
}