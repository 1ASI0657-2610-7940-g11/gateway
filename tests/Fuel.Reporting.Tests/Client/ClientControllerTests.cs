using Fuel.Reporting.Service.Features.Client.Api;
using Fuel.Reporting.Service.Features.Client.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fuel.Reporting.Tests.Client;

public class ClientControllerTests
{
    [Fact]
    public async Task GetClientKpis_ShouldReturnOk_WithClientKpis()
    {
        // Arrange
        var repository = new FakeClientRepository();
        var controller = new ClientController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetClientKpis();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var kpis = Assert.IsType<ClientKpis>(okResult.Value);
        Assert.Equal(10, kpis.TotalOrders);
        Assert.Equal(5000, kpis.TotalGallons);
    }

    [Fact]
    public async Task GetClientKpis_ShouldReturnOrdersByStatus()
    {
        // Arrange
        var repository = new FakeClientRepository();
        var controller = new ClientController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetClientKpis();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var kpis = Assert.IsType<ClientKpis>(okResult.Value);
        Assert.Contains(kpis.OrdersByStatus, item => item.Status == "Delivered" && item.Count == 7);
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

    private sealed class FakeClientRepository : IClientRepository
    {
        public Task<ClientKpis> GetClientKpisAsync(string userId)
        {
            return Task.FromResult(new ClientKpis
            {
                TotalOrders = 10,
                ActiveOrders = 2,
                DeliveredOrders = 7,
                PendingOrders = 1,
                CancelledOrders = 0,
                TotalGallons = 5000,
                TotalSpent = 25000,
                AverageOrderAmount = 2500,
                LastOrderDate = "2026-07-01",
                NextDeliveryDate = "2026-07-10",
                OrdersByStatus = new List<ClientKpiStatusItem>
                {
                    new() { Status = "Delivered", Count = 7 },
                    new() { Status = "Active", Count = 2 },
                    new() { Status = "Pending", Count = 1 }
                }
            });
        }
    }
}