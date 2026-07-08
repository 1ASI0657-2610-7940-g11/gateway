namespace Fuel.Identity.Service.Features.Profile.Domain;

public interface IProfileRepository
{
    Task<ProfileInfo> GetProfileAsync(string userId);
    Task<ProfileInfo> UpdateProfileAsync(string userId, ProfileInfo profile);
    Task<ProfileInfo> UpdateAvatarAsync(string userId, byte[] content, string contentType);
    Task<AvatarData?> GetAvatarAsync(string userId);
}