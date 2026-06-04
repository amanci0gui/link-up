using Dapper;
using LinkUp.Domain.Entities;
using LinkUp.Domain.Interfaces.Repositories;
using Npgsql;

namespace LinkUp.Infrastructure.Persistence.Repositories;

public class ContactShareRepository : IContactShareRepository
{
    private readonly string _connectionString;

    public ContactShareRepository(string connectionString)
        => _connectionString = connectionString;

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<bool> ExistsAsync(
        Guid recommendationId, Guid sharerId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM contact_shares
            WHERE recommendation_id = @RecommendationId
              AND sharer_id = @SharerId
            """;

        await using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql,
                new { RecommendationId = recommendationId, SharerId = sharerId },
                cancellationToken: ct));
        return count > 0;
    }

    public async Task AddAsync(ContactShare share, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO contact_shares (id, recommendation_id, sharer_id, recipient_id, shared_at)
            VALUES (@Id, @RecommendationId, @SharerId, @RecipientId, @SharedAt)
            """;

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            share.Id,
            share.RecommendationId,
            share.SharerId,
            share.RecipientId,
            share.SharedAt
        }, cancellationToken: ct));
    }
}
