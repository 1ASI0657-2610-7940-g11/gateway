namespace Fuel.Identity.Service.Features.Auth.Domain;

public interface IAuthRepository
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult?> LoginAsync(LoginRequest request);
}