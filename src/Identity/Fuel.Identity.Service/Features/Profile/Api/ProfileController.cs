using Fuel.Identity.Service.Features.Profile.Domain;
using Fuel.Identity.Service.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Identity.Service.Features.Profile.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private const long MaxAvatarRequestBytes = 3 * 1024 * 1024;
    private readonly IProfileRepository _repository;

    public ProfileController(IProfileRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ProfileInfo>> GetMe()
    {
        return Ok(await _repository.GetProfileAsync(User.GetRequiredUserId()));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ProfileInfo>> UpdateMe([FromBody] ProfileInfo request)
    {
        return Ok(await _repository.UpdateProfileAsync(User.GetRequiredUserId(), request));
    }

    [HttpGet("avatar")]
    public async Task<IActionResult> GetAvatar()
    {
        var avatar = await _repository.GetAvatarAsync(User.GetRequiredUserId());
        return avatar is null
            ? NotFound()
            : File(avatar.Content, avatar.ContentType);
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(MaxAvatarRequestBytes)]
    public async Task<ActionResult<ProfileInfo>> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Selecciona una imagen." });
        if (file.Length > MaxAvatarBytes)
            return BadRequest(new { message = "La imagen no puede superar 2 MB." });
        if (!AllowedImageTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Usa una imagen JPEG, PNG o WEBP." });

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        return Ok(await _repository.UpdateAvatarAsync(
            User.GetRequiredUserId(), stream.ToArray(), file.ContentType));
    }
}