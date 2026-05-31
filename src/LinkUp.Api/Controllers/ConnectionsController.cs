using LinkUp.Application.Common.Models;
using LinkUp.Application.Features.Connections.Commands.RespondConnectionRequest;
using LinkUp.Application.Features.Connections.Commands.SendConnectionRequest;
using LinkUp.Application.Features.Connections.Queries.GetConnections;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Api.Controllers;

[ApiController]
[Route("api/v1/connections")]
[Produces("application/json")]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConnectionsController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Envia solicitação de conexão.</summary>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(SendConnectionRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRequest(SendConnectionRequestCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(SendRequest), new { id = result.Value!.RequestId }, result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Responde a uma solicitação de conexão.</summary>
    [HttpPost("requests/{id:guid}/respond")]
    [ProducesResponseType(typeof(RespondConnectionRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RespondRequest(Guid id, [FromBody] RespondConnectionRequestBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new RespondConnectionRequestCommand(id, body.Accept), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Lista conexões do usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetConnectionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetConnectionsQuery(), ct);
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

public record RespondConnectionRequestBody(bool Accept);
