using LinkUp.Application.Common.Interfaces;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;

namespace LinkUp.UnitTests.Fakes;

// ── ICurrentUserService ──────────────────────────────────────────────────────
internal sealed class FakeCurrentUser : ICurrentUserService
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public string UserEmail => "fake@test.com";
}

// ── IUserRepository ──────────────────────────────────────────────────────────
internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, (User User, bool RecEnabled)> _store = new();

    public void Add(User user, bool recommendationsEnabled = true)
        => _store[user.Id] = (user, recommendationsEnabled);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var v) ? v.User : null);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_store.Values.Select(v => v.User).FirstOrDefault(u => u.Email == email));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_store.Values.Any(v => v.User.Email == email));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _store[user.Id] = (user, true);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        if (_store.TryGetValue(user.Id, out var existing))
            _store[user.Id] = (user, existing.RecEnabled);
        return Task.CompletedTask;
    }

    public Task<bool> HasRecommendationsEnabledAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(userId, out var v) && v.RecEnabled);
}

// ── IConnectionRepository ────────────────────────────────────────────────────
internal sealed class FakeConnectionRepository : IConnectionRepository
{
    private readonly HashSet<(Guid, Guid)> _store = new();

    public void Add(Guid a, Guid b) => _store.Add(Canonical(a, b));

    public Task<Connection?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
        => Task.FromResult<Connection?>(null);

    public Task<IReadOnlyList<Connection>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Connection>>(Array.Empty<Connection>());

    public Task AddAsync(Connection connection, CancellationToken ct = default)
    {
        _store.Add(Canonical(connection.UserId1, connection.UserId2));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
        => Task.FromResult(_store.Contains(Canonical(userId1, userId2)));

    private static (Guid, Guid) Canonical(Guid a, Guid b) =>
        string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal) < 0 ? (a, b) : (b, a);
}

// ── IBlockRepository ─────────────────────────────────────────────────────────
internal sealed class FakeBlockRepository : IBlockRepository
{
    private readonly HashSet<(Guid, Guid)> _store = new();

    public void Block(Guid blockerId, Guid blockedId) => _store.Add((blockerId, blockedId));

    public Task<bool> ExistsAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default)
        => Task.FromResult(_store.Contains((blockerId, blockedId)));

    public Task AddAsync(Block block, CancellationToken ct = default)
    {
        _store.Add((block.BlockerId, block.BlockedId));
        return Task.CompletedTask;
    }
}

// ── IRecommendationRepository ────────────────────────────────────────────────
internal sealed class FakeRecommendationRepository : IRecommendationRepository
{
    private readonly List<Recommendation> _store = new();

    public void Seed(Recommendation rec) => _store.Add(rec);

    public Task<Recommendation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Recommendation>> GetPendingByRecipientAsync(
        Guid userId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Recommendation>>(
            _store.Where(r =>
                (r.RecommendedId == userId || r.TargetId == userId)
                && r.Status == RecommendationStatus.Pending)
            .ToList());

    public Task<bool> ExistsPendingAsync(
        Guid recommenderId, Guid userA, Guid userB, CancellationToken ct = default)
    {
        var exists = _store.Any(r =>
            r.RecommenderId == recommenderId
            && r.Status == RecommendationStatus.Pending
            && ((r.RecommendedId == userA && r.TargetId == userB)
             || (r.RecommendedId == userB && r.TargetId == userA)));
        return Task.FromResult(exists);
    }

    public Task AddAsync(Recommendation recommendation, CancellationToken ct = default)
    {
        _store.Add(recommendation);
        return Task.CompletedTask;
    }

    // Entidade já mutada in-place antes desta chamada — no-op é suficiente nos testes
    public Task UpdateAsync(Recommendation recommendation, CancellationToken ct = default)
        => Task.CompletedTask;
}
