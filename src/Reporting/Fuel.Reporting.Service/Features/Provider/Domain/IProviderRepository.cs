namespace Fuel.Reporting.Service.Features.Provider.Domain;

public interface IProviderRepository
{
    Task<SalesReport> GetSalesReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<SalesChartPoint>> GetSalesChartAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<PdfReportResult> GetSalesReportPdfAsync(DateTime? fromDate = null, DateTime? toDate = null);
}