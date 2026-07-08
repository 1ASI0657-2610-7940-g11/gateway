using Fuel.Reporting.Service.Features.Provider.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Reporting.Service.Features.Provider.Api;

[ApiController]
[Route("api/provider")]
public class ProviderController : ControllerBase
{
    private readonly IProviderRepository _repository;

    public ProviderController(IProviderRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("sales-report")]
    public async Task<ActionResult<SalesReport>> GetSalesReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var report = await _repository.GetSalesReportAsync(fromDate, toDate);
        return Ok(report);
    }

    [HttpGet("sales-chart")]
    public async Task<ActionResult<IEnumerable<SalesChartPoint>>> GetSalesChart(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var chart = await _repository.GetSalesChartAsync(fromDate, toDate);
        return Ok(chart);
    }

    [HttpGet("sales-report/pdf")]
    public async Task<IActionResult> DownloadSalesReportPdf(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var pdf = await _repository.GetSalesReportPdfAsync(fromDate, toDate);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }
}