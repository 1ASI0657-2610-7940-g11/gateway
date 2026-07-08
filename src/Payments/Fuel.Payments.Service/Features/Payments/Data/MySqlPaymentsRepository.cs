using Fuel.Events;
using Fuel.Payments.Service.Features.Payments.Domain;
using Fuel.Payments.Service.Infrastructure.Data;
using Fuel.Payments.Service.Infrastructure.Data.Entities;
using Fuel.Payments.Service.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Payments.Service.Features.Payments.Data;

public sealed class MySqlPaymentsRepository : IPaymentsRepository
{
    private readonly PaymentsDbContext _db;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MySqlPaymentsRepository(
        PaymentsDbContext db,
        IMessagePublisher messagePublisher,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _messagePublisher = messagePublisher;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync(string userId)
    {
        return await _db.PaymentMethods
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new PaymentMethod
            {
                Id = x.Id,
                Brand = x.Brand,
                Masked = $"**** **** **** {x.Last4}",
                Holder = x.Holder,
                Expires = x.Expires,
                IsDefault = x.IsDefault
            })
            .ToListAsync();
    }

    public async Task<PaymentMethod> AddPaymentMethodAsync(string userId, NewPaymentMethodRequest request)
    {
        var digits = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            throw new ArgumentException("CARD_NUMBER_INVALID");

        var hasMethods = await _db.PaymentMethods.AnyAsync(x => x.UserId == userId);
        var entity = new PaymentMethodEntity
        {
            UserId = userId,
            Brand = request.Brand.Trim(),
            Last4 = digits[^4..],
            Holder = request.Holder.Trim(),
            Expires = request.Expires.Trim(),
            IsDefault = !hasMethods
        };
        _db.PaymentMethods.Add(entity);
        await _db.SaveChangesAsync();

        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId();
        _messagePublisher.Publish("payment-events", "", new PaymentMethodAddedEvent(
            entity.Id, userId, entity.Brand, entity.Last4), correlationId);

        return new PaymentMethod
        {
            Id = entity.Id,
            Brand = entity.Brand,
            Masked = $"**** **** **** {entity.Last4}",
            Holder = entity.Holder,
            Expires = entity.Expires,
            IsDefault = entity.IsDefault
        };
    }

    public async Task<IEnumerable<PaymentHistory>> GetPaymentHistoryAsync(string userId)
    {
        return await _db.PaymentHistory
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.DateUtc)
            .Select(x => new PaymentHistory
            {
                Id = x.Id,
                Date = $"{x.DateUtc:dd/MM/yyyy HH:mm} UTC",
                Description = x.Description,
                Amount = (double)x.Amount,
                Currency = x.Currency,
                Status = x.Status
            })
            .ToListAsync();
    }
}