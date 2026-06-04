using LinkUp.Application.Common.Models;
using LinkUp.Application.Features.Recommendations.Commands.RespondRecommendation;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.UnitTests.Fakes;

namespace LinkUp.UnitTests.Features.Recommendations;

public class RespondRecommendationHandlerTests
{
    private static Recommendation MakePending(Guid recommenderId, Guid userA, Guid userB) =>
        Recommendation.Create(recommenderId, userA, userB, RecommendationType.Friendship, null);

    private static RespondRecommendationCommandHandler BuildHandler(
        FakeCurrentUser currentUser,
        FakeRecommendationRepository recs,
        FakeConnectionRepository connections)
        => new(currentUser, recs, connections);

    [Fact]
    public async Task Should_Return_Failure_When_Recommendation_Not_Found()
    {
        // Arrange
        var currentUser = new FakeCurrentUser { UserId = Guid.NewGuid() };
        var recs = new FakeRecommendationRepository();
        var handler = BuildHandler(currentUser, recs, new());

        // Act
        var result = await handler.Handle(
            new RespondRecommendationCommand(Guid.NewGuid(), true),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Recommendation.NotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Should_Return_Failure_When_User_Not_Participant()
    {
        // Arrange
        var recommender = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var stranger = Guid.NewGuid();

        var currentUser = new FakeCurrentUser { UserId = stranger };
        var recs = new FakeRecommendationRepository();
        var rec = MakePending(recommender, userA, userB);
        recs.Seed(rec);

        var handler = BuildHandler(currentUser, recs, new());

        // Act
        var result = await handler.Handle(
            new RespondRecommendationCommand(rec.Id, true),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Recommendation.NotParticipant.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Already_Responded()
    {
        // Arrange
        var recommender = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        // Cria rec e já aceita antes de colocar no fake
        var rec = MakePending(recommender, userA, userB);
        rec.Accept();

        var currentUser = new FakeCurrentUser { UserId = rec.RecommendedId };
        var recs = new FakeRecommendationRepository();
        recs.Seed(rec);

        var handler = BuildHandler(currentUser, recs, new());

        // Act
        var result = await handler.Handle(
            new RespondRecommendationCommand(rec.Id, true),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Recommendation.AlreadyResponded.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Should_Accept_And_Create_Connection_Between_Pair()
    {
        // Arrange
        var recommender = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var rec = MakePending(recommender, userA, userB);

        // currentUser é um dos participantes (RecommendedId ou TargetId após canonicalização)
        var currentUser = new FakeCurrentUser { UserId = rec.RecommendedId };
        var recs = new FakeRecommendationRepository();
        recs.Seed(rec);

        var connections = new FakeConnectionRepository();
        var handler = BuildHandler(currentUser, recs, connections);

        // Act
        var result = await handler.Handle(
            new RespondRecommendationCommand(rec.Id, true),
            CancellationToken.None);

        // Assert — indicação aceita e conexão criada
        Assert.True(result.IsSuccess);
        Assert.Equal("ACCEPTED", result.Value!.Status);
        Assert.True(await connections.ExistsAsync(rec.RecommendedId, rec.TargetId));
    }

    [Fact]
    public async Task Should_Reject_Recommendation()
    {
        // Arrange
        var recommender = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var rec = MakePending(recommender, userA, userB);

        // target também é participante válido
        var currentUser = new FakeCurrentUser { UserId = rec.TargetId };
        var recs = new FakeRecommendationRepository();
        recs.Seed(rec);

        var handler = BuildHandler(currentUser, recs, new());

        // Act
        var result = await handler.Handle(
            new RespondRecommendationCommand(rec.Id, false),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("REJECTED", result.Value!.Status);
    }
}
