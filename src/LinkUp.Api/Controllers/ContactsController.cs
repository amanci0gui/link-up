using LinkUp.Application.Common.Models;
using LinkUp.Application.Features.Contacts.Commands.AddContact;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Api.Controllers;

[ApiController]
[Route("api/v1/contacts")]
[Produces("application/json")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Adiciona contato ao perfil do usuário autenticado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AddContactResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add([FromBody] AddContactCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Add), new { id = result.Value!.ContactId }, result.Value)
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
