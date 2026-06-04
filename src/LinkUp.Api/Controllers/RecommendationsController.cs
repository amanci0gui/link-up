using LinkUp.Application.Common.Models;
using LinkUp.Application.Features.Recommendations.Commands.CreateRecommendation;
using LinkUp.Application.Features.Recommendations.Commands.RespondRecommendation;
using LinkUp.Application.Features.Recommendations.Queries.GetRecommendations;
using LinkUp.Application.Features.Contacts.Commands.ShareContact;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Api.Controllers;

[ApiController]
[Route("api/v1/recommendations")]
[Produces("application/json")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecommendationsController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Envia indicação entre dois usuários conectados ao recomendador.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRecommendationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRecommendationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Create), new { id = result.Value!.RecommendationId }, result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Aceita ou rejeita indicação recebida.</summary>
    [HttpPost("{id:guid}/respond")]
    [ProducesResponseType(typeof(RespondRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Respond(
        Guid id, [FromBody] RespondRecommendationBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new RespondRecommendationCommand(id, body.Accept), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Lista indicações pendentes recebidas pelo usuário autenticado (inbox).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetRecommendationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRecommendationsQuery(), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
    }

    /// <summary>Compartilha contatos com o outro participante após indicação aceita.</summary>
    [HttpPost("{id:guid}/share-contact")]
    [ProducesResponseType(typeof(ShareContactResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ShareContact(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ShareContactCommand(id), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(ShareContact), new { id = result.Value!.ShareId }, result.Value)
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

public record RespondRecommendationBody(bool Accept);
