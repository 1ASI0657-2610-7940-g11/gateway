using Fuel.Events;
using Fuel.Identity.Service.Features.Auth.Domain;
using Fuel.Identity.Service.Infrastructure.Auth;
using Fuel.Identity.Service.Infrastructure.Data;
using Fuel.Identity.Service.Infrastructure.Data.Entities;
using Fuel.Identity.Service.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Identity.Service.Features.Auth.Data;

public sealed class MySqlAuthRepository : IAuthRepository
{
    private readonly IdentityDbContext _db;
    private readonly PasswordHashService _passwords;
    private readonly TokenService _tokens;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MySqlAuthRepository(
        IdentityDbContext db,
        PasswordHashService passwords,
        TokenService tokens,
        IMessagePublisher messagePublisher,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
        _messagePublisher = messagePublisher;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email == email))
            throw new InvalidOperationException("EMAIL_ALREADY_EXISTS");

        var user = new UserEntity
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwords.Hash(request.Password),
            Profile = new ProfileEntity
            {
                CompanyName = "",
                Ruc = "",
                Email = email,
                Phone = "",
                ContactName = request.FullName.Trim()
            }
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId();
        _messagePublisher.Publish("user-events", "", new UserRegisteredEvent(
            user.Id, user.FullName, user.Email), correlationId);

        return ToResult(user);
    }

    public async Task<AuthResult?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
            return null;

        return ToResult(user);
    }

    private AuthResult ToResult(UserEntity user)
    {
        return new AuthResult(_tokens.Create(user), new UserDto(user.Id, user.FullName, user.Email));
    }
}