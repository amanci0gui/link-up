using LinkUp.Application.Common.Models;
using LinkUp.Application.Features.Profile.Commands.UpdateProfile;
using LinkUp.Application.Features.Profile.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Retorna perfil público de um usuário.</summary>
    [HttpGet("{userId:guid}/profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserProfileQuery(userId), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Atualiza perfil do usuário autenticado.</summary>
    [HttpPut("me/profile")]
    [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
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
