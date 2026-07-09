using Fuel.Reporting.Service.Features.Home.Api;
using Fuel.Reporting.Service.Features.Home.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fuel.Reporting.Tests.Home;

public class HomeControllerTests
{
    [Fact]
    public async Task GetDashboard_ShouldReturnOk_WithDashboardSummary()
    {
        // Arrange
        var repository = new FakeHomeRepository();
        var controller = new HomeController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetDashboard();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dashboard = Assert.IsType<DashboardSummary>(okResult.Value);
        Assert.Equal("Mi Empresa", dashboard.CompanyName);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnActiveOrder()
    {
        // Arrange
        var repository = new FakeHomeRepository();
        var controller = new HomeController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetDashboard();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dashboard = Assert.IsType<DashboardSummary>(okResult.Value);
        Assert.NotNull(dashboard.ActiveOrder);
        Assert.Equal("Diesel B5", dashboard.ActiveOrder.FuelType);
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId)
        };

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    private sealed class FakeHomeRepository : IHomeRepository
    {
        public Task<DashboardSummary> GetDashboardAsync(string userId)
        {
            return Task.FromResult(new DashboardSummary
            {
                CompanyName = "Mi Empresa",
                AvatarUrl = null,
                ActiveOrder = new OrderSummary
                {
                    FuelType = "Diesel B5",
                    QuantityGallons = 500,
                    Status = "Active"
                },
                NextDelivery = new DeliverySummary
                {
                    DateTimeText = "Manana 9:00 AM",
                    Location = "Av. Siempre Viva 123",
                    Status = "Scheduled"
                },
                LastPayment = new PaymentSummary
                {
                    AmountText = "S/ 2500",
                    Method = "Visa **** 1234",
                    Status = "Paid"
                }
            });
        }
    }
}