using Fuel.Orders.Service.Features.Orders.Api;
using Fuel.Orders.Service.Features.Orders.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fuel.Orders.Tests.Orders;

public class OrdersControllerTests
{
    [Fact]
    public async Task GetOrders_ShouldReturnOk_WithOrders()
    {
        // Arrange
        var repository = new FakeOrdersRepository();
        var controller = new OrdersController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetOrders();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var orders = Assert.IsAssignableFrom<IEnumerable<OrderSummary>>(okResult.Value);
        Assert.Single(orders);
    }

    [Fact]
    public async Task GetOrderDetail_ShouldReturnOk_WhenOrderExists()
    {
        // Arrange
        var repository = new FakeOrdersRepository();
        var controller = new OrdersController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetOrderDetail("ORD-001");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var order = Assert.IsType<OrderDetail>(okResult.Value);
        Assert.Equal("ORD-001", order.Id);
    }

    [Fact]
    public async Task GetOrderDetail_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var repository = new FakeOrdersRepository { ShouldReturnNull = true };
        var controller = new OrdersController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetOrderDetail("ORD-404");

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenFuelTypeIsEmpty()
    {
        // Arrange
        var repository = new FakeOrdersRepository();
        var controller = new OrdersController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new NewOrderRequest
        {
            FuelType = "",
            QuantityGallons = 500,
            Address = "Av. Siempre Viva 123",
            TimeWindow = "Manana"
        };

        // Act
        var result = await controller.CreateOrder(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var repository = new FakeOrdersRepository();
        var controller = new OrdersController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new NewOrderRequest
        {
            FuelType = "Diesel B5",
            QuantityGallons = 500,
            Address = "Av. Siempre Viva 123",
            TimeWindow = "Manana"
        };

        // Act
        var result = await controller.CreateOrder(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var order = Assert.IsType<OrderDetail>(createdResult.Value);
        Assert.Equal("Diesel B5", order.Product);
    }

    [Fact]
    public async Task UpdateOrderStatus_ShouldReturnOk_WhenOrderExists()
    {
        // Arrange
        var repository = new FakeOrdersRepository();
        var controller = new OrdersController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new UpdateOrderStatusRequest
        {
            Status = OrderStatus.Delivered,
            Comment = "Delivered successfully"
        };

        // Act
        var result = await controller.UpdateOrderStatus("ORD-001", request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var order = Assert.IsType<OrderDetail>(okResult.Value);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public async Task AssignVehicle_ShouldReturnOk_WhenOrderExists()
    {
        // Arrange
        var repository = new FakeOrdersRepository();
        var controller = new OrdersController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new AssignVehicleRequest
        {
            VehicleId = "VEH-001",
            VehiclePlate = "ABC-123",
            DriverName = "Carlos"
        };

        // Act
        var result = await controller.AssignVehicle("ORD-001", request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var order = Assert.IsType<OrderDetail>(okResult.Value);
        Assert.Equal("ABC-123", order.VehiclePlate);
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }

    private sealed class FakeOrdersRepository : IOrdersRepository
    {
        public bool ShouldReturnNull { get; set; }

        public Task<IEnumerable<OrderSummary>> GetOrdersAsync(string userId, OrderStatus? status = null)
        {
            IEnumerable<OrderSummary> orders = new List<OrderSummary>
            {
                new()
                {
                    Id = "ORD-001",
                    Code = "FT-001",
                    Status = status ?? OrderStatus.Created,
                    ScheduledAt = "Manana",
                    PlantName = "Planta Norte",
                    FuelType = "Diesel B5",
                    QuantityGallons = 500,
                    VehiclePlate = "ABC-123"
                }
            };

            return Task.FromResult(orders);
        }

        public Task<OrderDetail?> GetOrderDetailAsync(string userId, string id)
        {
            if (ShouldReturnNull)
            {
                return Task.FromResult<OrderDetail?>(null);
            }

            return Task.FromResult<OrderDetail?>(CreateOrderDetail(id));
        }

        public Task<OrderDetail> CreateOrderAsync(string userId, NewOrderRequest request)
        {
            var order = new OrderDetail
            {
                Id = "ORD-001",
                Code = "FT-001",
                Status = OrderStatus.Created,
                Product = request.FuelType,
                QuantityGallons = request.QuantityGallons,
                CreatedAt = "Today",
                CreatedDate = DateTime.UtcNow,
                Eta = request.TimeWindow,
                Plant = "Planta Norte",
                Address = request.Address,
                TimeWindow = request.TimeWindow
            };

            return Task.FromResult(order);
        }

        public Task<OrderDetail?> UpdateOrderStatusAsync(string userId, string id, UpdateOrderStatusRequest request)
        {
            if (ShouldReturnNull)
            {
                return Task.FromResult<OrderDetail?>(null);
            }

            var order = CreateOrderDetail(id);
            order.Status = request.Status;
            order.LastStatusComment = request.Comment;

            return Task.FromResult<OrderDetail?>(order);
        }

        public Task<OrderDetail?> AssignVehicleAsync(string userId, string id, AssignVehicleRequest request)
        {
            if (ShouldReturnNull)
            {
                return Task.FromResult<OrderDetail?>(null);
            }

            var order = CreateOrderDetail(id);
            order.VehicleId = request.VehicleId;
            order.VehiclePlate = request.VehiclePlate;
            order.DriverName = request.DriverName;

            return Task.FromResult<OrderDetail?>(order);
        }

        private static OrderDetail CreateOrderDetail(string id)
        {
            return new OrderDetail
            {
                Id = id,
                Code = "FT-001",
                Status = OrderStatus.Created,
                Product = "Diesel B5",
                QuantityGallons = 500,
                CreatedAt = "Today",
                CreatedDate = DateTime.UtcNow,
                Eta = "Manana",
                Plant = "Planta Norte",
                Address = "Av. Siempre Viva 123",
                TimeWindow = "Manana"
            };
        }
    }
}