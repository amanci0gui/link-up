using Dapper;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Interfaces.Repositories;
using Npgsql;

namespace LinkUp.Infrastructure.Persistence.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly string _connectionString;

    public BlockRepository(string connectionString)
        => _connectionString = connectionString;

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<bool> ExistsAsync(Guid blockerId, Guid blockedId, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM blocks WHERE blocker_id = @BlockerId AND blocked_id = @BlockedId";
        await using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { BlockerId = blockerId, BlockedId = blockedId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task AddAsync(Block block, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO blocks (id, blocker_id, blocked_id, block_type, created_at)
            VALUES (@Id, @BlockerId, @BlockedId, @BlockType::block_type, @CreatedAt)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            block.Id,
            block.BlockerId,
            block.BlockedId,
            BlockType = block.BlockType.ToString().ToUpperInvariant(),
            block.CreatedAt
        }, cancellationToken: ct));
    }
}
