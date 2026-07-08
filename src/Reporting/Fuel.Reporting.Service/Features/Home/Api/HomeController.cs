using Fuel.Reporting.Service.Features.Home.Domain;
using Fuel.Reporting.Service.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Reporting.Service.Features.Home.Api;

[ApiController]
[Route("api/client")]
public sealed class HomeController : ControllerBase
{
    private readonly IHomeRepository _repository;

    public HomeController(IHomeRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummary>> GetDashboard()
    {
        return Ok(await _repository.GetDashboardAsync(User.GetRequiredUserId()));
    }
}
