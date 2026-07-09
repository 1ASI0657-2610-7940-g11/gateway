using Fuel.Reporting.Service.Features.Provider.Api;
using Fuel.Reporting.Service.Features.Provider.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Reporting.Tests.Provider;

public class ProviderControllerTests
{
    [Fact]
    public async Task GetSalesReport_ShouldReturnOk_WithSalesReport()
    {
        // Arrange
        var repository = new FakeProviderRepository();
        var controller = new ProviderController(repository);

        // Act
        var result = await controller.GetSalesReport();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var report = Assert.IsType<SalesReport>(okResult.Value);
        Assert.Equal(2500, report.TotalSales);
        Assert.Single(report.Items);
    }

    [Fact]
    public async Task GetSalesChart_ShouldReturnOk_WithChartPoints()
    {
        // Arrange
        var repository = new FakeProviderRepository();
        var controller = new ProviderController(repository);

        // Act
        var result = await controller.GetSalesChart();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var chart = Assert.IsAssignableFrom<IEnumerable<SalesChartPoint>>(okResult.Value);
        Assert.Single(chart);
    }

    [Fact]
    public async Task DownloadSalesReportPdf_ShouldReturnFileResult()
    {
        // Arrange
        var repository = new FakeProviderRepository();
        var controller = new ProviderController(repository);

        // Act
        var result = await controller.DownloadSalesReportPdf();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("sales-report.pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task GetSalesReport_ShouldPassDateFiltersToRepository()
    {
        // Arrange
        var repository = new FakeProviderRepository();
        var controller = new ProviderController(repository);
        var fromDate = new DateTime(2026, 7, 1);
        var toDate = new DateTime(2026, 7, 31);

        // Act
        await controller.GetSalesReport(fromDate, toDate);

        // Assert
        Assert.Equal(fromDate, repository.LastFromDate);
        Assert.Equal(toDate, repository.LastToDate);
    }

    private sealed class FakeProviderRepository : IProviderRepository
    {
        public DateTime? LastFromDate { get; private set; }
        public DateTime? LastToDate { get; private set; }

        public Task<SalesReport> GetSalesReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            LastFromDate = fromDate;
            LastToDate = toDate;

            return Task.FromResult(new SalesReport
            {
                TotalSales = 2500,
                TotalOrders = 1,
                DeliveredOrders = 1,
                PendingOrders = 0,
                TotalGallons = 500,
                Period = "July 2026",
                Items = new List<SalesReportItem>
                {
                    new()
                    {
                        OrderCode = "FT-001",
                        ClientName = "Mi Empresa",
                        FuelType = "Diesel B5",
                        QuantityGallons = 500,
                        Amount = 2500,
                        Status = "Delivered",
                        Date = "2026-07-01"
                    }
                }
            });
        }

        public Task<IEnumerable<SalesChartPoint>> GetSalesChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            IEnumerable<SalesChartPoint> chart = new List<SalesChartPoint>
            {
                new()
                {
                    Label = "July",
                    TotalSales = 2500,
                    TotalOrders = 1,
                    TotalGallons = 500
                }
            };

            return Task.FromResult(chart);
        }

        public Task<PdfReportResult> GetSalesReportPdfAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            return Task.FromResult(new PdfReportResult
            {
                FileName = "sales-report.pdf",
                ContentType = "application/pdf",
                Content = new byte[] { 1, 2, 3 }
            });
        }
    }
}