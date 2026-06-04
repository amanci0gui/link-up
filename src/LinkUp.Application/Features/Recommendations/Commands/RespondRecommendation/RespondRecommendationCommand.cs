using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Recommendations.Commands.RespondRecommendation;

public record RespondRecommendationCommand(
    Guid RecommendationId,
    bool Accept) : IRequest<Result<RespondRecommendationResponse>>;

public record RespondRecommendationResponse(Guid RecommendationId, string Status);

public class RespondRecommendationCommandHandler
    : IRequestHandler<RespondRecommendationCommand, Result<RespondRecommendationResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecommendationRepository _recommendations;
    private readonly IConnectionRepository _connections;

    public RespondRecommendationCommandHandler(
        ICurrentUserService currentUser,
        IRecommendationRepository recommendations,
        IConnectionRepository connections)
    {
        _currentUser = currentUser;
        _recommendations = recommendations;
        _connections = connections;
    }

    public async Task<Result<RespondRecommendationResponse>> Handle(
        RespondRecommendationCommand command, CancellationToken ct)
    {
        var recommendation = await _recommendations.GetByIdAsync(command.RecommendationId, ct);
        if (recommendation is null)
            return Errors.Recommendation.NotFound;

        var currentUserId = _currentUser.UserId;

        if (!recommendation.IsParticipant(currentUserId))
            return Errors.Recommendation.NotParticipant;

        if (recommendation.Status != RecommendationStatus.Pending)
            return Errors.Recommendation.AlreadyResponded;

        if (command.Accept)
        {
            recommendation.Accept();

            // Transação lógica: cria Connection entre o par indicado na mesma operação
            var connection = Connection.Create(recommendation.RecommendedId, recommendation.TargetId);
            await _connections.AddAsync(connection, ct);
        }
        else
        {
            recommendation.Reject();
        }

        await _recommendations.UpdateAsync(recommendation, ct);

        return new RespondRecommendationResponse(
            recommendation.Id,
            recommendation.Status.ToString().ToUpperInvariant());
    }
}
