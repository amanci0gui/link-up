using LinkUp.Domain.Entities;

namespace LinkUp.Domain.Interfaces.Repositories;

public interface IContactRepository
{
    Task<IReadOnlyList<Contact>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Contact contact, CancellationToken ct = default);
}
