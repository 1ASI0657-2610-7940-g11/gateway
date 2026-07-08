namespace Fuel.Orders.Service.Features.Orders.Domain;

public class AssignVehicleRequest
{
    public string VehicleId { get; set; } = default!;
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
}