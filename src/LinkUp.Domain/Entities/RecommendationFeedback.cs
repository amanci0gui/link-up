namespace LinkUp.Domain.Entities;

/// <summary>
/// Feedback deixado por um participante após indicação aceita.
/// Rating de 1 a 5 — invariante verificado em Create().
/// </summary>
public class RecommendationFeedback
{
    public Guid Id { get; private set; }
    public Guid RecommendationId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public Guid RevieweeId { get; private set; }
    public int? Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RecommendationFeedback() { }

    public static RecommendationFeedback Create(
        Guid recommendationId,
        Guid reviewerId,
        Guid revieweeId,
        int? rating,
        string? comment)
    {
        if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating deve ser entre 1 e 5.");

        return new RecommendationFeedback
        {
            Id = Guid.NewGuid(),
            RecommendationId = recommendationId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Reconstitui RecommendationFeedback do banco. Use apenas em repositórios.</summary>
    public static RecommendationFeedback Reconstitute(
        Guid id,
        Guid recommendationId,
        Guid reviewerId,
        Guid revieweeId,
        int? rating,
        string? comment,
        DateTime createdAt)
    {
        return new RecommendationFeedback
        {
            Id = id,
            RecommendationId = recommendationId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            Rating = rating,
            Comment = comment,
            CreatedAt = createdAt
        };
    }
}
