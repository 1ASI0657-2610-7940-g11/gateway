using Fuel.Reporting.Service.Features.Client.Domain;
using Fuel.Reporting.Service.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Reporting.Service.Features.Client.Api;

[ApiController]
[Route("api/client")]
public sealed class ClientController : ControllerBase
{
    private readonly IClientRepository _repository;

    public ClientController(IClientRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<ClientKpis>> GetClientKpis()
    {
        return Ok(await _repository.GetClientKpisAsync(User.GetRequiredUserId()));
    }
}
