using Fuel.Identity.Service.Features.Auth.Api;
using Fuel.Identity.Service.Features.Auth.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Identity.Tests.Auth;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordIsShort()
    {
        // Arrange
        var repository = new FakeAuthRepository();
        var controller = new AuthController(repository);
        var request = new RegisterRequest("Ana Perez", "ana@test.com", "123");

        // Act
        var result = await controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var repository = new FakeAuthRepository();
        var controller = new AuthController(repository);
        var request = new RegisterRequest("Ana Perez", "ana@test.com", "12345678");

        // Act
        var result = await controller.Register(request);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var repository = new FakeAuthRepository { LoginResult = null };
        var controller = new AuthController(repository);
        var request = new LoginRequest("wrong@test.com", "12345678");

        // Act
        var result = await controller.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public AuthResult? LoginResult { get; set; } =
            new("fake-token", new UserDto("user-1", "Ana Perez", "ana@test.com"));

        public Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            return Task.FromResult(new AuthResult(
                "fake-token",
                new UserDto("user-1", request.FullName, request.Email)));
        }

        public Task<AuthResult?> LoginAsync(LoginRequest request)
        {
            return Task.FromResult(LoginResult);
        }
    }
}