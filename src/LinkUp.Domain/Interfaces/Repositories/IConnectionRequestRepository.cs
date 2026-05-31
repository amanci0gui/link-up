using LinkUp.Domain.Entities;

namespace LinkUp.Domain.Interfaces.Repositories;

public interface IConnectionRequestRepository
{
    Task<ConnectionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ConnectionRequest?> GetPendingAsync(Guid requesterId, Guid targetId, CancellationToken ct = default);
    Task AddAsync(ConnectionRequest request, CancellationToken ct = default);
    Task UpdateAsync(ConnectionRequest request, CancellationToken ct = default);
}
