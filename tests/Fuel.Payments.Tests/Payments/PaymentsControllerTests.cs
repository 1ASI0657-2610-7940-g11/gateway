using Fuel.Payments.Service.Features.Payments.Api;
using Fuel.Payments.Service.Features.Payments.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fuel.Payments.Tests.Payments;

public class PaymentsControllerTests
{
    [Fact]
    public async Task GetPaymentMethods_ShouldReturnOk_WithMethods()
    {
        // Arrange
        var repository = new FakePaymentsRepository();
        var controller = new PaymentsController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetPaymentMethods();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var methods = Assert.IsAssignableFrom<IEnumerable<PaymentMethod>>(okResult.Value);
        Assert.Single(methods);
    }

    [Fact]
    public async Task AddPaymentMethod_ShouldReturnBadRequest_WhenBrandIsEmpty()
    {
        // Arrange
        var repository = new FakePaymentsRepository();
        var controller = new PaymentsController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new NewPaymentMethodRequest
        {
            Brand = "",
            CardNumber = "4111111111111234",
            Holder = "Ana Perez",
            Expires = "12/30"
        };

        // Act
        var result = await controller.AddPaymentMethod(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPaymentMethod_ShouldReturnBadRequest_WhenCardNumberIsInvalid()
    {
        // Arrange
        var repository = new FakePaymentsRepository { ThrowInvalidCardNumber = true };
        var controller = new PaymentsController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new NewPaymentMethodRequest
        {
            Brand = "Visa",
            CardNumber = "123",
            Holder = "Ana Perez",
            Expires = "12/30"
        };

        // Act
        var result = await controller.AddPaymentMethod(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPaymentMethod_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var repository = new FakePaymentsRepository();
        var controller = new PaymentsController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new NewPaymentMethodRequest
        {
            Brand = "Visa",
            CardNumber = "4111111111111234",
            Holder = "Ana Perez",
            Expires = "12/30"
        };

        // Act
        var result = await controller.AddPaymentMethod(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var method = Assert.IsType<PaymentMethod>(createdResult.Value);
        Assert.Equal("Visa", method.Brand);
        Assert.Equal("**** 1234", method.Masked);
    }

    [Fact]
    public async Task GetPaymentHistory_ShouldReturnOk_WithHistory()
    {
        // Arrange
        var repository = new FakePaymentsRepository();
        var controller = new PaymentsController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetPaymentHistory();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var history = Assert.IsAssignableFrom<IEnumerable<PaymentHistory>>(okResult.Value);
        Assert.Single(history);
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }

    private sealed class FakePaymentsRepository : IPaymentsRepository
    {
        public bool ThrowInvalidCardNumber { get; set; }

        public Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync(string userId)
        {
            IEnumerable<PaymentMethod> methods = new List<PaymentMethod>
            {
                new()
                {
                    Id = "PAY-001",
                    Brand = "Visa",
                    Masked = "**** 1234",
                    Holder = "Ana Perez",
                    Expires = "12/30",
                    IsDefault = true
                }
            };

            return Task.FromResult(methods);
        }

        public Task<PaymentMethod> AddPaymentMethodAsync(string userId, NewPaymentMethodRequest request)
        {
            if (ThrowInvalidCardNumber)
            {
                throw new ArgumentException("CARD_NUMBER_INVALID");
            }

            var last4 = request.CardNumber[^4..];

            var method = new PaymentMethod
            {
                Id = "PAY-001",
                Brand = request.Brand,
                Masked = $"**** {last4}",
                Holder = request.Holder,
                Expires = request.Expires,
                IsDefault = true
            };

            return Task.FromResult(method);
        }

        public Task<IEnumerable<PaymentHistory>> GetPaymentHistoryAsync(string userId)
        {
            IEnumerable<PaymentHistory> history = new List<PaymentHistory>
            {
                new()
                {
                    Id = "HIS-001",
                    Date = "Today",
                    Description = "Pago de combustible",
                    Amount = 2500,
                    Currency = "PEN",
                    Status = "Paid"
                }
            };

            return Task.FromResult(history);
        }
    }
}