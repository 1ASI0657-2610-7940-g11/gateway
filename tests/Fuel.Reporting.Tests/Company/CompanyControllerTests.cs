using Fuel.Reporting.Service.Features.Company.Api;
using Fuel.Reporting.Service.Features.Company.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Reporting.Tests.Company;

public class CompanyControllerTests
{
    [Fact]
    public async Task GetCompanyDetail_ShouldReturnOk_WhenCompanyExists()
    {
        // Arrange
        var repository = new FakeCompanyRepository();
        var controller = new CompanyController(repository);

        // Act
        var result = await controller.GetCompanyDetail("COM-001");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var company = Assert.IsType<CompanyDetail>(okResult.Value);
        Assert.Equal("COM-001", company.Id);
    }

    [Fact]
    public async Task GetCompanyDetail_ShouldReturnNotFound_WhenCompanyDoesNotExist()
    {
        // Arrange
        var repository = new FakeCompanyRepository { ShouldReturnNull = true };
        var controller = new CompanyController(repository);

        // Act
        var result = await controller.GetCompanyDetail("COM-404");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCompanyDetail_ShouldIncludeOrderHistory()
    {
        // Arrange
        var repository = new FakeCompanyRepository();
        var controller = new CompanyController(repository);

        // Act
        var result = await controller.GetCompanyDetail("COM-001");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var company = Assert.IsType<CompanyDetail>(okResult.Value);
        Assert.Single(company.OrderHistory);
    }

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public bool ShouldReturnNull { get; set; }

        public Task<CompanyDetail?> GetCompanyDetailAsync(string id)
        {
            if (ShouldReturnNull)
            {
                return Task.FromResult<CompanyDetail?>(null);
            }

            return Task.FromResult<CompanyDetail?>(new CompanyDetail
            {
                Id = id,
                Name = "Mi Empresa",
                Ruc = "123",
                ContactName = "Ana",
                ContactEmail = "ana@test.com",
                Phone = "999",
                Address = "Av. Siempre Viva 123",
                Status = "Active",
                TotalOrders = 1,
                TotalSpent = 2500,
                OrderHistory = new List<CompanyOrderHistoryItem>
                {
                    new()
                    {
                        OrderId = "ORD-001",
                        Code = "FT-001",
                        Status = "Delivered",
                        FuelType = "Diesel B5",
                        QuantityGallons = 500,
                        Date = "2026-07-01",
                        Amount = 2500
                    }
                }
            });
        }
    }
}