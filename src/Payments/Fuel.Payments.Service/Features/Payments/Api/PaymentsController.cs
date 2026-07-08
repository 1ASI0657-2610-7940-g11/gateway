using Fuel.Payments.Service.Features.Payments.Domain;
using Fuel.Payments.Service.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Payments.Service.Features.Payments.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentsRepository _repository;

    public PaymentsController(IPaymentsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("methods")]
    public async Task<ActionResult<IEnumerable<PaymentMethod>>> GetPaymentMethods()
    {
        return Ok(await _repository.GetPaymentMethodsAsync(User.GetRequiredUserId()));
    }

    [HttpPost("methods")]
    public async Task<ActionResult<PaymentMethod>> AddPaymentMethod(
        [FromBody] NewPaymentMethodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand)
            || string.IsNullOrWhiteSpace(request.CardNumber)
            || string.IsNullOrWhiteSpace(request.Holder)
            || string.IsNullOrWhiteSpace(request.Expires))
            return BadRequest(new { message = "Completa todos los datos del método." });

        try
        {
            var method = await _repository.AddPaymentMethodAsync(
                User.GetRequiredUserId(), request);
            return CreatedAtAction(nameof(GetPaymentMethods), new { id = method.Id }, method);
        }
        catch (ArgumentException ex) when (ex.Message == "CARD_NUMBER_INVALID")
        {
            return BadRequest(new { message = "El número debe contener al menos cuatro dígitos." });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<PaymentHistory>>> GetPaymentHistory()
    {
        return Ok(await _repository.GetPaymentHistoryAsync(User.GetRequiredUserId()));
    }
}