using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Connections.Queries.GetConnections;

public record GetConnectionsQuery() : IRequest<Result<GetConnectionsResponse>>;

public record GetConnectionsResponse(IReadOnlyList<ConnectionDto> Connections);

public record ConnectionDto(Guid UserId, string Name, string? ProfilePictureUrl, DateTime ConnectedAt);

public class GetConnectionsQueryHandler : IRequestHandler<GetConnectionsQuery, Result<GetConnectionsResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConnectionRepository _connections;
    private readonly IUserRepository _users;

    public GetConnectionsQueryHandler(
        ICurrentUserService currentUser,
        IConnectionRepository connections,
        IUserRepository users)
    {
        _currentUser = currentUser;
        _connections = connections;
        _users = users;
    }

    public async Task<Result<GetConnectionsResponse>> Handle(GetConnectionsQuery query, CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId;
        var connections = await _connections.GetByUserAsync(currentUserId, ct);

        var dtos = new List<ConnectionDto>(connections.Count);
        foreach (var conn in connections)
        {
            var otherUserId = conn.UserId1 == currentUserId ? conn.UserId2 : conn.UserId1;
            var user = await _users.GetByIdAsync(otherUserId, ct);
            if (user is null) continue;

            dtos.Add(new ConnectionDto(user.Id, user.Name, user.ProfilePictureUrl, conn.ConnectedAt));
        }

        return new GetConnectionsResponse(dtos);
    }
}
