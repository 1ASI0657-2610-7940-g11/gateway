namespace Fuel.Identity.Service.Infrastructure.Data.Entities;

public sealed class UserEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ProfileEntity Profile { get; set; } = default!;
}