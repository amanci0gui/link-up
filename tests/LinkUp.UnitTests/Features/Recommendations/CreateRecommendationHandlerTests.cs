using LinkUp.Application.Common.Models;
using LinkUp.Application.Features.Recommendations.Commands.CreateRecommendation;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.UnitTests.Fakes;

namespace LinkUp.UnitTests.Features.Recommendations;

public class CreateRecommendationHandlerTests
{
    // Cria User mínimo via Reconstitute para não depender de lógica de Create()
    private static User MakeUser(string email = "u@test.com") =>
        User.Reconstitute(
            Guid.NewGuid(), email, "hash", "Test User",
            null, null, true, DateTime.UtcNow, DateTime.UtcNow, null);

    private static CreateRecommendationCommandHandler BuildHandler(
        FakeCurrentUser currentUser,
        FakeUserRepository users,
        FakeConnectionRepository connections,
        FakeBlockRepository blocks,
        FakeRecommendationRepository recommendations)
        => new(currentUser, users, connections, blocks, recommendations);

    [Fact]
    public async Task Should_Return_Failure_When_Not_Connected_To_Both()
    {
        // Arrange
        var recommender = MakeUser("rec@test.com");
        var recommended = MakeUser("rmd@test.com");
        var target = MakeUser("tgt@test.com");

        var currentUser = new FakeCurrentUser { UserId = recommender.Id };
        var users = new FakeUserRepository();
        users.Add(recommended);
        users.Add(target);

        // Sem conexão entre recommender e recommended
        var connections = new FakeConnectionRepository();
        var handler = BuildHandler(currentUser, users, connections, new(), new());

        var command = new CreateRecommendationCommand(
            recommended.Id, target.Id, RecommendationType.Friendship, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Recommendation.NotConnectedToTarget.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Target_Blocked_Recommender()
    {
        // Arrange
        var recommender = MakeUser("rec@test.com");
        var recommended = MakeUser("rmd@test.com");
        var target = MakeUser("tgt@test.com");

        var currentUser = new FakeCurrentUser { UserId = recommender.Id };
        var users = new FakeUserRepository();
        users.Add(recommended);
        users.Add(target);

        var connections = new FakeConnectionRepository();
        connections.Add(recommender.Id, recommended.Id);
        connections.Add(recommender.Id, target.Id);

        var blocks = new FakeBlockRepository();
        blocks.Block(target.Id, recommender.Id); // target bloqueou recommender

        var handler = BuildHandler(currentUser, users, connections, blocks, new());

        var command = new CreateRecommendationCommand(
            recommended.Id, target.Id, RecommendationType.Professional, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Recommendation.TargetBlockedRecommender.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Duplicate_Pending_Exists()
    {
        // Arrange
        var recommender = MakeUser("rec@test.com");
        var recommended = MakeUser("rmd@test.com");
        var target = MakeUser("tgt@test.com");

        var currentUser = new FakeCurrentUser { UserId = recommender.Id };
        var users = new FakeUserRepository();
        users.Add(recommended);
        users.Add(target);

        var connections = new FakeConnectionRepository();
        connections.Add(recommender.Id, recommended.Id);
        connections.Add(recommender.Id, target.Id);

        var recs = new FakeRecommendationRepository();
        recs.Seed(Recommendation.Create(
            recommender.Id, recommended.Id, target.Id, RecommendationType.Friendship, null));

        var handler = BuildHandler(currentUser, users, connections, new(), recs);

        var command = new CreateRecommendationCommand(
            recommended.Id, target.Id, RecommendationType.Friendship, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Recommendation.DuplicatePending.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Recommendations_Disabled_On_Target()
    {
        // Arrange
        var recommender = MakeUser("rec@test.com");
        var recommended = MakeUser("rmd@test.com");
        var target = MakeUser("tgt@test.com");

        var currentUser = new FakeCurrentUser { UserId = recommender.Id };
        var users = new FakeUserRepository();
        users.Add(recommended);
        users.Add(target, recommendationsEnabled: false); // target desabilitou recebimento

        var connections = new FakeConnectionRepository();
        connections.Add(recommender.Id, recommended.Id);
        connections.Add(recommender.Id, target.Id);

        var handler = BuildHandler(currentUser, users, connections, new(), new());

        var command = new CreateRecommendationCommand(
            recommended.Id, target.Id, RecommendationType.Romance, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(Errors.Recommendation.TargetDisabledRecommendations.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Should_Create_Recommendation_When_All_Conditions_Met()
    {
        // Arrange
        var recommender = MakeUser("rec@test.com");
        var recommended = MakeUser("rmd@test.com");
        var target = MakeUser("tgt@test.com");

        var currentUser = new FakeCurrentUser { UserId = recommender.Id };
        var users = new FakeUserRepository();
        users.Add(recommended);
        users.Add(target);

        var connections = new FakeConnectionRepository();
        connections.Add(recommender.Id, recommended.Id);
        connections.Add(recommender.Id, target.Id);

        var recs = new FakeRecommendationRepository();
        var handler = BuildHandler(currentUser, users, connections, new(), recs);

        var command = new CreateRecommendationCommand(
            recommended.Id, target.Id, RecommendationType.Friendship, "Vocês devem se conhecer!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.RecommendationId);
        Assert.True(result.Value.CreatedAt > DateTime.MinValue);
    }
}
