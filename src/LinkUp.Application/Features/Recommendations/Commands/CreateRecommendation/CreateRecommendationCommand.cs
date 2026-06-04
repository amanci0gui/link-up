using FluentValidation;
using LinkUp.Application.Common.Interfaces;
using LinkUp.Application.Common.Models;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;
using MediatR;

namespace LinkUp.Application.Features.Recommendations.Commands.CreateRecommendation;

public record CreateRecommendationCommand(
    Guid RecommendedId,
    Guid TargetId,
    RecommendationType Type,
    string? Message) : IRequest<Result<CreateRecommendationResponse>>;

public record CreateRecommendationResponse(Guid RecommendationId, DateTime CreatedAt);

public class CreateRecommendationCommandValidator : AbstractValidator<CreateRecommendationCommand>
{
    public CreateRecommendationCommandValidator()
    {
        RuleFor(x => x.RecommendedId)
            .NotEqual(Guid.Empty).WithMessage("RecommendedId inválido.");

        RuleFor(x => x.TargetId)
            .NotEqual(Guid.Empty).WithMessage("TargetId inválido.");

        RuleFor(x => x)
            .Must(x => x.RecommendedId != x.TargetId)
            .WithMessage("Os usuários indicados devem ser diferentes.");

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("Mensagem deve ter no máximo 500 caracteres.")
            .When(x => x.Message is not null);
    }
}

public class CreateRecommendationCommandHandler
    : IRequestHandler<CreateRecommendationCommand, Result<CreateRecommendationResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _users;
    private readonly IConnectionRepository _connections;
    private readonly IBlockRepository _blocks;
    private readonly IRecommendationRepository _recommendations;

    public CreateRecommendationCommandHandler(
        ICurrentUserService currentUser,
        IUserRepository users,
        IConnectionRepository connections,
        IBlockRepository blocks,
        IRecommendationRepository recommendations)
    {
        _currentUser = currentUser;
        _users = users;
        _connections = connections;
        _blocks = blocks;
        _recommendations = recommendations;
    }

    public async Task<Result<CreateRecommendationResponse>> Handle(
        CreateRecommendationCommand command, CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId;

        // Recommender não pode indicar a si mesmo como recommended ou target
        if (command.RecommendedId == currentUserId || command.TargetId == currentUserId)
            return Errors.Connection.NotAuthorized;

        // Ambos os usuários devem existir e estar ativos
        var recommended = await _users.GetByIdAsync(command.RecommendedId, ct);
        if (recommended is null || !recommended.IsActive || recommended.IsDeleted)
            return Errors.User.NotFound;

        var target = await _users.GetByIdAsync(command.TargetId, ct);
        if (target is null || !target.IsActive || target.IsDeleted)
            return Errors.User.NotFound;

        // Recommender deve estar conectado a ambos (requisito core do produto)
        var connectedToRecommended = await _connections.ExistsAsync(currentUserId, command.RecommendedId, ct);
        var connectedToTarget = await _connections.ExistsAsync(currentUserId, command.TargetId, ct);
        if (!connectedToRecommended || !connectedToTarget)
            return Errors.Recommendation.NotConnectedToTarget;

        // Bloquei em qualquer direção com o recommender impede indicação
        var blockedByRecommended = await _blocks.ExistsAsync(command.RecommendedId, currentUserId, ct);
        var blockedByTarget = await _blocks.ExistsAsync(command.TargetId, currentUserId, ct);
        if (blockedByRecommended || blockedByTarget)
            return Errors.Recommendation.TargetBlockedRecommender;

        // Target deve aceitar receber indicações
        if (!await _users.HasRecommendationsEnabledAsync(command.TargetId, ct))
            return Errors.Recommendation.TargetDisabledRecommendations;

        // Evita duplicata PENDING para este recommender e par (qualquer ordenação)
        if (await _recommendations.ExistsPendingAsync(currentUserId, command.RecommendedId, command.TargetId, ct))
            return Errors.Recommendation.DuplicatePending;

        var recommendation = Recommendation.Create(
            currentUserId, command.RecommendedId, command.TargetId, command.Type, command.Message);

        await _recommendations.AddAsync(recommendation, ct);

        return new CreateRecommendationResponse(recommendation.Id, recommendation.CreatedAt);
    }
}
