using Fuel.Orders.Service.Features.Orders.Domain;
using Fuel.Orders.Service.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Orders.Service.Features.Orders.Api;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrdersRepository _repository;

    public OrdersController(IOrdersRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderSummary>>> GetOrders(
        [FromQuery] OrderStatus? status = null)
    {
        return Ok(await _repository.GetOrdersAsync(User.GetRequiredUserId(), status));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetail>> GetOrderDetail(string id)
    {
        var order = await _repository.GetOrderDetailAsync(User.GetRequiredUserId(), id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetail>> CreateOrder([FromBody] NewOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FuelType)
            || request.QuantityGallons <= 0
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.TimeWindow))
        {
            return BadRequest(new { message = "Completa combustible, cantidad, dirección y horario." });
        }

        var created = await _repository.CreateOrderAsync(User.GetRequiredUserId(), request);
        return CreatedAtAction(nameof(GetOrderDetail), new { id = created.Id }, created);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<OrderDetail>> UpdateOrderStatus(
        string id, [FromBody] UpdateOrderStatusRequest request)
    {
        var updated = await _repository.UpdateOrderStatusAsync(User.GetRequiredUserId(), id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id}/vehicle")]
    public async Task<ActionResult<OrderDetail>> AssignVehicle(
        string id, [FromBody] AssignVehicleRequest request)
    {
        var updated = await _repository.AssignVehicleAsync(User.GetRequiredUserId(), id, request);
        return updated is null ? NotFound() : Ok(updated);
    }
}
