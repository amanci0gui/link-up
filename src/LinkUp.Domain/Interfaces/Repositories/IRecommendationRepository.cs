using LinkUp.Domain.Entities;

namespace LinkUp.Domain.Interfaces.Repositories;

public interface IRecommendationRepository
{
    Task<Recommendation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Retorna indicações PENDING onde userId é recommended_id ou target_id (inbox).</summary>
    Task<IReadOnlyList<Recommendation>> GetPendingByRecipientAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Verifica duplicata PENDING para o recommender sobre qualquer ordenação do par (userA, userB).
    /// Usa ordenação canônica internamente — evita duplicatas invertidas.
    /// </summary>
    Task<bool> ExistsPendingAsync(Guid recommenderId, Guid userA, Guid userB, CancellationToken ct = default);

    Task AddAsync(Recommendation recommendation, CancellationToken ct = default);
    Task UpdateAsync(Recommendation recommendation, CancellationToken ct = default);
}
