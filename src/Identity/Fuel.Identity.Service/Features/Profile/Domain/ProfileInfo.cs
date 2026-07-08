namespace Fuel.Identity.Service.Features.Profile.Domain;

public class ProfileInfo
{
    public string CompanyName { get; set; } = default!;
    public string Ruc { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? ContactName { get; set; }
    public string? AvatarUrl { get; set; }
}