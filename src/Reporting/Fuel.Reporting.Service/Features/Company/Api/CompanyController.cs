using Fuel.Reporting.Service.Features.Company.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Reporting.Service.Features.Company.Api;

[ApiController]
[Route("api/company")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyRepository _repository;

    public CompanyController(ICompanyRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyDetail>> GetCompanyDetail(string id)
    {
        var company = await _repository.GetCompanyDetailAsync(id);
        if (company == null) return NotFound(new { message = "Company not found" });
        return Ok(company);
    }
}