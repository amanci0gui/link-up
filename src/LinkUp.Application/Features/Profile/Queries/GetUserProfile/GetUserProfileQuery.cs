using LinkUp.Application.Common.Models;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Profile.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<Result<UserProfileResponse>>;

public record UserProfileResponse(Guid Id, string Name, string? Bio, string? ProfilePictureUrl, DateTime CreatedAt);

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileResponse>>
{
    private readonly IUserRepository _users;

    public GetUserProfileQueryHandler(IUserRepository users)
        => _users = users;

    public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery query, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(query.UserId, ct);
        if (user is null || !user.IsActive || user.IsDeleted)
            return Errors.User.NotFound;

        return new UserProfileResponse(user.Id, user.Name, user.Bio, user.ProfilePictureUrl, user.CreatedAt);
    }
}
