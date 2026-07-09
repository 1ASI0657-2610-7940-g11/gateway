using Fuel.Identity.Service.Infrastructure.Auth;
using Fuel.Identity.Service.Infrastructure.Data.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace Fuel.Identity.Tests.Auth;

public class TokenServiceTests
{
    [Fact]
    public void Create_ShouldReturnValidJwtToken()
    {
        // Arrange
        var options = new JwtOptions(
            "1234567890123456789012345678901234567890123456789012345678901234",
            "FuelTrack.Api",
            "FuelTrack.Web",
            120);

        var service = new TokenService(options);

        var user = new UserEntity
        {
            Id = "user-1",
            FullName = "Ana Perez",
            Email = "ana@test.com"
        };

        // Act
        var token = service.Create(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(new JwtSecurityTokenHandler().CanReadToken(token));
    }

    [Fact]
    public void Create_ShouldIncludeUserEmailClaim()
    {
        // Arrange
        var options = new JwtOptions(
            "1234567890123456789012345678901234567890123456789012345678901234",
            "FuelTrack.Api",
            "FuelTrack.Web",
            120);

        var service = new TokenService(options);

        var user = new UserEntity
        {
            Id = "user-1",
            FullName = "Ana Perez",
            Email = "ana@test.com"
        };

        // Act
        var token = service.Create(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.Contains(jwt.Claims, c => c.Value == "ana@test.com");
    }
}