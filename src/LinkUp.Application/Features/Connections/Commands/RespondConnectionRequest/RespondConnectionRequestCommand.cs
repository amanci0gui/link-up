using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Connections.Commands.RespondConnectionRequest;

public record RespondConnectionRequestCommand(Guid RequestId, bool Accept) : IRequest<Result<RespondConnectionRequestResponse>>;

public record RespondConnectionRequestResponse(Guid RequestId, string Status);

public class RespondConnectionRequestCommandHandler : IRequestHandler<RespondConnectionRequestCommand, Result<RespondConnectionRequestResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConnectionRequestRepository _requests;
    private readonly IConnectionRepository _connections;

    public RespondConnectionRequestCommandHandler(
        ICurrentUserService currentUser,
        IConnectionRequestRepository requests,
        IConnectionRepository connections)
    {
        _currentUser = currentUser;
        _requests = requests;
        _connections = connections;
    }

    public async Task<Result<RespondConnectionRequestResponse>> Handle(RespondConnectionRequestCommand command, CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(command.RequestId, ct);
        if (request is null)
            return Errors.Connection.RequestNotFound;

        if (request.TargetId != _currentUser.UserId)
            return Errors.Connection.NotAuthorized;

        if (request.Status != ConnectionRequestStatus.Pending)
            return Errors.Connection.RequestNotPending;

        if (command.Accept)
        {
            request.Accept();
            var connection = Connection.Create(request.RequesterId, request.TargetId);
            await _connections.AddAsync(connection, ct);
        }
        else
        {
            request.Reject();
        }

        await _requests.UpdateAsync(request, ct);

        return new RespondConnectionRequestResponse(request.Id, request.Status.ToString().ToUpperInvariant());
    }
}
