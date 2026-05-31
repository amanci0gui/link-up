using LinkUp.Domain.Entities;

namespace LinkUp.Domain.Interfaces.Repositories;

public interface IConnectionRepository
{
    Task<Connection?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<IReadOnlyList<Connection>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Connection connection, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
}
