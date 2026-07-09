using Fuel.Payments.Service.Features.Payments.Domain;

namespace Fuel.Payments.Tests.Payments;

public class PaymentMethodTests
{
    [Fact]
    public void NewPaymentMethodRequest_ShouldStoreValuesCorrectly()
    {
        // Arrange
        var request = new NewPaymentMethodRequest
        {
            Brand = "Visa",
            CardNumber = "4111111111111234",
            Holder = "Ana Perez",
            Expires = "12/30"
        };

        // Act
        var brand = request.Brand;
        var cardNumber = request.CardNumber;

        // Assert
        Assert.Equal("Visa", brand);
        Assert.Equal("4111111111111234", cardNumber);
        Assert.Equal("Ana Perez", request.Holder);
        Assert.Equal("12/30", request.Expires);
    }

    [Fact]
    public void PaymentMethod_ShouldStoreMaskedCardCorrectly()
    {
        // Arrange
        var method = new PaymentMethod
        {
            Id = "PAY-001",
            Brand = "Visa",
            Masked = "**** 1234",
            Holder = "Ana Perez",
            Expires = "12/30",
            IsDefault = true
        };

        // Act
        var masked = method.Masked;

        // Assert
        Assert.Equal("**** 1234", masked);
        Assert.True(method.IsDefault);
    }

    [Fact]
    public void PaymentHistory_ShouldStoreAmountAndStatusCorrectly()
    {
        // Arrange
        var history = new PaymentHistory
        {
            Id = "HIS-001",
            Date = "Today",
            Description = "Pago de combustible",
            Amount = 2500,
            Currency = "PEN",
            Status = "Paid"
        };

        // Act
        var amount = history.Amount;
        var status = history.Status;

        // Assert
        Assert.Equal(2500, amount);
        Assert.Equal("Paid", status);
        Assert.Equal("PEN", history.Currency);
    }
}