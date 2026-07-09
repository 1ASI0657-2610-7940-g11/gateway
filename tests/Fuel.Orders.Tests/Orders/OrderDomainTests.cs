using Fuel.Orders.Service.Features.Orders.Domain;

namespace Fuel.Orders.Tests.Orders;

public class OrderDomainTests
{
    [Fact]
    public void NewOrderRequest_ShouldStoreValuesCorrectly()
    {
        // Arrange
        var request = new NewOrderRequest
        {
            FuelType = "Diesel B5",
            QuantityGallons = 500,
            Address = "Av. Siempre Viva 123",
            TimeWindow = "Manana",
            Notes = "Urgente"
        };

        // Act
        var fuelType = request.FuelType;
        var quantity = request.QuantityGallons;

        // Assert
        Assert.Equal("Diesel B5", fuelType);
        Assert.Equal(500, quantity);
        Assert.Equal("Urgente", request.Notes);
    }

    [Fact]
    public void OrderDetail_ShouldAllowDeliveredStatus()
    {
        // Arrange
        var order = new OrderDetail
        {
            Id = "ORD-001",
            Code = "FT-001",
            Status = OrderStatus.Delivered,
            Product = "Diesel B5",
            QuantityGallons = 500,
            CreatedAt = "Today",
            CreatedDate = DateTime.UtcNow,
            Eta = "Manana",
            Plant = "Planta Norte",
            Address = "Av. Siempre Viva 123",
            TimeWindow = "Manana"
        };

        // Act
        var status = order.Status;

        // Assert
        Assert.Equal(OrderStatus.Delivered, status);
    }

    [Fact]
    public void AssignVehicleRequest_ShouldStoreVehicleDataCorrectly()
    {
        // Arrange
        var request = new AssignVehicleRequest
        {
            VehicleId = "VEH-001",
            VehiclePlate = "ABC-123",
            DriverName = "Carlos"
        };

        // Act
        var vehiclePlate = request.VehiclePlate;

        // Assert
        Assert.Equal("ABC-123", vehiclePlate);
        Assert.Equal("Carlos", request.DriverName);
    }
}