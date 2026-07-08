namespace Fuel.Orders.Service.Features.Orders.Domain;

public interface IOrdersRepository
{
    Task<IEnumerable<OrderSummary>> GetOrdersAsync(string userId, OrderStatus? status = null);
    Task<OrderDetail?> GetOrderDetailAsync(string userId, string id);
    Task<OrderDetail> CreateOrderAsync(string userId, NewOrderRequest request);
    Task<OrderDetail?> UpdateOrderStatusAsync(string userId, string id, UpdateOrderStatusRequest request);
    Task<OrderDetail?> AssignVehicleAsync(string userId, string id, AssignVehicleRequest request);
}