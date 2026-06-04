using LinkUp.Domain.Entities;

namespace LinkUp.Domain.Interfaces.Repositories;

public interface IContactShareRepository
{
    /// <summary>Verifica se sharer já compartilhou contatos nesta indicação.</summary>
    Task<bool> ExistsAsync(Guid recommendationId, Guid sharerId, CancellationToken ct = default);
    Task AddAsync(ContactShare share, CancellationToken ct = default);
}
