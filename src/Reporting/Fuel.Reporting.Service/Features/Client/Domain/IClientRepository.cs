using Fuel.Reporting.Service.Features.Client.Domain;

namespace Fuel.Reporting.Service.Features.Client.Domain;

public interface IClientRepository
{
    Task<ClientKpis> GetClientKpisAsync(string userId);
}