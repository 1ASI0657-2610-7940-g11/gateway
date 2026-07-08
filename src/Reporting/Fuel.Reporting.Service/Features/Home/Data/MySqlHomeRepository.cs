using Fuel.Reporting.Service.Features.Home.Domain;
using Fuel.Reporting.Service.Infrastructure.Cache;
using Fuel.Reporting.Service.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Reporting.Service.Features.Home.Data;

public sealed class MySqlHomeRepository : IHomeRepository
{
    private readonly ReportingDbContext _db;
    private readonly RedisCacheService _cache;

    public MySqlHomeRepository(ReportingDbContext db, RedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<DashboardSummary> GetDashboardAsync(string userId)
    {
        var cached = await _cache.GetAsync<DashboardSummary>($"dashboard:{userId}");
        if (cached != null) return cached;

        var dashboard = await _db.Dashboards.FindAsync(userId);
        if (dashboard == null)
            return new DashboardSummary { CompanyName = "FuelTrack", AvatarUrl = null, ActiveOrder = null, NextDelivery = null, LastPayment = null };

        var summary = new DashboardSummary
        {
            CompanyName = dashboard.CompanyName,
            AvatarUrl = dashboard.AvatarUrl,
            ActiveOrder = dashboard.ActiveOrderFuelType != null ? new OrderSummary
            {
                FuelType = dashboard.ActiveOrderFuelType,
                QuantityGallons = dashboard.ActiveOrderQuantityGallons ?? 0,
                Status = dashboard.ActiveOrderStatus ?? ""
            } : null,
            NextDelivery = dashboard.NextDeliveryDateTime != null ? new DeliverySummary
            {
                DateTimeText = dashboard.NextDeliveryDateTime,
                Location = dashboard.NextDeliveryLocation ?? "",
                Status = dashboard.NextDeliveryStatus ?? ""
            } : null,
            LastPayment = dashboard.LastPaymentAmount != null ? new PaymentSummary
            {
                AmountText = dashboard.LastPaymentAmount,
                Method = dashboard.LastPaymentMethod ?? "",
                Status = dashboard.LastPaymentStatus ?? ""
            } : null
        };

        await _cache.SetAsync($"dashboard:{userId}", summary, TimeSpan.FromSeconds(30));
        return summary;
    }
}