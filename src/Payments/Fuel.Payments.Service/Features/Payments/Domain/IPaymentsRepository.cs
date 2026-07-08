namespace Fuel.Payments.Service.Features.Payments.Domain;

public interface IPaymentsRepository
{
    Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync(string userId);
    Task<PaymentMethod> AddPaymentMethodAsync(string userId, NewPaymentMethodRequest request);
    Task<IEnumerable<PaymentHistory>> GetPaymentHistoryAsync(string userId);
}