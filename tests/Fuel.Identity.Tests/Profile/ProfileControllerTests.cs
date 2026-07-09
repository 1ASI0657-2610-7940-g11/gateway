using Fuel.Identity.Service.Features.Profile.Api;
using Fuel.Identity.Service.Features.Profile.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Fuel.Identity.Tests.Profile;

public class ProfileControllerTests
{
    [Fact]
    public async Task GetMe_ShouldReturnOk_WithProfileInfo()
    {
        // Arrange
        var repository = new FakeProfileRepository();
        var controller = new ProfileController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        // Act
        var result = await controller.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<ProfileInfo>(okResult.Value);
        Assert.Equal("Mi Empresa", profile.CompanyName);
    }

    [Fact]
    public async Task UpdateMe_ShouldReturnOk_WithUpdatedProfile()
    {
        // Arrange
        var repository = new FakeProfileRepository();
        var controller = new ProfileController(repository);
        controller.ControllerContext = CreateControllerContext("user-1");

        var request = new ProfileInfo
        {
            CompanyName = "Empresa Actualizada",
            Ruc = "123",
            Email = "ana@test.com",
            Phone = "999",
            ContactName = "Ana"
        };

        // Act
        var result = await controller.UpdateMe(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<ProfileInfo>(okResult.Value);
        Assert.Equal("Empresa Actualizada", profile.CompanyName);
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

    private sealed class FakeProfileRepository : IProfileRepository
    {
        public Task<ProfileInfo> GetProfileAsync(string userId)
        {
            return Task.FromResult(new ProfileInfo
            {
                CompanyName = "Mi Empresa",
                Ruc = "123",
                Email = "ana@test.com",
                Phone = "999",
                ContactName = "Ana"
            });
        }

        public Task<ProfileInfo> UpdateProfileAsync(string userId, ProfileInfo profile)
        {
            return Task.FromResult(profile);
        }

        public Task<ProfileInfo> UpdateAvatarAsync(string userId, byte[] content, string contentType)
        {
            return Task.FromResult(new ProfileInfo
            {
                CompanyName = "Mi Empresa",
                Ruc = "123",
                Email = "ana@test.com",
                Phone = "999",
                ContactName = "Ana",
                AvatarUrl = "/api/profile/avatar"
            });
        }

        public Task<AvatarData?> GetAvatarAsync(string userId)
        {
            return Task.FromResult<AvatarData?>(null);
        }
    }
}