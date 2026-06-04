using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Contacts.Commands.ShareContact;

public record ShareContactCommand(Guid RecommendationId) : IRequest<Result<ShareContactResponse>>;

public record ShareContactResponse(Guid ShareId, DateTime SharedAt);

public class ShareContactCommandHandler
    : IRequestHandler<ShareContactCommand, Result<ShareContactResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecommendationRepository _recommendations;
    private readonly IContactShareRepository _contactShares;

    public ShareContactCommandHandler(
        ICurrentUserService currentUser,
        IRecommendationRepository recommendations,
        IContactShareRepository contactShares)
    {
        _currentUser = currentUser;
        _recommendations = recommendations;
        _contactShares = contactShares;
    }

    public async Task<Result<ShareContactResponse>> Handle(
        ShareContactCommand command, CancellationToken ct)
    {
        var recommendation = await _recommendations.GetByIdAsync(command.RecommendationId, ct);
        if (recommendation is null)
            return Errors.Recommendation.NotFound;

        var currentUserId = _currentUser.UserId;

        if (!recommendation.IsParticipant(currentUserId))
            return Errors.Recommendation.NotParticipant;

        // Compartilhamento só é permitido após indicação aceita
        if (recommendation.Status != RecommendationStatus.Accepted)
            return Errors.Contact.RecommendationNotAccepted;

        if (await _contactShares.ExistsAsync(command.RecommendationId, currentUserId, ct))
            return Errors.Contact.AlreadyShared;

        // Destinatário é o outro participante do par
        var recipientId = recommendation.RecommendedId == currentUserId
            ? recommendation.TargetId
            : recommendation.RecommendedId;

        var share = ContactShare.Create(command.RecommendationId, currentUserId, recipientId);
        await _contactShares.AddAsync(share, ct);

        return new ShareContactResponse(share.Id, share.SharedAt);
    }
}
