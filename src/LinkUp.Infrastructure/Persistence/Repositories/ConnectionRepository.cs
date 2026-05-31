using Dapper;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Interfaces.Repositories;
using Npgsql;

namespace LinkUp.Infrastructure.Persistence.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly string _connectionString;

    public ConnectionRepository(string connectionString)
        => _connectionString = connectionString;

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Connection?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        var (id1, id2) = Canonical(userId1, userId2);
        const string sql = """
            SELECT id, user_id_1, user_id_2, connected_at
            FROM connections
            WHERE user_id_1 = @Id1 AND user_id_2 = @Id2
            """;

        await using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<ConnectionRow>(
            new CommandDefinition(sql, new { Id1 = id1, Id2 = id2 }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Connection>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, user_id_1, user_id_2, connected_at
            FROM connections
            WHERE user_id_1 = @UserId OR user_id_2 = @UserId
            ORDER BY connected_at DESC
            """;

        await using var conn = CreateConnection();
        var rows = await conn.QueryAsync<ConnectionRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task AddAsync(Connection connection, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO connections (id, user_id_1, user_id_2, connected_at)
            VALUES (@Id, @UserId1, @UserId2, @ConnectedAt)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            connection.Id,
            connection.UserId1,
            connection.UserId2,
            connection.ConnectedAt
        }, cancellationToken: ct));
    }

    public async Task<bool> ExistsAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        var (id1, id2) = Canonical(userId1, userId2);
        const string sql = "SELECT COUNT(1) FROM connections WHERE user_id_1 = @Id1 AND user_id_2 = @Id2";
        await using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Id1 = id1, Id2 = id2 }, cancellationToken: ct));
        return count > 0;
    }

    private static (Guid, Guid) Canonical(Guid a, Guid b) =>
        string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal) < 0
            ? (a, b)
            : (b, a);

    private sealed class ConnectionRow
    {
        public Guid Id { get; init; }
        public Guid UserId1 { get; init; }
        public Guid UserId2 { get; init; }
        public DateTime ConnectedAt { get; init; }

        public Connection ToDomain() =>
            Connection.Reconstitute(Id, UserId1, UserId2, ConnectedAt);
    }
}
