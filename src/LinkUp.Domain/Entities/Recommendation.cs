using LinkUp.Domain.Enums;

namespace LinkUp.Domain.Entities;

/// <summary>
/// Indica que um usuário (Recommender) quer conectar dois outros (par canônico).
/// O par RecommendedId/TargetId é armazenado em ordem canônica para evitar
/// duplicatas invertidas — espelha o mesmo padrão de Connection.
/// </summary>
public class Recommendation
{
    public Guid Id { get; private set; }
    public Guid RecommenderId { get; private set; }
    /// <summary>Par canônico — menor GUID (string ordinal).</summary>
    public Guid RecommendedId { get; private set; }
    /// <summary>Par canônico — maior GUID (string ordinal).</summary>
    public Guid TargetId { get; private set; }
    public RecommendationType Type { get; private set; }
    public RecommendationStatus Status { get; private set; }
    public string? Message { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private Recommendation() { }

    /// <summary>
    /// Cria nova indicação aplicando ordenação canônica ao par indicado.
    /// Expira em 30 dias por padrão.
    /// </summary>
    public static Recommendation Create(
        Guid recommenderId,
        Guid recommendedId,
        Guid targetId,
        RecommendationType type,
        string? message)
    {
        var (id1, id2) = Canonical(recommendedId, targetId);
        return new Recommendation
        {
            Id = Guid.NewGuid(),
            RecommenderId = recommenderId,
            RecommendedId = id1,
            TargetId = id2,
            Type = type,
            Status = RecommendationStatus.Pending,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
    }

    /// <summary>Aceita indicação — cria Connection no handler (transação lógica).</summary>
    public void Accept()
    {
        Status = RecommendationStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Rejeita indicação.</summary>
    public void Reject()
    {
        Status = RecommendationStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Expira indicação — chamado por job de background.</summary>
    public void Expire()
    {
        Status = RecommendationStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Verifica se userId é participante (recommended ou target).</summary>
    public bool IsParticipant(Guid userId) =>
        RecommendedId == userId || TargetId == userId;

    /// <summary>Reconstitui Recommendation do banco. Use apenas em repositórios.</summary>
    public static Recommendation Reconstitute(
        Guid id,
        Guid recommenderId,
        Guid recommendedId,
        Guid targetId,
        RecommendationType type,
        RecommendationStatus status,
        string? message,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime expiresAt)
    {
        return new Recommendation
        {
            Id = id,
            RecommenderId = recommenderId,
            RecommendedId = recommendedId,
            TargetId = targetId,
            Type = type,
            Status = status,
            Message = message,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            ExpiresAt = expiresAt
        };
    }

    private static (Guid, Guid) Canonical(Guid a, Guid b) =>
        string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal) < 0
            ? (a, b)
            : (b, a);
}
