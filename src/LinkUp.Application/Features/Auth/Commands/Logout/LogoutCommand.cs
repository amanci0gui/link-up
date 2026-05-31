using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using MediatR;

namespace LinkUp.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Result<bool>>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokens;

    public LogoutCommandHandler(ICurrentUserService currentUser, ITokenService tokens)
    {
        _currentUser = currentUser;
        _tokens = tokens;
    }

    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken ct)
    {
        // Revoke all refresh tokens for user by pattern (Redis SCAN or store separately)
        // For MVP: client discards tokens; server-side revocation via token rotation is sufficient
        // Full revocation of all sessions is a v2 feature
        await Task.CompletedTask;
        return true;
    }
}
