using Dapper;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Enums;
using LinkUp.Domain.Interfaces.Repositories;
using Npgsql;

namespace LinkUp.Infrastructure.Persistence.Repositories;

public class ConnectionRequestRepository : IConnectionRequestRepository
{
    private readonly string _connectionString;

    public ConnectionRequestRepository(string connectionString)
        => _connectionString = connectionString;

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<ConnectionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, requester_id, target_id, status, message, created_at, updated_at
            FROM connection_requests
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<ConnectionRequestRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<ConnectionRequest?> GetPendingAsync(Guid requesterId, Guid targetId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, requester_id, target_id, status, message, created_at, updated_at
            FROM connection_requests
            WHERE requester_id = @RequesterId AND target_id = @TargetId AND status = 'PENDING'
            """;

        await using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<ConnectionRequestRow>(
            new CommandDefinition(sql, new { RequesterId = requesterId, TargetId = targetId }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task AddAsync(ConnectionRequest request, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO connection_requests (id, requester_id, target_id, status, message, created_at, updated_at)
            VALUES (@Id, @RequesterId, @TargetId, @Status::connection_request_status, @Message, @CreatedAt, @UpdatedAt)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            request.Id,
            request.RequesterId,
            request.TargetId,
            Status = request.Status.ToString().ToUpperInvariant(),
            request.Message,
            request.CreatedAt,
            request.UpdatedAt
        }, cancellationToken: ct));
    }

    public async Task UpdateAsync(ConnectionRequest request, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE connection_requests
            SET status = @Status::connection_request_status, updated_at = @UpdatedAt
            WHERE id = @Id
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            request.Id,
            Status = request.Status.ToString().ToUpperInvariant(),
            request.UpdatedAt
        }, cancellationToken: ct));
    }

    private sealed class ConnectionRequestRow
    {
        public Guid Id { get; init; }
        public Guid RequesterId { get; init; }
        public Guid TargetId { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? Message { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }

        public ConnectionRequest ToDomain()
        {
            var status = Enum.Parse<ConnectionRequestStatus>(Status, ignoreCase: true);
            return ConnectionRequest.Reconstitute(Id, RequesterId, TargetId, status, Message, CreatedAt, UpdatedAt);
        }
    }
}
