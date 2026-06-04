namespace LinkUp.Domain.Entities;

/// <summary>
/// Registra consentimento de compartilhamento de contatos após indicação aceita.
/// O sharer indica que o recipient pode ver seus contatos neste contexto.
/// </summary>
public class ContactShare
{
    public Guid Id { get; private set; }
    public Guid RecommendationId { get; private set; }
    public Guid SharerId { get; private set; }
    public Guid RecipientId { get; private set; }
    public DateTime SharedAt { get; private set; }

    private ContactShare() { }

    public static ContactShare Create(Guid recommendationId, Guid sharerId, Guid recipientId)
    {
        return new ContactShare
        {
            Id = Guid.NewGuid(),
            RecommendationId = recommendationId,
            SharerId = sharerId,
            RecipientId = recipientId,
            SharedAt = DateTime.UtcNow
        };
    }

    /// <summary>Reconstitui ContactShare do banco. Use apenas em repositórios.</summary>
    public static ContactShare Reconstitute(
        Guid id,
        Guid recommendationId,
        Guid sharerId,
        Guid recipientId,
        DateTime sharedAt)
    {
        return new ContactShare
        {
            Id = id,
            RecommendationId = recommendationId,
            SharerId = sharerId,
            RecipientId = recipientId,
            SharedAt = sharedAt
        };
    }
}
