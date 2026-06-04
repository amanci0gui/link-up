using LinkUp.Domain.Entities;

namespace LinkUp.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>Retorna se o usuário aceita receber indicações (coluna recommendations_enabled).</summary>
    Task<bool> HasRecommendationsEnabledAsync(Guid userId, CancellationToken ct = default);
}
