using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Recommendations.Queries.GetRecommendations;

public record GetRecommendationsQuery() : IRequest<Result<GetRecommendationsResponse>>;

public record GetRecommendationsResponse(IReadOnlyList<RecommendationDto> Recommendations);

public record RecommendationDto(
    Guid Id,
    Guid RecommenderId,
    string RecommenderName,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserProfilePictureUrl,
    string Type,
    string? Message,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public class GetRecommendationsQueryHandler
    : IRequestHandler<GetRecommendationsQuery, Result<GetRecommendationsResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecommendationRepository _recommendations;
    private readonly IUserRepository _users;

    public GetRecommendationsQueryHandler(
        ICurrentUserService currentUser,
        IRecommendationRepository recommendations,
        IUserRepository users)
    {
        _currentUser = currentUser;
        _recommendations = recommendations;
        _users = users;
    }

    public async Task<Result<GetRecommendationsResponse>> Handle(
        GetRecommendationsQuery query, CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId;
        var pending = await _recommendations.GetPendingByRecipientAsync(currentUserId, ct);

        var dtos = new List<RecommendationDto>(pending.Count);

        foreach (var rec in pending)
        {
            var recommender = await _users.GetByIdAsync(rec.RecommenderId, ct);
            if (recommender is null) continue;

            // "O outro usuário" é quem não sou eu no par indicado
            var otherUserId = rec.RecommendedId == currentUserId ? rec.TargetId : rec.RecommendedId;
            var otherUser = await _users.GetByIdAsync(otherUserId, ct);
            if (otherUser is null) continue;

            dtos.Add(new RecommendationDto(
                rec.Id,
                rec.RecommenderId,
                recommender.Name,
                otherUser.Id,
                otherUser.Name,
                otherUser.ProfilePictureUrl,
                rec.Type.ToString().ToUpperInvariant(),
                rec.Message,
                rec.CreatedAt,
                rec.ExpiresAt
            ));
        }

        return new GetRecommendationsResponse(dtos);
    }
}
