using LinkUp.Domain.Entities;

namespace LinkUp.Domain.Interfaces.Repositories;

public interface IBlockRepository
{
    Task<bool> ExistsAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default);
    Task AddAsync(Block block, CancellationToken ct = default);
}
