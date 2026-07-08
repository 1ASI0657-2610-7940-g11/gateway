using Fuel.Identity.Service.Features.Auth.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fuel.Identity.Service.Features.Auth.Api;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthRepository _repository;

    public AuthController(IAuthRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrEmpty(request.Password)
            || request.Password.Length < 8)
        {
            return BadRequest(new
            {
                message = "Nombre y correo son obligatorios; la contraseña debe tener al menos 8 caracteres."
            });
        }

        try
        {
            return Ok(await _repository.RegisterAsync(request));
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_ALREADY_EXISTS")
        {
            return Conflict(new { message = "El correo ya está registrado." });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Correo y contraseña son obligatorios." });
        }

        var result = await _repository.LoginAsync(request);
        return result is null
            ? Unauthorized(new { message = "Correo o contraseña incorrectos." })
            : Ok(result);
    }
}