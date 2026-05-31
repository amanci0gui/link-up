using LinkUp.Application.Common.Models;
using LinkUp.Application.Features.Auth.Commands.Login;
using LinkUp.Application.Features.Auth.Commands.Logout;
using LinkUp.Application.Features.Auth.Commands.RefreshToken;
using LinkUp.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Registra novo usuário.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Register), result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Autentica usuário e retorna tokens.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Renova access token usando refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Encerra sessão do usuário (MVP: client-side).</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _mediator.Send(new LogoutCommand(), ct);
        return NoContent();
    }

    private IActionResult Problem(AppError error) =>
        error.StatusCode switch
        {
            400 => BadRequest(new { code = error.Code, message = error.Message }),
            401 => Unauthorized(new { code = error.Code, message = error.Message }),
            403 => Forbid(),
            404 => NotFound(new { code = error.Code, message = error.Message }),
            409 => Conflict(new { code = error.Code, message = error.Message }),
            _ => StatusCode(error.StatusCode, new { code = error.Code, message = error.Message })
        };
}
