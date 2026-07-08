namespace Fuel.Identity.Service.Features.Auth.Domain;

public record AuthResult(string Token, UserDto User);