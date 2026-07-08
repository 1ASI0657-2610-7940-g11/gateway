namespace Fuel.Identity.Service.Infrastructure.Data.Entities;

public sealed class ProfileEntity
{
    public string UserId { get; set; } = default!;
    public string CompanyName { get; set; } = "";
    public string Ruc { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? ContactName { get; set; }
    public byte[]? AvatarContent { get; set; }
    public string? AvatarContentType { get; set; }
}