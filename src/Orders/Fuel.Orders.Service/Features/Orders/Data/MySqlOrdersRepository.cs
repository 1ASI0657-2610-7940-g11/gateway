using Fuel.Events;
using Fuel.Orders.Service.Features.Orders.Domain;
using Fuel.Orders.Service.Infrastructure.Data;
using Fuel.Orders.Service.Infrastructure.Data.Entities;
using Fuel.Orders.Service.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Orders.Service.Features.Orders.Data;

public sealed class MySqlOrdersRepository : IOrdersRepository
{
    private readonly OrdersDbContext _db;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MySqlOrdersRepository(OrdersDbContext db,
        IMessagePublisher messagePublisher,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _messagePublisher = messagePublisher;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<OrderSummary>> GetOrdersAsync(string userId, OrderStatus? status = null)
    {
        var query = _db.Orders.Where(x => x.UserId == userId);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value.ToString());
        return await query.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => ToSummary(x))
            .ToListAsync();
    }

    public async Task<OrderDetail?> GetOrderDetailAsync(string userId, string id)
    {
        var entity = await _db.Orders
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id);
        return entity is null ? null : ToDetail(entity);
    }

    public async Task<OrderDetail> CreateOrderAsync(string userId, NewOrderRequest request)
    {
        var now = DateTime.UtcNow;
        var entity = new OrderEntity
        {
            UserId = userId,
            Code = $"FT-{now:yyyy}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            Status = OrderStatus.Scheduled.ToString(),
            Product = request.FuelType.Trim(),
            QuantityGallons = request.QuantityGallons,
            Address = request.Address.Trim(),
            TimeWindow = request.TimeWindow.Trim(),
            Notes = request.Notes?.Trim()
        };
        _db.Orders.Add(entity);
        await _db.SaveChangesAsync();

        // Publicar evento
        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId();
        _messagePublisher.Publish("order-events", "", new OrderCreatedEvent(
            entity.Id, userId, entity.Code, entity.Product, entity.QuantityGallons,
            entity.Address, entity.TimeWindow, entity.Notes), correlationId);

        return ToDetail(entity);
    }

    public async Task<OrderDetail?> UpdateOrderAsync(string userId, string id, UpdateOrderRequest request)
    {
        var entity = await _db.Orders.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id);
        if (entity is null) return null;

        entity.Product = request.FuelType.Trim();
        entity.QuantityGallons = request.QuantityGallons;
        entity.Address = request.Address.Trim();
        entity.TimeWindow = request.TimeWindow.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.Eta = "Pedido actualizado. Entrega pendiente de programación";

        await _db.SaveChangesAsync();
        return ToDetail(entity);
    }

    public async Task<bool> DeleteOrderAsync(string userId, string id)
    {
        var entity = await _db.Orders.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id);
        if (entity is null) return false;

        _db.Orders.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<OrderDetail?> UpdateOrderStatusAsync(string userId, string id, UpdateOrderStatusRequest request)
    {
        var entity = await _db.Orders.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id);
        if (entity is null) return null;

        entity.Status = request.Status.ToString();
        entity.LastStatusComment = request.Comment?.Trim();
        entity.Eta = request.Status switch
        {
            OrderStatus.Created => "Pedido creado",
            OrderStatus.Scheduled => "Pedido programado",
            OrderStatus.OnRoute => "Pedido en ruta",
            OrderStatus.Delivered => $"Entregado el {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC",
            OrderStatus.Cancelled => $"Cancelado el {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC",
            _ => entity.Eta
        };
        await _db.SaveChangesAsync();

        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId();
        _messagePublisher.Publish("order-events", "", new OrderStatusUpdatedEvent(
            entity.Id, userId, entity.Code, entity.Status, request.Comment), correlationId);

        return ToDetail(entity);
    }

    public async Task<OrderDetail?> AssignVehicleAsync(string userId, string id, AssignVehicleRequest request)
    {
        var entity = await _db.Orders.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id);
        if (entity is null) return null;

        entity.VehicleId = request.VehicleId.Trim();
        entity.VehiclePlate = request.VehiclePlate?.Trim();
        entity.DriverName = request.DriverName?.Trim();
        if (entity.Status == OrderStatus.Created.ToString())
        {
            entity.Status = OrderStatus.Scheduled.ToString();
            entity.Eta = "Vehículo asignado. Entrega pendiente de programación";
        }
        await _db.SaveChangesAsync();

        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId();
        _messagePublisher.Publish("order-events", "", new VehicleAssignedEvent(
            entity.Id, userId, entity.Code, entity.VehicleId,
            entity.VehiclePlate, entity.DriverName), correlationId);

        return ToDetail(entity);
    }

    private static OrderSummary ToSummary(OrderEntity o) => new()
    {
        Id = o.Id,
        Code = o.Code,
        Status = ParseStatus(o.Status),
        ScheduledAt = o.Eta,
        PlantName = o.Plant,
        FuelType = o.Product,
        QuantityGallons = o.QuantityGallons,
        VehiclePlate = o.VehiclePlate
    };

    private static OrderDetail ToDetail(OrderEntity o) => new()
    {
        Id = o.Id,
        Code = o.Code,
        Status = ParseStatus(o.Status),
        Product = o.Product,
        QuantityGallons = o.QuantityGallons,
        CreatedAt = $"Creado el {o.CreatedAtUtc:dd/MM/yyyy HH:mm} UTC",
        CreatedDate = o.CreatedAtUtc,
        Eta = o.Eta,
        Plant = o.Plant,
        Address = o.Address,
        TimeWindow = o.TimeWindow,
        Notes = o.Notes,
        PaymentMethod = o.PaymentMethod,
        Amount = o.Amount.HasValue ? (double)o.Amount.Value : null,
        VehicleId = o.VehicleId,
        VehiclePlate = o.VehiclePlate,
        DriverName = o.DriverName,
        LastStatusComment = o.LastStatusComment
    };

    private static OrderStatus ParseStatus(string? status)
    {
        return Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : OrderStatus.Scheduled;
    }
}
