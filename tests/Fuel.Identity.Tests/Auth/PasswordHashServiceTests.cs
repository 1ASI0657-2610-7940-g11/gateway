using Fuel.Identity.Service.Infrastructure.Auth;

namespace Fuel.Identity.Tests.Auth;

public class PasswordHashServiceTests
{
    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordIsCorrect()
    {
        // Arrange
        var service = new PasswordHashService();
        var password = "12345678";

        // Act
        var hash = service.Hash(password);
        var result = service.Verify(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordIsIncorrect()
    {
        // Arrange
        var service = new PasswordHashService();
        var hash = service.Hash("12345678");

        // Act
        var result = service.Verify("wrong-password", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHashIsInvalid()
    {
        // Arrange
        var service = new PasswordHashService();

        // Act
        var result = service.Verify("12345678", "invalid-hash");

        // Assert
        Assert.False(result);
    }
}