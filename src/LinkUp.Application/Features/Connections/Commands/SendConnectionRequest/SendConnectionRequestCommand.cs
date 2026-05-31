using FluentValidation;
using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Connections.Commands.SendConnectionRequest;

public record SendConnectionRequestCommand(Guid TargetId, string? Message) : IRequest<Result<SendConnectionRequestResponse>>;

public record SendConnectionRequestResponse(Guid RequestId, DateTime CreatedAt);

public class SendConnectionRequestCommandValidator : AbstractValidator<SendConnectionRequestCommand>
{
    public SendConnectionRequestCommandValidator()
    {
        RuleFor(x => x.TargetId)
            .NotEqual(Guid.Empty).WithMessage("TargetId inválido.");

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("Mensagem deve ter no máximo 500 caracteres.")
            .When(x => x.Message is not null);
    }
}

public class SendConnectionRequestCommandHandler : IRequestHandler<SendConnectionRequestCommand, Result<SendConnectionRequestResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _users;
    private readonly IConnectionRepository _connections;
    private readonly IConnectionRequestRepository _requests;
    private readonly IBlockRepository _blocks;

    public SendConnectionRequestCommandHandler(
        ICurrentUserService currentUser,
        IUserRepository users,
        IConnectionRepository connections,
        IConnectionRequestRepository requests,
        IBlockRepository blocks)
    {
        _currentUser = currentUser;
        _users = users;
        _connections = connections;
        _requests = requests;
        _blocks = blocks;
    }

    public async Task<Result<SendConnectionRequestResponse>> Handle(SendConnectionRequestCommand command, CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId;

        if (command.TargetId == currentUserId)
            return Errors.Connection.NotAuthorized;

        var target = await _users.GetByIdAsync(command.TargetId, ct);
        if (target is null || !target.IsActive || target.IsDeleted)
            return Errors.User.NotFound;

        if (await _connections.ExistsAsync(currentUserId, command.TargetId, ct))
            return Errors.Connection.AlreadyExists;

        var pending = await _requests.GetPendingAsync(currentUserId, command.TargetId, ct);
        if (pending is not null)
            return Errors.Connection.AlreadyExists;

        var blockedByTarget = await _blocks.ExistsAsync(command.TargetId, currentUserId, ct);
        var blockedByMe = await _blocks.ExistsAsync(currentUserId, command.TargetId, ct);
        if (blockedByTarget || blockedByMe)
            return Errors.Connection.BlockedOrBlocking;

        var request = ConnectionRequest.Create(currentUserId, command.TargetId, command.Message);
        await _requests.AddAsync(request, ct);

        return new SendConnectionRequestResponse(request.Id, request.CreatedAt);
    }
}
