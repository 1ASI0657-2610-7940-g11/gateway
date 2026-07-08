namespace Fuel.Events;

// Eventos de IAM
public record UserRegisteredEvent(string UserId, string FullName, string Email);
public record ProfileUpdatedEvent(string UserId, string FullName, string? AvatarUrl);

// Eventos de Pedidos
public record OrderCreatedEvent(string OrderId, string UserId, string Code, string FuelType,
    int QuantityGallons, string Address, string TimeWindow, string? Notes);
public record OrderStatusUpdatedEvent(string OrderId, string UserId, string Code, 
    string NewStatus, string? Comment);
public record VehicleAssignedEvent(string OrderId, string UserId, string Code, 
    string VehicleId, string? VehiclePlate, string? DriverName);

// Eventos de Payments
public record PaymentMethodAddedEvent(string PaymentMethodId, string UserId, string Brand, string Last4);
public record PaymentCompletedEvent(string PaymentHistoryId, string UserId, decimal Amount, string Currency, string Description);

// Eventos de Reporting 
public record DashboardUpdatedEvent(string UserId);