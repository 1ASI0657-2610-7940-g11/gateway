namespace Fuel.Reporting.Service.Features.Home.Domain;

public interface IHomeRepository
{
    Task<DashboardSummary> GetDashboardAsync(string userId);
}